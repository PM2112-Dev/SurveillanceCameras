using Microsoft.Extensions.Logging;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Repository.Tyt;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Tyt.Commands.CheckTytApprovals;

/// <summary>
/// Chạy định kỳ: kiểm tra TYT đã duyệt (công khai) các truyện đang chờ chưa. Mỗi tài khoản chỉ tốn một request — đọc danh
/// sách truyện công khai của tài khoản. Truyện có trong danh sách thì chuyển sang <see cref="TytStage.Approved"/> để bắt đầu
/// đăng các chương còn lại theo lịch. Phiên đăng nhập lỗi thì bỏ qua cả tài khoản tới lượt kiểm tra sau.
/// </summary>
/// <returns>Số truyện vừa được duyệt.</returns>
public record CheckTytApprovalsCommand : IRequest<int>;

public class CheckTytApprovalsCommandHandler : IRequestHandler<CheckTytApprovalsCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ITytRepository _tyt;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CheckTytApprovalsCommandHandler> _logger;

    public CheckTytApprovalsCommandHandler(IApplicationDbContext context, ITytRepository tyt, TimeProvider timeProvider,
        ILogger<CheckTytApprovalsCommandHandler> logger)
    {
        _context = context;
        _tyt = tyt;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<int> Handle(CheckTytApprovalsCommand request, CancellationToken cancellationToken)
    {
        var pending = await _context.Works
            .Where(x => x.Type == WorkType.Story && x.TytStage == TytStage.PendingApproval && x.AccountId != null && x.TytStoryId != null)
            .ToListAsync(cancellationToken);

        var approved = 0;
        foreach (var group in pending.GroupBy(x => (x.CreatedBy, AccountId: x.AccountId!.Value)))
        {
            HashSet<string> publicIds;
            try
            {
                var context = await TytAccounts.ContextAsync(_context, group.Key.AccountId, group.Key.CreatedBy, cancellationToken);
                publicIds = (await _tyt.GetStoriesAsync(context, TytStoryVisibility.Public, cancellationToken)).Select(x => x.Id).ToHashSet();
            }
            catch (TytException ex)
            {
                _logger.LogWarning("Tài khoản {AccountId}: kiểm tra duyệt truyện lỗi: {Error}", group.Key.AccountId, ex.Message);
                foreach (var work in group) work.TytStageNote = $"Chưa kiểm tra được TYT đã duyệt chưa: {ex.Message}";
                continue;
            }

            foreach (var work in group.Where(x => publicIds.Contains(x.TytStoryId!)))
            {
                work.TytStage = TytStage.Approved;
                work.TytApprovedAt = _timeProvider.GetUtcNow();
                work.TytStageNote = "TYT đã duyệt — các chương còn lại được đăng theo lịch.";
                approved++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return approved;
    }
}
