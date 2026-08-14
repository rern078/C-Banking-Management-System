using BankingManagementSystem.Data;
using BankingManagementSystem.Models;
using BankingManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankingManagementSystem.Controllers;

[Authorize(Roles = "Admin,Manager,Staff")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;

    public DashboardController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var today = DateTime.UtcNow.Date;
        var flags = await LoadDashboardFlagsAsync();

        var vm = new DashboardViewModel
        {
            ShowStats = flags.GetValueOrDefault("DashShowStats", true),
            ShowCashToday = flags.GetValueOrDefault("DashShowCashToday", true),
            ShowAttention = flags.GetValueOrDefault("DashShowAttention", true),
            ShowGraphingCalculator = flags.GetValueOrDefault("DashShowGraphingCalculator", true),
            ShowRecentDeposits = flags.GetValueOrDefault("DashShowRecentDeposits", true),
            ShowRecentWithdrawals = flags.GetValueOrDefault("DashShowRecentWithdrawals", true),
            ShowRecentCustomers = flags.GetValueOrDefault("DashShowRecentCustomers", true),
            ShowRecentLedger = flags.GetValueOrDefault("DashShowRecentLedger", true),

            TotalBranches = await _db.Branches.CountAsync(),
            TotalCustomers = await _db.Customers.CountAsync(),
            TotalAccounts = await _db.Accounts.CountAsync(),
            ActiveAccounts = await _db.Accounts.CountAsync(a => a.Status == RecordStatus.Active),
            FrozenAccounts = await _db.Accounts.CountAsync(a => a.Status == RecordStatus.Frozen),
            TotalBalances = await _db.Accounts.SumAsync(a => (decimal?)a.Balance) ?? 0m,
            TransactionsToday = await _db.Transactions.CountAsync(t => t.TransactionDate >= today),
            ActiveLoans = await _db.Loans.CountAsync(l => l.Status == LoanStatus.Active),
            PendingLoans = await _db.Loans.CountAsync(l => l.Status == LoanStatus.Pending),
            LoanBook = await _db.Loans.Where(l => l.Status == LoanStatus.Active).SumAsync(l => (decimal?)l.RemainingAmount) ?? 0m,
            ActiveCards = await _db.Cards.CountAsync(c => c.Status == CardStatus.Active),
            OpenCheques = await _db.Cheques.CountAsync(c => c.Status == ChequeStatus.Issued),
            ActiveFixedDeposits = await _db.FixedDeposits.CountAsync(f => f.Status == FixedDepositStatus.Active),
            UnreadNotifications = await _db.Notifications.CountAsync(n => !n.IsRead),
            DepositsToday = await _db.Deposits.CountAsync(d => d.DepositDate >= today),
            DepositAmountToday = await _db.Deposits.Where(d => d.DepositDate >= today).SumAsync(d => (decimal?)d.Amount) ?? 0m,
            WithdrawalsToday = await _db.Withdrawals.CountAsync(w => w.WithdrawDate >= today),
            WithdrawAmountToday = await _db.Withdrawals.Where(w => w.WithdrawDate >= today).SumAsync(w => (decimal?)w.Amount) ?? 0m,
            TransfersToday = await _db.Transfers.CountAsync(t => t.TransferDate >= today),
            TransferAmountToday = await _db.Transfers.Where(t => t.TransferDate >= today).SumAsync(t => (decimal?)t.Amount) ?? 0m,
            RecentDeposits = flags.GetValueOrDefault("DashShowRecentDeposits", true)
                ? await _db.Deposits
                    .Include(d => d.Account).ThenInclude(a => a!.Customer)
                    .OrderByDescending(d => d.DepositDate)
                    .Take(6)
                    .ToListAsync()
                : new(),
            RecentWithdrawals = flags.GetValueOrDefault("DashShowRecentWithdrawals", true)
                ? await _db.Withdrawals
                    .Include(w => w.Account).ThenInclude(a => a!.Customer)
                    .OrderByDescending(w => w.WithdrawDate)
                    .Take(6)
                    .ToListAsync()
                : new(),
            RecentTransactions = flags.GetValueOrDefault("DashShowRecentLedger", true)
                ? await _db.Transactions
                    .Include(t => t.Account).ThenInclude(a => a!.Customer)
                    .OrderByDescending(t => t.TransactionDate)
                    .Take(8)
                    .ToListAsync()
                : new(),
            RecentCustomers = flags.GetValueOrDefault("DashShowRecentCustomers", true)
                ? await _db.Customers
                    .Include(c => c.Branch)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(5)
                    .ToListAsync()
                : new()
        };
        return View(vm);
    }

    private async Task<Dictionary<string, bool>> LoadDashboardFlagsAsync()
    {
        var rows = await _db.Settings
            .Where(s => s.GroupName == "Dashboard")
            .Select(s => new { s.SettingKey, s.SettingValue })
            .ToListAsync();

        return rows.ToDictionary(
            r => r.SettingKey,
            r => string.Equals(r.SettingValue, "true", StringComparison.OrdinalIgnoreCase)
                 || r.SettingValue == "1"
                 || string.Equals(r.SettingValue, "yes", StringComparison.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);
    }
}
