using WMS.Backend.API.Models;
namespace WMS.Backend.API.Repositories.Interfaces;

public interface IOrderRepository
{
    Task<CustomerOrder> GetOrderAsync(string orderId);
    Task<List<CustomerOrder>> GetReleasedOrdersAsync();
    Task UpdateOrderAsync(CustomerOrder order);
    Task SaveOrderAsync(CustomerOrder order);
}

