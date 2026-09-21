using Microsoft.EntityFrameworkCore;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Infrastructure.Data;

namespace SurveillanceCameras.Infrastructure.IntegrationTests;

public static class TestAccounts
{
    /// <summary>
    /// Đọc Account có cookie từ database thật. Bỏ qua test (không fail) nếu máy chưa chạy Postgres
    /// hoặc chưa có cookie. Tuỳ chọn: biến môi trường <c>TIKTOK_TEST_ACCOUNT_ID</c>.
    /// </summary>
    public static async Task<Account> LoadWithCookieAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(TestEnvironment.ConnectionString)
            .Options;

        await using var db = new ApplicationDbContext(options);

        if (!await db.Database.CanConnectAsync())
        {
            Assert.Ignore("Không kết nối được database thật (Postgres chưa chạy?).");
        }

        var query = db.Accounts.AsNoTracking().Where(a => a.Cookie != null && a.Cookie != "");
        if (int.TryParse(Environment.GetEnvironmentVariable("TIKTOK_TEST_ACCOUNT_ID"), out var accountId))
        {
            query = query.Where(a => a.Id == accountId);
        }

        var account = await query.OrderBy(a => a.Id).FirstOrDefaultAsync();
        if (account is null)
        {
            Assert.Ignore("Database chưa có Account nào có cookie. Hãy đăng nhập TikTok từ trang Tài khoản trước.");
        }

        return account;
    }
}
