using FundWise.Models;
using FundWise.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FundWise.Controllers
{
    [Authorize]
    public class TransactionsController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly IAccountRepository _accounts;
        private readonly ITransactionRepository _transactions;

        public TransactionsController(
            UserManager<ApplicationUser> users,
            IAccountRepository accounts,
            ITransactionRepository transactions)
        {
            _users = users;
            _accounts = accounts;
            _transactions = transactions;
        }

        private async Task<(string UserId, MainAccount? Main)> GetAccount()
        {
            var user = await _users.GetUserAsync(User);

            if (user == null)
                return ("", null);

            return (user.Id,
                await _accounts.GetMainAccountAsync(user.Id));
        }

        [HttpGet]
        public async Task<IActionResult> Send(int? miniAccountId)
        {
            var (_, main) = await GetAccount();

            if (main == null)
                return NotFound();

            ViewBag.MiniAccounts =
                await _accounts.GetMiniAccountsAsync(main.Id);

            ViewBag.SelectedMiniAccountId = miniAccountId;

            ViewBag.Unallocated =
                await _accounts.GetUnallocatedBalanceAsync(main.Id);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(
            decimal amount,
            string recipient,
            int? miniAccountId,
            bool confirmOverspending = false)
        {
            var (userId, main) = await GetAccount();

            if (main == null)
                return NotFound();

            try
            {
                await _accounts.SendMoneyAsync(
                    userId,
                    main.Id,
                    miniAccountId,
                    amount,
                    recipient,
                    confirmOverspending);

                return Json(new
                {
                    success = true,
                    message = "Money sent successfully."
                });
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.StartsWith("OVERSpending:"))
                {
                    var extra = ex.Message.Split(':')[1];

                    return Json(new
                    {
                        success = false,
                        confirmRequired = true,
                        extraAmount = extra,
                        message =
                            $"This payment exceeds the mini-account budget by PKR {extra}. Use unallocated funds?"
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        public IActionResult Receive() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Receive(
            decimal amount,
            string sender)
        {
            var (userId, main) = await GetAccount();

            if (main == null)
                return NotFound();

            try
            {
                await _accounts.ReceiveMoneyAsync(
                    userId, main.Id, amount, sender);

                return Json(new
                {
                    success = true,
                    message = "Money received successfully."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Statement(
            int? miniAccountId,
            string period = "week")
        {
            var (_, main) = await GetAccount();

            if (main == null)
                return NotFound();

            var from = GetFromDate(period);

            var transactions =
                await _transactions.GetTransactionsAsync(
                    main.Id, miniAccountId, from);

            var model = new ViewModels.StatementViewModel
            {
                Transactions = transactions,
                MiniAccountId = miniAccountId,
                Period = period
            };

            ViewBag.MiniAccounts =
                await _accounts.GetMiniAccountsAsync(main.Id);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Filter(
            int? miniAccountId,
            string period = "week")
        {
            var (_, main) = await GetAccount();

            if (main == null)
                return NotFound();

            var transactions =
                await _transactions.GetTransactionsAsync(
                    main.Id, miniAccountId, GetFromDate(period));

            return PartialView("_TransactionRows", transactions);
        }

        private static DateTime? GetFromDate(string period)
        {
            var now = DateTime.UtcNow;

            return period.ToLowerInvariant() switch
            {
                "week" => now.AddDays(-7),
                "month" => now.AddMonths(-1),
                "year" => now.AddYears(-1),
                "all" => null,
                _ => now.AddDays(-7)
            };
        }

        // Demonstration-only pages; no bill or card transactions are saved.
        public IActionResult PayBill() => View();

        public IActionResult TopUp() => View();

        public IActionResult Payments() => View();

        public IActionResult Cards() => View();
    }
}