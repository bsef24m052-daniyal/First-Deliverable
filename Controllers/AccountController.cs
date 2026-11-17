using FundWise.Models;
using FundWise.Repositories;
using FundWise.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
namespace FundWise.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IAccountRepository _accounts;
        private readonly PasswordHasher<ApplicationUser> _hasher = new();

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IAccountRepository accounts)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _accounts = accounts;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email,
                CNIC = model.CNIC,
                AccountNumber = model.AccountNumber,
                MobileNumber = model.MobileNumber,
                PhoneNumber = model.MobileNumber,
                DateOfBirth = model.DateOfBirth,
                SecurityQuestion = model.SecurityQuestion
            };

            user.SecurityAnswerHash =
                _hasher.HashPassword(user, model.SecurityAnswer.Trim());

            var result = await _userManager.CreateAsync(
                user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(model);
            }

            try
            {
                await _accounts.CreateMainAccountAsync(
                    user.Id, user.AccountNumber);
            }
            catch
            {
                await _userManager.DeleteAsync(user);

                ModelState.AddModelError(
                    "", "Your banking account could not be created. Please try again.");

                return View(model);
            }

            TempData["Success"] =
                "Registration successful. Please log in.";

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Username,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
                return RedirectToAction("Index", "Home");

            ModelState.AddModelError(
                "", "Invalid username or password.");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult ForgotPassword()
            => View(new ForgotPasswordViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetSecurityQuestion(
            ForgotPasswordViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Username))
            {
                ModelState.AddModelError(
                    nameof(model.Username), "Enter your username.");

                return View("ForgotPassword", model);
            }

            var user = await _userManager.FindByNameAsync(model.Username);

            if (user == null)
            {
                ModelState.AddModelError(
                    "", "Account not found.");

                return View("ForgotPassword", model);
            }

            model.SecurityQuestion = user.SecurityQuestion;

            return View("ForgotPassword", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ForgotPasswordViewModel model)
        {
            ModelState.Remove(nameof(model.SecurityQuestion));

            if (!ModelState.IsValid)
                return View("ForgotPassword", model);

            var user = await _userManager.FindByNameAsync(model.Username);

            if (user == null)
            {
                ModelState.AddModelError("", "Account not found.");
                return View("ForgotPassword", model);
            }

            var verification = _hasher.VerifyHashedPassword(
                user,
                user.SecurityAnswerHash,
                model.SecurityAnswer.Trim());

            if (verification == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    nameof(model.SecurityAnswer),
                    "Incorrect security answer.");

                model.SecurityQuestion = user.SecurityQuestion;

                return View("ForgotPassword", model);
            }

            var token =
                await _userManager.GeneratePasswordResetTokenAsync(user);

            var result = await _userManager.ResetPasswordAsync(
                user, token, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                model.SecurityQuestion = user.SecurityQuestion;

                return View("ForgotPassword", model);
            }

            TempData["Success"] =
                "Password changed successfully. Please log in.";

            return RedirectToAction(nameof(Login));
        }
    }
}