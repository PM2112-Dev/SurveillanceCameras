namespace SurveillanceCameras.Application.Common.Interfaces;

/// <summary>Mã hoá bí mật (khoá truy cập dịch vụ ngoài) trước khi lưu vào DB.</summary>
public interface ISecretProtector
{
    string Protect(string secret);

    /// <exception cref="System.Security.Cryptography.CryptographicException">Dữ liệu hỏng hoặc khoá mã hoá của hệ thống đã đổi.</exception>
    string Unprotect(string protectedSecret);
}
