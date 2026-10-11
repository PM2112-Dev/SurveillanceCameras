using System.Security.Cryptography;
using NUnit.Framework;
using Shouldly;
using SurveillanceCameras.Infrastructure.Security;

namespace SurveillanceCameras.Application.UnitTests.Security;

public class AesSecretProtectorTests
{
    private const string Key = "khoá-thử-nghiệm-đủ-dài-0123456789";

    [Test]
    public void Protect_RoundTrips_AndNeverStoresThePlainSecret()
    {
        var protector = new AesSecretProtector(Key);

        var stored = protector.Protect("bí-mật-R2");

        stored.ShouldNotContain("bí-mật-R2");
        protector.Unprotect(stored).ShouldBe("bí-mật-R2");
        // Mỗi lần mã hoá dùng nonce mới.
        protector.Protect("bí-mật-R2").ShouldNotBe(stored);
    }

    [Test]
    public void Unprotect_Throws_WhenKeyChangedOrDataTampered()
    {
        var stored = new AesSecretProtector(Key).Protect("bí-mật-R2");

        Should.Throw<CryptographicException>(() => new AesSecretProtector(Key + "x").Unprotect(stored));
        Should.Throw<CryptographicException>(() => new AesSecretProtector(Key).Unprotect("bí-mật-R2"));
        Should.Throw<CryptographicException>(() => new AesSecretProtector(Key).Unprotect("v1:AAAA"));
    }
}
