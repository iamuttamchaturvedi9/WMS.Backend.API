// ============================================================================
// Integration Tests (End-to-End Scenarios)
// ============================================================================

using FluentAssertions;
using WMS.Backend.API.Enums;
using WMS.Backend.API.Services;
using WMS.StockAllocation.Tests;

namespace WMS.StockAllocation.Tests;

[TestFixture]
public class IntegrationTests : TestBase
{
    private StockAllocationService _allocationService;
    private OrderCancellationService _cancellationService;
    private SKUCorrectionService _correctionService;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _allocationService = new StockAllocationService(_orderRepository, _skuRepository, _allocationRepository);
        _cancellationService = new OrderCancellationService(_orderRepository, _skuRepository, _allocationRepository);
        _correctionService = new SKUCorrectionService(_orderRepository, _skuRepository, _allocationRepository);
    }

    [Test]
    public async Task EndToEnd_AllocateThenCancel_ShouldRestoreStock()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 100);
        var order = await CreateTestOrder("ORD001", OrderPriority.High);
        await CreateTestLineItem("LINE001", "ORD001", "P001", 50);

        // Act - Allocate
        order = await _orderRepository.GetOrderAsync("ORD001");
        await _allocationService.AllocateStockForOrderAsync(order);

        var skuAfterAllocation = await _skuRepository.GetSKUAsync("SKU001");
        skuAfterAllocation.AvailableQuantity.Should().Be(50);

        // Act - Cancel
        await _cancellationService.CancelOrderAsync("ORD001");

        // Assert
        var skuAfterCancellation = await _skuRepository.GetSKUAsync("SKU001");
        skuAfterCancellation.AvailableQuantity.Should().Be(100);
    }

    [Test]
    public async Task EndToEnd_MultipleOrdersPriorityAllocation_ShouldAllocateByPriority()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 100);

        // Low priority order created first
        var lowOrder = await CreateTestOrder("ORD_LOW", OrderPriority.Low);
        lowOrder.OrderDate = DateTime.UtcNow.AddMinutes(-10);
        await _orderRepository.UpdateOrderAsync(lowOrder);
        await CreateTestLineItem("LINE_LOW", "ORD_LOW", "P001", 60);

        // High priority order created later
        var highOrder = await CreateTestOrder("ORD_HIGH", OrderPriority.High);
        highOrder.OrderDate = DateTime.UtcNow;
        await _orderRepository.UpdateOrderAsync(highOrder);
        await CreateTestLineItem("LINE_HIGH", "ORD_HIGH", "P001", 70);

        // Act
        await _allocationService.ProcessOrderAllocationsAsync();

        // Assert
        var highOrderResult = await _orderRepository.GetOrderAsync("ORD_HIGH");
        var lowOrderResult = await _orderRepository.GetOrderAsync("ORD_LOW");

        // High priority should get full allocation even though created later
        highOrderResult.LineItems.First().AllocatedQuantity.Should().Be(70);
        highOrderResult.Status.Should().Be(OrderStatus.ReadyForDelivery);

        // Low priority should get remaining stock
        lowOrderResult.LineItems.First().AllocatedQuantity.Should().Be(30);
        lowOrderResult.Status.Should().Be(OrderStatus.PartiallyAllocated);
    }

    [Test]
    public async Task EndToEnd_SKUCorrectionWithReallocation_ShouldMaintainConsistency()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 100);
        await CreateTestSKU("SKU002", "P001", 50);

        var order = await CreateTestOrder("ORD001", OrderPriority.High);
        await CreateTestLineItem("LINE001", "ORD001", "P001", 60);

        // Allocate from SKU001
        order = await _orderRepository.GetOrderAsync("ORD001");
        await _allocationService.AllocateStockForOrderAsync(order);

        // Verify initial allocation
        var sku1Before = await _skuRepository.GetSKUAsync("SKU001");
        sku1Before.AvailableQuantity.Should().Be(40);

        // Act - Correct SKU001 quantity down
        await _correctionService.CorrectSKUQuantityAsync("SKU001", 50);

        // Assert - Should reallocate shortage from SKU002
        var orderAfter = await _orderRepository.GetOrderAsync("ORD001");
        orderAfter.LineItems.First().AllocatedQuantity.Should().BeGreaterThanOrEqualTo(50);

        var sku1After = await _skuRepository.GetSKUAsync("SKU001");
        var sku2After = await _skuRepository.GetSKUAsync("SKU002");

        // Total allocated should still cover the order
        var totalAvailable = sku1After.AvailableQuantity + sku2After.AvailableQuantity;
        var totalStock = sku1After.TotalQuantity + sku2After.TotalQuantity;
        totalAvailable.Should().BeLessThanOrEqualTo(totalStock);
    }

    [Test]
    public async Task EndToEnd_CompleteDeliveryWorkflow_ShouldEnforceRule()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 100);

        var order1 = await CreateTestOrder("ORD001", OrderPriority.High, completeDeliveryRequired: true);
        await CreateTestLineItem("LINE001", "ORD001", "P001", 120);

        var order2 = await CreateTestOrder("ORD002", OrderPriority.Normal, completeDeliveryRequired: false);
        await CreateTestLineItem("LINE002", "ORD002", "P001", 120);

        // Act
        order1 = await _orderRepository.GetOrderAsync("ORD001");
        var result1 = await _allocationService.AllocateStockForOrderAsync(order1);

        order2 = await _orderRepository.GetOrderAsync("ORD002");
        var result2 = await _allocationService.AllocateStockForOrderAsync(order2);

        // Assert
        // Order 1 with complete delivery should not allocate at all
        result1.Success.Should().BeFalse();
        order1.Status.Should().Be(OrderStatus.NotAllocated);
        order1.LineItems.First().AllocatedQuantity.Should().Be(0);

        // Order 2 without complete delivery should partially allocate
        result2.Success.Should().BeFalse();
        order2.Status.Should().Be(OrderStatus.PartiallyAllocated);
        order2.LineItems.First().AllocatedQuantity.Should().Be(100);
    }

    [Test]
    public async Task EndToEnd_LockedLocationScenario_ShouldSkipLockedSKUs()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 100, "A-01", isLocked: true);
        await CreateTestSKU("SKU002", "P001", 50, "B-02", isLocked: false);
        await CreateTestSKU("SKU003", "P001", 30, "C-03", isLocked: false);

        var order = await CreateTestOrder("ORD001", OrderPriority.High);
        await CreateTestLineItem("LINE001", "ORD001", "P001", 70);

        // Act
        order = await _orderRepository.GetOrderAsync("ORD001");
        await _allocationService.AllocateStockForOrderAsync(order);

        // Assert
        var sku1 = await _skuRepository.GetSKUAsync("SKU001");
        var sku2 = await _skuRepository.GetSKUAsync("SKU002");
        var sku3 = await _skuRepository.GetSKUAsync("SKU003");

        // SKU001 locked - should not be used
        sku1.AvailableQuantity.Should().Be(100);

        // SKU002 and SKU003 should be used
        sku2.AvailableQuantity.Should().Be(0);
        sku3.AvailableQuantity.Should().Be(10);

        order.LineItems.First().AllocatedQuantity.Should().Be(70);
        order.Status.Should().Be(OrderStatus.ReadyForDelivery);
    }
}