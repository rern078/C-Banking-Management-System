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
        var vm = new DashboardViewModel
        {
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
            RecentDeposits = await _db.Deposits
                .Include(d => d.Account).ThenInclude(a => a!.Customer)
                .OrderByDescending(d => d.DepositDate)
                .Take(6)
                .ToListAsync(),
            RecentWithdrawals = await _db.Withdrawals
                .Include(w => w.Account).ThenInclude(a => a!.Customer)
                .OrderByDescending(w => w.WithdrawDate)
                .Take(6)
                .ToListAsync(),
            RecentTransactions = await _db.Transactions
                .Include(t => t.Account).ThenInclude(a => a!.Customer)
                .OrderByDescending(t => t.TransactionDate)
                .Take(8)
                .ToListAsync(),
            RecentCustomers = await _db.Customers
                .Include(c => c.Branch)
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .ToListAsync()
        };
        return View(vm);
    }
}
