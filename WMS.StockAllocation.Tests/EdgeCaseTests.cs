using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Backend.API.Enums;
using WMS.Backend.API.Services;

namespace WMS.StockAllocation.Tests;

// ============================================================================
// Performance and Edge Case Tests
// ============================================================================

[TestFixture]
public class EdgeCaseTests : TestBase
{
    private StockAllocationService _allocationService;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _allocationService = new StockAllocationService(_orderRepository, _skuRepository, _allocationRepository);
    }

    [Test]
    public async Task ZeroQuantityOrder_ShouldHandleGracefully()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 100);
        var order = await CreateTestOrder("ORD001", OrderPriority.High);
        await CreateTestLineItem("LINE001", "ORD001", "P001", 0);

        // Act
        order = await _orderRepository.GetOrderAsync("ORD001");
        var result = await _allocationService.AllocateStockForOrderAsync(order);

        // Assert - Should handle gracefully
        result.Should().NotBeNull();
    }

    [Test]
    public async Task ExactStockMatch_ShouldAllocateCompletely()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 50);
        var order = await CreateTestOrder("ORD001", OrderPriority.High);
        await CreateTestLineItem("LINE001", "ORD001", "P001", 50);

        // Act
        order = await _orderRepository.GetOrderAsync("ORD001");
        await _allocationService.AllocateStockForOrderAsync(order);

        // Assert
        var sku = await _skuRepository.GetSKUAsync("SKU001");
        sku.AvailableQuantity.Should().Be(0);
        order.LineItems.First().AllocatedQuantity.Should().Be(50);
    }

    [Test]
    public async Task MultipleOrdersSameProduct_ShouldDistributeStock()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 100);

        var order1 = await CreateTestOrder("ORD001", OrderPriority.High);
        await CreateTestLineItem("LINE001", "ORD001", "P001", 40);

        var order2 = await CreateTestOrder("ORD002", OrderPriority.High);
        order2.OrderDate = DateTime.UtcNow.AddSeconds(1); // Slightly later
        await _orderRepository.UpdateOrderAsync(order2);
        await CreateTestLineItem("LINE002", "ORD002", "P001", 40);

        var order3 = await CreateTestOrder("ORD003", OrderPriority.High);
        order3.OrderDate = DateTime.UtcNow.AddSeconds(2);
        await _orderRepository.UpdateOrderAsync(order3);
        await CreateTestLineItem("LINE003", "ORD003", "P001", 40);

        // Act
        await _allocationService.ProcessOrderAllocationsAsync();

        // Assert
        var result1 = await _orderRepository.GetOrderAsync("ORD001");
        var result2 = await _orderRepository.GetOrderAsync("ORD002");
        var result3 = await _orderRepository.GetOrderAsync("ORD003");

        result1.LineItems.First().AllocatedQuantity.Should().Be(40);
        result2.LineItems.First().AllocatedQuantity.Should().Be(40);
        result3.LineItems.First().AllocatedQuantity.Should().Be(20); // Partial

        var sku = await _skuRepository.GetSKUAsync("SKU001");
        sku.AvailableQuantity.Should().Be(0);
    }
}


