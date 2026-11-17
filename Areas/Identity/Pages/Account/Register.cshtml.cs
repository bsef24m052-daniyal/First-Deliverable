using FundWise.Models;
using FundWise.Services;
using FundWise.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace FundWise.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAccountService _accountService;

        public RegisterModel(UserManager<ApplicationUser> userManager,
                             IAccountService accountService)
        {
            _userManager = userManager;
            _accountService = accountService;
        }

        [BindProperty]
        public RegisterViewModel Input { get; set; } = new();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = new ApplicationUser
            {
                UserName = Input.Username,
                Email = Input.Email,
                CNIC = Input.CNIC,
                AccountNumber = Input.AccountNumber,
                MobileNumber = Input.MobileNumber,
                DateOfBirth = Input.DateOfBirth,
                SecurityQuestion = Input.SecurityQuestion,
                // Hash security answer using Identity's password hasher (no user needed for hash)
                SecurityAnswerHash = _userManager.PasswordHasher.HashPassword(null!, Input.SecurityAnswer)
            };

            var result = await _userManager.CreateAsync(user, Input.Password);
            if (result.Succeeded)
            {
                // Create the demo main account with 100,000 PKR
                await _accountService.CreateMainAccountAsync(user.Id);
                return RedirectToPage("./Login");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return Page();
        }
    }
}
