using FluentAssertions;
using WMS.Backend.API.Enums;
using WMS.Backend.API.Services;
using WMS.StockAllocation.Tests;

namespace WMS.StockAllocation.Tests;

[TestFixture]
public class OrderCancellationServiceTests : TestBase
{
    private OrderCancellationService _service;
    private StockAllocationService _allocationService;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _service = new OrderCancellationService(_orderRepository, _skuRepository, _allocationRepository);
        _allocationService = new StockAllocationService(_orderRepository, _skuRepository, _allocationRepository);
    }

    [Test]
    public async Task CancelOrder_WithAllocatedStock_ShouldReleaseStock()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 100);
        var order = await CreateTestOrder("ORD001", OrderPriority.High);
        await CreateTestLineItem("LINE001", "ORD001", "P001", 50);

        order = await _orderRepository.GetOrderAsync("ORD001");
        await _allocationService.AllocateStockForOrderAsync(order);

        // Verify stock was allocated
        var skuBefore = await _skuRepository.GetSKUAsync("SKU001");
        skuBefore.AvailableQuantity.Should().Be(50);

        // Act
        var result = await _service.CancelOrderAsync("ORD001");

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("cancelled successfully");

        var cancelledOrder = await _orderRepository.GetOrderAsync("ORD001");
        cancelledOrder.Status.Should().Be(OrderStatus.Cancelled);
        cancelledOrder.LineItems.First().Status.Should().Be(LineItemStatus.Cancelled);
        cancelledOrder.LineItems.First().AllocatedQuantity.Should().Be(0);

        // Stock should be released
        var skuAfter = await _skuRepository.GetSKUAsync("SKU001");
        skuAfter.AvailableQuantity.Should().Be(100);
    }

    [Test]
    public async Task CancelOrder_NonExistent_ShouldReturnFailure()
    {
        // Act
        var result = await _service.CancelOrderAsync("NON_EXISTENT");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Test]
    public async Task CancelLineItem_ShouldReleaseOnlyLineItemStock()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 100);
        await CreateTestSKU("SKU002", "P002", 50);

        var order = await CreateTestOrder("ORD001", OrderPriority.High);
        await CreateTestLineItem("LINE001", "ORD001", "P001", 30);
        await CreateTestLineItem("LINE002", "ORD001", "P002", 20);

        order = await _orderRepository.GetOrderAsync("ORD001");
        await _allocationService.AllocateStockForOrderAsync(order);

        // Act
        var result = await _service.CancelLineItemAsync("ORD001", "LINE001");

        // Assert
        result.Success.Should().BeTrue();

        var updatedOrder = await _orderRepository.GetOrderAsync("ORD001");
        var line1 = updatedOrder.LineItems.First(li => li.LineItemId == "LINE001");
        var line2 = updatedOrder.LineItems.First(li => li.LineItemId == "LINE002");

        line1.Status.Should().Be(LineItemStatus.Cancelled);
        line1.AllocatedQuantity.Should().Be(0);
        line2.Status.Should().Be(LineItemStatus.Allocated);
        line2.AllocatedQuantity.Should().Be(20);

        // Check stock
        var sku1 = await _skuRepository.GetSKUAsync("SKU001");
        var sku2 = await _skuRepository.GetSKUAsync("SKU002");
        sku1.AvailableQuantity.Should().Be(100); // Fully released
        sku2.AvailableQuantity.Should().Be(30);  // Still allocated
    }

    [Test]
    public async Task CancelLineItem_LastActiveLineItem_ShouldCancelOrder()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 100);
        var order = await CreateTestOrder("ORD001", OrderPriority.High);
        await CreateTestLineItem("LINE001", "ORD001", "P001", 30);

        order = await _orderRepository.GetOrderAsync("ORD001");
        await _allocationService.AllocateStockForOrderAsync(order);

        // Act
        var result = await _service.CancelLineItemAsync("ORD001", "LINE001");

        // Assert
        var updatedOrder = await _orderRepository.GetOrderAsync("ORD001");
        updatedOrder.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Test]
    public async Task CancelOrder_WithMultipleAllocations_ShouldReleaseAll()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 30);
        await CreateTestSKU("SKU002", "P001", 25);

        var order = await CreateTestOrder("ORD001", OrderPriority.High);
        await CreateTestLineItem("LINE001", "ORD001", "P001", 50);

        order = await _orderRepository.GetOrderAsync("ORD001");
        await _allocationService.AllocateStockForOrderAsync(order);

        // Act
        var result = await _service.CancelOrderAsync("ORD001");

        // Assert
        result.Success.Should().BeTrue();

        var sku1 = await _skuRepository.GetSKUAsync("SKU001");
        var sku2 = await _skuRepository.GetSKUAsync("SKU002");
        sku1.AvailableQuantity.Should().Be(30);
        sku2.AvailableQuantity.Should().Be(25);
    }
}
