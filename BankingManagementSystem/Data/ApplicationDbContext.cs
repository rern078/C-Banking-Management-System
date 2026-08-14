using BankingManagementSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BankingManagementSystem.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<AccountTypeEntity> AccountTypes => Set<AccountTypeEntity>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Deposit> Deposits => Set<Deposit>();
    public DbSet<Withdrawal> Withdrawals => Set<Withdrawal>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanPayment> LoanPayments => Set<LoanPayment>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Beneficiary> Beneficiaries => Set<Beneficiary>();
    public DbSet<Cheque> Cheques => Set<Cheque>();
    public DbSet<FixedDeposit> FixedDeposits => Set<FixedDeposit>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Setting> Settings => Set<Setting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Branch>(e =>
        {
            e.HasKey(x => x.BranchId);
            e.HasIndex(x => x.BranchCode).IsUnique();
        });

        builder.Entity<Customer>(e =>
        {
            e.HasKey(x => x.CustomerId);
            e.HasIndex(x => x.CustomerCode).IsUnique();
            e.HasOne(x => x.Branch).WithMany(b => b.Customers).HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AccountTypeEntity>(e =>
        {
            e.HasKey(x => x.AccountTypeId);
            e.Property(x => x.MinimumBalance).HasPrecision(18, 2);
            e.Property(x => x.InterestRate).HasPrecision(9, 4);
        });

        builder.Entity<Account>(e =>
        {
            e.HasKey(x => x.AccountId);
            e.HasIndex(x => x.AccountNumber).IsUnique();
            e.Property(x => x.Balance).HasPrecision(18, 2);
            e.HasOne(x => x.Customer).WithMany(c => c.Accounts).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AccountType).WithMany(t => t.Accounts).HasForeignKey(x => x.AccountTypeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Branch).WithMany(b => b.Accounts).HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Transaction>(e =>
        {
            e.HasKey(x => x.TransactionId);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.BalanceBefore).HasPrecision(18, 2);
            e.Property(x => x.BalanceAfter).HasPrecision(18, 2);
            e.HasOne(x => x.Account).WithMany(a => a.Transactions).HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Deposit>(e =>
        {
            e.ToTable("Deposits");
            e.HasKey(x => x.DepositId);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.BalanceBefore).HasPrecision(18, 2);
            e.Property(x => x.BalanceAfter).HasPrecision(18, 2);
            e.HasOne(x => x.Account).WithMany(a => a.Deposits).HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Withdrawal>(e =>
        {
            e.ToTable("Withdrawals");
            e.HasKey(x => x.WithdrawalId);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.BalanceBefore).HasPrecision(18, 2);
            e.Property(x => x.BalanceAfter).HasPrecision(18, 2);
            e.HasOne(x => x.Account).WithMany(a => a.Withdrawals).HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Transfer>(e =>
        {
            e.HasKey(x => x.TransferId);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.TransferFee).HasPrecision(18, 2);
            e.HasOne(x => x.FromAccount).WithMany().HasForeignKey(x => x.FromAccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ToAccount).WithMany().HasForeignKey(x => x.ToAccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Loan>(e =>
        {
            e.HasKey(x => x.LoanId);
            e.Property(x => x.PrincipalAmount).HasPrecision(18, 2);
            e.Property(x => x.InterestRate).HasPrecision(9, 4);
            e.Property(x => x.MonthlyPayment).HasPrecision(18, 2);
            e.Property(x => x.RemainingAmount).HasPrecision(18, 2);
            e.HasOne(x => x.Customer).WithMany(c => c.Loans).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Account).WithMany(a => a.Loans).HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<LoanPayment>(e =>
        {
            e.HasKey(x => x.PaymentId);
            e.Property(x => x.PaymentAmount).HasPrecision(18, 2);
            e.Property(x => x.PrincipalAmount).HasPrecision(18, 2);
            e.Property(x => x.InterestAmount).HasPrecision(18, 2);
            e.Property(x => x.RemainingAmount).HasPrecision(18, 2);
            e.HasOne(x => x.Loan).WithMany(l => l.Payments).HasForeignKey(x => x.LoanId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Card>(e =>
        {
            e.HasKey(x => x.CardId);
            e.HasIndex(x => x.CardNumber).IsUnique();
            e.HasOne(x => x.Account).WithMany(a => a.Cards).HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.LogId);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Beneficiary>(e =>
        {
            e.HasKey(x => x.BeneficiaryId);
            e.HasOne(x => x.Customer).WithMany(c => c.Beneficiaries).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Cheque>(e =>
        {
            e.HasKey(x => x.ChequeId);
            e.HasIndex(x => x.ChequeNumber).IsUnique();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.HasOne(x => x.Account).WithMany(a => a.Cheques).HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Customer).WithMany(c => c.Cheques).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<FixedDeposit>(e =>
        {
            e.HasKey(x => x.FixedDepositId);
            e.HasIndex(x => x.CertificateNo).IsUnique();
            e.Property(x => x.PrincipalAmount).HasPrecision(18, 2);
            e.Property(x => x.InterestRate).HasPrecision(9, 4);
            e.Property(x => x.MaturityAmount).HasPrecision(18, 2);
            e.HasOne(x => x.Customer).WithMany(c => c.FixedDeposits).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Account).WithMany(a => a.FixedDeposits).HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Notification>(e =>
        {
            e.HasKey(x => x.NotificationId);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Setting>(e =>
        {
            e.ToTable("Settings");
            e.HasKey(x => x.SettingId);
            e.HasIndex(x => x.SettingKey).IsUnique();
        });

        builder.Entity<ApplicationUser>(e =>
        {
            e.HasOne(x => x.Branch).WithMany(b => b.Users).HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}
