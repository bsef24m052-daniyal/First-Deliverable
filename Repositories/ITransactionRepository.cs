using FundWise.Models;

namespace FundWise.Repositories
{
    public interface ITransactionRepository
    {
        Task<List<Transaction>> GetTransactionsAsync(
            int mainAccountId,
            int? miniAccountId,
            DateTime? fromDate);
    }
}