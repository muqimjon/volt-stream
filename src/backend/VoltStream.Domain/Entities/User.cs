namespace VoltStream.Domain.Entities;

using VoltStream.Domain.Enums;

public class User : Auditable
{
    public string Username { get; set; } = string.Empty;
    public string NormalizedUsername { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? Name { get; set; }
    public string? NormalizedName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public string? Address { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? ImageUrl { get; set; }
    public string? LanguagePreference { get; set; }
    public string? TimeZone { get; set; }
    public string? PasswordHash { get; set; }
    public bool IsActive { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTimeOffset? LastLoginDate { get; set; }
    public int FailedLoginAttempts { get; set; }
    public string? PreferencesJson { get; set; }
    public string[] Permissions { get; set; } = [];
    public PrintPolicy PrintPolicy { get; set; }

    public long? BranchId { get; set; }
    public Branch? Branch { get; set; }
}
