using BankingManagementSystem.Data;
using BankingManagementSystem.Models;
using BankingManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankingManagementSystem.Controllers;

[Authorize(Roles = "Admin,Manager,Staff")]
public class BranchesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly AuditService _audit;

    public BranchesController(ApplicationDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IActionResult> Index() =>
        View(await _db.Branches.OrderBy(b => b.BranchCode).ToListAsync());

    [HttpGet]
    public IActionResult Create() => View(new Branch());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Branch model)
    {
        if (!ModelState.IsValid) return View(model);
        model.CreatedAt = DateTime.UtcNow;
        _db.Branches.Add(model);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, "Create", "Branches", model.BranchId.ToString(), model.BranchName);
        TempData["Success"] = "Branch created.";
        return RedirectToAction(nameof(Index));
    }
}

[Authorize(Roles = "Admin,Manager,Staff")]
public class CustomersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly AuditService _audit;

    public CustomersController(ApplicationDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.Customers.Include(c => c.Branch).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(c =>
                c.CustomerCode.Contains(q) ||
                c.FirstName.Contains(q) ||
                c.LastName.Contains(q) ||
                (c.Phone != null && c.Phone.Contains(q)));
        }

        ViewBag.Q = q;
        return View(await query.OrderByDescending(c => c.CreatedAt).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Branches = await _db.Branches.Where(b => b.Status == RecordStatus.Active).ToListAsync();
        return View(new ViewModels.CustomerFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ViewModels.CustomerFormViewModel model)
    {
        ViewBag.Branches = await _db.Branches.Where(b => b.Status == RecordStatus.Active).ToListAsync();
        if (!ModelState.IsValid) return View(model);

        var count = await _db.Customers.CountAsync();
        var customer = new Customer
        {
            CustomerCode = $"CUS{(count + 1):D4}",
            BranchId = model.BranchId,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Gender = model.Gender,
            DateOfBirth = model.DateOfBirth,
            NationalId = model.NationalId,
            Phone = model.Phone,
            Email = model.Email,
            Address = model.Address,
            Status = model.Status,
            CreatedAt = DateTime.UtcNow
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, "Create", "Customers", customer.CustomerId.ToString(), customer.FullName);
        TempData["Success"] = "Customer created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var customer = await _db.Customers.Include(c => c.Branch)
            .Include(c => c.Accounts).ThenInclude(a => a.AccountType)
            .Include(c => c.Loans)
            .FirstOrDefaultAsync(c => c.CustomerId == id);
        return customer is null ? NotFound() : View(customer);
    }
}

[Authorize(Roles = "Admin,Manager,Staff")]
public class AccountTypesController : Controller
{
    private readonly ApplicationDbContext _db;

    public AccountTypesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.AccountTypes.OrderBy(t => t.TypeName).ToListAsync());

    [HttpGet]
    public IActionResult Create() => View(new AccountTypeEntity());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccountTypeEntity model)
    {
        if (!ModelState.IsValid) return View(model);
        model.CreatedAt = DateTime.UtcNow;
        _db.AccountTypes.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Account type created.";
        return RedirectToAction(nameof(Index));
    }
}
