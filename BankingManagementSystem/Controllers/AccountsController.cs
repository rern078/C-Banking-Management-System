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
public class AccountsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly BankingService _banking;
    private readonly UserManager<ApplicationUser> _users;

    public AccountsController(ApplicationDbContext db, BankingService banking, UserManager<ApplicationUser> users)
    {
        _db = db;
        _banking = banking;
        _users = users;
    }

    public async Task<IActionResult> Index(string? status)
    {
        var query = _db.Accounts
            .Include(a => a.Customer)
            .Include(a => a.AccountType)
            .Include(a => a.Branch)
            .AsQueryable();

        if (Enum.TryParse<RecordStatus>(status, true, out var parsed))
            query = query.Where(a => a.Status == parsed);

        ViewBag.Status = status;
        return View(await query.OrderByDescending(a => a.CreatedAt).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateLookups();
        return View(new CreateAccountViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAccountViewModel model)
    {
        await PopulateLookups();
        if (!ModelState.IsValid) return View(model);

        try
        {
            var account = await _banking.CreateAccountAsync(model.CustomerId, model.AccountTypeId, model.BranchId, model.OpeningBalance ?? 0m, _users.GetUserId(User));
            TempData["Success"] = $"Account {account.AccountNumber} opened.";
            return RedirectToAction(nameof(Details), new { id = account.AccountId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        var account = await _db.Accounts
            .Include(a => a.Customer)
            .Include(a => a.AccountType)
            .Include(a => a.Branch)
            .Include(a => a.Cards)
            .FirstOrDefaultAsync(a => a.AccountId == id);
        if (account is null) return NotFound();

        ViewBag.Transactions = await _db.Transactions
            .Where(t => t.AccountId == id)
            .OrderByDescending(t => t.TransactionDate)
            .Take(30)
            .ToListAsync();
        return View(account);
    }

    [HttpGet]
    public async Task<IActionResult> Deposit(int id)
    {
        var account = await _db.Accounts.FindAsync(id);
        if (account is null) return NotFound();
        return View(new MoneyActionViewModel { AccountId = id, AccountNumber = account.AccountNumber });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Deposit(MoneyActionViewModel model)
    {
        var account = await _db.Accounts.FindAsync(model.AccountId);
        if (account is null) return NotFound();
        model.AccountNumber = account.AccountNumber;
        if (!ModelState.IsValid) return View(model);

        try
        {
            await _banking.DepositAsync(model.AccountId, model.Amount ?? 0m, model.Description, _users.GetUserId(User), model.Method);
            TempData["Success"] = "Deposit completed.";
            return RedirectToAction(nameof(Details), new { id = model.AccountId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Withdraw(int id)
    {
        var account = await _db.Accounts.FindAsync(id);
        if (account is null) return NotFound();
        return View(new MoneyActionViewModel { AccountId = id, AccountNumber = account.AccountNumber });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(MoneyActionViewModel model)
    {
        var account = await _db.Accounts.FindAsync(model.AccountId);
        if (account is null) return NotFound();
        model.AccountNumber = account.AccountNumber;
        if (!ModelState.IsValid) return View(model);

        try
        {
            await _banking.WithdrawAsync(model.AccountId, model.Amount ?? 0m, model.Description, _users.GetUserId(User), model.Method);
            TempData["Success"] = "Withdrawal completed.";
            return RedirectToAction(nameof(Details), new { id = model.AccountId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Transfer(int id)
    {
        var account = await _db.Accounts.FindAsync(id);
        if (account is null) return NotFound();
        ViewBag.FromAccount = account;
        return View(new TransferFormViewModel { FromAccountId = id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Transfer(TransferFormViewModel model)
    {
        var account = await _db.Accounts.FindAsync(model.FromAccountId);
        if (account is null) return NotFound();
        ViewBag.FromAccount = account;
        if (!ModelState.IsValid) return View(model);

        try
        {
            await _banking.TransferAsync(model.FromAccountId, model.ToAccountNumber.Trim(), model.Amount ?? 0m, model.TransferFee ?? 0m, model.Description, _users.GetUserId(User));
            TempData["Success"] = "Transfer completed.";
            return RedirectToAction(nameof(Details), new { id = model.FromAccountId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, RecordStatus status)
    {
        try
        {
            await _banking.SetAccountStatusAsync(id, status, _users.GetUserId(User));
            TempData["Success"] = "Account status updated.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateLookups()
    {
        ViewBag.Customers = await _db.Customers.Where(c => c.Status == RecordStatus.Active)
            .OrderBy(c => c.FirstName)
            .Select(c => new SelectListItem($"{c.CustomerCode} — {c.FirstName} {c.LastName}", c.CustomerId.ToString()))
            .ToListAsync();
        ViewBag.AccountTypes = await _db.AccountTypes
            .Select(t => new SelectListItem(t.TypeName, t.AccountTypeId.ToString()))
            .ToListAsync();
        ViewBag.Branches = await _db.Branches.Where(b => b.Status == RecordStatus.Active)
            .Select(b => new SelectListItem($"{b.BranchCode} — {b.BranchName}", b.BranchId.ToString()))
            .ToListAsync();
    }
}

[Authorize(Roles = "Admin,Manager,Staff")]
public class TransactionsController : Controller
{
    private readonly ApplicationDbContext _db;
    public TransactionsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? accountNumber, TxType? type)
    {
        var query = _db.Transactions.Include(t => t.Account).ThenInclude(a => a!.Customer).AsQueryable();
        if (!string.IsNullOrWhiteSpace(accountNumber))
            query = query.Where(t => t.Account!.AccountNumber.Contains(accountNumber));
        if (type.HasValue)
            query = query.Where(t => t.TransactionType == type);

        ViewBag.AccountNumber = accountNumber;
        ViewBag.Type = type;
        return View(await query.OrderByDescending(t => t.TransactionDate).Take(200).ToListAsync());
    }
}

[Authorize(Roles = "Admin,Manager,Staff")]
public class TransfersController : Controller
{
    private readonly ApplicationDbContext _db;
    public TransfersController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.Transfers
            .Include(t => t.FromAccount)
            .Include(t => t.ToAccount)
            .OrderByDescending(t => t.TransferDate)
            .Take(100)
            .ToListAsync());
}
