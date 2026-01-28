using WMS.Backend.API.Dtos;
using WMS.Backend.API.Models;
using WMS.Backend.API.Repositories.Interfaces;

namespace WMS.Backend.API.Services
{
    public class SKUCorrectionService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ISKURepository _skuRepository;
        private readonly IAllocationRepository _allocationRepository;

        public SKUCorrectionService(
            IOrderRepository orderRepository,
            ISKURepository skuRepository,
            IAllocationRepository allocationRepository)
        {
            _orderRepository = orderRepository;
            _skuRepository = skuRepository;
            _allocationRepository = allocationRepository;
        }

        public async Task<CorrectionResult> CorrectSKUQuantityAsync(string skuId, int newQuantity)
        {
            if (newQuantity < 0)
            {
                return new CorrectionResult
                {
                    Success = false,
                    Message = "Quantity cannot be negative"
                };
            }

            var sku = await _skuRepository.GetSKUAsync(skuId);
            if (sku == null)
            {
                return new CorrectionResult
                {
                    Success = false,
                    Message = $"SKU {skuId} not found"
                };
            }

            // Get current allocated quantity on this specific SKU
            var allocationsForSku = await _allocationRepository.GetAllocationsBySKUAsync(skuId);
            var allocatedOnThisSku = allocationsForSku.Sum(a => a.AllocatedQuantity);

            var oldTotalQuantity = sku.TotalQuantity;

            // If the new quantity is still enough to cover existing allocations on this SKU,
            // we can simply adjust totals/availability without touching any allocations.
            if (newQuantity >= allocatedOnThisSku)
            {
                sku.TotalQuantity = newQuantity;
                sku.AvailableQuantity = newQuantity - allocatedOnThisSku;
                sku.LastModifiedDate = DateTime.UtcNow;

                await _skuRepository.UpdateSKUAsync(sku);

                return new CorrectionResult
                {
                    Success = true,
                    Message = $"SKU {skuId} quantity corrected from {oldTotalQuantity} to {newQuantity}. Available quantity: {sku.AvailableQuantity}, Allocated quantity: {allocatedOnThisSku}."
                };
            }

            // ---------------------------------------------------------------------
            // At this point, the new quantity is LESS than what is currently
            // allocated on this SKU. We need to:
            //   1. Deallocate affected orders/line items for this product.
            //   2. Reduce this SKU's total quantity.
            //   3. Re-run stock allocation for affected orders so that:
            //        - substitutes are used when available
            //        - complete-delivery orders are fully deallocated when
            //          substitutes are insufficient
            //        - partial-delivery orders are partially allocated.
            // ---------------------------------------------------------------------

            if (!allocationsForSku.Any())
            {
                // No allocations actually exist, even though newQuantity < allocatedOnThisSku
                // (defensive guard). In this case we just update totals.
                sku.TotalQuantity = newQuantity;
                sku.AvailableQuantity = newQuantity;
                sku.LastModifiedDate = DateTime.UtcNow;
                await _skuRepository.UpdateSKUAsync(sku);

                return new CorrectionResult
                {
                    Success = true,
                    Message = $"SKU {skuId} quantity corrected from {oldTotalQuantity} to {newQuantity}. No existing allocations to adjust."
                };
            }

            // Identify affected orders so we can re-run allocation logic.
            var affectedOrderIds = allocationsForSku
                .Select(a => a.OrderId)
                .Distinct()
                .ToList();

            // Load affected orders with line items + allocations.
            var affectedOrders = new List<CustomerOrder>();
            foreach (var orderId in affectedOrderIds)
            {
                var order = await _orderRepository.GetOrderAsync(orderId);
                if (order != null)
                {
                    affectedOrders.Add(order);
                }
            }

            // 1) Deallocate all allocations for this product on affected line items.
            //    We restore the stock back to the SKUs so we can re-run allocation
            //    using the existing StockAllocationService logic.
            var allocationIdsToDelete = new List<string>();

            foreach (var order in affectedOrders)
            {
                foreach (var lineItem in order.LineItems.Where(li => li.ProductNumber == sku.ProductNumber))
                {
                    var allocationsForLine = lineItem.Allocations.ToList();
                    foreach (var allocation in allocationsForLine)
                    {
                        // Restore stock to the SKU that previously had this allocation.
                        var allocatedSku = await _skuRepository.GetSKUAsync(allocation.SkuId);
                        if (allocatedSku != null)
                        {
                            allocatedSku.AvailableQuantity += allocation.AllocatedQuantity;
                            allocatedSku.LastModifiedDate = DateTime.UtcNow;
                        }

                        allocationIdsToDelete.Add(allocation.AllocationId);
                        lineItem.AllocatedQuantity -= allocation.AllocatedQuantity;
                        lineItem.Allocations.Remove(allocation);
                    }

                    // After full deallocation, mark the line item as pending.
                    lineItem.Status = Enums.LineItemStatus.Pending;
                }
            }

            if (allocationIdsToDelete.Count > 0)
            {
                await _allocationRepository.DeleteAllocationsAsync(allocationIdsToDelete);
            }

            // 2) Reduce the corrected SKU's total quantity and corresponding availability.
            // After step (1), ALL stock that was allocated from this SKU has been
            // returned to its AvailableQuantity, so AvailableQuantity should equal
            // the old total quantity.
            var quantityToRemove = oldTotalQuantity - newQuantity;
            if (quantityToRemove < 0)
            {
                quantityToRemove = 0;
            }

            sku.TotalQuantity = newQuantity;
            sku.AvailableQuantity = Math.Max(0, sku.AvailableQuantity - quantityToRemove);
            sku.LastModifiedDate = DateTime.UtcNow;

            // 3) Re-run allocation for each affected order using the existing
            //    stock allocation logic so that substitutes and delivery rules
            //    are applied consistently.
            var stockAllocationService = new StockAllocationService(
                _orderRepository,
                _skuRepository,
                _allocationRepository);

            var anyCompleteDeliveryFailure = false;

            foreach (var order in affectedOrders)
            {
                var allocationResult = await stockAllocationService.AllocateStockForOrderAsync(order);

                if (order.CompleteDeliveryRequired && !allocationResult.Success)
                {
                    anyCompleteDeliveryFailure = true;
                }
            }

            // At this stage, SKUs, orders, and allocations have been updated by the
            // allocation service. We now just need to return an appropriate result.

            if (anyCompleteDeliveryFailure)
            {
                return new CorrectionResult
                {
                    Success = false,
                    Message = $"SKU {skuId} quantity corrected from {oldTotalQuantity} to {newQuantity}, but existing allocations were deallocated due to insufficient substitutes to maintain the complete delivery requirement."
                };
            }

            // For partial-delivery scenarios (or when substitutes fully cover the
            // shortfall), we treat the correction as successful even if some orders
            // end up only partially allocated.
            return new CorrectionResult
            {
                Success = true,
                Message = $"SKU {skuId} quantity corrected from {oldTotalQuantity} to {newQuantity}. Allocations were recalculated across available SKUs."
            };
        }
    }
}
