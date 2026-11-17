using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FundWise.Models
{
    public class MiniAccount
    {
        public int Id { get; set; }

        public int MainAccountId { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = "";

        [MaxLength(30)]
        public string Tag { get; set; } = "";

        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AssignedBudget { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalSpent { get; set; }

        [NotMapped]
        public decimal RemainingBalance =>
            AssignedBudget - TotalSpent;

        public bool IsArchived { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}