using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.App.Commands;

/// <summary>Người đang đăng nhập bình luận một phim.</summary>
/// <param name="AuthorName">Tên hiển thị của người viết — do endpoint lấy từ tài khoản, không nhận từ client.</param>
public record AddAppCommentCommand(int MovieId, string? Content, string? AuthorName) : IRequest<AppCommentDto>;

public class AddAppCommentCommandValidator : AbstractValidator<AddAppCommentCommand>
{
    public const int MaxContentLength = 1000;

    public AddAppCommentCommandValidator()
    {
        RuleFor(x => x.Content)
            .Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage("Bình luận không được để trống.")
            .MaximumLength(MaxContentLength).WithMessage($"Bình luận tối đa {MaxContentLength} ký tự.");
    }
}

public class AddAppCommentCommandHandler : IRequestHandler<AddAppCommentCommand, AppCommentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public AddAppCommentCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<AppCommentDto> Handle(AddAppCommentCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.RequireId();
        if (!await _context.PublishedMovies().AnyAsync(x => x.Id == request.MovieId, cancellationToken))
            throw new NotFoundException(request.MovieId.ToString(), "Movie");

        var comment = new AppComment
        {
            WorkId = request.MovieId,
            Content = request.Content!.Trim(),
            AuthorName = request.AuthorName,
            CreatedBy = userId
        };
        _context.AppComments.Add(comment);
        await _context.SaveChangesAsync(cancellationToken);

        return new AppCommentDto
        {
            Id = comment.Id,
            Author = comment.AuthorName,
            Content = comment.Content,
            Created = comment.Created,
            IsMine = true
        };
    }
}
