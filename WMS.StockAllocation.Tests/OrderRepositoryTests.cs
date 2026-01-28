// ============================================================================
// Repository Tests
// ============================================================================

using FluentAssertions;
using WMS.Backend.API.Enums;
using WMS.Backend.API.Models;
using WMS.StockAllocation.Tests;

[TestFixture]
public class OrderRepositoryTests : TestBase
{
    [Test]
    public async Task GetOrderAsync_ExistingOrder_ShouldReturnWithLineItems()
    {
        // Arrange
        var order = await CreateTestOrder("ORD001", OrderPriority.High);
        await CreateTestLineItem("LINE001", "ORD001", "P001", 50);

        // Act
        var result = await _orderRepository.GetOrderAsync("ORD001");

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be("ORD001");
        result.LineItems.Should().HaveCount(1);
        result.LineItems.First().ProductNumber.Should().Be("P001");
    }

    [Test]
    public async Task GetReleasedOrdersAsync_ShouldReturnOnlyReleasedOrders()
    {
        // Arrange
        var order1 = await CreateTestOrder("ORD001", OrderPriority.High);
        order1.Status = OrderStatus.Received;
        await _orderRepository.UpdateOrderAsync(order1);

        var order2 = await CreateTestOrder("ORD002", OrderPriority.Normal);
        order2.Status = OrderStatus.Cancelled;
        await _orderRepository.UpdateOrderAsync(order2);

        var order3 = await CreateTestOrder("ORD003", OrderPriority.Low);
        order3.Status = OrderStatus.NotAllocated;
        await _orderRepository.UpdateOrderAsync(order3);

        // Act
        var result = await _orderRepository.GetReleasedOrdersAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(o => o.OrderId == "ORD001");
        result.Should().Contain(o => o.OrderId == "ORD003");
        result.Should().NotContain(o => o.OrderId == "ORD002");
    }

    [Test]
    public async Task SaveOrderAsync_NewOrder_ShouldPersist()
    {
        // Arrange
        var order = new CustomerOrder
        {
            OrderId = "ORD_NEW",
            Priority = OrderPriority.High,
            CompleteDeliveryRequired = true
        };

        // Act
        await _orderRepository.SaveOrderAsync(order);

        // Assert
        var saved = await _orderRepository.GetOrderAsync("ORD_NEW");
        saved.Should().NotBeNull();
        saved.Priority.Should().Be(OrderPriority.High);
        saved.CompleteDeliveryRequired.Should().BeTrue();
    }
}

[TestFixture]
public class SKURepositoryTests : TestBase
{
    [Test]
    public async Task GetAvailableSKUsByProductAsyncShouldReturnOnlyUnlockedWithStock()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 100, "A-01", isLocked: false);
        await CreateTestSKU("SKU002", "P001", 0, "A-02", isLocked: false);
        await CreateTestSKU("SKU003", "P001", 50, "A-03", isLocked: true);
        await CreateTestSKU("SKU004", "P002", 30, "B-01", isLocked: false);

        // Act
        var result = await _skuRepository.GetAvailableSKUsByProductAsync("P001");

        // Assert
        result.Should().HaveCount(1);
        result.First().SkuId.Should().Be("SKU001");
    }

    [Test]
    public async Task GetSKUsByProductAsync_ShouldReturnAllSKUsForProduct()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 100, "A-01");
        await CreateTestSKU("SKU002", "P001", 50, "A-02");
        await CreateTestSKU("SKU003", "P002", 30, "B-01");

        // Act
        var result = await _skuRepository.GetSKUsByProductAsync("P001");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(s => s.SkuId == "SKU001");
        result.Should().Contain(s => s.SkuId == "SKU002");
    }

    [Test]
    public async Task UpdateSKUAsync_ShouldPersistChanges()
    {
        // Arrange
        var sku = await CreateTestSKU("SKU001", "P001", 100);

        // Act
        sku.AvailableQuantity = 75;
        await _skuRepository.UpdateSKUAsync(sku);

        // Assert
        var updated = await _skuRepository.GetSKUAsync("SKU001");
        updated.AvailableQuantity.Should().Be(75);
    }
}

[TestFixture]
public class AllocationRepositoryTests : TestBase
{
    [Test]
    public async Task SaveAllocationAsync_ShouldPersist()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 100);
        var order = await CreateTestOrder("ORD001", OrderPriority.High);
        await CreateTestLineItem("LINE001", "ORD001", "P001", 50);

        var allocation = new StockAllocation
        {
            OrderId = "ORD001",
            LineItemId = "LINE001",
            SkuId = "SKU001",
            AllocatedQuantity = 50
        };

        // Act
        await _allocationRepository.SaveAllocationAsync(allocation);

        // Assert
        var saved = await _allocationRepository.GetAllocationsByOrderAsync("ORD001");
        saved.Should().HaveCount(1);
        saved.First().AllocatedQuantity.Should().Be(50);
    }

    [Test]
    public async Task GetAllocationsByLineItemAsync_ShouldReturnCorrectAllocations()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 100);
        await CreateTestSKU("SKU002", "P001", 50);
        var order = await CreateTestOrder("ORD001", OrderPriority.High);
        await CreateTestLineItem("LINE001", "ORD001", "P001", 80);

        var allocation1 = new StockAllocation
        {
            OrderId = "ORD001",
            LineItemId = "LINE001",
            SkuId = "SKU001",
            AllocatedQuantity = 50
        };

        var allocation2 = new StockAllocation
        {
            OrderId = "ORD001",
            LineItemId = "LINE001",
            SkuId = "SKU002",
            AllocatedQuantity = 30
        };

        await _allocationRepository.SaveAllocationAsync(allocation1);
        await _allocationRepository.SaveAllocationAsync(allocation2);

        // Act
        var result = await _allocationRepository.GetAllocationsByLineItemAsync("LINE001");

        // Assert
        result.Should().HaveCount(2);
        result.Sum(a => a.AllocatedQuantity).Should().Be(80);
    }

    [Test]
    public async Task DeleteAllocationAsync_ShouldRemoveAllocation()
    {
        // Arrange
        await CreateTestSKU("SKU001", "P001", 100);
        var order = await CreateTestOrder("ORD001", OrderPriority.High);
        await CreateTestLineItem("LINE001", "ORD001", "P001", 50);

        var allocation = new StockAllocation
        {
            AllocationId = "ALLOC001",
            OrderId = "ORD001",
            LineItemId = "LINE001",
            SkuId = "SKU001",
            AllocatedQuantity = 50
        };

        await _allocationRepository.SaveAllocationAsync(allocation);

        // Act
        await _allocationRepository.DeleteAllocationAsync("ALLOC001");

        // Assert
        var remaining = await _allocationRepository.GetAllocationsByOrderAsync("ORD001");
        remaining.Should().BeEmpty();
    }
}