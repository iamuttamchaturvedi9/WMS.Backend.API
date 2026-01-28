// ============================================================================
// SCENARIO 3: SKU Correction Service Tests
// ============================================================================

using FluentAssertions;
using WMS.Backend.API.Enums;
using WMS.Backend.API.Services;
using WMS.StockAllocation.Tests;

namespace WMS.StockAllocation.Tests;

[TestFixture]
public class SKUCorrectionServiceTests : TestBase
{
    private SKUCorrectionService _service;
    private StockAllocationService _allocationService;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _allocationService = new StockAllocationService(_orderRepository, _skuRepository, _allocationRepository);
        _service = new SKUCorrectionService(_orderRepository, _skuRepository, _allocationRepository);
    }

    [Test]
    public async Task CorrectSKUQuantity_IncreaseQuantity_ShouldUpdateSuccessfully()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 100);

        // Act
        var result = await _service.CorrectSKUQuantityAsync("SKU001", 150);

        // Assert
        result.Success.Should().BeTrue();

        var sku = await _skuRepository.GetSKUAsync("SKU001");
        sku.TotalQuantity.Should().Be(150);
        sku.AvailableQuantity.Should().Be(150);
    }

    [Test]
    public async Task CorrectSKUQuantity_DecreaseWithoutAllocations_ShouldUpdateSuccessfully()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 100);

        // Act
        var result = await _service.CorrectSKUQuantityAsync("SKU001", 80);

        // Assert
        result.Success.Should().BeTrue();

        var sku = await _skuRepository.GetSKUAsync("SKU001");
        sku.TotalQuantity.Should().Be(80);
        sku.AvailableQuantity.Should().Be(80);
    }

    [Test]
    public async Task CorrectSKUQuantity_DecreaseWithAllocations_WithSubstitutes_ShouldReallocate()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 100);
        await CreateTestSKU("SKU002", "P001", 50); // Substitute

        var order = await CreateTestOrder("ORD001", OrderPriority.High);
        await CreateTestLineItem("LINE001", "ORD001", "P001", 60);

        order = await _orderRepository.GetOrderAsync("ORD001");
        await _allocationService.AllocateStockForOrderAsync(order);

        // SKU001 should have 40 available (100 - 60 allocated)
        var skuBefore = await _skuRepository.GetSKUAsync("SKU001");
        skuBefore.AvailableQuantity.Should().Be(40);

        // Act - Reduce SKU001 total from 100 to 50 (creates 10 unit shortfall: 60 allocated > 50 total)
        var result = await _service.CorrectSKUQuantityAsync("SKU001", 50);

        // Assert
        result.Success.Should().BeTrue();

        var sku1 = await _skuRepository.GetSKUAsync("SKU001");
        var sku2 = await _skuRepository.GetSKUAsync("SKU002");

        sku1.TotalQuantity.Should().Be(50);
        // Should reallocate shortage from SKU002
        sku2.AvailableQuantity.Should().BeLessThan(50);
    }

    [Test]
    public async Task CorrectSKUQuantity_WithCompleteDeliveryRequired_InsufficientSubstitutes_ShouldDeallocate()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 100);
        await CreateTestSKU("SKU002", "P001", 5); // Insufficient substitute

        var order = await CreateTestOrder("ORD001", OrderPriority.High, completeDeliveryRequired: true);
        await CreateTestLineItem("LINE001", "ORD001", "P001", 60);

        order = await _orderRepository.GetOrderAsync("ORD001");
        await _allocationService.AllocateStockForOrderAsync(order);

        // Act - Reduce quantity creating shortfall that can't be covered
        var result = await _service.CorrectSKUQuantityAsync("SKU001", 50);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("deallocated");

        var updatedOrder = await _orderRepository.GetOrderAsync("ORD001");
        updatedOrder.Status.Should().Be(OrderStatus.NotAllocated);
        updatedOrder.LineItems.First().AllocatedQuantity.Should().Be(0);
    }

    [Test]
    public async Task CorrectSKUQuantity_NonExistentSKU_ShouldReturnFailure()
    {
        // Act
        var result = await _service.CorrectSKUQuantityAsync("NON_EXISTENT", 100);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Test]
    public async Task CorrectSKUQuantity_WithPartialDeliveryAllowed_InsufficientSubstitutes_ShouldPartiallyAllocate()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 100);
        await CreateTestSKU("SKU002", "P001", 10); // Partial substitute

        var order = await CreateTestOrder("ORD001", OrderPriority.Normal, completeDeliveryRequired: false);
        await CreateTestLineItem("LINE001", "ORD001", "P001", 60);

        order = await _orderRepository.GetOrderAsync("ORD001");
        await _allocationService.AllocateStockForOrderAsync(order);

        // Act - Create shortfall
        var result = await _service.CorrectSKUQuantityAsync("SKU001", 40);

        // Assert
        var updatedOrder = await _orderRepository.GetOrderAsync("ORD001");
        updatedOrder.Status.Should().Be(OrderStatus.PartiallyAllocated);
        updatedOrder.LineItems.First().Status.Should().Be(LineItemStatus.PartiallyAllocated);
    }
}