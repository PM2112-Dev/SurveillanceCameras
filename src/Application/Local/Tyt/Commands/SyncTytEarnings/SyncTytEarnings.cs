using Microsoft.Extensions.Logging;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Repository.Tyt;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Tyt.Commands.SyncTytEarnings;

/// <summary>
/// Chạy định kỳ: với truyện đã được TYT duyệt, xem trang Nhận Thưởng. Truyện đủ điều kiện (công khai, đủ chương, lượt xem,
/// thời gian đăng) thì chuyển sang bật kiếm tiền (<c>IsEarning</c>) và đồng bộ số liệu về truyện: Tổng → <c>EarnCount</c>,
/// Đang Có → <c>EarnNow</c>, Đã Thanh Toán + Đã Chuyển → <c>Paid</c>, Số Lượt QC → <c>AdCount</c>. Chưa đủ điều kiện thì chưa có số liệu để
/// lấy, chỉ ghi chú và kiểm tra lại ở lượt sau. Ô nhận thưởng của TYT đang Tắt thì bật lên (<c>update_earning</c>).
/// </summary>
/// <returns>Số truyện đã đồng bộ được thu nhập.</returns>
public record SyncTytEarningsCommand : IRequest<int>;

public class SyncTytEarningsCommandHandler : IRequestHandler<SyncTytEarningsCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ITytRepository _tyt;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SyncTytEarningsCommandHandler> _logger;

    public SyncTytEarningsCommandHandler(IApplicationDbContext context, ITytRepository tyt, TimeProvider timeProvider,
        ILogger<SyncTytEarningsCommandHandler> logger)
    {
        _context = context;
        _tyt = tyt;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<int> Handle(SyncTytEarningsCommand request, CancellationToken cancellationToken)
    {
        var works = await _context.Works
            .Where(x => x.Type == WorkType.Story && x.TytStage == TytStage.Approved && x.AccountId != null && x.TytStoryId != null)
            .ToListAsync(cancellationToken);

        var synced = 0;
        foreach (var group in works.GroupBy(x => (x.CreatedBy, AccountId: x.AccountId!.Value)))
        {
            TytContext context;
            try
            {
                context = await TytAccounts.ContextAsync(_context, group.Key.AccountId, group.Key.CreatedBy, cancellationToken);
            }
            catch (TytException ex)
            {
                foreach (var work in group) work.TytEarningNote = $"Chưa đồng bộ được thu nhập: {ex.Message}";
                continue;
            }

            foreach (var work in group)
            {
                TytEarningDto earning;
                try
                {
                    earning = await _tyt.GetEarningAsync(context, work.TytStoryId!, cancellationToken);
                }
                catch (TytException ex)
                {
                    // Phiên hết hạn / TYT lỗi: bỏ qua cả tài khoản tới lượt sau.
                    _logger.LogWarning("Tài khoản {AccountId}: đồng bộ thu nhập lỗi: {Error}", group.Key.AccountId, ex.Message);
                    foreach (var other in group) other.TytEarningNote = $"Chưa đồng bộ được thu nhập: {ex.Message}";
                    break;
                }

                work.TytEarningSyncedAt = _timeProvider.GetUtcNow();

                if (!earning.Eligible)
                {
                    // Từng đủ điều kiện mà nay bị khoá lại (vi phạm, bị báo cáo…): tắt kiếm tiền, giữ số liệu cũ.
                    work.IsEarning = false;
                    work.TytEarningNote = "Chưa đủ điều kiện nhận thưởng trên TYT (cần công khai, tối thiểu 5 chương, 50 lượt xem, đăng 1 ngày) — kiểm tra lại ở lượt sau.";
                    continue;
                }

                // Đủ điều kiện mà ô thưởng đang Tắt: bật lên (đây là "bật kiếm tiền" bên phía TYT).
                var turnedOn = false;
                if (!earning.EarningEnabled)
                {
                    try
                    {
                        await _tyt.SetEarningAsync(context, work.TytStoryId!, true, cancellationToken);
                        turnedOn = true;
                    }
                    catch (TytException ex)
                    {
                        work.TytEarningNote = $"Đủ điều kiện nhận thưởng nhưng chưa bật được: {ex.Message}";
                        continue;
                    }
                }

                work.IsEarning = true;
                if (earning.Total is { } total) work.EarnCount = (int)Math.Min(int.MaxValue, total);
                if (earning.Available is { } available) work.EarnNow = (int)Math.Min(int.MaxValue, available);
                // Đã Thanh Toán và Đã Chuyển đều là tiền đã rời khỏi số dư của truyện: cộng lại thành "Đã thanh toán".
                if (earning.Paid is not null || earning.Transferred is not null)
                    work.Paid = (int)Math.Min(int.MaxValue, (earning.Paid ?? 0) + (earning.Transferred ?? 0));
                if (earning.AdViews is { } adViews) work.AdCount = (int)Math.Min(int.MaxValue, adViews);

                work.TytEarningNote = earning.Total is null
                    ? "Đủ điều kiện nhận thưởng nhưng chưa đọc được số liệu thu nhập (giao diện trang có thể đã đổi)."
                    : $"{(turnedOn ? "Đã bật nhận thưởng trên TYT. " : "")}Đã đồng bộ thu nhập{(earning.Withdrawing is > 0 ? $"; đang rút {earning.Withdrawing:N0} đ" : "")}{(earning.FromAds is { } ads ? $"; từ quảng cáo {ads:N0} đ" : "")}.";
                synced++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return synced;
    }
}
