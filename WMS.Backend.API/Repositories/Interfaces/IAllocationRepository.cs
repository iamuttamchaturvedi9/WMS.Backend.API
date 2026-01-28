using WMS.Backend.API.Models;
namespace WMS.Backend.API.Repositories.Interfaces;
public interface IAllocationRepository
{
    Task SaveAllocationAsync(StockAllocation allocation);
    Task SaveAllocationsAsync(List<StockAllocation> allocations);
    Task<List<StockAllocation>> GetAllocationsByOrderAsync(string orderId);
    Task<List<StockAllocation>> GetAllocationsByLineItemAsync(string lineItemId);
    Task<List<StockAllocation>> GetAllocationsBySKUAsync(string skuId);
    Task DeleteAllocationAsync(string allocationId);
    Task DeleteAllocationsAsync(List<string> allocationIds);

}
