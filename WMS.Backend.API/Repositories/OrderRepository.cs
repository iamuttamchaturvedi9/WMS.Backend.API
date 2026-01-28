using Microsoft.EntityFrameworkCore;
using WMS.Backend.API.Data;
using WMS.Backend.API.Enums;
using WMS.Backend.API.Models;
using WMS.Backend.API.Repositories.Interfaces;

namespace WMS.Backend.API.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly WMSDbContext _context;

        public OrderRepository(WMSDbContext context)
        {
            _context = context;
        }

        public async Task<CustomerOrder> GetOrderAsync(string orderId)
        {
            return await _context.CustomerOrders
                .Include(o => o.LineItems)
                    .ThenInclude(li => li.Allocations)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<List<CustomerOrder>> GetReleasedOrdersAsync()
        {
            return await _context.CustomerOrders
                .Include(o => o.LineItems)
                .Where(o => o.Status == OrderStatus.Received || o.Status == OrderStatus.NotAllocated)
                .ToListAsync();
        }

        public async Task UpdateOrderAsync(CustomerOrder order)
        {
            _context.CustomerOrders.Update(order);
            await _context.SaveChangesAsync();
        }

        public async Task SaveOrderAsync(CustomerOrder order)
        {
            await _context.CustomerOrders.AddAsync(order);
            await _context.SaveChangesAsync();
        }
    }
}
