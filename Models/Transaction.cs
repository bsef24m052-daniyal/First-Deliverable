using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FundWise.Models
{
    public enum TransactionType
    {
        SendMoney,
        ReceiveMoney,
        PayBill,
        TopUp
    }

    public class Transaction
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string TransactionReference { get; set; }
            = Guid.NewGuid().ToString("N");

        [Required]
        public string UserId { get; set; } = "";

        public int MainAccountId { get; set; }

        public int? MiniAccountId { get; set; }

        public TransactionType Type { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public string? Description { get; set; }

        public string? SenderDetails { get; set; }

        public string? RecipientDetails { get; set; }

        public string Status { get; set; } = "Completed";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public string AccountLabel { get; set; } = "Main Account";
    }
}