using FundWise.Models;

namespace FundWise.ViewModels
{
    public class ProfileViewModel
    {
        public ApplicationUser? User { get; set; }

        public MainAccount? MainAccount { get; set; }
    }
}