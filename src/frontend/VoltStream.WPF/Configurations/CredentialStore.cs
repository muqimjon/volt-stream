namespace VoltStream.WPF.Configurations;

using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

public class CredentialStore
{
    private const string FilePath = "config/credentials.dat";
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(5);

    private sealed record CredentialsDto(string Username, string Password, DateTime SavedAtUtc);

    public (string username, string password)? Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return null;

            var bytes = ProtectedData.Unprotect(File.ReadAllBytes(FilePath), null, DataProtectionScope.CurrentUser);
            var dto = JsonSerializer.Deserialize<CredentialsDto>(bytes);

            if (dto is null)
                return null;

            if (DateTime.UtcNow - dto.SavedAtUtc > Lifetime)
            {
                Clear();
                return null;
            }

            return (dto.Username, dto.Password);
        }
        catch
        {
            return null;
        }
    }

    public void Save(string username, string password)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(new CredentialsDto(username, password, DateTime.UtcNow));
            var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllBytes(FilePath, encrypted);
        }
        catch { }
    }

    public void Clear()
    {
        try
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
        }
        catch { }
    }
}
