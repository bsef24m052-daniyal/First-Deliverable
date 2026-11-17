using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace FundWise.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required, MaxLength(15)]
        public string CNIC { get; set; } = "";

        [Required, MaxLength(20)]
        public string AccountNumber { get; set; } = "";

        [Required, Phone]
        public string MobileNumber { get; set; } = "";

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required, MaxLength(100)]
        public string SecurityQuestion { get; set; } = "";

        [Required]
        public string SecurityAnswerHash { get; set; } = "";
    }
}