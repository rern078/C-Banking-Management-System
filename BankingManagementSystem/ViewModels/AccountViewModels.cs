using System.ComponentModel.DataAnnotations;
using BankingManagementSystem.Models;

namespace BankingManagementSystem.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Enter your email or username.")]
    [Display(Name = "Email or username")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(60)]
    [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "Use letters, numbers, dots, dashes, or underscores only.")]
    public string UserName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(30)]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [Display(Name = "Branch")]
    public int? BranchId { get; set; }

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class DashboardViewModel
{
    public int TotalBranches { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalAccounts { get; set; }
    public int ActiveAccounts { get; set; }
    public int FrozenAccounts { get; set; }
    public decimal TotalBalances { get; set; }
    public int TransactionsToday { get; set; }
    public int ActiveLoans { get; set; }
    public int PendingLoans { get; set; }
    public decimal LoanBook { get; set; }
    public int ActiveCards { get; set; }
    public int OpenCheques { get; set; }
    public int ActiveFixedDeposits { get; set; }
    public int UnreadNotifications { get; set; }

    public int DepositsToday { get; set; }
    public decimal DepositAmountToday { get; set; }
    public int WithdrawalsToday { get; set; }
    public decimal WithdrawAmountToday { get; set; }
    public int TransfersToday { get; set; }
    public decimal TransferAmountToday { get; set; }
    public decimal NetCashToday => DepositAmountToday - WithdrawAmountToday;

    public List<Transaction> RecentTransactions { get; set; } = new();
    public List<Customer> RecentCustomers { get; set; } = new();
    public List<Deposit> RecentDeposits { get; set; } = new();
    public List<Withdrawal> RecentWithdrawals { get; set; } = new();

    public bool ShowStats { get; set; } = true;
    public bool ShowCashToday { get; set; } = true;
    public bool ShowAttention { get; set; } = true;
    public bool ShowGraphingCalculator { get; set; } = true;
    public bool ShowRecentDeposits { get; set; } = true;
    public bool ShowRecentWithdrawals { get; set; } = true;
    public bool ShowRecentCustomers { get; set; } = true;
    public bool ShowRecentLedger { get; set; } = true;
}

public class CustomerFormViewModel
{
    public int? CustomerId { get; set; }

    [Required]
    public int BranchId { get; set; }

    [Required, StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    public Gender Gender { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(50)]
    public string? NationalId { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [EmailAddress, StringLength(100)]
    public string? Email { get; set; }

    [StringLength(250)]
    public string? Address { get; set; }

    public RecordStatus Status { get; set; } = RecordStatus.Active;
}

public class CreateAccountViewModel
{
    [Required]
    public int CustomerId { get; set; }

    [Required]
    public int AccountTypeId { get; set; }

    [Required]
    public int BranchId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? OpeningBalance { get; set; }
}

public class MoneyActionViewModel
{
    [Required]
    public int AccountId { get; set; }
    public string? AccountNumber { get; set; }

    [Required, Range(0.01, double.MaxValue)]
    public decimal? Amount { get; set; }

    [StringLength(30)]
    public string Method { get; set; } = "Cash";

    [StringLength(250)]
    public string? Description { get; set; }
}

public class TransferFormViewModel
{
    [Required]
    public int FromAccountId { get; set; }

    [Required, Display(Name = "To account number")]
    public string ToAccountNumber { get; set; } = string.Empty;

    [Required, Range(0.01, double.MaxValue)]
    public decimal? Amount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? TransferFee { get; set; }

    [StringLength(250)]
    public string? Description { get; set; }
}

public class LoanFormViewModel
{
    [Required]
    public int CustomerId { get; set; }

    public int? AccountId { get; set; }

    [Required, StringLength(50)]
    public string LoanType { get; set; } = "Personal";

    [Required, Range(1, double.MaxValue)]
    public decimal? PrincipalAmount { get; set; }

    [Range(0, 100)]
    public decimal InterestRate { get; set; } = 12;

    [Range(1, 360)]
    public int TermMonths { get; set; } = 12;
}

public class CardFormViewModel
{
    [Required]
    public int AccountId { get; set; }

    public CardType CardType { get; set; } = CardType.Debit;

    [Range(1, 10)]
    public int YearsValid { get; set; } = 3;
}

public class BeneficiaryFormViewModel
{
    [Required]
    public int CustomerId { get; set; }

    [Required, StringLength(100)]
    public string Nickname { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string AccountNumber { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string BankName { get; set; } = "Chamrern Bank";

    [StringLength(30)]
    public string? Phone { get; set; }
}

public class ChequeFormViewModel
{
    [Required]
    public int AccountId { get; set; }

    [Required, StringLength(100)]
    public string PayeeName { get; set; } = string.Empty;

    [Required, Range(0.01, double.MaxValue)]
    public decimal? Amount { get; set; }

    [StringLength(250)]
    public string? Description { get; set; }
}

public class FixedDepositFormViewModel
{
    [Required]
    public int CustomerId { get; set; }

    [Required]
    public int AccountId { get; set; }

    [Required, Range(1, double.MaxValue)]
    public decimal? PrincipalAmount { get; set; }

    [Range(0, 100)]
    public decimal InterestRate { get; set; } = 5;

    [Range(1, 120)]
    public int TermMonths { get; set; } = 12;
}

public class NotificationFormViewModel
{
    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Message { get; set; } = string.Empty;

    [StringLength(40)]
    public string Category { get; set; } = "General";
}

public class SettingItemViewModel
{
    public int SettingId { get; set; }
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
    public string GroupName { get; set; } = "General";
    public string? Description { get; set; }

    public bool IsToggle =>
        string.Equals(GroupName, "Dashboard", StringComparison.OrdinalIgnoreCase)
        || string.Equals(SettingValue, "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(SettingValue, "false", StringComparison.OrdinalIgnoreCase);

    public bool IsEnabled =>
        string.Equals(SettingValue, "true", StringComparison.OrdinalIgnoreCase)
        || SettingValue == "1"
        || string.Equals(SettingValue, "yes", StringComparison.OrdinalIgnoreCase);

    public string DisplayName => SettingKey switch
    {
        "DashShowStats" => "Summary stats",
        "DashShowCashToday" => "Cash today",
        "DashShowAttention" => "Attention chips",
        "DashShowGraphingCalculator" => "Graphing calculator",
        "DashShowRecentDeposits" => "Recent deposits",
        "DashShowRecentWithdrawals" => "Recent withdrawals",
        "DashShowRecentCustomers" => "Recent customers",
        "DashShowRecentLedger" => "Recent ledger",
        _ => SettingKey
    };
}

public class SettingsPageViewModel
{
    public List<SettingItemViewModel> Items { get; set; } = new();
}

public class SettingCreateViewModel
{
    [Required, StringLength(80)]
    public string SettingKey { get; set; } = string.Empty;

    [Required, StringLength(250)]
    public string SettingValue { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string GroupName { get; set; } = "General";

    [StringLength(250)]
    public string? Description { get; set; }
}

public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? BranchName { get; set; }
    public string Roles { get; set; } = string.Empty;
    public RecordStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserDetailViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? BranchName { get; set; }
    public string? BranchCode { get; set; }
    public RecordStatus Status { get; set; }
    public bool EmailConfirmed { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<AuditLog> RecentAuditLogs { get; set; } = new();
}

public class UserEditViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(256)]
    [Display(Name = "Username")]
    public string UserName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Phone { get; set; }

    public int? BranchId { get; set; }

    [Required]
    public string Role { get; set; } = "Staff";

    public RecordStatus Status { get; set; } = RecordStatus.Active;
}
