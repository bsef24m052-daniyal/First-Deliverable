using FundWise.Models;
using FundWise.Repositories;
using FundWise.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FundWise.Controllers
{
    [Authorize]
    public class AccountsController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly IAccountRepository _accounts;
        private readonly ITransactionRepository _transactions;

        public AccountsController(
            UserManager<ApplicationUser> users,
            IAccountRepository accounts,
            ITransactionRepository transactions)
        {
            _users = users;
            _accounts = accounts;
            _transactions = transactions;
        }

        private async Task<MainAccount?> GetMainAsync()
        {
            var user = await _users.GetUserAsync(User);

            return user == null
                ? null
                : await _accounts.GetMainAccountAsync(user.Id);
        }

        public async Task<IActionResult> Index()
        {
            var main = await GetMainAsync();

            if (main == null)
                return NotFound();

            var available =
                await _accounts.GetUnallocatedBalanceAsync(main.Id);

            ViewBag.Unallocated = available;

            if (available < 0)
            {
                ViewBag.AllocationWarning =
                    "Your allocations exceed your available main balance. Adjust your mini-account budgets before accessing them.";
            }

            return View(await _accounts.GetMiniAccountsAsync(main.Id));
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var main = await GetMainAsync();

            if (main == null)
                return NotFound();

            ViewBag.Unallocated =
                await _accounts.GetUnallocatedBalanceAsync(main.Id);

            return View(new MiniAccountViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MiniAccountViewModel model)
        {
            var main = await GetMainAsync();

            if (main == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Unallocated =
                    await _accounts.GetUnallocatedBalanceAsync(main.Id);

                return View(model);
            }

            try
            {
                await _accounts.CreateMiniAccountAsync(
                    main.Id,
                    model.Name,
                    model.Tag,
                    model.Description,
                    model.AssignedBudget);

                TempData["Success"] = "Mini account created.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);

                ViewBag.Unallocated =
                    await _accounts.GetUnallocatedBalanceAsync(main.Id);

                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var main = await GetMainAsync();

            if (main == null)
                return NotFound();

            var minis = await _accounts.GetMiniAccountsAsync(main.Id);
            var mini = minis.FirstOrDefault(x => x.Id == id);

            if (mini == null)
                return NotFound();

            return View(new MiniAccountViewModel
            {
                Id = mini.Id,
                Name = mini.Name,
                Tag = mini.Tag,
                Description = mini.Description,
                AssignedBudget = mini.AssignedBudget
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MiniAccountViewModel model)
        {
            var main = await GetMainAsync();

            if (main == null)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                await _accounts.UpdateMiniAccountAsync(
                    main.Id,
                    model.Id,
                    model.Name,
                    model.Tag,
                    model.Description,
                    model.AssignedBudget);

                TempData["Success"] = "Mini account updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var main = await GetMainAsync();

            if (main == null)
                return NotFound();

            var minis = await _accounts.GetMiniAccountsAsync(main.Id);
            var mini = minis.FirstOrDefault(x => x.Id == id);

            if (mini == null)
                return NotFound();

            if (await _accounts.GetUnallocatedBalanceAsync(main.Id) < 0)
            {
                TempData["Error"] =
                    "Adjust your allocations before accessing mini accounts.";

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Transactions =
                await _transactions.GetTransactionsAsync(
                    main.Id, id, null);

            return View(mini);
        }
    }
}