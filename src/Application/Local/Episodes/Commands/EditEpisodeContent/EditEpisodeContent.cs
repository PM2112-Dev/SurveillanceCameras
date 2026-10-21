using System.Text;
using Microsoft.Extensions.Logging;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.AIs;
using SurveillanceCameras.Application.Local.WebSources;
using SurveillanceCameras.Domain.Constants;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Application.Local.Episodes.Commands.EditEpisodeContent;

/// <summary>
/// Chạy nền: dùng AI biên tập nội dung một chương theo prompt loại <see cref="PromptTypeCodes.EditChapterContent"/>.
/// Chương dài được chia đoạn để không vượt giới hạn output của model. Lỗi AI được ghi vào chương (EditFailed) thay vì
/// ném ra: khi hết quota, thử lại hàng nghìn chương chỉ tốn lượt gọi — người dùng bấm "Thử lại" khi đã có quota.
/// Nguồn web không cần biên tập thì giữ nguyên nội dung gốc, trừ khi người dùng chủ động yêu cầu (<c>Force</c>).
/// </summary>
public record EditEpisodeContentCommand(int ChapterId, bool Force = false) : IRequest;

public class EditEpisodeContentCommandHandler : IRequestHandler<EditEpisodeContentCommand>
{
    /// <summary>Độ dài tối đa (ký tự) mỗi lần gửi AI; chương dài hơn được chia theo đoạn văn.</summary>
    public const int MaxChunkChars = 8000;

    private const string OutputRule =
        "Chỉ trả về nội dung chương đã biên tập, giữ nguyên thứ tự và cách chia đoạn, "
        + "không thêm tiêu đề, lời bình hay giải thích.";

    private readonly IApplicationDbContext _context;
    private readonly AIPromptRunner _runner;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<EditEpisodeContentCommandHandler> _logger;

    public EditEpisodeContentCommandHandler(IApplicationDbContext context, AIPromptRunner runner, TimeProvider timeProvider,
        ILogger<EditEpisodeContentCommandHandler> logger)
    {
        _context = context;
        _runner = runner;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task Handle(EditEpisodeContentCommand request, CancellationToken cancellationToken)
    {
        var chapter = await _context.Episodes
            .Include(x => x.Work!).ThenInclude(x => x.StorySource)
            .FirstOrDefaultAsync(x => x.Id == request.ChapterId, cancellationToken);

        if (chapter is null || string.IsNullOrWhiteSpace(chapter.ContentRaw)) return;
        if (chapter.Status == EpisodeStatus.Edited && !request.Force) return;

        if (!request.Force
            && !await WebSourceEditPolicy.IsNeedEditAsync(_context, chapter.Work?.StorySource?.WebSourceId, cancellationToken))
        {
            EpisodeContentStatus.KeepOriginal(chapter, _timeProvider.GetUtcNow());
            await TytUploadQueue.QueueIfImmediateAsync(_context, chapter, chapter.Work, _timeProvider.GetUtcNow(), cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        var owner = chapter.Work?.CreatedBy ?? chapter.CreatedBy;
        var prompt = await _runner.LatestPromptAsync(owner, PromptTypeCodes.EditChapterContent, cancellationToken);

        if (prompt is null)
        {
            // Không phải lỗi của chương: giữ nội dung gốc, người dùng thêm prompt rồi bấm biên tập.
            chapter.Status = EpisodeStatus.Fetched;
            chapter.Error = $"Chưa có prompt loại {PromptTypeCodes.EditChapterContent} — chương giữ nội dung gốc.";
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        var ais = await _runner.AvailableAIsAsync(owner, cancellationToken);
        var chunks = SplitIntoChunks(chapter.ContentRaw, MaxChunkChars);

        var edited = new List<string>();
        try
        {
            for (var i = 0; i < chunks.Count; i++)
            {
                var part = chunks.Count == 1 ? "" : $"\n\n(Đây là phần {i + 1}/{chunks.Count} của chương — chỉ biên tập phần này.)";
                var input = $"Chương: {chapter.NameRaw ?? chapter.Title}\n\n{chunks[i]}";
                edited.Add((await _runner.RunAsync(prompt, ais, prompt.Content + "\n\n" + OutputRule + part, input,
                    cancellationToken)).Trim());
            }
        }
        catch (AIProviderException ex)
        {
            _logger.LogWarning("Chapter {ChapterId}: AI biên tập lỗi: {Error}", chapter.Id, ex.Message);
            chapter.Status = EpisodeStatus.EditFailed;
            chapter.Error = ex.Message.Length <= 2000 ? ex.Message : ex.Message[..2000];
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        chapter.ContentEdit = string.Join('\n', edited);
        chapter.PromptId = prompt.Id;
        chapter.Status = EpisodeStatus.Edited;
        chapter.EditedAt = _timeProvider.GetUtcNow();
        chapter.Error = null;
        await TytUploadQueue.QueueIfImmediateAsync(_context, chapter, chapter.Work, _timeProvider.GetUtcNow(), cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Chia theo dòng (đoạn văn) thành các phần ≤ <paramref name="maxChars"/>; đoạn quá dài thì cắt cứng.</summary>
    public static List<string> SplitIntoChunks(string content, int maxChars)
    {
        var chunks = new List<string>();
        var current = new StringBuilder();

        foreach (var line in content.Split('\n'))
        {
            for (var start = 0; start < Math.Max(line.Length, 1); start += maxChars)
            {
                var piece = line.Length == 0 ? "" : line.Substring(start, Math.Min(maxChars, line.Length - start));
                if (current.Length > 0 && current.Length + 1 + piece.Length > maxChars)
                {
                    chunks.Add(current.ToString());
                    current.Clear();
                }

                if (current.Length > 0) current.Append('\n');
                current.Append(piece);
            }
        }

        if (current.Length > 0) chunks.Add(current.ToString());
        return chunks;
    }
}
