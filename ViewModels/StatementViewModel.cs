using FundWise.Models;

namespace FundWise.ViewModels
{
    public class StatementViewModel
    {
        public List<Transaction> Transactions { get; set; } = new();

        public int? MiniAccountId { get; set; }

        public string Period { get; set; } = "week";
    }
}