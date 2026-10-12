using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Languages.Commands;

public record CreateLanguageCommand : IRequest<int>
{
    public string? Title { get; init; }

    /// <summary>Mã ngôn ngữ như nền tảng nguồn trả về (vd. <c>en</c>, <c>vi</c>).</summary>
    public string? Code { get; init; }
}

public record UpdateLanguageCommand : IRequest
{
    public int Id { get; init; }

    public string? Title { get; init; }

    public string? Code { get; init; }
}

public class CreateLanguageCommandValidator : AbstractValidator<CreateLanguageCommand>
{
    public CreateLanguageCommandValidator(IApplicationDbContext context, IUser user)
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).ValidLanguageCode()
            .MustAsync((code, ct) => LanguageRules.IsCodeFreeAsync(context, user.Id, code, null, ct))
            .WithMessage("Mã ngôn ngữ này đã được khai báo.");
    }
}

public class UpdateLanguageCommandValidator : AbstractValidator<UpdateLanguageCommand>
{
    public UpdateLanguageCommandValidator(IApplicationDbContext context, IUser user)
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).ValidLanguageCode()
            .MustAsync((command, code, ct) => LanguageRules.IsCodeFreeAsync(context, user.Id, code, command.Id, ct))
            .WithMessage("Mã ngôn ngữ này đã được khai báo.");
    }
}

internal static class LanguageRules
{
    public static IRuleBuilderOptions<T, string?> ValidLanguageCode<T>(this IRuleBuilder<T, string?> rule)
        => rule
            .Must(code => LanguageCodes.Normalize(code) is not null)
            .WithMessage($"Nhập mã ngôn ngữ (vd. en, vi), tối đa {LanguageCodes.MaxCodeLength} ký tự; \"un\" không dùng được.")
            .Matches("^\\s*[A-Za-z0-9_-]+\\s*$").WithMessage("Mã ngôn ngữ chỉ gồm chữ, số, gạch nối.");

    /// <summary>Mỗi người dùng chỉ khai báo một ngôn ngữ cho mỗi mã — đồng bộ tìm ngôn ngữ theo mã.</summary>
    public static async Task<bool> IsCodeFreeAsync(IApplicationDbContext context, string? ownerId, string? code, int? exceptId,
        CancellationToken cancellationToken)
    {
        var normalized = LanguageCodes.Normalize(code);
        if (normalized is null) return true; // đã có lỗi định dạng ở rule trước
        return !await context.Languages.AnyAsync(
            x => x.CreatedBy == ownerId && x.Id != exceptId && x.Code != null && x.Code.ToLower() == normalized,
            cancellationToken);
    }
}

public class CreateLanguageCommandHandler : IRequestHandler<CreateLanguageCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateLanguageCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<int> Handle(CreateLanguageCommand request, CancellationToken cancellationToken)
    {
        var entity = new Language { Title = request.Title!.Trim(), Code = LanguageCodes.Normalize(request.Code) };

        _context.Languages.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

public class UpdateLanguageCommandHandler : IRequestHandler<UpdateLanguageCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateLanguageCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdateLanguageCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Languages
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        entity.Title = request.Title!.Trim();
        entity.Code = LanguageCodes.Normalize(request.Code);

        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Xoá ngôn ngữ; phim và tập đang dùng chỉ mất liên kết.</summary>
public record DeleteLanguageCommand(int Id) : IRequest;

public class DeleteLanguageCommandHandler : IRequestHandler<DeleteLanguageCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteLanguageCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(DeleteLanguageCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Languages
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        _context.Languages.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
