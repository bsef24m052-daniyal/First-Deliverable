using System.ComponentModel.DataAnnotations;

namespace FundWise.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        public string Username { get; set; } = "";

        public string? SecurityQuestion { get; set; }

        [Required]
        public string SecurityAnswer { get; set; } = "";

        [Required, DataType(DataType.Password)]
        public string NewPassword { get; set; } = "";

        [Required, Compare(nameof(NewPassword))]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = "";
    }
}