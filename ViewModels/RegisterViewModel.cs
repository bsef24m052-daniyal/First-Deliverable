using System.ComponentModel.DataAnnotations;

namespace FundWise.ViewModels
{
    public class RegisterViewModel
    {
        [Required, MaxLength(15)]
        public string CNIC { get; set; } = "";

        [Required, MaxLength(20)]
        public string AccountNumber { get; set; } = "";

        [Required, Phone]
        public string MobileNumber { get; set; } = "";

        [Required, DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required, Compare(nameof(Password))]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = "";

        [Required]
        public string SecurityQuestion { get; set; } = "";

        [Required]
        public string SecurityAnswer { get; set; } = "";
    }
}