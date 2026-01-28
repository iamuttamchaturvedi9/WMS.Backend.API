using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using WMS.Backend.API.Enums;

namespace WMS.Backend.API.Models
{
    [Table("CustomerOrders")]
    [Index(nameof(Status))]
    [Index(nameof(Priority), nameof(OrderDate))]
    public class CustomerOrder
    {
        [Key]
        [StringLength(50)]
        public string OrderId { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        [Required]
        public OrderPriority Priority { get; set; }

        [Required]
        public bool CompleteDeliveryRequired { get; set; }

        [Required]
        [StringLength(30)]
        public OrderStatus Status { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }

        [Required]
        public DateTime LastModifiedDate { get; set; }

        // Navigation Properties
        public virtual ICollection<OrderLineItem> LineItems { get; set; }
        public virtual ICollection<StockAllocation> Allocations { get; set; }

        public CustomerOrder()
        {
            LineItems = new List<OrderLineItem>();
            Allocations = new List<StockAllocation>();
            Status = OrderStatus.Received;
            OrderDate = DateTime.UtcNow;
            CreatedDate = DateTime.UtcNow;
            LastModifiedDate = DateTime.UtcNow;
        }

        public bool IsFullyAllocated()
        {
            return LineItems != null && LineItems.All(li => li.Status == LineItemStatus.Allocated);
        }

        public bool IsPartiallyAllocated()
        {
            return LineItems != null && LineItems.Any(li => li.Status == LineItemStatus.Allocated ||
                                      li.Status == LineItemStatus.PartiallyAllocated);
        }
    }
}
