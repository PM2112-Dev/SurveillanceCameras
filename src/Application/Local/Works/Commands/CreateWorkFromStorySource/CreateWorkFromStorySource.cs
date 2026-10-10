using Microsoft.Extensions.Logging;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.AIs;
using SurveillanceCameras.Application.Local.Episodes.IntegrationEvents;
using SurveillanceCameras.Application.Local.Works.IntegrationEvents;
using SurveillanceCameras.Domain.Constants;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Works.Commands.CreateWorkFromStorySource;

/// <summary>
/// Tạo <see cref="Work"/> từ một nguồn truyện, dùng AI biên tập tên và giới thiệu theo prompt của loại
/// <see cref="PromptTypeCodes.EditTitle"/> / <see cref="PromptTypeCodes.EditDescription"/>.
/// Nguồn web có tài khoản TYT (<c>WebSource.TytAccountId</c>) thì truyện gắn tài khoản đó và được tạo tiếp trên TYT.
/// Nguồn web không cần biên tập (<c>WebSource.IsNeedEdit</c> tắt) thì giữ nguyên tên và giới thiệu, không gọi AI.
/// Chạy nền (consumer Kafka), không có người dùng đăng nhập: prompt, AI và truyện mới đều theo chủ của nguồn truyện.
/// Gọi lại nhiều lần an toàn — nguồn đã có truyện thì bỏ qua.
/// </summary>
/// <returns>Id truyện (mới hoặc đã có); <c>null</c> nếu nguồn truyện không còn.</returns>
public record CreateWorkFromStorySourceCommand(int StorySourceId) : IRequest<int?>;

public class CreateWorkFromStorySourceCommandHandler : IRequestHandler<CreateWorkFromStorySourceCommand, int?>
{
    private const int MaxTitleLength = 500;
    private const int MaxErrorLength = 2000;

    private const string TitleOutputRule =
        "Chỉ trả về tên truyện đã biên tập, trên một dòng, không giải thích, không đặt trong ngoặc kép.";

    private const string DescriptionOutputRule =
        "Chỉ trả về phần giới thiệu đã biên tập (có thể nhiều đoạn), không giải thích, không thêm tiêu đề.";

    private readonly IApplicationDbContext _context;
    private readonly AIPromptRunner _runner;
    private readonly ILogger<CreateWorkFromStorySourceCommandHandler> _logger;

    public CreateWorkFromStorySourceCommandHandler(IApplicationDbContext context, AIPromptRunner runner,
        ILogger<CreateWorkFromStorySourceCommandHandler> logger)
    {
        _context = context;
        _runner = runner;
        _logger = logger;
    }

