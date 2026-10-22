using Microsoft.Extensions.Logging;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Repository.AI;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.AIs;

/// <summary>
/// Chạy prompt của người dùng với các AI (API key) của họ: thử lần lượt theo thứ tự khai báo, key lỗi (hết quota,
/// sai key, model từ chối…) thì chuyển key kế tiếp. Dùng cho các tác vụ nền (biên tập truyện, chương).
/// </summary>
public class AIPromptRunner
{
    private readonly IApplicationDbContext _context;
    private readonly IAITextRepository _ai;
    private readonly ILogger<AIPromptRunner> _logger;
    private readonly AIKeyCooldowns _cooldowns;
    private readonly TimeProvider _timeProvider;

    public AIPromptRunner(IApplicationDbContext context, IAITextRepository ai, ILogger<AIPromptRunner> logger,
        AIKeyCooldowns? cooldowns = null, TimeProvider? timeProvider = null)
    {
        _context = context;
        _ai = ai;
        _logger = logger;
        _cooldowns = cooldowns ?? new AIKeyCooldowns();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Prompt mới sửa gần nhất của loại <paramref name="promptTypeCode"/>; <c>null</c> nếu người dùng chưa khai báo.</summary>
    public Task<Prompt?> LatestPromptAsync(string? ownerId, string promptTypeCode, CancellationToken cancellationToken)
        => _context.Prompts
            .AsNoTracking()
            .Where(x => x.CreatedBy == ownerId && x.PromptType!.Code == promptTypeCode && x.Content != null && x.Content != "")
            .OrderByDescending(x => x.LastModified)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>Các AI dùng được (có key và model), theo thứ tự thử.</summary>
    public async Task<List<AI>> AvailableAIsAsync(string? ownerId, CancellationToken cancellationToken)
    {
        var all = await _context.AIs
            .AsNoTracking()
            .Where(x => x.CreatedBy == ownerId)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var usable = all.Where(x => !string.IsNullOrWhiteSpace(x.ApiKey) && !string.IsNullOrWhiteSpace(x.Model)).ToList();
        if (usable.Count < all.Count)
        {
            // Key thiếu model (hoặc key) bị bỏ qua — dễ nhầm là "thêm key rồi mà không được dùng".
            _logger.LogWarning("Bỏ qua {Count} AI chưa có API key hoặc model: {Ids}", all.Count - usable.Count,
                string.Join(", ", all.Except(usable).Select(x => x.Id)));
        }

        return usable;
    }

    /// <summary>
    /// Thử lần lượt các key theo thứ tự khai báo, bỏ qua key đang nghỉ (hết quota / bị từ chối); key vừa hết quota được nghỉ
    /// theo thời gian nền tảng báo rồi mới thử lại — nên request sau dùng thẳng key còn dùng được kế tiếp.
    /// </summary>
    /// <exception cref="AIProviderException">Không AI nào, hoặc mọi AI đều lỗi / đang nghỉ (thông điệp gồm lỗi của từng AI).</exception>
    public async Task<string> RunAsync(Prompt prompt, IReadOnlyList<AI> ais, string instructions, string input,
        CancellationToken cancellationToken)
    {
        if (ais.Count == 0)
            throw new AIProviderException("Chưa khai báo AI nào (API key + model) để biên tập — thêm ở mục AI.");

        var errors = new List<string>();
        var resting = new List<(AI Ai, DateTimeOffset Until)>();

        foreach (var ai in ais)
        {
            if (_cooldowns.BlockedUntil(ai.Id, _timeProvider.GetUtcNow()) is { } until)
            {
                resting.Add((ai, until));
                continue;
            }

            try
            {
                var result = await _ai.GenerateAsync(
                    new AITextRequest(ai.Provider, ai.Model!, ai.ApiKey!, instructions, input), cancellationToken);
                _cooldowns.Clear(ai.Id);
                return result;
            }
            catch (AIKeyUnavailableException ex)
            {
                _cooldowns.Block(ai.Id, ex.RetryAfter, _timeProvider.GetUtcNow());
                _logger.LogWarning("AI {AIId} ({Provider}/{Model}) hết quota hoặc bị từ chối, nghỉ {Wait}, chuyển key khác: {Error}",
                    ai.Id, ai.Provider, ai.Model, ex.RetryAfter, ex.Message);
                errors.Add($"{ai.Title ?? ai.Model}: {Short(ex.Message)}");
            }
            catch (AIProviderException ex)
            {
                _logger.LogWarning("AI {AIId} ({Provider}/{Model}) lỗi khi dùng prompt {PromptId}: {Error}",
                    ai.Id, ai.Provider, ai.Model, prompt.Id, ex.Message);
                errors.Add($"{ai.Title ?? ai.Model}: {Short(ex.Message)}");
            }
        }

        var restingText = resting.Count == 0
            ? ""
            : $" {resting.Count} key đang nghỉ do hết quota/bị từ chối, sớm nhất dùng lại lúc {resting.Min(x => x.Until).ToLocalTime():HH:mm dd/MM}.";
        throw new AIProviderException(
            $"Không AI nào xử lý được prompt “{prompt.Title}” ({ais.Count} key, {errors.Count} vừa thử lỗi).{restingText} {string.Join(" | ", errors)}".TrimEnd());
    }

    /// <summary>Lỗi nền tảng hay kèm đường dẫn và hướng dẫn dài: chỉ giữ phần đầu để thông báo không bị cắt mất các key sau.</summary>
    private static string Short(string message)
    {
        var line = message.ReplaceLineEndings(" ");
        return line.Length <= 140 ? line : line[..140] + "…";
    }
}
