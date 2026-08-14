using BankingManagementSystem.Data;
using BankingManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BankingManagementSystem.Services;

public class AuditService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;

    public AuditService(ApplicationDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task LogAsync(string? userId, string action, string? tableName, string? recordId, string? description)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            TableName = tableName,
            RecordId = recordId,
            Description = description,
            IpAddress = _http.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}

public class BankingService
{
    private readonly ApplicationDbContext _db;
    private readonly AuditService _audit;

    public BankingService(ApplicationDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<Account> CreateAccountAsync(int customerId, int accountTypeId, int branchId, decimal openingBalance, string? userId)
    {
        if (openingBalance < 0)
            throw new InvalidOperationException("Opening balance cannot be negative.");

        var type = await _db.AccountTypes.FindAsync(accountTypeId)
            ?? throw new InvalidOperationException("Account type not found.");

        if (openingBalance < type.MinimumBalance)
            throw new InvalidOperationException($"Opening balance must be at least {type.MinimumBalance:C}.");

        var account = new Account
        {
            CustomerId = customerId,
            AccountTypeId = accountTypeId,
            BranchId = branchId,
            AccountNumber = await NextAccountNumberAsync(),
            Balance = openingBalance,
            Currency = "USD",
            OpenDate = DateTime.UtcNow,
            Status = RecordStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        if (openingBalance > 0)
        {
            var reference = Ref("OPN");
            _db.Transactions.Add(new Transaction
            {
                AccountId = account.AccountId,
                TransactionType = TxType.Deposit,
                Amount = openingBalance,
                BalanceBefore = 0,
                BalanceAfter = openingBalance,
                Description = "Opening deposit",
                ReferenceNumber = reference,
                TransactionDate = DateTime.UtcNow,
                CreatedBy = userId
            });
            _db.Deposits.Add(new Deposit
            {
                AccountId = account.AccountId,
                Amount = openingBalance,
                BalanceBefore = 0,
                BalanceAfter = openingBalance,
                Method = "Opening",
                ReferenceNumber = reference,
                Description = "Opening deposit",
                DepositDate = DateTime.UtcNow,
                CreatedBy = userId
            });
            await _db.SaveChangesAsync();
        }

        await _audit.LogAsync(userId, "Create", "Accounts", account.AccountId.ToString(), $"Opened {account.AccountNumber}");
        return account;
    }

    public async Task DepositAsync(int accountId, decimal amount, string? description, string? userId, string? method = "Cash")
    {
        if (amount <= 0) throw new InvalidOperationException("Amount must be greater than zero.");

        await using var dbTx = await _db.Database.BeginTransactionAsync();
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountId == accountId)
            ?? throw new InvalidOperationException("Account not found.");
        EnsureActive(account);

        var before = account.Balance;
        account.Balance += amount;
        account.UpdatedAt = DateTime.UtcNow;
        var reference = Ref("DEP");

        _db.Transactions.Add(new Transaction
        {
            AccountId = account.AccountId,
            TransactionType = TxType.Deposit,
            Amount = amount,
            BalanceBefore = before,
            BalanceAfter = account.Balance,
            Description = description ?? "Cash deposit",
            ReferenceNumber = reference,
            TransactionDate = DateTime.UtcNow,
            CreatedBy = userId
        });

        _db.Deposits.Add(new Deposit
        {
            AccountId = account.AccountId,
            Amount = amount,
            BalanceBefore = before,
            BalanceAfter = account.Balance,
            Method = string.IsNullOrWhiteSpace(method) ? "Cash" : method.Trim(),
            ReferenceNumber = reference,
            Description = description ?? "Cash deposit",
            DepositDate = DateTime.UtcNow,
            CreatedBy = userId
        });

        await _db.SaveChangesAsync();
        await dbTx.CommitAsync();
        await _audit.LogAsync(userId, "Deposit", "Deposits", accountId.ToString(), $"Deposit {amount:C}");
    }

    public async Task WithdrawAsync(int accountId, decimal amount, string? description, string? userId, string? method = "Cash")
    {
        if (amount <= 0) throw new InvalidOperationException("Amount must be greater than zero.");

        await using var dbTx = await _db.Database.BeginTransactionAsync();
        var account = await _db.Accounts.Include(a => a.AccountType).FirstOrDefaultAsync(a => a.AccountId == accountId)
            ?? throw new InvalidOperationException("Account not found.");
        EnsureActive(account);

        if (account.Balance < amount)
            throw new InvalidOperationException("Insufficient funds.");

        var remaining = account.Balance - amount;
        if (account.AccountType is not null && remaining < account.AccountType.MinimumBalance)
            throw new InvalidOperationException($"Balance cannot fall below minimum {account.AccountType.MinimumBalance:C}.");

        var before = account.Balance;
        account.Balance = remaining;
        account.UpdatedAt = DateTime.UtcNow;
        var reference = Ref("WDR");

        _db.Transactions.Add(new Transaction
        {
            AccountId = account.AccountId,
            TransactionType = TxType.Withdraw,
            Amount = amount,
            BalanceBefore = before,
            BalanceAfter = account.Balance,
            Description = description ?? "Cash withdrawal",
            ReferenceNumber = reference,
            TransactionDate = DateTime.UtcNow,
            CreatedBy = userId
        });

        _db.Withdrawals.Add(new Withdrawal
        {
            AccountId = account.AccountId,
            Amount = amount,
            BalanceBefore = before,
            BalanceAfter = account.Balance,
            Method = string.IsNullOrWhiteSpace(method) ? "Cash" : method.Trim(),
            ReferenceNumber = reference,
            Description = description ?? "Cash withdrawal",
            WithdrawDate = DateTime.UtcNow,
            CreatedBy = userId
        });

        await _db.SaveChangesAsync();
        await dbTx.CommitAsync();
        await _audit.LogAsync(userId, "Withdraw", "Withdrawals", accountId.ToString(), $"Withdraw {amount:C}");
    }

    public async Task TransferAsync(int fromAccountId, string toAccountNumber, decimal amount, decimal fee, string? description, string? userId)
    {
        if (amount <= 0) throw new InvalidOperationException("Amount must be greater than zero.");
        if (fee < 0) throw new InvalidOperationException("Fee cannot be negative.");

        await using var dbTx = await _db.Database.BeginTransactionAsync();

        var from = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountId == fromAccountId)
            ?? throw new InvalidOperationException("Source account not found.");
        var to = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == toAccountNumber)
            ?? throw new InvalidOperationException("Destination account not found.");

