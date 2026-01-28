using WMS.Backend.API.Dtos;
using WMS.Backend.API.Enums;
using WMS.Backend.API.Models;
using WMS.Backend.API.Repositories.Interfaces;

namespace WMS.Backend.API.Services
{
    public class OrderCancellationService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ISKURepository _skuRepository;
        private readonly IAllocationRepository _allocationRepository;

        public OrderCancellationService(
            IOrderRepository orderRepository,
            ISKURepository skuRepository,
            IAllocationRepository allocationRepository)
        {
            _orderRepository = orderRepository;
            _skuRepository = skuRepository;
            _allocationRepository = allocationRepository;
        }

        public async Task<CancellationResult> CancelOrderAsync(string orderId)
        {
            var order = await _orderRepository.GetOrderAsync(orderId);
            if (order == null)
            {
                return new CancellationResult
                {
                    Success = false,
                    Message = $"Order {orderId} not found"
                };
            }

            var allocations = await _allocationRepository.GetAllocationsByOrderAsync(orderId);
            await ReleaseAllocationsAsync(allocations);

            order.Status = OrderStatus.Cancelled;
            foreach (var lineItem in order.LineItems)
            {
                lineItem.Status = LineItemStatus.Cancelled;
                lineItem.AllocatedQuantity = 0;
                lineItem.Allocations.Clear();
            }

            await _orderRepository.UpdateOrderAsync(order);

            return new CancellationResult
            {
                Success = true,
                Message = $"Order {orderId} cancelled successfully. Released {allocations.Count} allocations."
            };
        }

        public async Task<CancellationResult> CancelLineItemAsync(string orderId, string lineItemId)
        {
            var order = await _orderRepository.GetOrderAsync(orderId);
            if (order == null)
            {
                return new CancellationResult
                {
                    Success = false,
                    Message = $"Order {orderId} not found"
                };
            }

            var lineItem = order.LineItems.FirstOrDefault(li => li.LineItemId == lineItemId);
            if (lineItem == null)
            {
                return new CancellationResult
                {
                    Success = false,
                    Message = $"Line item {lineItemId} not found in order {orderId}"
                };
            }

            var allocations = await _allocationRepository.GetAllocationsByLineItemAsync(lineItemId);
            await ReleaseAllocationsAsync(allocations);

            lineItem.Status = LineItemStatus.Cancelled;
            lineItem.AllocatedQuantity = 0;
            lineItem.Allocations.Clear();

            UpdateOrderStatusAfterCancellation(order);
            await _orderRepository.UpdateOrderAsync(order);

            return new CancellationResult
            {
                Success = true,
                Message = $"Line item {lineItemId} cancelled. Released {allocations.Count} allocations."
            };
        }

        private async Task ReleaseAllocationsAsync(List<StockAllocation> allocations)
        {
            foreach (var allocation in allocations)
            {
                var sku = await _skuRepository.GetSKUAsync(allocation.SkuId);
                if (sku != null)
                {
                    sku.AvailableQuantity += allocation.AllocatedQuantity;
                    await _skuRepository.UpdateSKUAsync(sku);
                }

                await _allocationRepository.DeleteAllocationAsync(allocation.AllocationId);
            }
        }

        private void UpdateOrderStatusAfterCancellation(CustomerOrder order)
        {
            var activeLineItems = order.LineItems
                .Where(li => li.Status != LineItemStatus.Cancelled)
                .ToList();

            if (!activeLineItems.Any())
            {
                order.Status = OrderStatus.Cancelled;
            }
            else if (activeLineItems.All(li => li.Status == LineItemStatus.Allocated))
            {
                order.Status = OrderStatus.ReadyForDelivery;
            }
            else if (activeLineItems.Any(li => li.Status == LineItemStatus.Allocated ||
                                              li.Status == LineItemStatus.PartiallyAllocated))
            {
                order.Status = OrderStatus.PartiallyAllocated;
            }
            else
            {
                order.Status = OrderStatus.NotAllocated;
            }
        }
    }
}
