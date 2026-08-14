using System.ComponentModel.DataAnnotations;

namespace BankingManagementSystem.Models;

public enum RecordStatus
{
    Active = 0,
    Inactive = 1,
    Frozen = 2,
    Closed = 3
}

public enum Gender
{
    Male = 0,
    Female = 1,
    Other = 2
}

public enum TxType
{
    Deposit = 0,
    Withdraw = 1,
    TransferIn = 2,
    TransferOut = 3,
    Fee = 4,
    Interest = 5
}

public enum TransferStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Cancelled = 3
}

public enum LoanStatus
{
    Pending = 0,
    Active = 1,
    PaidOff = 2,
    Defaulted = 3,
    Cancelled = 4
}

public enum CardType
{
    Debit = 0,
    Credit = 1
}

public enum CardStatus
{
    Active = 0,
    Blocked = 1,
    Expired = 2,
    Cancelled = 3
}

public class Branch
{
    public int BranchId { get; set; }

    [Required, StringLength(20)]
    public string BranchCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string BranchName { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    public RecordStatus Status { get; set; } = RecordStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
}

public class Customer
{
    public int CustomerId { get; set; }

    [Required, StringLength(30)]
    public string CustomerCode { get; set; } = string.Empty;

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    [Required, StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    public Gender Gender { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [StringLength(50)]
    public string? NationalId { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(250)]
    public string? Address { get; set; }

    public RecordStatus Status { get; set; } = RecordStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    public ICollection<Account> Accounts { get; set; } = new List<Account>();
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    public ICollection<Beneficiary> Beneficiaries { get; set; } = new List<Beneficiary>();
    public ICollection<Cheque> Cheques { get; set; } = new List<Cheque>();
    public ICollection<FixedDeposit> FixedDeposits { get; set; } = new List<FixedDeposit>();
}

public class AccountTypeEntity
{
    public int AccountTypeId { get; set; }

    [Required, StringLength(50)]
    public string TypeName { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Description { get; set; }

    public decimal MinimumBalance { get; set; }

    public decimal InterestRate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Account> Accounts { get; set; } = new List<Account>();
}

public class Account
{
    public int AccountId { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int AccountTypeId { get; set; }
    public AccountTypeEntity? AccountType { get; set; }

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    [Required, StringLength(30)]
    public string AccountNumber { get; set; } = string.Empty;

    public decimal Balance { get; set; }

    [StringLength(10)]
    public string Currency { get; set; } = "USD";

    public DateTime OpenDate { get; set; } = DateTime.UtcNow;

    public RecordStatus Status { get; set; } = RecordStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Card> Cards { get; set; } = new List<Card>();
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    public ICollection<Cheque> Cheques { get; set; } = new List<Cheque>();
    public ICollection<FixedDeposit> FixedDeposits { get; set; } = new List<FixedDeposit>();
    public ICollection<Deposit> Deposits { get; set; } = new List<Deposit>();
    public ICollection<Withdrawal> Withdrawals { get; set; } = new List<Withdrawal>();
}

public class Transaction
{
    public int TransactionId { get; set; }

    public int AccountId { get; set; }
    public Account? Account { get; set; }

    public TxType TransactionType { get; set; }

    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }

    [StringLength(250)]
    public string? Description { get; set; }

    [StringLength(40)]
    public string? ReferenceNumber { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
}

public class Deposit
{
    public int DepositId { get; set; }

    public int AccountId { get; set; }
    public Account? Account { get; set; }

    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }

    [StringLength(30)]
    public string Method { get; set; } = "Cash";

    [StringLength(40)]
    public string? ReferenceNumber { get; set; }

    [StringLength(250)]
    public string? Description { get; set; }

    public DateTime DepositDate { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
}

public class Withdrawal
{
    public int WithdrawalId { get; set; }

    public int AccountId { get; set; }
    public Account? Account { get; set; }

    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }

    [StringLength(30)]
    public string Method { get; set; } = "Cash";

    [StringLength(40)]
    public string? ReferenceNumber { get; set; }

    [StringLength(250)]
    public string? Description { get; set; }

    public DateTime WithdrawDate { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
}

public class Transfer
{
    public int TransferId { get; set; }

    public int FromAccountId { get; set; }
    public Account? FromAccount { get; set; }

    public int ToAccountId { get; set; }
    public Account? ToAccount { get; set; }

    public decimal Amount { get; set; }
    public decimal TransferFee { get; set; }

    [StringLength(40)]
    public string? ReferenceNumber { get; set; }

    [StringLength(250)]
    public string? Description { get; set; }

    public TransferStatus Status { get; set; } = TransferStatus.Completed;

    public DateTime TransferDate { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
}

public class Loan
{
    public int LoanId { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? AccountId { get; set; }
    public Account? Account { get; set; }

    [Required, StringLength(50)]
    public string LoanType { get; set; } = "Personal";

    public decimal PrincipalAmount { get; set; }
    public decimal InterestRate { get; set; }
    public int TermMonths { get; set; }
    public decimal MonthlyPayment { get; set; }

    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime EndDate { get; set; }

    public decimal RemainingAmount { get; set; }

    public LoanStatus Status { get; set; } = LoanStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<LoanPayment> Payments { get; set; } = new List<LoanPayment>();
}

public class LoanPayment
{
    public int PaymentId { get; set; }

    public int LoanId { get; set; }
    public Loan? Loan { get; set; }

    public decimal PaymentAmount { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal RemainingAmount { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    [StringLength(50)]
    public string PaymentMethod { get; set; } = "Cash";

    [StringLength(40)]
    public string? ReferenceNumber { get; set; }

    public string? CreatedBy { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
}

public class Card
{
    public int CardId { get; set; }

    public int AccountId { get; set; }
    public Account? Account { get; set; }

    [Required, StringLength(20)]
    public string CardNumber { get; set; } = string.Empty;

    public CardType CardType { get; set; } = CardType.Debit;

    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public DateTime ExpiryDate { get; set; }

    public CardStatus Status { get; set; } = CardStatus.Active;
}

public class AuditLog
{
    public int LogId { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [Required, StringLength(80)]
    public string Action { get; set; } = string.Empty;

    [StringLength(80)]
    public string? TableName { get; set; }

    [StringLength(50)]
    public string? RecordId { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum ChequeStatus
{
    Issued = 0,
    Cleared = 1,
    Stopped = 2,
    Bounced = 3
}

public enum FixedDepositStatus
{
    Active = 0,
    Matured = 1,
    Closed = 2
}

public class Beneficiary
{
    public int BeneficiaryId { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Required, StringLength(100)]
    public string Nickname { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string AccountNumber { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string BankName { get; set; } = "Chamrern Bank";

    [StringLength(30)]
    public string? Phone { get; set; }

    public RecordStatus Status { get; set; } = RecordStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Cheque
{
    public int ChequeId { get; set; }

    public int AccountId { get; set; }
    public Account? Account { get; set; }

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Required, StringLength(20)]
    public string ChequeNumber { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string PayeeName { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public DateTime? ClearedDate { get; set; }

    public ChequeStatus Status { get; set; } = ChequeStatus.Issued;

    [StringLength(250)]
    public string? Description { get; set; }

    public string? CreatedBy { get; set; }
}

public class FixedDeposit
{
    public int FixedDepositId { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int AccountId { get; set; }
    public Account? Account { get; set; }

    [Required, StringLength(30)]
    public string CertificateNo { get; set; } = string.Empty;

    public decimal PrincipalAmount { get; set; }
    public decimal InterestRate { get; set; }
    public int TermMonths { get; set; }
    public decimal MaturityAmount { get; set; }

    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime MaturityDate { get; set; }

    public FixedDepositStatus Status { get; set; } = FixedDepositStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Notification
{
    public int NotificationId { get; set; }

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Message { get; set; } = string.Empty;

    [StringLength(40)]
    public string Category { get; set; } = "General";

    public bool IsRead { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Setting
{
    public int SettingId { get; set; }

    [Required, StringLength(80)]
    public string SettingKey { get; set; } = string.Empty;

    [Required, StringLength(250)]
    public string SettingValue { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string GroupName { get; set; } = "General";

    [StringLength(250)]
    public string? Description { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(450)]
    public string? UpdatedBy { get; set; }
}
