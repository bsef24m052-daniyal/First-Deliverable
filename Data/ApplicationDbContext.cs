using FundWise.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FundWise.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<MainAccount> MainAccounts => Set<MainAccount>();

        public DbSet<MiniAccount> MiniAccounts => Set<MiniAccount>();

        public DbSet<Transaction> Transactions => Set<Transaction>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<MainAccount>()
                .Property(x => x.Balance)
                .HasPrecision(18, 2);

            builder.Entity<MiniAccount>()
                .Property(x => x.AssignedBudget)
                .HasPrecision(18, 2);

            builder.Entity<MiniAccount>()
                .Property(x => x.TotalSpent)
                .HasPrecision(18, 2);

            builder.Entity<Transaction>()
                .Property(x => x.Amount)
                .HasPrecision(18, 2);

            builder.Entity<MainAccount>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Transaction>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Transaction>()
                .HasOne<MainAccount>()
                .WithMany()
                .HasForeignKey(x => x.MainAccountId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Transaction>()
                .HasOne<MiniAccount>()
                .WithMany()
                .HasForeignKey(x => x.MiniAccountId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}