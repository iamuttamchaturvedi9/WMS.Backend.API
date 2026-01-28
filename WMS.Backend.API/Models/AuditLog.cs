using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WMS.Backend.API.Models
{
    [Table("AuditLog")]
    [Index(nameof(TableName), nameof(RecordId))]
    [Index(nameof(ChangedDate))]
    public class AuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AuditId { get; set; }

        [Required]
        [StringLength(50)]
        public string TableName { get; set; }

        [Required]
        [StringLength(50)]
        public string RecordId { get; set; }

        [Required]
        [StringLength(20)]
        public string Action { get; set; }

        public string OldValue { get; set; }

        public string NewValue { get; set; }

        [StringLength(100)]
        public string ChangedBy { get; set; }

        [Required]
        public DateTime ChangedDate { get; set; }

        [StringLength(500)]
        public string Reason { get; set; }

        public AuditLog()
        {
            ChangedDate = DateTime.UtcNow;
        }
    }
}
