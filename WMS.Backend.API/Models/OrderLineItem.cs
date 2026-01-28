using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using WMS.Backend.API.Enums;

namespace WMS.Backend.API.Models
{
    [Table("OrderLineItems")]
    [Index(nameof(OrderId))]
    [Index(nameof(ProductNumber))]
    [Index(nameof(Status))]
    public class OrderLineItem
    {
        [Key]
        [StringLength(50)]
        public string LineItemId { get; set; }

        [Required]
        [StringLength(50)]
        public string OrderId { get; set; }

        [Required]
        [StringLength(50)]
        public string ProductNumber { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int RequestedQuantity { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int AllocatedQuantity { get; set; }

        [Required]
        [StringLength(30)]
        public LineItemStatus Status { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }

        [Required]
        public DateTime LastModifiedDate { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(OrderId))]
        public virtual CustomerOrder Order { get; set; }

        public virtual ICollection<StockAllocation> Allocations { get; set; }

        public OrderLineItem()
        {
            Allocations = new List<StockAllocation>();
            Status = LineItemStatus.Pending;
            CreatedDate = DateTime.UtcNow;
            LastModifiedDate = DateTime.UtcNow;
        }

        [NotMapped]
        public int RemainingQuantity => RequestedQuantity - AllocatedQuantity;
    }
}
