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

            // Get current allocated quantity
            var allocations = await _allocationRepository.GetAllocationsBySKUAsync(skuId);
            var allocatedQuantity = allocations.Sum(a => a.AllocatedQuantity);

            // Validate that new quantity is not less than allocated quantity
            if (newQuantity < allocatedQuantity)
            {
                return new CorrectionResult
                {
                    Success = false,
                    Message = $"Cannot set quantity to {newQuantity}. There are {allocatedQuantity} units already allocated. New quantity must be at least {allocatedQuantity}."
                };
            }

            // Update the SKU quantities
            var oldTotalQuantity = sku.TotalQuantity;
            sku.TotalQuantity = newQuantity;
            sku.AvailableQuantity = newQuantity - allocatedQuantity;
            sku.LastModifiedDate = DateTime.UtcNow;

            await _skuRepository.UpdateSKUAsync(sku);

            return new CorrectionResult
            {
                Success = true,
                Message = $"SKU {skuId} quantity corrected from {oldTotalQuantity} to {newQuantity}. Available quantity: {sku.AvailableQuantity}, Allocated quantity: {allocatedQuantity}."
            };
        }
    }
}
