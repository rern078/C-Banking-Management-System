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
public class BeneficiariesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly AuditService _audit;
    private readonly UserManager<ApplicationUser> _users;

    public BeneficiariesController(ApplicationDbContext db, AuditService audit, UserManager<ApplicationUser> users)
    {
        _db = db;
        _audit = audit;
        _users = users;
    }

    public async Task<IActionResult> Index() =>
        View(await _db.Beneficiaries.Include(b => b.Customer).OrderByDescending(b => b.CreatedAt).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await Populate();
        return View(new BeneficiaryFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BeneficiaryFormViewModel model)
    {
        await Populate();
        if (!ModelState.IsValid) return View(model);

        var item = new Beneficiary
        {
            CustomerId = model.CustomerId,
            Nickname = model.Nickname,
            AccountNumber = model.AccountNumber.Trim(),
            BankName = model.BankName,
            Phone = model.Phone,
            Status = RecordStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _db.Beneficiaries.Add(item);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(_users.GetUserId(User), "Create", "Beneficiaries", item.BeneficiaryId.ToString(), item.Nickname);
        TempData["Success"] = "Beneficiary saved.";
        return RedirectToAction(nameof(Index));
    }

    private async Task Populate()
    {
        ViewBag.Customers = await _db.Customers.Where(c => c.Status == RecordStatus.Active)
            .Select(c => new SelectListItem(c.CustomerCode + " — " + c.FirstName + " " + c.LastName, c.CustomerId.ToString()))
            .ToListAsync();
    }
}

[Authorize(Roles = "Admin,Manager,Staff")]
public class ChequesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly AuditService _audit;
    private readonly UserManager<ApplicationUser> _users;

    public ChequesController(ApplicationDbContext db, AuditService audit, UserManager<ApplicationUser> users)
    {
        _db = db;
        _audit = audit;
        _users = users;
    }

    public async Task<IActionResult> Index() =>
        View(await _db.Cheques.Include(c => c.Account).ThenInclude(a => a!.Customer)
            .OrderByDescending(c => c.IssueDate).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await Populate();
        return View(new ChequeFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ChequeFormViewModel model)
    {
        await Populate();
        if (!ModelState.IsValid) return View(model);

        var account = await _db.Accounts.FindAsync(model.AccountId);
        if (account is null)
        {
            ModelState.AddModelError(string.Empty, "Account not found.");
            return View(model);
        }

        var count = await _db.Cheques.CountAsync();
        var cheque = new Cheque
        {
            AccountId = model.AccountId,
            CustomerId = account.CustomerId,
            ChequeNumber = $"CHQ{(count + 1):D6}",
            PayeeName = model.PayeeName,
            Amount = model.Amount ?? 0m,
            IssueDate = DateTime.UtcNow,
            Status = ChequeStatus.Issued,
            Description = model.Description,
            CreatedBy = _users.GetUserId(User)
        };
        _db.Cheques.Add(cheque);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(_users.GetUserId(User), "Create", "Cheques", cheque.ChequeId.ToString(), cheque.ChequeNumber);
        TempData["Success"] = $"Cheque {cheque.ChequeNumber} issued.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, ChequeStatus status)
    {
        var cheque = await _db.Cheques.FindAsync(id);
        if (cheque is null) return NotFound();
        cheque.Status = status;
        if (status == ChequeStatus.Cleared) cheque.ClearedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Cheque status updated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task Populate()
    {
        ViewBag.Accounts = await _db.Accounts.Where(a => a.Status == RecordStatus.Active)
            .Select(a => new SelectListItem(a.AccountNumber, a.AccountId.ToString()))
            .ToListAsync();
    }
}

[Authorize(Roles = "Admin,Manager,Staff")]
public class FixedDepositsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly AuditService _audit;
    private readonly UserManager<ApplicationUser> _users;

    public FixedDepositsController(ApplicationDbContext db, AuditService audit, UserManager<ApplicationUser> users)
    {
        _db = db;
        _audit = audit;
        _users = users;
    }

    public async Task<IActionResult> Index() =>
        View(await _db.FixedDeposits.Include(f => f.Customer).Include(f => f.Account)
            .OrderByDescending(f => f.CreatedAt).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await Populate();
        return View(new FixedDepositFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FixedDepositFormViewModel model)
    {
        await Populate();
        if (!ModelState.IsValid) return View(model);

        var principal = model.PrincipalAmount ?? 0m;
        var interest = principal * (model.InterestRate / 100m) * (model.TermMonths / 12m);
        var count = await _db.FixedDeposits.CountAsync();

        var fd = new FixedDeposit
        {
            CustomerId = model.CustomerId,
            AccountId = model.AccountId,
            CertificateNo = $"FD{DateTime.UtcNow:yyMMdd}{(count + 1):D4}",
            PrincipalAmount = principal,
            InterestRate = model.InterestRate,
            TermMonths = model.TermMonths,
            MaturityAmount = Math.Round(principal + interest, 2),
            StartDate = DateTime.UtcNow,
            MaturityDate = DateTime.UtcNow.AddMonths(model.TermMonths),
            Status = FixedDepositStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _db.FixedDeposits.Add(fd);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(_users.GetUserId(User), "Create", "FixedDeposits", fd.FixedDepositId.ToString(), fd.CertificateNo);
        TempData["Success"] = "Fixed deposit opened.";
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
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public NotificationsController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IActionResult> Index() =>
        View(await _db.Notifications.Include(n => n.User).OrderByDescending(n => n.CreatedAt).Take(200).ToListAsync());

    [HttpGet]
    public IActionResult Create() => View(new NotificationFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NotificationFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        _db.Notifications.Add(new Notification
        {
            Title = model.Title,
            Message = model.Message,
            Category = model.Category,
            UserId = _users.GetUserId(User),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Notification posted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var item = await _db.Notifications.FindAsync(id);
        if (item is null) return NotFound();
        item.IsRead = true;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
