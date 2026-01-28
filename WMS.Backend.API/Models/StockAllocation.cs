using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Backend.API.Models
{
    [Table("StockAllocations")]
    [Index(nameof(OrderId))]
    [Index(nameof(LineItemId))]
    [Index(nameof(SkuId))]
    [Index(nameof(OrderId), nameof(LineItemId))]
    public class StockAllocation
    {
        [Key]
        [StringLength(50)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string AllocationId { get; set; }

        [Required]
        [StringLength(50)]
        public string OrderId { get; set; }

        [Required]
        [StringLength(50)]
        public string LineItemId { get; set; }

        [Required]
        [StringLength(50)]
        public string SkuId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int AllocatedQuantity { get; set; }

        [Required]
        public DateTime AllocationDate { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(OrderId))]
        public virtual CustomerOrder Order { get; set; }

        [ForeignKey(nameof(LineItemId))]
        public virtual OrderLineItem LineItem { get; set; }

        [ForeignKey(nameof(SkuId))]
        public virtual SKU SKU { get; set; }

        public StockAllocation()
        {
            AllocationId = Guid.NewGuid().ToString();
            AllocationDate = DateTime.UtcNow;
        }
    }
}