    public async Task<int?> Handle(CreateWorkFromStorySourceCommand request, CancellationToken cancellationToken)
    {
        var source = await _context.StorySources
            .FirstOrDefaultAsync(x => x.Id == request.StorySourceId, cancellationToken);

        if (source is null)
        {
            _logger.LogWarning("StorySource {StorySourceId} không còn, bỏ qua tạo truyện", request.StorySourceId);
            return null;
        }

        var existingId = await _context.Works
            .Where(x => x.StorySourceId == source.Id)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (existingId is not null) return existingId;

        var owner = source.CreatedBy;
        var webSource = await _context.WebSources
            .Where(x => x.Id == source.WebSourceId)
            .Select(x => new { x.IsNeedEdit, x.TytAccountId })
            .FirstOrDefaultAsync(cancellationToken);
        // Không xác định được nguồn web thì giữ hành vi mặc định: có biên tập, không đăng TYT.
        var isNeedEdit = webSource?.IsNeedEdit ?? true;
        var titlePrompt = isNeedEdit
            ? await _runner.LatestPromptAsync(owner, PromptTypeCodes.EditTitle, cancellationToken)
            : null;
        var descriptionPrompt = isNeedEdit
            ? await _runner.LatestPromptAsync(owner, PromptTypeCodes.EditDescription, cancellationToken)
            : null;
        IReadOnlyList<AI> ais = titlePrompt is null && descriptionPrompt is null
            ? []
            : await _runner.AvailableAIsAsync(owner, cancellationToken);

        string? title;
        string? description;
        try
        {
            var context = StoryContext(source);

            title = titlePrompt is null || string.IsNullOrWhiteSpace(source.Title)
                ? source.Title
                : CleanTitle(await EditAsync(titlePrompt, source, context, TitleOutputRule, ais, cancellationToken));

            description = descriptionPrompt is null || string.IsNullOrWhiteSpace(source.Description)
                ? source.Description
                : (await EditAsync(descriptionPrompt, source, context, DescriptionOutputRule, ais, cancellationToken)).Trim();
        }
        catch (AIProviderException ex)
        {
            // Lưu lỗi để giao diện hiện cho người dùng; ném lại để consumer thử lại / đưa vào DLQ.
            source.StoryCreationError = Truncate(ex.Message, MaxErrorLength);
            await _context.SaveChangesAsync(cancellationToken);
            throw;
        }

        var story = new Work
        {
            StorySourceId = source.Id,
            Title = title,
            Description = description,
            Author = source.Author,
            IsFull = IsCompleted(source.Status),
            AccountId = webSource?.TytAccountId,
            CreatedBy = owner
        };

        source.StoryCreationError = null;

        // Lưu để có Id rồi phát sự kiện đồng bộ chương cùng transaction (outbox): truyện đã tạo thì chắc chắn
        // có yêu cầu lấy chương đi kèm.
        await _context.ExecuteInTransactionAsync(async ct =>
        {
            _context.Works.Add(story);
            await _context.SaveChangesAsync(ct);

            story.AddIntegrationEvent(new StoryChaptersSyncRequestedIntegrationEvent { StoryId = story.Id, Page = 1 }, ct);
            if (story.AccountId is not null)
                story.AddIntegrationEvent(new TytStoryCreateRequestedIntegrationEvent { StoryId = story.Id }, ct);
            await _context.SaveChangesAsync(ct);
        }, cancellationToken);

        _logger.LogInformation("Đã tạo Story {StoryId} từ StorySource {StorySourceId} (AI tên: {TitleEdited}, AI giới thiệu: {DescriptionEdited})",
            story.Id, source.Id, titlePrompt is not null, descriptionPrompt is not null);

        return story.Id;
    }

    private Task<string> EditAsync(Prompt prompt, StorySource source, string input, string outputRule,
        IReadOnlyList<AI> ais, CancellationToken cancellationToken)
        => _runner.RunAsync(prompt, ais, $"{FillPlaceholders(prompt.Content!, source)}\n\n{outputRule}", input, cancellationToken);

    /// <summary>Thông tin truyện gửi cho AI làm ngữ cảnh.</summary>
    private static string StoryContext(StorySource source)
        => $"""
            Tên truyện: {source.Title}
            Tác giả: {source.Author ?? "(không rõ)"}

            Giới thiệu:
            {source.Description ?? "(chưa có)"}
            """;

    /// <summary>Prompt có thể dùng biến {title}, {author}, {description} để chèn thông tin truyện.</summary>
    private static string FillPlaceholders(string content, StorySource source)
        => content
            .Replace("{title}", source.Title ?? "", StringComparison.OrdinalIgnoreCase)
            .Replace("{author}", source.Author ?? "", StringComparison.OrdinalIgnoreCase)
            .Replace("{description}", source.Description ?? "", StringComparison.OrdinalIgnoreCase);

    // Model hay thêm ngoặc kép, dấu ** hoặc nhiều dòng quanh tên truyện.
    private static string CleanTitle(string text)
    {
        var line = text.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0) ?? text.Trim();
        line = line.Trim('"', '“', '”', '\'', '*', '#', ' ');
        return Truncate(line, MaxTitleLength);
    }

    private static bool IsCompleted(string? status)
        => status is not null && (status.Contains("hoàn", StringComparison.OrdinalIgnoreCase)
                                  || status.Contains("full", StringComparison.OrdinalIgnoreCase));

    private static string Truncate(string text, int max) => text.Length <= max ? text : text[..max];
}
