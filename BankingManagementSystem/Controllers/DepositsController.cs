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

[Authorize(Roles = "Admin,Manager,Staff")]
public class DepositsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly BankingService _banking;
    private readonly UserManager<ApplicationUser> _users;

    public DepositsController(ApplicationDbContext db, BankingService banking, UserManager<ApplicationUser> users)
    {
        _db = db;
        _banking = banking;
        _users = users;
    }

    public async Task<IActionResult> Index(string? accountNumber)
    {
        var query = _db.Deposits
            .Include(d => d.Account).ThenInclude(a => a!.Customer)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(accountNumber))
            query = query.Where(d => d.Account!.AccountNumber.Contains(accountNumber));

        ViewBag.AccountNumber = accountNumber;
        return View(await query.OrderByDescending(d => d.DepositDate).Take(200).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await Populate();
        return View(new MoneyActionViewModel { Method = "Cash" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MoneyActionViewModel model)
    {
        await Populate();
        if (!ModelState.IsValid) return View(model);

        try
        {
            await _banking.DepositAsync(model.AccountId, model.Amount ?? 0m, model.Description, _users.GetUserId(User), model.Method);
            TempData["Success"] = "Deposit recorded.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    private async Task Populate()
    {
        ViewBag.Accounts = await _db.Accounts
            .Where(a => a.Status == RecordStatus.Active)
            .OrderBy(a => a.AccountNumber)
            .Select(a => new SelectListItem(a.AccountNumber + " — " + a.Customer!.FirstName + " " + a.Customer.LastName, a.AccountId.ToString()))
            .ToListAsync();
        ViewBag.Methods = new[] { "Cash", "Cheque", "Transfer", "ATM", "Other" }
            .Select(m => new SelectListItem(m, m))
            .ToList();
    }
}
