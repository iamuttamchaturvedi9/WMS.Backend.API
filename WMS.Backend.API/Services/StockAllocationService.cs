using WMS.Backend.API.Dtos;
using WMS.Backend.API.Enums;
using WMS.Backend.API.Models;
using WMS.Backend.API.Repositories;
using WMS.Backend.API.Repositories.Interfaces;
namespace WMS.Backend.API.Services;

public class StockAllocationService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ISKURepository _skuRepository;
    private readonly IAllocationRepository _allocationRepository;

    public StockAllocationService(
        IOrderRepository orderRepository,
        ISKURepository skuRepository,
        IAllocationRepository allocationRepository)
    {
        _orderRepository = orderRepository;
        _skuRepository = skuRepository;
        _allocationRepository = allocationRepository;
    }

    public async Task ProcessOrderAllocationsAsync()
    {
        var orders = await _orderRepository.GetReleasedOrdersAsync();
        var sortedOrders = SortOrdersByPriority(orders);

        foreach (var order in sortedOrders)
        {
            await AllocateStockForOrderAsync(order);
        }
    }

    private List<CustomerOrder> SortOrdersByPriority(List<CustomerOrder> orders)
    {
        return orders
            .OrderByDescending(o => o.Priority)
            .ThenBy(o => o.OrderDate)
            .ToList();
    }

    public async Task<AllocationResult> AllocateStockForOrderAsync(CustomerOrder order)
    {
        var result = new AllocationResult { OrderId = order.OrderId };

        if (order.CompleteDeliveryRequired)
        {
            if (!await CanFulfillCompleteOrderAsync(order))
            {
                order.Status = OrderStatus.NotAllocated;
                await _orderRepository.UpdateOrderAsync(order);
                result.Success = false;
                result.Message = "Insufficient stock for complete delivery requirement";
                return result;
            }
        }

        foreach (var lineItem in order.LineItems)
        {
            await AllocateStockForLineItemAsync(order, lineItem);
        }

        UpdateOrderStatus(order);
        await _orderRepository.UpdateOrderAsync(order);

        result.Success = order.Status == OrderStatus.Allocated ||
                       order.Status == OrderStatus.ReadyForDelivery;
        result.Message = $"Order {order.OrderId} allocated with status: {order.Status}";
        return result;
    }

    private async Task<bool> CanFulfillCompleteOrderAsync(CustomerOrder order)
    {
        foreach (var lineItem in order.LineItems)
        {
            var availableSKUs = await _skuRepository.GetAvailableSKUsByProductAsync(lineItem.ProductNumber);
            var totalAvailable = availableSKUs.Sum(s => s.AvailableQuantity);

            if (totalAvailable < lineItem.RequestedQuantity)
            {
                return false;
            }
        }
        return true;
    }

    private async Task AllocateStockForLineItemAsync(CustomerOrder order, OrderLineItem lineItem)
    {
        var availableSKUs = await _skuRepository.GetAvailableSKUsByProductAsync(lineItem.ProductNumber);
        var remainingQuantity = lineItem.RequestedQuantity;

        foreach (var sku in availableSKUs)
        {
            if (remainingQuantity <= 0) break;

            var quantityToAllocate = Math.Min(remainingQuantity, sku.AvailableQuantity);

            var allocation = new StockAllocation
            {
                OrderId = order.OrderId,
                LineItemId = lineItem.LineItemId,
                SkuId = sku.SkuId,
                AllocatedQuantity = quantityToAllocate
            };

            sku.AvailableQuantity -= quantityToAllocate;
            await _skuRepository.UpdateSKUAsync(sku);

            await _allocationRepository.SaveAllocationAsync(allocation);
            lineItem.Allocations.Add(allocation);

            lineItem.AllocatedQuantity += quantityToAllocate;
            remainingQuantity -= quantityToAllocate;
        }

        if (lineItem.AllocatedQuantity == lineItem.RequestedQuantity)
        {
            lineItem.Status = LineItemStatus.Allocated;
        }
        else if (lineItem.AllocatedQuantity > 0)
        {
            lineItem.Status = LineItemStatus.PartiallyAllocated;
        }
        else
        {
            lineItem.Status = LineItemStatus.NotAllocated;
        }
    }

    private void UpdateOrderStatus(CustomerOrder order)
    {
        if (order.IsFullyAllocated())
        {
            order.Status = OrderStatus.ReadyForDelivery;
        }
        else if (order.IsPartiallyAllocated())
        {
            order.Status = OrderStatus.PartiallyAllocated;
        }
        else
        {
            order.Status = OrderStatus.NotAllocated;
        }
    }
}

