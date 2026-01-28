// ============================================================================
// SCENARIO 1: Stock Allocation Service Tests
// ============================================================================

using FluentAssertions;
using WMS.Backend.API.Enums;
using WMS.Backend.API.Services;
using WMS.StockAllocation.Tests;

namespace WMS.StockAllocation.Tests;

[TestFixture]
public class StockAllocationServiceTests : TestBase
{
    private StockAllocationService _service;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _service = new StockAllocationService(_orderRepository, _skuRepository, _allocationRepository);
    }

    [Test]
    public async Task AllocateStockForOrder_WithSufficientStock_ShouldAllocateSuccessfully()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 100);
        var order = await CreateTestOrder("ORD001", OrderPriority.High);
        await CreateTestLineItem("LINE001", "ORD001", "P001", 50);

        // Reload order with line items
        order = await _orderRepository.GetOrderAsync("ORD001");

        // Act
        var result = await _service.AllocateStockForOrderAsync(order);

        // Assert
        result.Success.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.ReadyForDelivery);
        order.LineItems.First().AllocatedQuantity.Should().Be(50);
        order.LineItems.First().Status.Should().Be(LineItemStatus.Allocated);

        var sku = await _skuRepository.GetSKUAsync("SKU001");
        sku.AvailableQuantity.Should().Be(50);
    }

    [Test]
    public async Task AllocateStockForOrder_WithInsufficientStock_ShouldPartiallyAllocate()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 30);
        var order = await CreateTestOrder("ORD001", OrderPriority.Normal);
        await CreateTestLineItem("LINE001", "ORD001", "P001", 50);

        order = await _orderRepository.GetOrderAsync("ORD001");

        // Act
        var result = await _service.AllocateStockForOrderAsync(order);

        // Assert
        result.Success.Should().BeFalse();
        order.Status.Should().Be(OrderStatus.PartiallyAllocated);
        order.LineItems.First().AllocatedQuantity.Should().Be(30);
        order.LineItems.First().Status.Should().Be(LineItemStatus.PartiallyAllocated);
    }

    [Test]
    public async Task AllocateStockForOrder_CompleteDeliveryRequired_WithInsufficientStock_ShouldNotAllocate()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 30);
        var order = await CreateTestOrder("ORD001", OrderPriority.High, completeDeliveryRequired: true);
        await CreateTestLineItem("LINE001", "ORD001", "P001", 50);

        order = await _orderRepository.GetOrderAsync("ORD001");

        // Act
        var result = await _service.AllocateStockForOrderAsync(order);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Insufficient stock");
        order.Status.Should().Be(OrderStatus.NotAllocated);
        order.LineItems.First().AllocatedQuantity.Should().Be(0);
    }

    [Test]
    public async Task AllocateStockForOrder_WithMultipleSKUs_ShouldAllocateFromMultipleSources()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 30, "A-01");
        await CreateTestSKU("SKU002", "P001", 25, "B-02");
        var order = await CreateTestOrder("ORD001", OrderPriority.High);
        await CreateTestLineItem("LINE001", "ORD001", "P001", 50);

        order = await _orderRepository.GetOrderAsync("ORD001");

        // Act
        var result = await _service.AllocateStockForOrderAsync(order);

        // Assert
        result.Success.Should().BeTrue();
        order.LineItems.First().AllocatedQuantity.Should().Be(50);
        order.LineItems.First().Allocations.Should().HaveCount(2);

        var sku1 = await _skuRepository.GetSKUAsync("SKU001");
        var sku2 = await _skuRepository.GetSKUAsync("SKU002");
        sku1.AvailableQuantity.Should().Be(0);
        sku2.AvailableQuantity.Should().Be(5);
    }

    [Test]
    public async Task AllocateStockForOrder_WithLockedLocation_ShouldSkipLockedSKU()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 100, "A-01", isLocked: true);
        await CreateTestSKU("SKU002", "P001", 50, "B-02", isLocked: false);
        var order = await CreateTestOrder("ORD001", OrderPriority.High);
        await CreateTestLineItem("LINE001", "ORD001", "P001", 40);

        order = await _orderRepository.GetOrderAsync("ORD001");

        // Act
        var result = await _service.AllocateStockForOrderAsync(order);

        // Assert
        result.Success.Should().BeTrue();
        order.LineItems.First().AllocatedQuantity.Should().Be(40);

        // Should only allocate from SKU002 (unlocked)
        var sku1 = await _skuRepository.GetSKUAsync("SKU001");
        var sku2 = await _skuRepository.GetSKUAsync("SKU002");
        sku1.AvailableQuantity.Should().Be(100); // Unchanged
        sku2.AvailableQuantity.Should().Be(10);  // Used
    }

    [Test]
    public async Task ProcessOrderAllocations_ShouldProcessOrdersByPriority()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 50);

        var lowPriorityOrder = await CreateTestOrder("ORD001", OrderPriority.Low);
        await CreateTestLineItem("LINE001", "ORD001", "P001", 30);

        var highPriorityOrder = await CreateTestOrder("ORD002", OrderPriority.High);
        await CreateTestLineItem("LINE002", "ORD002", "P001", 40);

        // Act
        await _service.ProcessOrderAllocationsAsync();

        // Assert
        var highOrder = await _orderRepository.GetOrderAsync("ORD002");
        var lowOrder = await _orderRepository.GetOrderAsync("ORD001");

        // High priority should be fully allocated
        highOrder.LineItems.First().AllocatedQuantity.Should().Be(40);
        highOrder.Status.Should().Be(OrderStatus.ReadyForDelivery);

        // Low priority should get remaining stock
        lowOrder.LineItems.First().AllocatedQuantity.Should().Be(10);
        lowOrder.Status.Should().Be(OrderStatus.PartiallyAllocated);
    }

    [Test]
    public async Task AllocateStockForOrder_WithMultipleLineItems_ShouldAllocateAll()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 100);
        await CreateTestSKU("SKU002", "P002", 50);

        var order = await CreateTestOrder("ORD001", OrderPriority.High);
        await CreateTestLineItem("LINE001", "ORD001", "P001", 30);
        await CreateTestLineItem("LINE002", "ORD001", "P002", 20);

        order = await _orderRepository.GetOrderAsync("ORD001");

        // Act
        var result = await _service.AllocateStockForOrderAsync(order);

        // Assert
        result.Success.Should().BeTrue();
        order.LineItems.Should().HaveCount(2);
        order.LineItems.Should().OnlyContain(li => li.Status == LineItemStatus.Allocated);
        order.Status.Should().Be(OrderStatus.ReadyForDelivery);
    }
}