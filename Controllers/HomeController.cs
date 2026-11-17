using FundWise.Models;
using FundWise.Repositories;
using FundWise.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FundWise.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly IAccountRepository _accounts;
        private readonly ITransactionRepository _transactions;

        public HomeController(
            UserManager<ApplicationUser> users,
            IAccountRepository accounts,
            ITransactionRepository transactions)
        {
            _users = users;
            _accounts = accounts;
            _transactions = transactions;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _users.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var main = await _accounts.GetMainAccountAsync(user.Id);

            if (main == null)
                return RedirectToAction("Profile");

            var minis = await _accounts.GetMiniAccountsAsync(main.Id);

            var recent = await _transactions.GetTransactionsAsync(
                main.Id, null, null);

            var vm = new DashboardViewModel
            {
                User = user,
                MainAccount = main,
                MiniAccounts = minis,
                RecentTransactions = recent.Take(5).ToList(),
                UnallocatedBalance =
                    await _accounts.GetUnallocatedBalanceAsync(main.Id)
            };

            return View(vm);
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _users.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var main = await _accounts.GetMainAccountAsync(user.Id);

            return View(new ProfileViewModel
            {
                User = user,
                MainAccount = main
            });
        }

        [AllowAnonymous]
        public IActionResult Privacy() => View();
    }
}