using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using SurveillanceCameras.Application.Common.Interfaces;

namespace SurveillanceCameras.Infrastructure.Security;

/// <summary>
/// Mã hoá bí mật bằng AES-256-GCM. Khoá lấy từ cấu hình <c>Security:SecretKey</c>, chưa đặt thì dùng <c>Jwt:SigningKey</c>
/// (luôn có) — đổi giá trị đang dùng thì các bí mật đã lưu không giải mã được nữa và phải nhập lại.
/// </summary>
public sealed class AesSecretProtector : ISecretProtector
{
    private const string Prefix = "v1:";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public AesSecretProtector(IConfiguration configuration)
        : this(configuration["Security:SecretKey"] is { Length: > 0 } key ? key : configuration["Jwt:SigningKey"] ?? "")
    {
    }

    public AesSecretProtector(string secretKey)
    {
        if (string.IsNullOrEmpty(secretKey))
            throw new InvalidOperationException("Chưa cấu hình Security:SecretKey (hoặc Jwt:SigningKey) để mã hoá bí mật.");

        _key = HKDF.DeriveKey(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes(secretKey), 32,
            info: "SurveillanceCameras.SecretProtector.v1"u8.ToArray());
    }

    public string Protect(string secret)
    {
        var plain = Encoding.UTF8.GetBytes(secret);
        var output = new byte[NonceSize + TagSize + plain.Length];
        var nonce = output.AsSpan(0, NonceSize);
        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plain, output.AsSpan(NonceSize + TagSize), output.AsSpan(NonceSize, TagSize));

        return Prefix + Convert.ToBase64String(output);
    }

    public string Unprotect(string protectedSecret)
    {
        if (!protectedSecret.StartsWith(Prefix, StringComparison.Ordinal))
            throw new CryptographicException("Bí mật không đúng định dạng.");

        byte[] data;
        try
        {
            data = Convert.FromBase64String(protectedSecret[Prefix.Length..]);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException("Bí mật không đúng định dạng.", ex);
        }

        if (data.Length < NonceSize + TagSize) throw new CryptographicException("Bí mật không đúng định dạng.");

        var plain = new byte[data.Length - NonceSize - TagSize];
        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(data.AsSpan(0, NonceSize), data.AsSpan(NonceSize + TagSize), data.AsSpan(NonceSize, TagSize), plain);

        return Encoding.UTF8.GetString(plain);
    }
}
