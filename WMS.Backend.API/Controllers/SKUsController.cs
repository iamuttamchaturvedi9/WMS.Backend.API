using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WMS.Backend.API.Repositories.Interfaces;
using WMS.Backend.API.Services;

namespace WMS.Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SKUsController : ControllerBase
    {
        private readonly SKUCorrectionService _correctionService;
        private readonly ISKURepository _skuRepository;

        public SKUsController(
            SKUCorrectionService correctionService,
            ISKURepository skuRepository)
        {
            _correctionService = correctionService;
            _skuRepository = skuRepository;
        }

        [HttpGet("{skuId}")]
        public async Task<IActionResult> GetSKU(string skuId)
        {
            var sku = await _skuRepository.GetSKUAsync(skuId);
            if (sku == null)
                return NotFound();

            return Ok(sku);
        }

        [HttpGet("product/{productNumber}")]
        public async Task<IActionResult> GetSKUsByProduct(string productNumber)
        {
            var skus = await _skuRepository.GetAvailableSKUsByProductAsync(productNumber);
            return Ok(skus);
        }

        [HttpPut("{skuId}/correct-quantity")]
        public async Task<IActionResult> CorrectQuantity(string skuId, [FromBody] int newQuantity)
        {
            var result = await _correctionService.CorrectSKUQuantityAsync(skuId, newQuantity);
            return Ok(result);
        }
    }
}
