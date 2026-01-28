using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.StockAllocation.Tests;

public class SKURepositoryTests : TestBase
{

    [Test]
    public async Task GetAvailableSKUsByProductAsync_ShouldReturnOnlyUnlockedWithStock()
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

