namespace Forex.Wpf.Common.Services;

using System.IO;
using System.Management;
using System.Security.Cryptography;

public static partial class DevKeyService
{
    private const string SecureKeyFileName = "voltstream.key";

    public static (string login, string password)? TryGetSecureCredentials()
    {
        try
        {
            var removableDrives = DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.Removable && d.IsReady);

            foreach (var drive in removableDrives)
            {
                var keyFilePath = Path.Combine(drive.RootDirectory.FullName, SecureKeyFileName);
                if (File.Exists(keyFilePath))
                {
                    var serialNumber = GetDriveSerialNumber(drive.Name);
                    var credentials = DecryptAndValidateKey(keyFilePath, serialNumber);
                    if (credentials != null) return credentials;
                }
            }
        }
        catch { }
        return null;
    }

    public static void GenerateHardwareKey(string drivePath, string login, string password)
    {
        if (!Directory.Exists(drivePath))
            return;

        var serialNumber = GetDriveSerialNumber(drivePath);
        var rawContent = $"{login}|{password}|{serialNumber}";
        
        using var aes = Aes.Create();
        aes.Key = EncryptionKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length);
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs)) { sw.Write(rawContent); }

        var secureKeyPath = Path.Combine(drivePath, SecureKeyFileName);
        File.WriteAllBytes(secureKeyPath, ms.ToArray());
    }

    private static (string, string)? DecryptAndValidateKey(string filePath, string hardwareSerial)
    {
        try
        {
            var encryptedBytes = File.ReadAllBytes(filePath);
            using var aes = Aes.Create();
            aes.Key = EncryptionKey;

            var ivLength = aes.BlockSize / 8;
            if (encryptedBytes.Length <= ivLength) return null;

            var iv = new byte[ivLength];
            Array.Copy(encryptedBytes, iv, ivLength);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(encryptedBytes, ivLength, encryptedBytes.Length - ivLength);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            
            var decryptedContent = sr.ReadToEnd();
            var parts = decryptedContent.Split('|');
            
            if (parts.Length == 3 && parts[2] == hardwareSerial)
                return (parts[0], parts[1]);
        }
        catch {}
        return null;
    }

    private static string GetDriveSerialNumber(string drivePath)
    {
        try
        {
            var driveId = drivePath.TrimEnd('\\').TrimEnd('/');
            using var searcher = new ManagementObjectSearcher($"SELECT VolumeSerialNumber FROM Win32_LogicalDisk WHERE DeviceID = '{driveId}'");
            using var collection = searcher.Get();
            foreach (var item in collection)
                return item["VolumeSerialNumber"]?.ToString() ?? "00000000";
        }
        catch { }
        return "UNKNOWN_HARDWARE";
    }
}
