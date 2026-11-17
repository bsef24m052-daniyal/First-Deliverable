using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FundWise.Models
{
    public class MainAccount
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = "";

        [Required, MaxLength(20)]
        public string AccountNumber { get; set; } = "";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}