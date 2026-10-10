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

    public AIPromptRunner(IApplicationDbContext context, IAITextRepository ai, ILogger<AIPromptRunner> logger)
    {
        _context = context;
        _ai = ai;
        _logger = logger;
    }

    /// <summary>Prompt mới sửa gần nhất của loại <paramref name="promptTypeCode"/>; <c>null</c> nếu người dùng chưa khai báo.</summary>
    public Task<Prompt?> LatestPromptAsync(string? ownerId, string promptTypeCode, CancellationToken cancellationToken)
        => _context.Prompts
            .AsNoTracking()
            .Where(x => x.CreatedBy == ownerId && x.PromptType!.Code == promptTypeCode && x.Content != null && x.Content != "")
            .OrderByDescending(x => x.LastModified)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>Các AI dùng được (có key và model), theo thứ tự thử.</summary>
    public Task<List<AI>> AvailableAIsAsync(string? ownerId, CancellationToken cancellationToken)
        => _context.AIs
            .AsNoTracking()
            .Where(x => x.CreatedBy == ownerId && x.ApiKey != null && x.ApiKey != "" && x.Model != null && x.Model != "")
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

    /// <exception cref="AIProviderException">Không có AI nào, hoặc mọi AI đều lỗi (thông điệp gồm lỗi của từng AI).</exception>
    public async Task<string> RunAsync(Prompt prompt, IReadOnlyList<AI> ais, string instructions, string input,
        CancellationToken cancellationToken)
    {
        if (ais.Count == 0)
            throw new AIProviderException("Chưa khai báo AI nào (API key + model) để biên tập — thêm ở mục AI.");

        var errors = new List<string>();

        foreach (var ai in ais)
        {
            try
            {
                return await _ai.GenerateAsync(
                    new AITextRequest(ai.Provider, ai.Model!, ai.ApiKey!, instructions, input), cancellationToken);
            }
            catch (AIProviderException ex)
            {
                _logger.LogWarning("AI {AIId} ({Provider}/{Model}) lỗi khi dùng prompt {PromptId}: {Error}",
                    ai.Id, ai.Provider, ai.Model, prompt.Id, ex.Message);
                errors.Add($"{ai.Title ?? ai.Model}: {ex.Message}");
            }
        }

        throw new AIProviderException($"Không AI nào xử lý được prompt “{prompt.Title}”. {string.Join(" | ", errors)}");
    }
}
