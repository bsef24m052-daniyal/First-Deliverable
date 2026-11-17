using Dapper;
using FundWise.Models;
using Microsoft.Data.SqlClient;

namespace FundWise.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly string _connectionString;

        public AccountRepository(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")!;
        }

        private SqlConnection Connection()
            => new SqlConnection(_connectionString);

        public async Task CreateMainAccountAsync(
            string userId,
            string accountNumber)
        {
            using var db = Connection();

            const string sql = """
                INSERT INTO MainAccounts
                    (UserId, AccountNumber, Balance, CreatedAt)
                VALUES
                    (@UserId, @AccountNumber, 100000, GETUTCDATE());
                """;

            await db.ExecuteAsync(sql, new
            {
                UserId = userId,
                AccountNumber = accountNumber
            });
        }

        public async Task<MainAccount?> GetMainAccountAsync(string userId)
        {
            using var db = Connection();

            return await db.QueryFirstOrDefaultAsync<MainAccount>(
                """
                SELECT TOP 1 *
                FROM MainAccounts
                WHERE UserId = @UserId
                """,
                new { UserId = userId });
        }

        public async Task<List<MiniAccount>> GetMiniAccountsAsync(
            int mainAccountId)
        {
            using var db = Connection();

            var result = await db.QueryAsync<MiniAccount>(
                """
                SELECT *
                FROM MiniAccounts
                WHERE MainAccountId = @MainAccountId
                  AND IsArchived = 0
                ORDER BY CreatedAt DESC
                """,
                new { MainAccountId = mainAccountId });

            return result.ToList();
        }

        public async Task<decimal> GetUnallocatedBalanceAsync(
            int mainAccountId)
        {
            using var db = Connection();

            return await db.QuerySingleAsync<decimal>(
                """
                SELECT
                    m.Balance -
                    COALESCE(
                        SUM(
                            CASE
                                WHEN a.AssignedBudget > a.TotalSpent
                                THEN a.AssignedBudget - a.TotalSpent
                                ELSE 0
                            END
                        ), 0
                    )
                FROM MainAccounts m
                LEFT JOIN MiniAccounts a
                    ON a.MainAccountId = m.Id
                    AND a.IsArchived = 0
                WHERE m.Id = @Id
                GROUP BY m.Balance
                """,
                new { Id = mainAccountId });
        }

        public async Task CreateMiniAccountAsync(
            int mainAccountId,
            string name,
            string tag,
            string? description,
            decimal budget)
        {
            if (budget <= 0)
                throw new InvalidOperationException(
                    "Budget must be greater than zero.");

            using var db = Connection();
            await db.OpenAsync();

            using var tx = db.BeginTransaction(
                System.Data.IsolationLevel.Serializable);

            try
            {
                var unallocated = await db.ExecuteScalarAsync<decimal>(
                    """
                    SELECT
                        m.Balance - COALESCE(SUM(
                            CASE
                                WHEN a.AssignedBudget > a.TotalSpent
                                THEN a.AssignedBudget - a.TotalSpent
                                ELSE 0
                            END
                        ), 0)
                    FROM MainAccounts m
                    LEFT JOIN MiniAccounts a
                        ON a.MainAccountId = m.Id
                        AND a.IsArchived = 0
                    WHERE m.Id = @Id
                    GROUP BY m.Balance
                    """,
                    new { Id = mainAccountId }, tx);

                if (budget > unallocated)
                    throw new InvalidOperationException(
                        $"Insufficient unallocated balance. Available: PKR {unallocated:N2}");

                await db.ExecuteAsync(
                    """
                    INSERT INTO MiniAccounts
                    (MainAccountId, Name, Tag, Description,
                     AssignedBudget, TotalSpent, IsArchived,
                     CreatedAt)
                    VALUES
                    (@MainAccountId, @Name, @Tag, @Description,
                     @Budget, 0, 0, GETUTCDATE())
                    """,
                    new
                    {
                        MainAccountId = mainAccountId,
                        Name = name,
                        Tag = tag,
                        Description = description,
                        Budget = budget
                    }, tx);

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task UpdateMiniAccountAsync(
            int mainAccountId,
            int miniAccountId,
            string name,
            string tag,
            string? description,
            decimal budget)
        {
            if (budget <= 0)
                throw new InvalidOperationException(
                    "Budget must be greater than zero.");

            using var db = Connection();
            await db.OpenAsync();

            using var tx = db.BeginTransaction(
                System.Data.IsolationLevel.Serializable);

            try
            {
                var mini = await db.QuerySingleOrDefaultAsync<MiniAccount>(
                    """
                    SELECT *
                    FROM MiniAccounts
                    WHERE Id = @MiniId
                      AND MainAccountId = @MainId
                      AND IsArchived = 0
                    """,
                    new { MiniId = miniAccountId, MainId = mainAccountId },
                    tx);

                if (mini == null)
                    throw new InvalidOperationException(
                        "Mini account not found.");

                if (budget < mini.TotalSpent)
                    throw new InvalidOperationException(
                        "New budget cannot be less than the amount already spent.");

                var unallocated = await db.ExecuteScalarAsync<decimal>(
                    """
                    SELECT
                        m.Balance - COALESCE(SUM(
                            CASE
                                WHEN a.AssignedBudget > a.TotalSpent
                                THEN a.AssignedBudget - a.TotalSpent
                                ELSE 0
                            END
                        ), 0)
                    FROM MainAccounts m
                    LEFT JOIN MiniAccounts a
                        ON a.MainAccountId = m.Id
                        AND a.IsArchived = 0
                    WHERE m.Id = @Id
                    GROUP BY m.Balance
                    """,
                    new { Id = mainAccountId }, tx);

                var oldRemaining =
                    Math.Max(0, mini.AssignedBudget - mini.TotalSpent);

                var requiredAdditional =
                    budget - mini.TotalSpent - oldRemaining;

                if (requiredAdditional > unallocated)
                    throw new InvalidOperationException(
                        $"Insufficient unallocated balance. Available: PKR {unallocated:N2}");

                await db.ExecuteAsync(
                    """
                    UPDATE MiniAccounts
                    SET Name = @Name,
                        Tag = @Tag,
                        Description = @Description,
                        AssignedBudget = @Budget,
                        UpdatedAt = GETUTCDATE()
                    WHERE Id = @MiniId
                      AND MainAccountId = @MainId
                    """,
                    new
                    {
                        Name = name,
                        Tag = tag,
                        Description = description,
                        Budget = budget,
                        MiniId = miniAccountId,
                        MainId = mainAccountId
                    }, tx);

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task SendMoneyAsync(
            string userId,
            int mainAccountId,
            int? miniAccountId,
            decimal amount,
            string recipient,
            bool confirmOverspending)
        {
            if (amount <= 0)
                throw new InvalidOperationException(
                    "Enter an amount greater than zero.");

            using var db = Connection();
            await db.OpenAsync();

            using var tx = db.BeginTransaction(
                System.Data.IsolationLevel.Serializable);

            try
            {
                var main = await db.QuerySingleOrDefaultAsync<MainAccount>(
                    """
                    SELECT *
                    FROM MainAccounts WITH (UPDLOCK, HOLDLOCK)
                    WHERE Id = @MainId AND UserId = @UserId
                    """,
                    new { MainId = mainAccountId, UserId = userId }, tx);

                if (main == null)
                    throw new InvalidOperationException(
                        "Main account not found.");

                var unallocated = await db.ExecuteScalarAsync<decimal>(
                    """
                    SELECT
                        m.Balance - COALESCE(SUM(
                            CASE
                                WHEN a.AssignedBudget > a.TotalSpent
                                THEN a.AssignedBudget - a.TotalSpent
                                ELSE 0
                            END
                        ), 0)
                    FROM MainAccounts m
                    LEFT JOIN MiniAccounts a
                        ON a.MainAccountId = m.Id
                        AND a.IsArchived = 0
                    WHERE m.Id = @Id
                    GROUP BY m.Balance
                    """,
                    new { Id = mainAccountId }, tx);

                if (amount > main.Balance)
                    throw new InvalidOperationException(
                        "Insufficient main account balance.");
                if (miniAccountId.HasValue && unallocated < 0)
                {
                    throw new InvalidOperationException(
                        "Your mini-account allocations exceed your main balance. Adjust your budgets first.");
                }

                decimal extra = 0;

                if (miniAccountId.HasValue)
                {
                    var mini = await db.QuerySingleOrDefaultAsync<MiniAccount>(
                        """
                        SELECT *
                        FROM MiniAccounts WITH (UPDLOCK, HOLDLOCK)
                        WHERE Id = @MiniId
                          AND MainAccountId = @MainId
                          AND IsArchived = 0
                        """,
                        new
                        {
                            MiniId = miniAccountId,
                            MainId = mainAccountId
                        }, tx);

                    if (mini == null)
                        throw new InvalidOperationException(
                            "Mini account not found.");

                    var remaining =
                        mini.AssignedBudget - mini.TotalSpent;

                    extra = Math.Max(0, amount - remaining);

                    if (extra > 0 && !confirmOverspending)
                        throw new InvalidOperationException(
                            $"OVERSpending:{extra:N2}");

                    if (extra > unallocated)
                        throw new InvalidOperationException(
                            "Insufficient unallocated balance for overspending.");

                    await db.ExecuteAsync(
                        """
                        UPDATE MiniAccounts
                        SET TotalSpent = TotalSpent + @Amount,
                            UpdatedAt = GETUTCDATE()
                        WHERE Id = @MiniId
                        """,
                        new { Amount = amount, MiniId = miniAccountId }, tx);
                }
                else
                {
                    if (amount > unallocated)
                        throw new InvalidOperationException(
                            "You can only spend unallocated money from the main account.");
                }

                await db.ExecuteAsync(
                    """
                    UPDATE MainAccounts
                    SET Balance = Balance - @Amount
                    WHERE Id = @MainId AND UserId = @UserId
                    """,
                    new
                    {
                        Amount = amount,
                        MainId = mainAccountId,
                        UserId = userId
                    }, tx);

                await db.ExecuteAsync(
                    """
                    INSERT INTO Transactions
                    (TransactionReference, UserId, MainAccountId,
                     MiniAccountId, Type, Amount, Description,
                     RecipientDetails, Status, CreatedAt)
                    VALUES
                    (@Reference, @UserId, @MainId, @MiniId,
                     @Type, @Amount, @Description,
                     @Recipient, 'Completed', GETUTCDATE())
                    """,
                    new
                    {
                        Reference = Guid.NewGuid().ToString("N"),
                        UserId = userId,
                        MainId = mainAccountId,
                        MiniId = miniAccountId,
                        Type = (int)TransactionType.SendMoney,
                        Amount = amount,
                        Description = "Money sent",
                        Recipient = recipient
                    }, tx);

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task ReceiveMoneyAsync(
            string userId,
            int mainAccountId,
            decimal amount,
            string sender)
        {
            if (amount <= 0)
                throw new InvalidOperationException(
                    "Enter an amount greater than zero.");

            using var db = Connection();
            await db.OpenAsync();

            using var tx = db.BeginTransaction(
                System.Data.IsolationLevel.Serializable);

            try
            {
                var updated = await db.ExecuteAsync(
                    """
                    UPDATE MainAccounts
                    SET Balance = Balance + @Amount
                    WHERE Id = @MainId AND UserId = @UserId
                    """,
                    new
                    {
                        Amount = amount,
                        MainId = mainAccountId,
                        UserId = userId
                    }, tx);

                if (updated == 0)
                    throw new InvalidOperationException(
                        "Main account not found.");

                await db.ExecuteAsync(
                    """
                    INSERT INTO Transactions
                    (TransactionReference, UserId, MainAccountId,
                     MiniAccountId, Type, Amount, Description,
                     SenderDetails, Status, CreatedAt)
                    VALUES
                    (@Reference, @UserId, @MainId, NULL,
                     @Type, @Amount, 'Money received',
                     @Sender, 'Completed', GETUTCDATE())
                    """,
                    new
                    {
                        Reference = Guid.NewGuid().ToString("N"),
                        UserId = userId,
                        MainId = mainAccountId,
                        Type = (int)TransactionType.ReceiveMoney,
                        Amount = amount,
                        Sender = sender
                    }, tx);

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }
}