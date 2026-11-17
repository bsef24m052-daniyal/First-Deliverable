using FundWise.Data;
using FundWise.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace FundWise.Services
{
    public class AccountService : IAccountService
    {
        private readonly ApplicationDbContext _db;
        private const decimal InitialBalance = 100_000m; // PKR 100,000

        public AccountService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task CreateMainAccountAsync(string userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) throw new ArgumentException("User not found.", nameof(userId));

            // Generate a simple demo account number
            var accountNumber = $"AC{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
            user.AccountNumber = accountNumber;

            var main = new MainAccount
            {
                UserId = userId,
                AccountNumber = accountNumber,
                Balance = InitialBalance,
                CreatedAt = DateTime.UtcNow
            };

            await using var tx = await _db.Database.BeginTransactionAsync();
            _db.MainAccounts.Add(main);
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
    }
}
