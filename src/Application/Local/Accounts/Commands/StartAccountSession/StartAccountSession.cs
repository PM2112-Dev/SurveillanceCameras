using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Repository.AccountSession;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.Accounts.Commands.StartAccountSession;

public record StartAccountSessionCommand(int AccountId) : IRequest;

public class StartAccountSessionCommandValidator : AbstractValidator<StartAccountSessionCommand>
{
    public StartAccountSessionCommandValidator()
    {
    }
}

public class StartAccountSessionCommandHandler : IRequestHandler<StartAccountSessionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IAccountSessionManagerRepository _sessionManagerRepository;

    public StartAccountSessionCommandHandler(
        IApplicationDbContext context, IUser user, IAccountSessionManagerRepository sessionManagerRepository)
    {
        _context = context;
        _user = user;
        _sessionManagerRepository = sessionManagerRepository;
    }

    public async Task Handle(StartAccountSessionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Accounts
            .AsNoTracking()
            .Include(x => x.AccountType)
            .FirstOrDefaultAsync(x => x.Id == request.AccountId && x.CreatedBy == _user.Id, cancellationToken);

        Guard.Against.NotFound(request.AccountId, entity);

        if (string.IsNullOrWhiteSpace(entity.AccountType?.BaseUrl))
        {
            throw new Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(AccountType.BaseUrl),
                $"Loại tài khoản “{entity.AccountType?.Title}” chưa có Base URL (trang đăng nhập) — hãy cập nhật loại tài khoản trước.")]);
        }

        var target = new AccountLoginTarget(entity.AccountType.BaseUrl, entity.AccountType.LoginCookieName);
        await _sessionManagerRepository.StartAsync(request.AccountId, target, cancellationToken);
    }
}
