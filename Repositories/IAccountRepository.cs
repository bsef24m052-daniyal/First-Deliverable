using FundWise.Models;

namespace FundWise.Repositories
{
    public interface IAccountRepository
    {
        Task CreateMainAccountAsync(string userId, string accountNumber);

        Task<MainAccount?> GetMainAccountAsync(string userId);

        Task<List<MiniAccount>> GetMiniAccountsAsync(int mainAccountId);

        Task<decimal> GetUnallocatedBalanceAsync(int mainAccountId);

        Task CreateMiniAccountAsync(
            int mainAccountId,
            string name,
            string tag,
            string? description,
            decimal budget);

        Task UpdateMiniAccountAsync(
            int mainAccountId,
            int miniAccountId,
            string name,
            string tag,
            string? description,
            decimal budget);

        Task SendMoneyAsync(
            string userId,
            int mainAccountId,
            int? miniAccountId,
            decimal amount,
            string recipient,
            bool confirmOverspending);

        Task ReceiveMoneyAsync(
            string userId,
            int mainAccountId,
            decimal amount,
            string sender);
    }
}