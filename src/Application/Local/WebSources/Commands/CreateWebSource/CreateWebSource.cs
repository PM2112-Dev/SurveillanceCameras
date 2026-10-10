using SurveillanceCameras.Domain.Enums;
using FluentValidation;
using MediatR;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.WebSources.IntegrationEvents;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Local.WebSources.Commands.CreateWebSource;

public record CreateWebSourceCommand : IRequest<int>
{
    public string? Title { get; init; }

    public string? BaseUrl { get; init; }

    public WebSourceType? Type { get; init; }

    public bool IsCookie { get; init; }

    /// <summary>Tài khoản cung cấp cookie; bắt buộc khi <see cref="IsCookie"/> bật, bị bỏ qua khi tắt.</summary>
    public int? AccountId { get; init; }

    /// <summary>Truyện từ nguồn này cần AI biên tập lại; tắt thì giữ nguyên nội dung gốc.</summary>
    public bool IsNeedEdit { get; init; } = true;

    /// <summary>Tài khoản TYT tự đăng truyện tạo từ nguồn này; để trống thì không đăng.</summary>
    public int? TytAccountId { get; init; }

    /// <summary>Cấu hình crawl dạng JSON; bỏ trống thì dùng repository viết bằng code theo <see cref="Type"/>.</summary>
    public string? CrawlConfig { get; init; }
}

public class CreateWebSourceCommandHandler : IRequestHandler<CreateWebSourceCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public CreateWebSourceCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<int> Handle(CreateWebSourceCommand request, CancellationToken cancellationToken)
    {
        if (request.IsCookie)
        {
            var accountExists = await _context.Accounts
                .AnyAsync(x => x.Id == request.AccountId && x.CreatedBy == _user.Id, cancellationToken);

            if (!accountExists) throw new NotFoundException(request.AccountId.ToString()!, nameof(Account));
        }

        await EnsureOwnAccountAsync(request.TytAccountId, cancellationToken);

        var entity = new WebSource
        {
            Title = request.Title,
            BaseUrl = request.BaseUrl,
            Type = request.Type,
            IsCookie = request.IsCookie,
            AccountId = request.IsCookie ? request.AccountId : null,
            IsNeedEdit = request.IsNeedEdit,
            TytAccountId = request.TytAccountId,
            CrawlConfig = string.IsNullOrWhiteSpace(request.CrawlConfig) ? null : request.CrawlConfig
        };

        await _context.ExecuteInTransactionAsync(async ct =>
        {
            _context.WebSources.Add(entity);
            await _context.SaveChangesAsync(ct);

            entity.AddIntegrationEvent(
                new FetchStoryWebSourceIntegrationEvent
                {
                    WebSourceId = entity.Id, 
                    LinkRaw = entity.BaseUrl
                }, ct);
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    private async Task EnsureOwnAccountAsync(int? accountId, CancellationToken cancellationToken)
    {
        if (accountId is null) return;

        var exists = await _context.Accounts.AnyAsync(x => x.Id == accountId && x.CreatedBy == _user.Id, cancellationToken);
        if (!exists) throw new NotFoundException(accountId.ToString()!, nameof(Account));
    }
}
