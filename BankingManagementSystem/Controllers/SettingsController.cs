using BankingManagementSystem.Data;
using BankingManagementSystem.Models;
using BankingManagementSystem.Services;
using BankingManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankingManagementSystem.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class SettingsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly AuditService _audit;
    private readonly UserManager<ApplicationUser> _users;

    public SettingsController(ApplicationDbContext db, AuditService audit, UserManager<ApplicationUser> users)
    {
        _db = db;
        _audit = audit;
        _users = users;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _db.Settings.OrderBy(s => s.GroupName).ThenBy(s => s.SettingKey).ToListAsync();
        var vm = new SettingsPageViewModel
        {
            Items = items.Select(s => new SettingItemViewModel
            {
                SettingId = s.SettingId,
                SettingKey = s.SettingKey,
                SettingValue = s.SettingValue,
                GroupName = s.GroupName,
                Description = s.Description
            }).ToList()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SettingsPageViewModel model)
    {
        if (model.Items.Count == 0)
            return RedirectToAction(nameof(Index));

        var ids = model.Items.Select(i => i.SettingId).ToList();
        var existing = await _db.Settings.Where(s => ids.Contains(s.SettingId)).ToListAsync();
        var userId = _users.GetUserId(User);

        foreach (var item in model.Items)
        {
            var row = existing.FirstOrDefault(s => s.SettingId == item.SettingId);
            if (row is null) continue;

            var isToggle = string.Equals(row.GroupName, "Dashboard", StringComparison.OrdinalIgnoreCase)
                || string.Equals(row.SettingValue, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(row.SettingValue, "false", StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.SettingValue, "true", StringComparison.OrdinalIgnoreCase);

            if (isToggle)
            {
                row.SettingValue = string.Equals(item.SettingValue, "true", StringComparison.OrdinalIgnoreCase)
                    ? "true"
                    : "false";
            }
            else
            {
                row.SettingValue = item.SettingValue?.Trim() ?? string.Empty;
            }

            row.UpdatedAt = DateTime.UtcNow;
            row.UpdatedBy = userId;
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "Update", "Settings", null, "Updated bank settings");
        TempData["Success"] = "Settings saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Create() => View(new SettingCreateViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SettingCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (await _db.Settings.AnyAsync(s => s.SettingKey == model.SettingKey))
        {
            ModelState.AddModelError(nameof(model.SettingKey), "This key already exists.");
            return View(model);
        }

        _db.Settings.Add(new Setting
        {
            SettingKey = model.SettingKey.Trim(),
            SettingValue = model.SettingValue.Trim(),
            GroupName = model.GroupName.Trim(),
            Description = model.Description,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = _users.GetUserId(User)
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Setting added.";
        return RedirectToAction(nameof(Index));
    }
}
