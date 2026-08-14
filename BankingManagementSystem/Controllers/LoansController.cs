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
public class LoansController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly AuditService _audit;
    private readonly UserManager<ApplicationUser> _users;

    public LoansController(ApplicationDbContext db, AuditService audit, UserManager<ApplicationUser> users)
    {
        _db = db;
        _audit = audit;
        _users = users;
    }

    public async Task<IActionResult> Index() =>
        View(await _db.Loans.Include(l => l.Customer).OrderByDescending(l => l.CreatedAt).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await Populate();
        return View(new LoanFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LoanFormViewModel model)
    {
        await Populate();
        if (!ModelState.IsValid) return View(model);

        var principal = model.PrincipalAmount ?? 0m;
        var monthlyRate = model.InterestRate / 100m / 12m;
        var payment = monthlyRate == 0
            ? principal / model.TermMonths
            : principal * monthlyRate * (decimal)Math.Pow((double)(1 + monthlyRate), model.TermMonths)
              / ((decimal)Math.Pow((double)(1 + monthlyRate), model.TermMonths) - 1);

        var loan = new Loan
        {
            CustomerId = model.CustomerId,
            AccountId = model.AccountId,
            LoanType = model.LoanType,
            PrincipalAmount = principal,
            InterestRate = model.InterestRate,
            TermMonths = model.TermMonths,
            MonthlyPayment = Math.Round(payment, 2),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(model.TermMonths),
            RemainingAmount = principal,
            Status = LoanStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _db.Loans.Add(loan);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(_users.GetUserId(User), "Create", "Loans", loan.LoanId.ToString(), $"{loan.LoanType} {loan.PrincipalAmount:C}");
        TempData["Success"] = "Loan created.";
        return RedirectToAction(nameof(Index));
    }

    private async Task Populate()
    {
        ViewBag.Customers = await _db.Customers.Where(c => c.Status == RecordStatus.Active)
            .Select(c => new SelectListItem(c.CustomerCode + " — " + c.FirstName + " " + c.LastName, c.CustomerId.ToString()))
            .ToListAsync();
        ViewBag.Accounts = await _db.Accounts.Where(a => a.Status == RecordStatus.Active)
            .Select(a => new SelectListItem(a.AccountNumber, a.AccountId.ToString()))
            .ToListAsync();
    }
}

[Authorize(Roles = "Admin,Manager,Staff")]
public class CardsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly AuditService _audit;
    private readonly UserManager<ApplicationUser> _users;

    public CardsController(ApplicationDbContext db, AuditService audit, UserManager<ApplicationUser> users)
    {
        _db = db;
        _audit = audit;
        _users = users;
    }

    public async Task<IActionResult> Index() =>
        View(await _db.Cards.Include(c => c.Account).ThenInclude(a => a!.Customer)
            .OrderByDescending(c => c.IssueDate).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Accounts = await _db.Accounts.Where(a => a.Status == RecordStatus.Active)
            .Select(a => new SelectListItem(a.AccountNumber, a.AccountId.ToString()))
            .ToListAsync();
        return View(new CardFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CardFormViewModel model)
    {
        ViewBag.Accounts = await _db.Accounts.Where(a => a.Status == RecordStatus.Active)
            .Select(a => new SelectListItem(a.AccountNumber, a.AccountId.ToString()))
            .ToListAsync();
        if (!ModelState.IsValid) return View(model);

        var number = $"4{Random.Shared.NextInt64(100_000_000_000_000, 999_999_999_999_999)}";
        var card = new Card
        {
            AccountId = model.AccountId,
            CardNumber = number[..16],
            CardType = model.CardType,
            IssueDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(model.YearsValid),
            Status = CardStatus.Active
        };
        _db.Cards.Add(card);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(_users.GetUserId(User), "Create", "Cards", card.CardId.ToString(), card.CardNumber);
        TempData["Success"] = "Card issued.";
        return RedirectToAction(nameof(Index));
    }
}

[Authorize(Roles = "Admin,Manager")]
public class AuditLogsController : Controller
{
    private readonly ApplicationDbContext _db;
    public AuditLogsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.AuditLogs.Include(a => a.User)
            .OrderByDescending(a => a.CreatedAt).Take(200).ToListAsync());
}
