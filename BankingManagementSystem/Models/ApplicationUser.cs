using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BankingManagementSystem.Models;

/// <summary>
/// Staff/system users (maps to noted.txt Users table). Roles use Identity (Admin, Manager, Staff).
/// </summary>
public class ApplicationUser : IdentityUser
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }

    public RecordStatus Status { get; set; } = RecordStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
