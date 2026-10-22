using SurveillanceCameras.Domain.Enums;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.Episodes;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Works.Commands.UpdateWork;

public record UpdateWorkCommand : IRequest
{
    public  int Id { get; init; }
    
    public string? Title { get; init; }

    public int? StoryVideoTypeId { get; init; }

    /// <summary>Tài khoản đăng truyện (truyện do AI tạo từ nguồn truyện chưa có, gắn ở đây).</summary>
    public int? AccountId { get; init; }
    
    public string? TytStoryId { get; init; }
    
    public string? Author { get; init; }
    
    public string? Description { get; init; }
    
    public bool IsFull { get; init; }
    
    public bool IsPublished { get; init; }
    
    public bool IsEarning { get; init; }
    
    public bool IsComment { get; init; }
    
    public int ChapterUploaded { get; init; }
    
    public int EarnCount { get; init; }
    
    public int EarnNow { get; init; }
    
    public int AdCount { get; init; }
    
    public int Paid { get; init; }

    // Lịch đăng chương lên TYT — không gửi (null) thì giữ nguyên giá trị đang có.
    public TytUploadMode? TytUploadMode { get; init; }

    public int? TytUploadBatchSize { get; init; }

    public int? TytUploadEveryHours { get; init; }

    /// <summary>Giờ đăng mỗi ngày "HH:mm"; chuỗi rỗng là xoá (dùng giờ mặc định).</summary>
    public string? TytUploadDailyTime { get; init; }

    /// <summary>Số chương tối đa đăng mỗi ngày; 0 là không giới hạn.</summary>
    public int? TytUploadDailyLimit { get; init; }
}

public class UpdateWorkCommandValidator : AbstractValidator<UpdateWorkCommand>
{
    public UpdateWorkCommandValidator()
    {
        RuleFor(x => x.TytUploadBatchSize).InclusiveBetween(1, 100).When(x => x.TytUploadBatchSize.HasValue)
            .WithMessage("Số chương mỗi lượt đăng từ 1 đến 100.");
        RuleFor(x => x.TytUploadEveryHours).InclusiveBetween(1, 168).When(x => x.TytUploadEveryHours.HasValue)
            .WithMessage("Khoảng cách giữa hai lượt đăng từ 1 đến 168 giờ.");
        RuleFor(x => x.TytUploadDailyLimit).InclusiveBetween(0, 1000).When(x => x.TytUploadDailyLimit.HasValue)
            .WithMessage("Số chương mỗi ngày từ 0 (không giới hạn) đến 1000.");
        RuleFor(x => x.TytUploadDailyTime).Must(TytUploadSchedule.IsValidDailyTime)
            .When(x => !string.IsNullOrEmpty(x.TytUploadDailyTime))
            .WithMessage("Giờ đăng mỗi ngày phải có dạng HH:mm, nhiều giờ cách nhau dấu phẩy (vd. 08:00,20:30).");
    }
}

public class UpdateWorkCommandHandler : IRequestHandler<UpdateWorkCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateWorkCommandHandler(IApplicationDbContext context,  IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdateWorkCommand request, CancellationToken cancellationToken)
    {
        var entity = await  _context.Works
            .FirstOrDefaultAsync(x=> x.Id == request.Id && x.CreatedBy == _user.Id && x.Type == WorkType.Story, cancellationToken);
        
        Guard.Against.NotFound(request.Id, entity);

        if (request.AccountId is { } accountId)
        {
            var accountExists = await _context.Accounts
                .AnyAsync(x => x.Id == accountId && x.CreatedBy == _user.Id, cancellationToken);

            if (!accountExists) throw new NotFoundException(accountId.ToString(), nameof(Account));
        }

        if (request.StoryVideoTypeId is { } storyVideoTypeId)
        {
            var videoTypeExists = await _context.StoryVideoTypes
                .AnyAsync(x => x.Id == storyVideoTypeId && x.CreatedBy == _user.Id, cancellationToken);

            if (!videoTypeExists) throw new NotFoundException(storyVideoTypeId.ToString(), nameof(StoryVideoType));
        }
        
        entity.Title = request.Title;
        entity.StoryVideoTypeId = request.StoryVideoTypeId;
        entity.AccountId = request.AccountId;
        entity.TytStoryId = request.TytStoryId;
        entity.Author = request.Author;
        entity.Description = request.Description;
        entity.IsFull = request.IsFull;
        entity.IsPublished = request.IsPublished;
        entity.IsEarning = request.IsEarning;
        entity.IsComment = request.IsComment;
        entity.ChapterUploaded = request.ChapterUploaded;
        entity.EarnCount = request.EarnCount;
        entity.EarnNow = request.EarnNow;
        entity.AdCount = request.AdCount;
        entity.Paid = request.Paid;

        if (request.TytUploadMode is { } mode) entity.TytUploadMode = mode;
        if (request.TytUploadBatchSize is { } batch) entity.TytUploadBatchSize = batch;
        if (request.TytUploadEveryHours is { } hours) entity.TytUploadEveryHours = hours;
        if (request.TytUploadDailyLimit is { } dailyLimit) entity.TytUploadDailyLimit = dailyLimit;
        if (request.TytUploadDailyTime is { } dailyTime) entity.TytUploadDailyTime = dailyTime.Length == 0 ? null : dailyTime;
        
        await _context.SaveChangesAsync(cancellationToken);
    }
}
