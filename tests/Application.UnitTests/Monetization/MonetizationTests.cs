using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using SurveillanceCameras.Application.Common.Exceptions;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Local.App;
using SurveillanceCameras.Application.Local.App.Commands;
using SurveillanceCameras.Application.Local.App.Monetization;
using SurveillanceCameras.Application.Local.App.Monetization.Commands;
using SurveillanceCameras.Application.Local.App.Monetization.Queries;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Domain.Enums;
using SurveillanceCameras.Infrastructure.Data;

namespace SurveillanceCameras.Application.UnitTests.Monetization;

public class MonetizationTests
{
    private static readonly string Viewer = Guid.NewGuid().ToString();
    private static readonly string Other = Guid.NewGuid().ToString();
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 8, 0, 0, TimeSpan.Zero);
    private static readonly IOptions<AppOptions> Options =
        Microsoft.Extensions.Options.Options.Create(new AppOptions { FreeEpisodes = 2, AdEpisodes = 2 });

    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static IUser User(string? id) => Mock.Of<IUser>(x => x.Id == id);

    private static async Task<bool> IsVipAsync(ApplicationDbContext db, string userId, DateTimeOffset at)
        => await db.IsVipAsync(userId, at, CancellationToken.None);

    // ── App Store ──

    private sealed class FakeAppStore : IAppStoreVerifier
    {
        public readonly Dictionary<string, AppStoreTransaction> Transactions = [];
        public readonly Dictionary<string, AppStoreNotification> Notifications = [];
        public AppStoreTransaction VerifyTransaction(string? signed) => Transactions.TryGetValue(signed ?? "", out var t) ? t : throw new StoreVerificationException("sai chữ ký");
        public AppStoreRenewal VerifyRenewal(string? signed) => throw new NotSupportedException();
        public AppStoreNotification VerifyNotification(string? signed) => Notifications[signed!];
    }

    private static AppStoreTransaction Apple(string original, DateTimeOffset expires, string? owner = null, DateTimeOffset? revoked = null)
        => new($"{original}-{expires.ToUnixTimeSeconds()}", original, "luma.vip.month", "com.aica.movie", Now, expires, revoked,
            owner is null ? null : StoreAccount.TokenFor(owner), true);

    private static async Task SeedPlansAsync(ApplicationDbContext db)
    {
        db.AppSubscriptionPlans.Add(new AppSubscriptionPlan
        {
            Code = "month", Title = "Gói tháng", PeriodDays = 30, AppleProductId = "luma.vip.month", GoogleProductId = "vip_month",
            IsHighlighted = true, SortOrder = 1
        });
        db.AppSubscriptionPlans.Add(new AppSubscriptionPlan { Code = "year", Title = "Gói năm", PeriodDays = 365, GoogleProductId = "vip_year", SortOrder = 2 });
        db.AppSubscriptionPlans.Add(new AppSubscriptionPlan { Code = "old", Title = "Ngừng bán", PeriodDays = 7, AppleProductId = "x", IsActive = false });
        await db.SaveChangesAsync();
    }

    [Test]
    public async Task AppStorePurchase_GrantsVip_OnlyToTheAccountThatBoughtIt()
    {
        using var db = TestDb.Create();
        await SeedPlansAsync(db);
        var store = new FakeAppStore();
        store.Transactions["mine"] = Apple("100", Now.AddDays(30), owner: Viewer);
        store.Transactions["mine-older"] = Apple("100", Now.AddDays(-1), owner: Viewer);
        store.Transactions["someone-elses"] = Apple("200", Now.AddDays(30), owner: Other);
        var clock = new Clock(Now);
        VerifyAppStorePurchaseCommandHandler Handler(string user) => new(db, User(user), store, clock);

        // Khôi phục gửi nhiều kỳ của cùng một gói: lấy kỳ mới nhất.
        await Handler(Viewer).Handle(new(["mine-older", "mine"]), CancellationToken.None);
        var subscription = db.AppSubscriptions.Single();
        subscription.UserId.ShouldBe(Viewer);
        subscription.Source.ShouldBe(SubscriptionSource.AppStore);
        subscription.Plan.ShouldBe("month");
        subscription.ExpiresAt.ShouldBe(Now.AddDays(30));
        (await IsVipAsync(db, Viewer, Now)).ShouldBeTrue();

        // Cùng gói đó gửi từ tài khoản khác, hoặc gói người khác mua: không nhận.
        (await Should.ThrowAsync<AppRequestException>(() => Handler(Other).Handle(new(["mine"]), CancellationToken.None))).StatusCode.ShouldBe(409);
        (await Should.ThrowAsync<AppRequestException>(() => Handler(Viewer).Handle(new(["someone-elses"]), CancellationToken.None))).StatusCode.ShouldBe(409);
        await Should.ThrowAsync<StoreVerificationException>(() => Handler(Viewer).Handle(new(["forged"]), CancellationToken.None));
        db.AppSubscriptions.Count().ShouldBe(1);
        (await IsVipAsync(db, Other, Now)).ShouldBeFalse();
    }

    [Test]
    public async Task AppStoreNotifications_RenewExtend_GraceKeepsAccess_RefundRevokes_DuplicatesAreIgnored()
    {
        using var db = TestDb.Create();
        var store = new FakeAppStore();
        var clock = new Clock(Now);
        var handler = new HandleAppStoreNotificationCommandHandler(db, store, clock, NullLogger<HandleAppStoreNotificationCommandHandler>.Instance);
        Task Send(string id) => handler.Handle(new(id), CancellationToken.None);

        // Gói mua ở thiết bị chưa kịp gửi lên: gắn theo appAccountToken.
        store.Notifications["n1"] = new("n1", "SUBSCRIBED", null, "com.aica.movie", true, Apple("300", Now.AddDays(30), Viewer), new(true, false, null));
        await Send("n1");
        (await IsVipAsync(db, Viewer, Now)).ShouldBeTrue();

        store.Notifications["n2"] = new("n2", "DID_RENEW", null, "com.aica.movie", true, Apple("300", Now.AddDays(60), Viewer), new(true, false, null));
        await Send("n2");
        // Thông báo cũ tới muộn không làm lùi hạn.
        store.Notifications["n0"] = new("n0", "SUBSCRIBED", null, "com.aica.movie", true, Apple("300", Now.AddDays(30), Viewer), new(true, false, null));
        await Send("n0");
        db.AppSubscriptions.Single().ExpiresAt.ShouldBe(Now.AddDays(60));

        // Hết kỳ, gia hạn lỗi nhưng còn ân hạn.
        clock.Now = Now.AddDays(61);
        store.Notifications["n3"] = new("n3", "DID_FAIL_TO_RENEW", "GRACE_PERIOD", "com.aica.movie", true, Apple("300", Now.AddDays(60), Viewer),
            new(true, true, Now.AddDays(66)));
        await Send("n3");
        db.AppSubscriptions.Single().Status.ShouldBe(SubscriptionStatus.GracePeriod);
        (await IsVipAsync(db, Viewer, clock.Now)).ShouldBeTrue();

        store.Notifications["n4"] = new("n4", "REFUND", null, "com.aica.movie", true, Apple("300", Now.AddDays(60), Viewer, revoked: Now.AddDays(61)), null);
        await Send("n4");
        await Send("n4");
        db.AppSubscriptions.Single().Status.ShouldBe(SubscriptionStatus.Revoked);
        (await IsVipAsync(db, Viewer, clock.Now)).ShouldBeFalse();
        db.AppStoreEvents.Count().ShouldBe(5);
    }

    // ── Google Play ──

    private sealed class FakeGooglePlay : IGooglePlayClient
    {
        public readonly Dictionary<string, GooglePlaySubscription> States = [];
        public readonly List<string> Acknowledged = [];
        public Task<GooglePlaySubscription> GetSubscriptionAsync(string? token, CancellationToken ct)
            => States.TryGetValue(token ?? "", out var s) ? Task.FromResult(s) : throw new StoreVerificationException("không có");
        public Task AcknowledgeAsync(string productId, string token, CancellationToken ct) { Acknowledged.Add(token); return Task.CompletedTask; }
        public bool IsNotificationTokenValid(string? token) => token == "secret";
    }

    private static GooglePlaySubscription Google(string token, string state, DateTimeOffset expires, string? owner, string? linked = null,
        bool acknowledged = false)
        => new(token, linked, state, "vip_month", expires, state == "SUBSCRIPTION_STATE_ACTIVE", acknowledged,
            owner is null ? null : StoreAccount.TokenFor(owner).ToString(), true);

    private static string PubSub(object data) => Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data)));

    [Test]
    public async Task GooglePlayPurchase_IsAcknowledgedAfterGrantingVip_AndUpgradeKeepsOneSubscription()
    {
        using var db = TestDb.Create();
        await SeedPlansAsync(db);
        var play = new FakeGooglePlay();
        var clock = new Clock(Now);
        var purchase = new VerifyGooglePlayPurchaseCommandHandler(db, User(Viewer), play, clock);

        play.States["t1"] = Google("t1", "SUBSCRIPTION_STATE_ACTIVE", Now.AddDays(30), Viewer);
        await purchase.Handle(new(["t1"]), CancellationToken.None);
        play.Acknowledged.ShouldBe(["t1"]);
        db.AppSubscriptions.Single().Plan.ShouldBe("month");
        (await IsVipAsync(db, Viewer, Now)).ShouldBeTrue();

        // Đổi gói: token mới thay token cũ, vẫn một dòng.
        play.States["t2"] = Google("t2", "SUBSCRIPTION_STATE_ACTIVE", Now.AddDays(365), Viewer, linked: "t1", acknowledged: true);
        await purchase.Handle(new(["t2"]), CancellationToken.None);
        var subscription = db.AppSubscriptions.Single();
        (subscription.ExternalId, subscription.PreviousExternalId, subscription.ExpiresAt).ShouldBe(("t2", "t1", Now.AddDays(365)));
        play.Acknowledged.ShouldBe(["t1"]);

        play.States["t3"] = Google("t3", "SUBSCRIPTION_STATE_ACTIVE", Now.AddDays(30), Other);
        (await Should.ThrowAsync<AppRequestException>(() => purchase.Handle(new(["t3"]), CancellationToken.None))).StatusCode.ShouldBe(409);
    }

    [Test]
    public async Task GooglePlayNotifications_OnHoldRemovesAccess_RecoveryRestores_VoidedRevokes()
    {
        using var db = TestDb.Create();
        var play = new FakeGooglePlay();
        var clock = new Clock(Now);
        var handler = new HandleGooglePlayNotificationCommandHandler(db, play, clock, NullLogger<HandleGooglePlayNotificationCommandHandler>.Instance);
        Task Send(string messageId, object data) => handler.Handle(new(messageId, PubSub(data)), CancellationToken.None);
        object Sub(int type) => new { packageName = "com.mte.movie.movieai", subscriptionNotification = new { notificationType = type, purchaseToken = "t1", subscriptionId = "vip_month" } };

        play.States["t1"] = Google("t1", "SUBSCRIPTION_STATE_ACTIVE", Now.AddDays(30), Viewer);
        await Send("m1", Sub(4));
        (await IsVipAsync(db, Viewer, Now)).ShouldBeTrue();
        play.Acknowledged.ShouldBe(["t1"]);

        play.States["t1"] = Google("t1", "SUBSCRIPTION_STATE_ON_HOLD", Now.AddDays(30), Viewer, acknowledged: true);
        await Send("m2", Sub(5));
        (await IsVipAsync(db, Viewer, Now)).ShouldBeFalse();

        play.States["t1"] = Google("t1", "SUBSCRIPTION_STATE_ACTIVE", Now.AddDays(35), Viewer, acknowledged: true);
        await Send("m3", Sub(1));
        (await IsVipAsync(db, Viewer, Now)).ShouldBeTrue();

        await Send("m4", new { voidedPurchaseNotification = new { purchaseToken = "t1", orderId = "GPA.1" } });
        await Send("m4", new { voidedPurchaseNotification = new { purchaseToken = "t1", orderId = "GPA.1" } });
        db.AppSubscriptions.Single().Status.ShouldBe(SubscriptionStatus.Revoked);
        (await IsVipAsync(db, Viewer, Now)).ShouldBeFalse();
        db.AppStoreEvents.Count().ShouldBe(4);
    }

    [Test]
    public async Task AdminGrant_DoesNotTouchStoreSubscriptions_AndVipLastsUntilTheLatestOne()
    {
        using var db = TestDb.Create();
        db.AppSubscriptions.Add(new AppSubscription { UserId = Viewer, Source = SubscriptionSource.GooglePlay, ExternalId = "t1", ExpiresAt = Now.AddDays(10) });
        await db.SaveChangesAsync();

        await new SetAppViewerVipCommandHandler(db).Handle(new(Viewer, "gift", Now.AddDays(3)), CancellationToken.None);
        await new SetAppViewerVipCommandHandler(db).Handle(new(Viewer, "gift", Now.AddDays(40)), CancellationToken.None);

        db.AppSubscriptions.Count().ShouldBe(2);
        (await db.ActiveSubscriptionAsync(Viewer, Now, CancellationToken.None))!.Plan.ShouldBe("gift");
        db.AppSubscriptions.Single(x => x.Source == SubscriptionSource.GooglePlay).ExpiresAt.ShouldBe(Now.AddDays(10));
    }

    // ── Quảng cáo ──

    private static async Task<(ApplicationDbContext Db, Episode AdEpisode, Episode OtherAdEpisode)> SeedMovieAsync(bool requireVerification,
        int dailyLimit = 0)
    {
        var db = TestDb.Create();
        var movie = new Work
        {
            Title = "Phim", Type = WorkType.Movie, IsPublished = true,
            Episodes = Enumerable.Range(1, 5).Select(n => new Episode { EpisodeNumber = n, ExternalId = $"e{n}" }).ToList()
        };
        db.Works.Add(movie);
        db.AppMonetizationSettings.Add(new AppMonetizationSettings { RequireAdVerification = requireVerification });
        db.AppAdPlacements.Add(new AppAdPlacement { Code = AppAdPlacement.RewardedUnlock, Format = AdFormat.Rewarded, AndroidUnitId = "a", DailyLimit = dailyLimit });
        await db.SaveChangesAsync();
        var episodes = movie.Episodes.OrderBy(x => x.EpisodeNumber).ToList();
        return (db, episodes[2], episodes[3]);
    }

    private static Task<AppEpisodeDto> UnlockAsync(ApplicationDbContext db, int episodeId, TimeProvider? clock = null)
        => new UnlockAppEpisodeCommandHandler(db, User(Viewer), clock ?? new Clock(Now), Options).Handle(new(episodeId), CancellationToken.None);

    private sealed class FakeAdMob(params AdMobReward[] rewards) : IAdMobRewardVerifier
    {
        public Task<AdMobReward> VerifyAsync(string? query, CancellationToken ct) => Task.FromResult(rewards.Single(x => x.TransactionId == query));
    }

    [Test]
    public async Task WithVerificationOn_AnEpisodeOpensOnlyAfterAdMobConfirmsTheViewForThatEpisode()
    {
        var (db, episode, other) = await SeedMovieAsync(requireVerification: true);
        using var _ = db;

        (await Should.ThrowAsync<AppRequestException>(() => UnlockAsync(db, episode.Id))).Code.ShouldBe("ad_reward_pending");

        var ssv = new RecordAdRewardCommandHandler(db, new FakeAdMob(
            new AdMobReward("tx-other", Viewer, other.Id.ToString(), "u", Now),
            new AdMobReward("tx-1", Viewer, episode.Id.ToString(), "u", Now),
            new AdMobReward("tx-test", null, null, "u", Now)));
        await ssv.Handle(new("tx-other"), CancellationToken.None);
        await ssv.Handle(new("tx-test"), CancellationToken.None); // nút kiểm tra URL của AdMob: không ghi
        (await Should.ThrowAsync<AppRequestException>(() => UnlockAsync(db, episode.Id))).Code.ShouldBe("ad_reward_pending");

        await ssv.Handle(new("tx-1"), CancellationToken.None);
        await ssv.Handle(new("tx-1"), CancellationToken.None);
        db.AppAdRewards.Count().ShouldBe(2);

        (await UnlockAsync(db, episode.Id)).Access.ShouldBe(AppAccess.Free);
        db.AppAdRewards.Single(x => x.TransactionId == "tx-1").ConsumedAt.ShouldBe(Now);
        // Lượt cũ quá một giờ thì hết dùng được.
        (await Should.ThrowAsync<AppRequestException>(() => UnlockAsync(db, other.Id, new Clock(Now.AddHours(2))))).Code.ShouldBe("ad_reward_pending");
    }

    [Test]
    public async Task DailyLimit_StopsAdUnlocks_ButVerificationOffStillWorksWithoutAdMob()
    {
        var (db, episode, other) = await SeedMovieAsync(requireVerification: false, dailyLimit: 1);
        using var _ = db;

        (await UnlockAsync(db, episode.Id)).Access.ShouldBe(AppAccess.Free);
        // Mở lại tập đã mở thì không tính thêm.
        (await UnlockAsync(db, episode.Id)).Access.ShouldBe(AppAccess.Free);
        // TestDb không có interceptor ghi Created: đặt giờ mở cho đúng ngày.
        db.AppEpisodeUnlocks.Single().Created = Now;
        await db.SaveChangesAsync();

        var limit = await Should.ThrowAsync<AppRequestException>(() => UnlockAsync(db, other.Id));
        (limit.StatusCode, limit.Code).ShouldBe((429, "ad_limit_reached"));
        (await UnlockAsync(db, other.Id, new Clock(Now.AddDays(1)))).Access.ShouldBe(AppAccess.Free);
    }

    // ── Cấu hình từ xa ──

    [Test]
    public async Task RemoteConfig_GivesPlatformUnitsAndProducts_AndVipGetsNoAds()
    {
        using var db = TestDb.Create();
        await SeedPlansAsync(db);
        db.AppSubscriptions.Add(new AppSubscription { UserId = Other, Source = SubscriptionSource.Admin, ExpiresAt = Now.AddDays(1) });
        await db.SaveChangesAsync();
        Task<AppRemoteConfigDto> ConfigAsync(string? user, string platform)
            => new GetAppRemoteConfigQueryHandler(db, User(user), new Clock(Now), Options).Handle(new(platform), CancellationToken.None);

        // Chưa khai báo vị trí nào: dùng ad unit thử nghiệm của Google.
        var android = await ConfigAsync(Viewer, "android");
        android.AdsEnabled.ShouldBeTrue();
        android.Placements.Select(x => (x.Code, x.Format)).ShouldBe([
            ("banner_home", "banner"), ("interstitial_episode", "interstitial"), ("rewarded_unlock", "rewarded")]);
        android.Placements.Single(x => x.Code == "rewarded_unlock").UnitId.ShouldBe("ca-app-pub-3940256099942544/5224354917");
        android.Plans.Select(x => (x.Code, x.ProductId)).ShouldBe([("month", "vip_month"), ("year", "vip_year")]);

        var ios = await ConfigAsync(null, "IOS");
        ios.Placements.Single(x => x.Code == "rewarded_unlock").UnitId.ShouldBe("ca-app-pub-3940256099942544/1712485313");
        ios.Plans.Select(x => x.ProductId).ShouldBe(["luma.vip.month"]);
        ios.FreeEpisodes.ShouldBe(2);

        var vip = await ConfigAsync(Other, "android");
        vip.AdsEnabled.ShouldBeFalse();
        vip.Placements.ShouldBeEmpty();
        vip.PurchasesEnabled.ShouldBeTrue();

        db.AppMonetizationSettings.Add(new AppMonetizationSettings { AdsEnabled = false, PurchasesEnabled = false, PaywallVariant = "C" });
        await db.SaveChangesAsync();
        var off = await ConfigAsync(Viewer, "android");
        (off.AdsEnabled, off.PurchasesEnabled, off.PaywallVariant).ShouldBe((false, false, "C"));
    }

    [Test]
    public async Task StoreAccountToken_IsTheUserIdWhenItIsAGuid_AndStableOtherwise()
    {
        var id = Guid.NewGuid();
        StoreAccount.TokenFor(id.ToString()).ShouldBe(id);
        StoreAccount.UserIdFor(id).ShouldBe(id.ToString());
        StoreAccount.TokenFor("admin-1").ShouldBe(StoreAccount.TokenFor("admin-1"));
        StoreAccount.TokenFor("admin-1").ShouldNotBe(StoreAccount.TokenFor("admin-2"));
        await Task.CompletedTask;
    }

    // ── Quảng cáo: nhiều vị trí, màn hình, thống kê ──

    [Test]
    public async Task RemoteConfig_OrdersByPriority_AndDropsPlacementsOfDisabledScreens()
    {
        using var db = TestDb.Create();
        db.AppAdScreens.AddRange(new AppAdScreen { Code = "home", Title = "Home" }, new AppAdScreen { Code = "search", Title = "Search", IsEnabled = false });
        db.AppAdPlacements.AddRange(
            new AppAdPlacement { Code = "native_b", Format = AdFormat.Native, AndroidUnitId = "b", ScreenCode = "home", Priority = 2, DisplaySeconds = 15, EveryItems = 6 },
            new AppAdPlacement { Code = "native_a", Format = AdFormat.Native, AndroidUnitId = "a", ScreenCode = "home", Priority = 1 },
            new AppAdPlacement { Code = "native_search", Format = AdFormat.Native, AndroidUnitId = "s", ScreenCode = "search" },
            new AppAdPlacement { Code = "app_open", Format = AdFormat.AppOpen, AndroidUnitId = "o" },
            new AppAdPlacement { Code = "off", Format = AdFormat.Banner, AndroidUnitId = "x", ScreenCode = "home", IsEnabled = false });
        await db.SaveChangesAsync();

        var config = await new GetAppRemoteConfigQueryHandler(db, User(Viewer), new Clock(Now), Options).Handle(new("android"), CancellationToken.None);

        config.Placements.Select(x => x.Code).ShouldBe(["app_open", "native_a", "native_b"]);
        var b = config.Placements.Single(x => x.Code == "native_b");
        (b.Format, b.Screen, b.DisplaySeconds, b.EveryItems).ShouldBe(("native", "home", 15, 6));
        config.Placements.Single(x => x.Code == "app_open").Format.ShouldBe("app_open");
    }

    [Test]
    public async Task RecordAdEvents_AccumulatesPerDayPlacementScreenAndPlatform_AndIgnoresUnknownPlacements()
    {
        using var db = TestDb.Create();
        db.AppAdPlacements.Add(new AppAdPlacement { Code = "native_home", Format = AdFormat.Native, AndroidUnitId = "a", ScreenCode = "home" });
        await db.SaveChangesAsync();
        var handler = new RecordAdEventsCommandHandler(db, new Clock(Now));

        await handler.Handle(new("android", [
            new AdEventItem { Placement = "native_home", Screen = "home", Requests = 3, Filled = 2, Failed = 1, Error = "no fill" },
            new AdEventItem { Placement = "native_home", Screen = "home", Impressions = 2, Clicks = 1, RevenueMicros = 5000, Currency = "usd" },
            new AdEventItem { Placement = "does_not_exist", Screen = "home", Requests = 9 }]), CancellationToken.None);
        await handler.Handle(new("android", [new AdEventItem { Placement = "native_home", Screen = "home", Requests = 1, Impressions = 1 }]), CancellationToken.None);
        await handler.Handle(new("ios", [new AdEventItem { Placement = "native_home", Screen = "search", Requests = 4 }]), CancellationToken.None);

        var rows = await db.AppAdStats.AsNoTracking().OrderBy(x => x.Platform).ThenBy(x => x.ScreenCode).ToListAsync();
        rows.Count.ShouldBe(2);
        var android = rows[0];
        (android.Platform, android.ScreenCode, android.Format).ShouldBe(("android", "home", AdFormat.Native));
        (android.Requests, android.Filled, android.Failed, android.Impressions, android.Clicks, android.RevenueMicros)
            .ShouldBe((4, 2, 1, 3, 1, 5000));
        android.LastError.ShouldBe("no fill");
        (rows[1].Platform, rows[1].ScreenCode, rows[1].Requests).ShouldBe(("ios", "search", 4));
    }

    [Test]
    public async Task RecordAdEvents_DoesNothingWhenStatsAreSwitchedOff()
    {
        using var db = TestDb.Create();
        db.AppAdPlacements.Add(new AppAdPlacement { Code = "native_home", Format = AdFormat.Native, AndroidUnitId = "a", ScreenCode = "home" });
        db.AppMonetizationSettings.Add(new AppMonetizationSettings { AdStatsEnabled = false });
        await db.SaveChangesAsync();

        await new RecordAdEventsCommandHandler(db, new Clock(Now))
            .Handle(new("android", [new AdEventItem { Placement = "native_home", Screen = "home", Requests = 1 }]), CancellationToken.None);

        (await db.AppAdStats.CountAsync()).ShouldBe(0);
    }

    [Test]
    public async Task AdStats_GroupsByPlacementScreenAndDay_AndComparesWithThePreviousPeriod()
    {
        using var db = TestDb.Create();
        var today = DateOnly.FromDateTime(Now.UtcDateTime);
        db.AppAdPlacements.Add(new AppAdPlacement { Code = "native_home", Title = "Native Home", Format = AdFormat.Native, AndroidUnitId = "a", ScreenCode = "home" });
        db.AppAdScreens.Add(new AppAdScreen { Code = "home", Title = "Khám phá" });
        db.AppAdStats.AddRange(
            new AppAdStat { Day = today, PlacementCode = "native_home", ScreenCode = "home", Platform = "android", Format = AdFormat.Native, Requests = 10, Filled = 8, Impressions = 6, Clicks = 1, RevenueMicros = 2000 },
            new AppAdStat { Day = today.AddDays(-1), PlacementCode = "native_home", ScreenCode = "home", Platform = "ios", Format = AdFormat.Native, Requests = 5, Filled = 5, Impressions = 5 },
            new AppAdStat { Day = today.AddDays(-2), PlacementCode = "native_home", ScreenCode = "home", Platform = "ios", Format = AdFormat.Native, Requests = 7, Impressions = 7 });
        await db.SaveChangesAsync();
        var handler = new GetAdStatsQueryHandler(db, new Clock(Now));

        var stats = await handler.Handle(new(today.AddDays(-1), today, null, null, null, null), CancellationToken.None);
        (stats.Totals.Requests, stats.Totals.Impressions, stats.Totals.Revenue["USD"]).ShouldBe((15, 11, 2000));
        stats.Previous.Requests.ShouldBe(7);
        stats.ByDay.Select(x => x.Totals.Requests).ShouldBe([5, 10]);
        var placement = stats.ByPlacement.Single();
        (placement.Key, placement.Label, placement.Format).ShouldBe(("native_home", "Native Home", "native"));
        stats.ByScreen.Single().Label.ShouldBe("Khám phá");
        stats.ByPlatform.Select(x => (x.Key, x.Totals.Requests)).ShouldBe([("android", 10), ("ios", 5)]);

        var ios = await handler.Handle(new(today.AddDays(-1), today, "ios", null, null, "native"), CancellationToken.None);
        ios.Totals.Requests.ShouldBe(5);
    }

    [Test]
    public async Task AdPlacements_CanHaveSeveralOfTheSameFormat_ButBannerAndNativeNeedAScreen()
    {
        using var db = TestDb.Create();
        db.AppAdScreens.Add(new AppAdScreen { Code = "home", Title = "Home" });
        await db.SaveChangesAsync();
        var validator = new SaveAdPlacementCommandValidator(db);
        var handler = new SaveAdPlacementCommandHandler(db);

        SaveAdPlacementCommand Native(string code, string? screen) => new()
        {
            Code = code, Title = code, Format = "native", AndroidUnitId = "ca-app-pub-1234567890123456/1234567890", ScreenCode = screen
        };

        (await validator.ValidateAsync(Native("native_1", null))).IsValid.ShouldBeFalse();
        (await validator.ValidateAsync(Native("native_1", "ghost"))).IsValid.ShouldBeFalse();
        await handler.Handle(Native("native_1", "home"), CancellationToken.None);
        (await validator.ValidateAsync(Native("native_1", "home"))).IsValid.ShouldBeFalse(); // trùng mã
        var second = Native("native_2", "home");
        (await validator.ValidateAsync(second)).IsValid.ShouldBeTrue();
        await handler.Handle(second with { Priority = 2, DisplaySeconds = 20, EveryItems = 5 }, CancellationToken.None);

        var list = await new GetAdPlacementsQueryHandler(db).Handle(new(), CancellationToken.None);
        list.Where(x => x.Format == "native" && x.ScreenCode == "home").Select(x => x.Code).ShouldContain("native_1");
        list.Single(x => x.Code == "native_2").DisplaySeconds.ShouldBe(20);

        await Should.ThrowAsync<ValidationException>(() => new DeleteAdScreenCommandHandler(db)
            .Handle(new(db.AppAdScreens.Single(x => x.Code == "home").Id), CancellationToken.None));
    }
}
