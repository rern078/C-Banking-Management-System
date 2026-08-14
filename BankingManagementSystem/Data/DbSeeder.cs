using BankingManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BankingManagementSystem.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<ApplicationDbContext>();

        if (db.Database.IsSqlite())
            await db.Database.EnsureCreatedAsync();
        else
            await db.Database.MigrateAsync();

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in new[] { "Admin", "Manager", "Staff" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var demoBranches = new[]
        {
            new Branch { BranchCode = "HQ01", BranchName = "Head Office", City = "Phnom Penh", Address = "Norodom Blvd", Phone = "023000111", Email = "hq@bank.local", Status = RecordStatus.Active },
            new Branch { BranchCode = "SR01", BranchName = "Siem Reap Branch", City = "Siem Reap", Address = "Sivatha St", Phone = "063000222", Email = "sr@bank.local", Status = RecordStatus.Active },
            new Branch { BranchCode = "PP02", BranchName = "Toul Kork Branch", City = "Phnom Penh", Address = "Street 289", Phone = "023000333", Email = "tk@bank.local", Status = RecordStatus.Active },
            new Branch { BranchCode = "PP03", BranchName = "Meanchey Branch", City = "Phnom Penh", Address = "Monivong Blvd", Phone = "023000444", Email = "mc@bank.local", Status = RecordStatus.Active },
            new Branch { BranchCode = "BT01", BranchName = "Battambang Branch", City = "Battambang", Address = "National Road 5", Phone = "053000555", Email = "bt@bank.local", Status = RecordStatus.Active },
            new Branch { BranchCode = "KD01", BranchName = "Ta Khmau Branch", City = "Kandal", Address = "Preah Monivong", Phone = "024000666", Email = "kd@bank.local", Status = RecordStatus.Active },
            new Branch { BranchCode = "KP01", BranchName = "Kampot Branch", City = "Kampot", Address = "River Road", Phone = "033000777", Email = "kp@bank.local", Status = RecordStatus.Active },
            new Branch { BranchCode = "SH01", BranchName = "Sihanoukville Branch", City = "Sihanoukville", Address = "Ekareach St", Phone = "034000888", Email = "sh@bank.local", Status = RecordStatus.Active },
            new Branch { BranchCode = "KT01", BranchName = "Kampong Thom Branch", City = "Kampong Thom", Address = "National Road 6", Phone = "062000999", Email = "kt@bank.local", Status = RecordStatus.Active },
            new Branch { BranchCode = "PV01", BranchName = "Prey Veng Branch", City = "Prey Veng", Address = "Market St", Phone = "043000000", Email = "pv@bank.local", Status = RecordStatus.Active }
        };

        foreach (var branch in demoBranches)
        {
            if (!await db.Branches.AnyAsync(b => b.BranchCode == branch.BranchCode))
                db.Branches.Add(branch);
        }
        await db.SaveChangesAsync();

        if (!await db.AccountTypes.AnyAsync())
        {
            db.AccountTypes.AddRange(
                new AccountTypeEntity { TypeName = "Saving", Description = "Savings account", MinimumBalance = 10m, InterestRate = 2.5m },
                new AccountTypeEntity { TypeName = "Current", Description = "Checking / current account", MinimumBalance = 0m, InterestRate = 0m },
                new AccountTypeEntity { TypeName = "Fixed Deposit", Description = "Fixed term deposit", MinimumBalance = 100m, InterestRate = 5.0m }
            );
            await db.SaveChangesAsync();
        }

        var hq = await db.Branches.FirstAsync(b => b.BranchCode == "HQ01");

        async Task EnsureUser(string email, string password, string fullName, string role)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is not null) return;

            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                PhoneNumber = "010000000",
                BranchId = hq.BranchId,
                Status = RecordStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(user, role);
        }

        await EnsureUser("admin@bank.local", "Admin@123", "System Administrator", "Admin");
        await EnsureUser("manager@bank.local", "Manager@123", "Branch Manager", "Manager");
        await EnsureUser("staff@bank.local", "Staff@123", "Bank Staff", "Staff");

        var admin = await userManager.FindByEmailAsync("admin@bank.local");

        var branchByCode = await db.Branches.ToDictionaryAsync(b => b.BranchCode, b => b.BranchId);
        int BranchId(string code) => branchByCode.GetValueOrDefault(code, hq.BranchId);

        var demoCustomers = new[]
        {
            new Customer { CustomerCode = "CUS0001", BranchId = BranchId("HQ01"), FirstName = "Demo", LastName = "Customer", Gender = Gender.Male, DateOfBirth = new DateTime(1995, 5, 1), NationalId = "NID-001", Phone = "012345678", Email = "demo.customer@bank.local", Address = "Phnom Penh", Status = RecordStatus.Active },
            new Customer { CustomerCode = "CUS0002", BranchId = BranchId("HQ01"), FirstName = "Sokha", LastName = "Chan", Gender = Gender.Female, DateOfBirth = new DateTime(1992, 3, 12), NationalId = "NID-002", Phone = "012111001", Email = "sokha.chan@email.local", Address = "Toul Kork, Phnom Penh", Status = RecordStatus.Active },
            new Customer { CustomerCode = "CUS0003", BranchId = BranchId("SR01"), FirstName = "Dara", LastName = "Lim", Gender = Gender.Male, DateOfBirth = new DateTime(1988, 7, 20), NationalId = "NID-003", Phone = "012111002", Email = "dara.lim@email.local", Address = "Sivatha, Siem Reap", Status = RecordStatus.Active },
            new Customer { CustomerCode = "CUS0004", BranchId = BranchId("PP02"), FirstName = "Sopheap", LastName = "Mey", Gender = Gender.Female, DateOfBirth = new DateTime(1990, 11, 8), NationalId = "NID-004", Phone = "012111003", Email = "sopheap.mey@email.local", Address = "Street 289, Phnom Penh", Status = RecordStatus.Active },
            new Customer { CustomerCode = "CUS0005", BranchId = BranchId("PP03"), FirstName = "Vannak", LastName = "Heng", Gender = Gender.Male, DateOfBirth = new DateTime(1985, 1, 25), NationalId = "NID-005", Phone = "012111004", Email = "vannak.heng@email.local", Address = "Meanchey, Phnom Penh", Status = RecordStatus.Active },
            new Customer { CustomerCode = "CUS0006", BranchId = BranchId("BT01"), FirstName = "Chanthou", LastName = "Keo", Gender = Gender.Female, DateOfBirth = new DateTime(1993, 9, 14), NationalId = "NID-006", Phone = "012111005", Email = "chanthou.keo@email.local", Address = "Battambang City", Status = RecordStatus.Active },
            new Customer { CustomerCode = "CUS0007", BranchId = BranchId("KD01"), FirstName = "Ratha", LastName = "Nou", Gender = Gender.Male, DateOfBirth = new DateTime(1987, 4, 30), NationalId = "NID-007", Phone = "012111006", Email = "ratha.nou@email.local", Address = "Ta Khmau, Kandal", Status = RecordStatus.Active },
            new Customer { CustomerCode = "CUS0008", BranchId = BranchId("KP01"), FirstName = "Bopha", LastName = "San", Gender = Gender.Female, DateOfBirth = new DateTime(1991, 6, 18), NationalId = "NID-008", Phone = "012111007", Email = "bopha.san@email.local", Address = "Kampot Town", Status = RecordStatus.Active },
            new Customer { CustomerCode = "CUS0009", BranchId = BranchId("SH01"), FirstName = "Piseth", LastName = "Ouk", Gender = Gender.Male, DateOfBirth = new DateTime(1989, 12, 5), NationalId = "NID-009", Phone = "012111008", Email = "piseth.ouk@email.local", Address = "Sihanoukville", Status = RecordStatus.Active },
            new Customer { CustomerCode = "CUS0010", BranchId = BranchId("KT01"), FirstName = "Sreymom", LastName = "Try", Gender = Gender.Female, DateOfBirth = new DateTime(1994, 2, 22), NationalId = "NID-010", Phone = "012111009", Email = "sreymom.try@email.local", Address = "Kampong Thom", Status = RecordStatus.Active }
        };

        foreach (var item in demoCustomers)
        {
            if (await db.Customers.AnyAsync(c => c.CustomerCode == item.CustomerCode)) continue;
            item.CreatedAt = DateTime.UtcNow;
            db.Customers.Add(item);
        }
        await db.SaveChangesAsync();

        var saving = await db.AccountTypes.FirstAsync(t => t.TypeName == "Saving");
        var current = await db.AccountTypes.FirstAsync(t => t.TypeName == "Current");
        var customers = await db.Customers.OrderBy(c => c.CustomerCode).ToListAsync();

        var demoAccounts = new (string Number, string CustomerCode, string Type, decimal Balance)[]
        {
            ("BMS000000000001", "CUS0001", "Saving", 1000m),
            ("BMS000000000002", "CUS0001", "Current", 250m),
            ("BMS000000000003", "CUS0002", "Saving", 800m),
            ("BMS000000000004", "CUS0002", "Current", 150m),
            ("BMS000000000005", "CUS0003", "Saving", 1200m),
            ("BMS000000000006", "CUS0004", "Saving", 450m),
            ("BMS000000000007", "CUS0005", "Current", 90m),
            ("BMS000000000008", "CUS0006", "Saving", 675m),
            ("BMS000000000009", "CUS0007", "Saving", 310m),
            ("BMS000000000010", "CUS0008", "Current", 180m),
            ("BMS000000000011", "CUS0009", "Saving", 940m),
            ("BMS000000000012", "CUS0010", "Saving", 520m),
            ("BMS000000000013", "CUS0003", "Current", 75m),
            ("BMS000000000014", "CUS0005", "Saving", 1600m),
            ("BMS000000000015", "CUS0008", "Saving", 220m)
        };

        foreach (var item in demoAccounts)
        {
            if (await db.Accounts.AnyAsync(a => a.AccountNumber == item.Number)) continue;
            var owner = customers.FirstOrDefault(c => c.CustomerCode == item.CustomerCode);
            if (owner is null) continue;
            var typeId = item.Type == "Current" ? current.AccountTypeId : saving.AccountTypeId;
            db.Accounts.Add(new Account
            {
                CustomerId = owner.CustomerId,
                AccountTypeId = typeId,
                BranchId = owner.BranchId,
                AccountNumber = item.Number,
                Balance = item.Balance,
                Currency = "USD",
                OpenDate = DateTime.UtcNow.AddDays(-20),
                Status = RecordStatus.Active,
                CreatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();

        var customer = await db.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUS0001");
        var primaryAccount = await db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == "BMS000000000001");
        if (customer is not null && primaryAccount is not null && !await db.Cards.AnyAsync())
        {
            db.Transactions.Add(new Transaction
            {
                AccountId = primaryAccount.AccountId,
                TransactionType = TxType.Deposit,
                Amount = 1000m,
                BalanceBefore = 0,
                BalanceAfter = 1000m,
                Description = "Demo opening balance",
                ReferenceNumber = "OPNDEMO001",
                TransactionDate = DateTime.UtcNow,
                CreatedBy = admin?.Id
            });

            db.Cards.Add(new Card
            {
                AccountId = primaryAccount.AccountId,
                CardNumber = "4111111111111111",
                CardType = CardType.Debit,
                IssueDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddYears(3),
                Status = CardStatus.Active
            });

            if (!await db.Beneficiaries.AnyAsync())
            {
                db.Beneficiaries.Add(new Beneficiary
                {
                    CustomerId = customer.CustomerId,
                    Nickname = "Family account",
                    AccountNumber = "BMS000000000002",
                    BankName = "Chamrern Bank",
                    Phone = "012111222",
                    Status = RecordStatus.Active,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (!await db.Cheques.AnyAsync())
            {
                db.Cheques.Add(new Cheque
                {
                    AccountId = primaryAccount.AccountId,
                    CustomerId = customer.CustomerId,
                    ChequeNumber = "CHQ000001",
                    PayeeName = "Utility Company",
                    Amount = 50m,
                    IssueDate = DateTime.UtcNow,
                    Status = ChequeStatus.Issued,
                    Description = "Demo cheque",
                    CreatedBy = admin?.Id
                });
            }

            if (!await db.FixedDeposits.AnyAsync())
            {
                db.FixedDeposits.Add(new FixedDeposit
                {
                    CustomerId = customer.CustomerId,
                    AccountId = primaryAccount.AccountId,
                    CertificateNo = "FD0001",
                    PrincipalAmount = 500m,
                    InterestRate = 5m,
                    TermMonths = 12,
                    MaturityAmount = 525m,
                    StartDate = DateTime.UtcNow,
                    MaturityDate = DateTime.UtcNow.AddMonths(12),
                    Status = FixedDepositStatus.Active,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (!await db.Notifications.AnyAsync())
            {
                db.Notifications.Add(new Notification
                {
                    Title = "Welcome to operations desk",
                    Message = "Demo data is ready. Open accounts, cheques, and fixed deposits from the sidebar.",
                    Category = "System",
                    UserId = admin?.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync();
        }

        if (await db.Customers.AnyAsync() && !await db.Notifications.AnyAsync())
        {
            var demoCustomer = await db.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUS0001")
                ?? await db.Customers.FirstAsync();
            var account = await db.Accounts.FirstOrDefaultAsync(a => a.CustomerId == demoCustomer.CustomerId);
            var adminUser = await userManager.FindByEmailAsync("admin@bank.local");

            if (account is not null && !await db.Beneficiaries.AnyAsync())
            {
                db.Beneficiaries.Add(new Beneficiary
                {
                    CustomerId = demoCustomer.CustomerId,
                    Nickname = "Family account",
                    AccountNumber = "BMS000000000002",
                    BankName = "Chamrern Bank",
                    Status = RecordStatus.Active,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (account is not null && !await db.Cheques.AnyAsync())
            {
                db.Cheques.Add(new Cheque
                {
                    AccountId = account.AccountId,
                    CustomerId = demoCustomer.CustomerId,
                    ChequeNumber = "CHQ000001",
                    PayeeName = "Utility Company",
                    Amount = 50m,
                    IssueDate = DateTime.UtcNow,
                    Status = ChequeStatus.Issued,
                    Description = "Demo cheque",
                    CreatedBy = adminUser?.Id
                });
            }

            if (account is not null && !await db.FixedDeposits.AnyAsync())
            {
                db.FixedDeposits.Add(new FixedDeposit
                {
                    CustomerId = demoCustomer.CustomerId,
                    AccountId = account.AccountId,
                    CertificateNo = "FD0001",
                    PrincipalAmount = 500m,
                    InterestRate = 5m,
                    TermMonths = 12,
                    MaturityAmount = 525m,
                    StartDate = DateTime.UtcNow,
                    MaturityDate = DateTime.UtcNow.AddMonths(12),
                    Status = FixedDepositStatus.Active,
                    CreatedAt = DateTime.UtcNow
                });
            }

            db.Notifications.Add(new Notification
            {
                Title = "New modules enabled",
                Message = "Beneficiaries, cheques, fixed deposits, and notifications are now available.",
                Category = "System",
                UserId = adminUser?.Id,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        if (!await db.Settings.AnyAsync())
        {
            db.Settings.AddRange(
                new Setting { SettingKey = "BankName", SettingValue = "Chamrern Bank", GroupName = "General", Description = "Display name of the bank" },
                new Setting { SettingKey = "BankCode", SettingValue = "CB01", GroupName = "General", Description = "Short bank code" },
                new Setting { SettingKey = "DefaultCurrency", SettingValue = "USD", GroupName = "General", Description = "Default account currency" },
                new Setting { SettingKey = "SupportPhone", SettingValue = "023000111", GroupName = "General", Description = "Customer support phone" },
                new Setting { SettingKey = "SupportEmail", SettingValue = "support@bank.local", GroupName = "General", Description = "Customer support email" },
                new Setting { SettingKey = "TransferFee", SettingValue = "0.50", GroupName = "Fees", Description = "Default transfer fee" },
                new Setting { SettingKey = "ChequeStopFee", SettingValue = "2.00", GroupName = "Fees", Description = "Fee to stop a cheque" },
                new Setting { SettingKey = "MinOpeningBalance", SettingValue = "10", GroupName = "Limits", Description = "Suggested minimum opening balance" },
                new Setting { SettingKey = "MaxDailyWithdraw", SettingValue = "5000", GroupName = "Limits", Description = "Daily withdrawal limit" }
            );
            await db.SaveChangesAsync();
        }

        await EnsureSettingAsync(db, "DashShowStats", "true", "Dashboard", "Show summary stats on the dashboard");
        await EnsureSettingAsync(db, "DashShowCashToday", "true", "Dashboard", "Show deposits, withdrawals, transfers, and net cash today");
        await EnsureSettingAsync(db, "DashShowAttention", "true", "Dashboard", "Show attention chips for frozen accounts, cheques, loans, and notices");
        await EnsureSettingAsync(db, "DashShowGraphingCalculator", "true", "Dashboard", "Show the graphing calculator panel");
        await EnsureSettingAsync(db, "DashShowRecentDeposits", "true", "Dashboard", "Show the recent deposits table");
        await EnsureSettingAsync(db, "DashShowRecentWithdrawals", "true", "Dashboard", "Show the recent withdrawals table");
        await EnsureSettingAsync(db, "DashShowRecentCustomers", "true", "Dashboard", "Show the recent customers table");
        await EnsureSettingAsync(db, "DashShowRecentLedger", "true", "Dashboard", "Show the recent ledger table");

        var seededAccounts = await db.Accounts.OrderBy(a => a.AccountNumber).ToListAsync();
        var demoDeposits = new (string Ref, string AccountNumber, decimal Amount, string Method, string Description, int DaysAgo)[]
        {
            ("DEPSEED001", "BMS000000000001", 1000m, "Opening", "Opening deposit", 30),
            ("DEPSEED002", "BMS000000000002", 250m, "Cash", "Counter cash deposit", 28),
            ("DEPSEED003", "BMS000000000003", 500m, "Cheque", "Cheque deposit cleared", 26),
            ("DEPSEED004", "BMS000000000004", 75m, "Cash", "Small cash top-up", 24),
            ("DEPSEED005", "BMS000000000005", 1200m, "Transfer", "Inbound transfer credit", 22),
            ("DEPSEED006", "BMS000000000006", 300m, "ATM", "ATM cash deposit", 20),
            ("DEPSEED007", "BMS000000000007", 180m, "Cash", "Branch cash deposit", 18),
            ("DEPSEED008", "BMS000000000008", 650m, "Cheque", "Salary cheque deposit", 16),
            ("DEPSEED009", "BMS000000000009", 90m, "Cash", "Weekend cash deposit", 14),
            ("DEPSEED010", "BMS000000000010", 420m, "Transfer", "Mobile banking transfer in", 12),
            ("DEPSEED011", "BMS000000000011", 150m, "ATM", "Night ATM deposit", 10),
            ("DEPSEED012", "BMS000000000012", 800m, "Cash", "Business cash deposit", 8),
            ("DEPSEED013", "BMS000000000013", 220m, "Cheque", "Customer cheque deposit", 6),
            ("DEPSEED014", "BMS000000000014", 350m, "Transfer", "Interbank transfer in", 4),
            ("DEPSEED015", "BMS000000000015", 125m, "Cash", "Express cash deposit", 2)
        };

        foreach (var item in demoDeposits)
        {
            var account = seededAccounts.FirstOrDefault(a => a.AccountNumber == item.AccountNumber);
            if (account is null) continue;

            var existing = await db.Deposits.FirstOrDefaultAsync(d => d.ReferenceNumber == item.Ref);
            if (existing is not null)
            {
                existing.AccountId = account.AccountId;
                existing.Amount = item.Amount;
                existing.BalanceBefore = 0;
                existing.BalanceAfter = item.Amount;
                existing.Method = item.Method;
                existing.Description = item.Description;
                existing.DepositDate = DateTime.UtcNow.AddDays(-item.DaysAgo);
                continue;
            }

            db.Deposits.Add(new Deposit
            {
                AccountId = account.AccountId,
                Amount = item.Amount,
                BalanceBefore = 0,
                BalanceAfter = item.Amount,
                Method = item.Method,
                ReferenceNumber = item.Ref,
                Description = item.Description,
                DepositDate = DateTime.UtcNow.AddDays(-item.DaysAgo),
                CreatedBy = admin?.Id
            });
        }
        await db.SaveChangesAsync();

        var demoWithdrawals = new (string Ref, string AccountNumber, decimal Amount, string Method, string Description, int DaysAgo)[]
        {
            ("WDRSEED001", "BMS000000000001", 100m, "Cash", "Counter cash withdrawal", 27),
            ("WDRSEED002", "BMS000000000002", 50m, "ATM", "ATM cash withdrawal", 25),
            ("WDRSEED003", "BMS000000000003", 200m, "Cash", "Branch cash withdrawal", 23),
            ("WDRSEED004", "BMS000000000004", 40m, "Cash", "Small cash withdrawal", 21),
            ("WDRSEED005", "BMS000000000005", 300m, "Cheque", "Cheque cash-out", 19),
            ("WDRSEED006", "BMS000000000006", 75m, "ATM", "Night ATM withdrawal", 17),
            ("WDRSEED007", "BMS000000000007", 60m, "Cash", "Express cash withdrawal", 15),
            ("WDRSEED008", "BMS000000000008", 150m, "Cash", "Business cash withdrawal", 13),
            ("WDRSEED009", "BMS000000000009", 45m, "ATM", "ATM withdrawal", 11),
            ("WDRSEED010", "BMS000000000010", 80m, "Cash", "Weekend cash withdrawal", 9)
        };

        foreach (var item in demoWithdrawals)
        {
            var account = seededAccounts.FirstOrDefault(a => a.AccountNumber == item.AccountNumber);
            if (account is null) continue;

            var existing = await db.Withdrawals.FirstOrDefaultAsync(w => w.ReferenceNumber == item.Ref);
            if (existing is not null)
            {
                existing.AccountId = account.AccountId;
                existing.Amount = item.Amount;
                existing.BalanceBefore = account.Balance;
                existing.BalanceAfter = account.Balance - item.Amount;
                existing.Method = item.Method;
                existing.Description = item.Description;
                existing.WithdrawDate = DateTime.UtcNow.AddDays(-item.DaysAgo);
                continue;
            }

            db.Withdrawals.Add(new Withdrawal
            {
                AccountId = account.AccountId,
                Amount = item.Amount,
                BalanceBefore = account.Balance,
                BalanceAfter = account.Balance - item.Amount,
                Method = item.Method,
                ReferenceNumber = item.Ref,
                Description = item.Description,
                WithdrawDate = DateTime.UtcNow.AddDays(-item.DaysAgo),
                CreatedBy = admin?.Id
            });
        }
        await db.SaveChangesAsync();
    }

    private static async Task EnsureSettingAsync(
        ApplicationDbContext db,
        string key,
        string value,
        string group,
        string description)
    {
        if (await db.Settings.AnyAsync(s => s.SettingKey == key)) return;
        db.Settings.Add(new Setting
        {
            SettingKey = key,
            SettingValue = value,
            GroupName = group,
            Description = description,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
