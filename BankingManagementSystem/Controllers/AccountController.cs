using BankingManagementSystem.Data;
using BankingManagementSystem.Models;
using BankingManagementSystem.Services;
using BankingManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BankingManagementSystem.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly AuditService _audit;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        AuditService audit)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _db = db;
        _audit = audit;
    }

    private async Task PopulateBranches()
    {
        ViewBag.Branches = await _db.Branches
            .Where(b => b.Status == RecordStatus.Active)
            .OrderBy(b => b.BranchName)
            .Select(b => new SelectListItem($"{b.BranchCode} — {b.BranchName}", b.BranchId.ToString()))
            .ToListAsync();
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email)
            ?? await _userManager.FindByNameAsync(model.Email);

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        if (user.Status != RecordStatus.Active)
        {
            var statusMessage = user.Status switch
            {
                RecordStatus.Inactive => "Your account is inactive. Contact an administrator.",
                RecordStatus.Frozen => "Your account is frozen. Contact an administrator.",
                RecordStatus.Closed => "Your account is closed. Contact an administrator.",
                _ => "Your account cannot sign in. Contact an administrator."
            };
            ModelState.AddModelError(string.Empty, statusMessage);
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        await PopulateBranches();
        return View(new RegisterViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        await PopulateBranches();
        if (!ModelState.IsValid) return View(model);

        var userName = model.UserName.Trim();
        var email = model.Email.Trim();

        if (await _userManager.FindByNameAsync(userName) is not null)
        {
            ModelState.AddModelError(nameof(model.UserName), "This username is already taken.");
            return View(model);
        }

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            FullName = model.FullName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim(),
            BranchId = model.BranchId,
            Status = RecordStatus.Inactive
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, "Staff");
        await _audit.LogAsync(user.Id, "Register", "AspNetUsers", user.Id,
            $"Access requested by {user.UserName} (pending activation)");

        TempData["AuthSuccess"] = "Request received. An administrator must activate your account before you can sign in.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();
}
