using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WMS.Backend.API.Repositories.Interfaces;
using WMS.Backend.API.Services;

namespace WMS.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {

        private readonly StockAllocationService _allocationService;
        private readonly OrderCancellationService _cancellationService;
        private readonly IOrderRepository _orderRepository;

        public OrderController(
            StockAllocationService allocationService,
            OrderCancellationService cancellationService,
            IOrderRepository orderRepository)
        {
            _allocationService = allocationService;
            _cancellationService = cancellationService;
            _orderRepository = orderRepository;
        }

        [HttpPost("process-allocations")]
        public async Task<IActionResult> ProcessAllocations()
        {
            await _allocationService.ProcessOrderAllocationsAsync();
            return Ok(new { message = "Allocations processed successfully" });
        }

        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrder(string orderId)
        {
            var order = await _orderRepository.GetOrderAsync(orderId);
            if (order == null)
                return NotFound();

            return Ok(order);
        }

        [HttpPost("{orderId}/allocate")]
        public async Task<IActionResult> AllocateOrder(string orderId)
        {
            var order = await _orderRepository.GetOrderAsync(orderId);
            if (order == null)
                return NotFound();

            var result = await _allocationService.AllocateStockForOrderAsync(order);
            return Ok(result);
        }

        [HttpDelete("{orderId}")]
        public async Task<IActionResult> CancelOrder(string orderId)
        {
            var result = await _cancellationService.CancelOrderAsync(orderId);
            return Ok(result);
        }

        [HttpDelete("{orderId}/line-items/{lineItemId}")]
        public async Task<IActionResult> CancelLineItem(string orderId, string lineItemId)
        {
            var result = await _cancellationService.CancelLineItemAsync(orderId, lineItemId);
            return Ok(result);
        }

    }
}