        if (from.AccountId == to.AccountId)
            throw new InvalidOperationException("Cannot transfer to the same account.");

        EnsureActive(from);
        EnsureActive(to);

        var total = amount + fee;
        if (from.Balance < total)
            throw new InvalidOperationException("Insufficient funds.");

        var fromBefore = from.Balance;
        from.Balance -= total;
        from.UpdatedAt = DateTime.UtcNow;

        var toBefore = to.Balance;
        to.Balance += amount;
        to.UpdatedAt = DateTime.UtcNow;

        var reference = Ref("TRF");
        var note = description ?? "Transfer";

        _db.Transactions.Add(new Transaction
        {
            AccountId = from.AccountId,
            TransactionType = TxType.TransferOut,
            Amount = amount,
            BalanceBefore = fromBefore,
            BalanceAfter = from.Balance + fee,
            Description = $"{note} → {to.AccountNumber}",
            ReferenceNumber = reference,
            TransactionDate = DateTime.UtcNow,
            CreatedBy = userId
        });

        if (fee > 0)
        {
            _db.Transactions.Add(new Transaction
            {
                AccountId = from.AccountId,
                TransactionType = TxType.Fee,
                Amount = fee,
                BalanceBefore = from.Balance + fee,
                BalanceAfter = from.Balance,
                Description = "Transfer fee",
                ReferenceNumber = reference,
                TransactionDate = DateTime.UtcNow,
                CreatedBy = userId
            });
        }

        _db.Transactions.Add(new Transaction
        {
            AccountId = to.AccountId,
            TransactionType = TxType.TransferIn,
            Amount = amount,
            BalanceBefore = toBefore,
            BalanceAfter = to.Balance,
            Description = $"{note} ← {from.AccountNumber}",
            ReferenceNumber = reference,
            TransactionDate = DateTime.UtcNow,
            CreatedBy = userId
        });

        _db.Transfers.Add(new Transfer
        {
            FromAccountId = from.AccountId,
            ToAccountId = to.AccountId,
            Amount = amount,
            TransferFee = fee,
            ReferenceNumber = reference,
            Description = note,
            Status = TransferStatus.Completed,
            TransferDate = DateTime.UtcNow,
            CreatedBy = userId
        });

        await _db.SaveChangesAsync();
        await dbTx.CommitAsync();
        await _audit.LogAsync(userId, "Transfer", "Transfers", reference, $"{amount:C} from {from.AccountNumber} to {to.AccountNumber}");
    }

    public async Task SetAccountStatusAsync(int accountId, RecordStatus status, string? userId)
    {
        var account = await _db.Accounts.FindAsync(accountId)
            ?? throw new InvalidOperationException("Account not found.");
        account.Status = status;
        account.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "UpdateStatus", "Accounts", accountId.ToString(), status.ToString());
    }

    private static void EnsureActive(Account account)
    {
        if (account.Status != RecordStatus.Active)
            throw new InvalidOperationException($"Account is {account.Status} and cannot be used.");
    }

    private async Task<string> NextAccountNumberAsync()
    {
        var count = await _db.Accounts.CountAsync();
        return $"BMS{DateTime.UtcNow:yyMMdd}{(count + 1):D5}";
    }

    private static string Ref(string prefix) => $"{prefix}{DateTime.UtcNow:yyMMddHHmmss}{Random.Shared.Next(10, 99)}";
}
