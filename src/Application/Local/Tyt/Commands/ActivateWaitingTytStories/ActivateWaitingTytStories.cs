using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Works.IntegrationEvents;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Tyt.Commands.ActivateWaitingTytStories;

/// <summary>
/// Chạy định kỳ: truyện đang chờ chỗ (<see cref="TytStage.WaitingSlot"/>) được đưa vào TYT khi tài khoản đã có chỗ trống — tức
/// là số truyện đang đăng (chưa hoàn thành) của tài khoản thấp hơn mức tối đa. Truyện hoàn thành giải phóng chỗ cho truyện khác.
/// </summary>
/// <returns>Số truyện được kích hoạt (đưa vào hàng đợi tạo trên TYT).</returns>
public record ActivateWaitingTytStoriesCommand : IRequest<int>;

public class ActivateWaitingTytStoriesCommandHandler : IRequestHandler<ActivateWaitingTytStoriesCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly TytPolicyOptions _policy;

    public ActivateWaitingTytStoriesCommandHandler(IApplicationDbContext context, IOptions<TytPolicyOptions> policy)
    {
        _context = context;
        _policy = policy.Value;
    }

    public async Task<int> Handle(ActivateWaitingTytStoriesCommand request, CancellationToken cancellationToken)
    {
        var waiting = await _context.Works
            .Where(x => x.Type == WorkType.Story && x.TytStage == TytStage.WaitingSlot && x.AccountId != null)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var activated = 0;
        foreach (var group in waiting.GroupBy(x => x.AccountId!.Value))
        {
            var free = _policy.MaxActiveStoriesPerAccount - await TytSlots.ActiveCountAsync(_context, group.Key, null, cancellationToken);
            foreach (var work in group.Take(Math.Max(0, free)))
            {
                work.TytStage = TytStage.None;
                work.TytStageNote = null;
                work.AddIntegrationEvent(new TytStoryCreateRequestedIntegrationEvent { StoryId = work.Id }, cancellationToken);
                activated++;
            }
        }

        if (activated > 0) await _context.SaveChangesAsync(cancellationToken);
        return activated;
    }
}
