using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WMS.Backend.API.Models
{
    [Table("SKUs")]
    [Index(nameof(ProductNumber))]
    [Index(nameof(WarehouseLocation))]
    [Index(nameof(ProductNumber), nameof(AvailableQuantity), nameof(IsLocationLocked))]
    public class SKU
    {
        [Key]
        [StringLength(50)]
        public string SkuId { get; set; }

        [Required]
        [StringLength(50)]
        public string ProductNumber { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int TotalQuantity { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int AvailableQuantity { get; set; }

        [Required]
        [StringLength(50)]
        public string WarehouseLocation { get; set; }

        [Required]
        public bool IsLocationLocked { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }

        [Required]
        public DateTime LastModifiedDate { get; set; }

        // Navigation Properties
        public virtual ICollection<StockAllocation> Allocations { get; set; }

        public SKU()
        {
            Allocations = new List<StockAllocation>();
            CreatedDate = DateTime.UtcNow;
            LastModifiedDate = DateTime.UtcNow;
        }

        [NotMapped]
        public int AllocatedQuantity => TotalQuantity - AvailableQuantity;

        public bool CanAllocate(int quantity)
        {
            return !IsLocationLocked && AvailableQuantity >= quantity;
        }
    }
}
