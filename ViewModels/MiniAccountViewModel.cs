using System.ComponentModel.DataAnnotations;

namespace FundWise.ViewModels
{
    public class MiniAccountViewModel
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = "";

        [MaxLength(30)]
        public string Tag { get; set; } = "";

        public string? Description { get; set; }

        [Range(typeof(decimal), "0.01", "999999999")]
        public decimal AssignedBudget { get; set; }
    }
}