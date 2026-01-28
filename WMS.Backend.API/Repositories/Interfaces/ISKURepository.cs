using WMS.Backend.API.Models;

namespace WMS.Backend.API.Repositories.Interfaces
{
    public interface ISKURepository
    {
        Task<SKU> GetSKUAsync(string skuId);
        Task<List<SKU>> GetSKUsByProductAsync(string productNumber);
        Task<List<SKU>> GetAvailableSKUsByProductAsync(string productNumber);
        Task UpdateSKUAsync(SKU sku);
        Task UpdateSKUsAsync(List<SKU> skus);
    }
}
