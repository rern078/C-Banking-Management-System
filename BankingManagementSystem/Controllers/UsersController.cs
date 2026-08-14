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

[Authorize(Roles = "Admin,Manager")]
public class UsersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly AuditService _audit;

    public UsersController(ApplicationDbContext db, UserManager<ApplicationUser> users, AuditService audit)
    {
        _db = db;
        _users = users;
        _audit = audit;
    }

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.Users.Include(u => u.Branch).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(u =>
                u.FullName.Contains(q) ||
                (u.Email != null && u.Email.Contains(q)) ||
                (u.UserName != null && u.UserName.Contains(q)));
        }

        var users = await query.OrderBy(u => u.FullName).ToListAsync();
        var items = new List<UserListItemViewModel>();
        foreach (var user in users)
        {
            var roles = await _users.GetRolesAsync(user);
            items.Add(new UserListItemViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                UserName = user.UserName ?? "",
                Email = user.Email ?? user.UserName ?? "",
                Phone = user.PhoneNumber,
                BranchName = user.Branch?.BranchName,
                Roles = string.Join(", ", roles),
                Status = user.Status,
                CreatedAt = user.CreatedAt
            });
        }

        ViewBag.Q = q;
        return View(items);
    }

    public async Task<IActionResult> Details(string id)
    {
        var user = await _db.Users.Include(u => u.Branch).FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        var roles = await _users.GetRolesAsync(user);
        var logs = await _db.AuditLogs
            .Where(a => a.UserId == id)
            .OrderByDescending(a => a.CreatedAt)
            .Take(20)
            .ToListAsync();

        var vm = new UserDetailViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            UserName = user.UserName ?? "",
            Email = user.Email ?? "",
            Phone = user.PhoneNumber,
            BranchName = user.Branch?.BranchName,
            BranchCode = user.Branch?.BranchCode,
            Status = user.Status,
            EmailConfirmed = user.EmailConfirmed,
            Roles = roles.ToList(),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            RecentAuditLogs = logs
        };

        return View(vm);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        var roles = await _users.GetRolesAsync(user);
        await PopulateLookups();
        return View(new UserEditViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            UserName = user.UserName ?? "",
            Email = user.Email ?? "",
            Phone = user.PhoneNumber,
            BranchId = user.BranchId,
            Role = roles.FirstOrDefault() ?? "Staff",
            Status = user.Status
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
        await PopulateLookups();
        if (!ModelState.IsValid) return View(model);

        var user = await _users.FindByIdAsync(model.Id);
        if (user is null) return NotFound();

        var userName = model.UserName.Trim();
        var email = model.Email.Trim();

        var takenName = await _users.FindByNameAsync(userName);
        if (takenName is not null && takenName.Id != user.Id)
        {
            ModelState.AddModelError(nameof(model.UserName), "This username is already in use.");
            return View(model);
        }

        var takenEmail = await _users.FindByEmailAsync(email);
        if (takenEmail is not null && takenEmail.Id != user.Id)
        {
            ModelState.AddModelError(nameof(model.Email), "This email is already in use.");
            return View(model);
        }

        if (!string.Equals(user.UserName, userName, StringComparison.Ordinal))
        {
            var nameResult = await _users.SetUserNameAsync(user, userName);
            if (!nameResult.Succeeded)
            {
                foreach (var error in nameResult.Errors)
                    ModelState.AddModelError(nameof(model.UserName), error.Description);
                return View(model);
            }
        }

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            var emailResult = await _users.SetEmailAsync(user, email);
            if (!emailResult.Succeeded)
            {
                foreach (var error in emailResult.Errors)
                    ModelState.AddModelError(nameof(model.Email), error.Description);
                return View(model);
            }
        }

        user.FullName = model.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
        user.BranchId = model.BranchId;
        user.Status = model.Status;
        user.UpdatedAt = DateTime.UtcNow;

        var updateResult = await _users.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        var currentRoles = await _users.GetRolesAsync(user);
        var nextRole = string.IsNullOrWhiteSpace(model.Role) ? "Staff" : model.Role.Trim();
        if (!currentRoles.Contains(nextRole) || currentRoles.Count != 1)
        {
            if (currentRoles.Count > 0)
                await _users.RemoveFromRolesAsync(user, currentRoles);
            await _users.AddToRoleAsync(user, nextRole);
        }

        await _audit.LogAsync(_users.GetUserId(User), "Update", "AspNetUsers", user.Id, $"Updated {user.UserName}");
        TempData["Success"] = "User updated.";
        return RedirectToAction(nameof(Details), new { id = user.Id });
    }

    private async Task PopulateLookups()
    {
        ViewBag.Branches = await _db.Branches
            .Where(b => b.Status == RecordStatus.Active)
            .OrderBy(b => b.BranchName)
            .Select(b => new SelectListItem($"{b.BranchCode} — {b.BranchName}", b.BranchId.ToString()))
            .ToListAsync();
        ViewBag.Roles = new[] { "Admin", "Manager", "Staff" }
            .Select(r => new SelectListItem(r, r))
            .ToList();
    }
}
