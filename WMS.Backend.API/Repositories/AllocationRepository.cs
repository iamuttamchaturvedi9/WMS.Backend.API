using Microsoft.EntityFrameworkCore;
using WMS.Backend.API.Data;
using WMS.Backend.API.Models;
using WMS.Backend.API.Repositories.Interfaces;
namespace WMS.Backend.API.Repositories;

public class AllocationRepository : IAllocationRepository
{
    private readonly WMSDbContext _context;

    public AllocationRepository(WMSDbContext context)
    {
        _context = context;
    }

    public async Task SaveAllocationAsync(StockAllocation allocation)
    {
        await _context.StockAllocations.AddAsync(allocation);
        await _context.SaveChangesAsync();
    }

    public async Task SaveAllocationsAsync(List<StockAllocation> allocations)
    {
        await _context.StockAllocations.AddRangeAsync(allocations);
        await _context.SaveChangesAsync();
    }

    public async Task<List<StockAllocation>> GetAllocationsByOrderAsync(string orderId)
    {
        return await _context.StockAllocations
            .Where(a => a.OrderId == orderId)
            .ToListAsync();
    }

    public async Task<List<StockAllocation>> GetAllocationsByLineItemAsync(string lineItemId)
    {
        return await _context.StockAllocations
            .Where(a => a.LineItemId == lineItemId)
            .ToListAsync();
    }

    public async Task<List<StockAllocation>> GetAllocationsBySKUAsync(string skuId)
    {
        return await _context.StockAllocations
            .Where(a => a.SkuId == skuId)
            .ToListAsync();
    }

    public async Task DeleteAllocationAsync(string allocationId)
    {
        var allocation = await _context.StockAllocations.FindAsync(allocationId);
        if (allocation != null)
        {
            _context.StockAllocations.Remove(allocation);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAllocationsAsync(List<string> allocationIds)
    {
        var allocations = await _context.StockAllocations
            .Where(a => allocationIds.Contains(a.AllocationId))
            .ToListAsync();

        _context.StockAllocations.RemoveRange(allocations);
        await _context.SaveChangesAsync();
    }
}
