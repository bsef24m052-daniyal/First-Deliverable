using FundWise.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FundWise.Areas.Identity.Pages.Account
{
    public class ForgotCredentialsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ForgotCredentialsModel(UserManager<ApplicationUser> userManager,
                                      SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public ForgotCredentialsInput Input { get; set; } = new();

        public bool ShowQuestion { get; set; } = false;
        public string SecurityQuestion { get; set; } = string.Empty;
        private ApplicationUser? _targetUser;

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            // First step: just the username, fetch security question
            if (!ShowQuestion)
            {
                _targetUser = await _userManager.FindByNameAsync(Input.Username);
                if (_targetUser == null)
                {
                    ModelState.AddModelError(string.Empty, "User not found.");
                    return Page();
                }
                // Use null‑forgiving operators because we validated existence
                SecurityQuestion = _targetUser!.SecurityQuestion;
                ShowQuestion = true;
                // Keep the username for the next post (non‑null guarantee)
                Input.Username = _targetUser!.UserName;
                return Page();
            }
            else
            {
                // Verify security answer and reset password
                _targetUser = await _userManager.FindByNameAsync(Input.Username);
                if (_targetUser == null)
                {
                    ModelState.AddModelError(string.Empty, "User not found.");
                    return Page();
                }
                var verification = _userManager.PasswordHasher
                    .VerifyHashedPassword(_targetUser, _targetUser.SecurityAnswerHash, Input.SecurityAnswer!);
                if (verification != PasswordVerificationResult.Success)
                {
                    ModelState.AddModelError(string.Empty, "Security answer is incorrect.");
                    SecurityQuestion = _targetUser.SecurityQuestion;
                    ShowQuestion = true;
                    return Page();
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(_targetUser);
                var resetResult = await _userManager.ResetPasswordAsync(_targetUser, token, Input.NewPassword);
                if (resetResult.Succeeded)
                {
                    // After resetting, redirect to login page
                    return RedirectToPage("./Login");
                }

                foreach (var err in resetResult.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);

                SecurityQuestion = _targetUser.SecurityQuestion;
                ShowQuestion = true;
                return Page();
            }
        }
    }

    public class ForgotCredentialsInput
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        public string? SecurityAnswer { get; set; }

        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string? ConfirmPassword { get; set; }
    }
}
