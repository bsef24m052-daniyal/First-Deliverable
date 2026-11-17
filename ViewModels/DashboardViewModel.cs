using FundWise.Models;

namespace FundWise.ViewModels
{
    public class DashboardViewModel
    {
        public ApplicationUser? User { get; set; }

        public MainAccount? MainAccount { get; set; }

        public List<MiniAccount> MiniAccounts { get; set; } = new();

        public List<Transaction> RecentTransactions { get; set; } = new();

        public decimal UnallocatedBalance { get; set; }
    }
}