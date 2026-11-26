using System.Security.Cryptography;
using System.Text;

namespace RailwayErp.MvvmDemo.Services;

public interface ICryptoService
{
    Task<string?> EncryptAsync(string? plain);
    Task<string?> DecryptAsync(string? cipher);
}

// AES-256 для окремих полів; ключ генерується один раз і зберігається в SecureStorage
public class AesCryptoService : ICryptoService
{
    private const string KeyName = "db_field_key";
    private const string Prefix = "enc:";
    private byte[]? _key;

    private async Task<byte[]> GetKeyAsync()
    {
        if (_key is not null) return _key;

        var stored = await SecureStorage.Default.GetAsync(KeyName);
        if (string.IsNullOrEmpty(stored))
        {
            _key = RandomNumberGenerator.GetBytes(32);
            await SecureStorage.Default.SetAsync(KeyName, Convert.ToBase64String(_key));
        }
        else
        {
            _key = Convert.FromBase64String(stored);
        }
        return _key;
    }

    public async Task<string?> EncryptAsync(string? plain)
    {
        if (string.IsNullOrEmpty(plain)) return plain;

        using var aes = Aes.Create();
        aes.Key = await GetKeyAsync();
        aes.GenerateIV();
        var data = Encoding.UTF8.GetBytes(plain);
        var cipher = aes.EncryptCbc(data, aes.IV);

        // формат: enc:Base64(IV + шифротекст)
        return Prefix + Convert.ToBase64String(aes.IV.Concat(cipher).ToArray());
    }

    public async Task<string?> DecryptAsync(string? cipher)
    {
        if (string.IsNullOrEmpty(cipher) || !cipher.StartsWith(Prefix)) return cipher;

        var bytes = Convert.FromBase64String(cipher[Prefix.Length..]);
        using var aes = Aes.Create();
        aes.Key = await GetKeyAsync();
        var iv = bytes[..16];
        var data = aes.DecryptCbc(bytes[16..], iv);
        return Encoding.UTF8.GetString(data);
    }
}
