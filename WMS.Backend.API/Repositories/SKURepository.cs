using Microsoft.EntityFrameworkCore;
using WMS.Backend.API.Data;
using WMS.Backend.API.Models;
using WMS.Backend.API.Repositories.Interfaces;

namespace WMS.Backend.API.Repositories
{
    public class SKURepository : ISKURepository
    {
        private readonly WMSDbContext _context;

        public SKURepository(WMSDbContext context)
        {
            _context = context;
        }

        public async Task<SKU> GetSKUAsync(string skuId)
        {
            return await _context.SKUs
                .Include(s => s.Allocations)
                .FirstOrDefaultAsync(s => s.SkuId == skuId);
        }

        public async Task<List<SKU>> GetSKUsByProductAsync(string productNumber)
        {
            return await _context.SKUs
                .Where(s => s.ProductNumber == productNumber)
                .ToListAsync();
        }

        public async Task<List<SKU>> GetAvailableSKUsByProductAsync(string productNumber)
        {
            return await _context.SKUs
                .Where(s => s.ProductNumber == productNumber &&
                           !s.IsLocationLocked &&
                           s.AvailableQuantity > 0)
                .OrderBy(s => s.WarehouseLocation)
                .ToListAsync();
        }

        public async Task UpdateSKUAsync(SKU sku)
        {
            _context.SKUs.Update(sku);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateSKUsAsync(List<SKU> skus)
        {
            _context.SKUs.UpdateRange(skus);
            await _context.SaveChangesAsync();
        }
    }
}
