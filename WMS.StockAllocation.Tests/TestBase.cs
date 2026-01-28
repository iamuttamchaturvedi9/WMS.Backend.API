
// ============================================================================
// Test Setup and Helpers
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using FluentAssertions;
using WMS.StockAllocation;
using WMS.Backend.API.Data;
using WMS.Backend.API.Repositories.Interfaces;
using WMS.Backend.API.Repositories;
using WMS.Backend.API.Models;
using WMS.Backend.API.Enums;

namespace WMS.StockAllocation.Tests;

    // Base Test Class with Common Setup
    public abstract class TestBase
    {
        protected WMSDbContext _context;
        protected IOrderRepository _orderRepository;
        protected ISKURepository _skuRepository;
        protected IAllocationRepository _allocationRepository;

        [SetUp]
        public virtual void Setup()
        {
            // Create in-memory database for testing
            var options = new DbContextOptionsBuilder<WMSDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new WMSDbContext(options);

            // Initialize repositories
            _orderRepository = new OrderRepository(_context);
            _skuRepository = new SKURepository(_context);
            _allocationRepository = new AllocationRepository(_context);
        }

        [TearDown]
        public virtual void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        // Helper Methods
        protected async Task<SKU> CreateTestSKU(string skuId, string productNumber, int quantity,
            string location = "A-01", bool isLocked = false)
        {
            var sku = new SKU
            {
                SkuId = skuId,
                ProductNumber = productNumber,
                TotalQuantity = quantity,
                AvailableQuantity = quantity,
                WarehouseLocation = location,
                IsLocationLocked = isLocked
            };

            await _context.SKUs.AddAsync(sku);
            await _context.SaveChangesAsync();
            return sku;
        }

        protected async Task<CustomerOrder> CreateTestOrder(string orderId, OrderPriority priority,
            bool completeDeliveryRequired = false)
        {
            var order = new CustomerOrder
            {
                OrderId = orderId,
                Priority = priority,
                CompleteDeliveryRequired = completeDeliveryRequired,
                OrderDate = DateTime.UtcNow
            };

            await _context.CustomerOrders.AddAsync(order);
            await _context.SaveChangesAsync();
            return order;
        }

        protected async Task<OrderLineItem> CreateTestLineItem(string lineItemId, string orderId,
            string productNumber, int quantity)
        {
            var lineItem = new OrderLineItem
            {
                LineItemId = lineItemId,
                OrderId = orderId,
                ProductNumber = productNumber,
                RequestedQuantity = quantity
            };

            await _context.OrderLineItems.AddAsync(lineItem);
            await _context.SaveChangesAsync();
            return lineItem;
        }
    }