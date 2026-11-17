using Dapper;
using FundWise.Models;
using Microsoft.Data.SqlClient;

namespace FundWise.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly string _connectionString;

        public TransactionRepository(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<List<Transaction>> GetTransactionsAsync(
            int mainAccountId,
            int? miniAccountId,
            DateTime? fromDate)
        {
            using var db = new SqlConnection(_connectionString);

            var rows = await db.QueryAsync<Transaction>(
                """
                SELECT
                    t.*,
                    COALESCE(m.Name, 'Main Account') AS AccountLabel
                FROM Transactions t
                LEFT JOIN MiniAccounts m
                    ON m.Id = t.MiniAccountId
                WHERE t.MainAccountId = @MainAccountId
                  AND (@MiniAccountId IS NULL
                       OR t.MiniAccountId = @MiniAccountId)
                  AND (@FromDate IS NULL
                       OR t.CreatedAt >= @FromDate)
                ORDER BY t.CreatedAt DESC
                """,
                new
                {
                    MainAccountId = mainAccountId,
                    MiniAccountId = miniAccountId,
                    FromDate = fromDate
                });

            return rows.ToList();
        }
    }
}