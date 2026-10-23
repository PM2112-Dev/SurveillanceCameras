using Microsoft.AspNetCore.HttpOverrides;
using SurveillanceCameras.Infrastructure.Data;
using Scalar.AspNetCore;
using SurveillanceCameras.Application;
using SurveillanceCameras.Infrastructure;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Infrastructure.Repository.Media;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

// Hỗ trợ nhận đúng HTTPS và địa chỉ IP thật khi đứng sau cả ngrok (test) lẫn Cloudflare Tunnel (production).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
// Development luôn tự áp migration + seed; môi trường khác (vd. Docker) bật bằng Database:InitialiseOnStartup = true.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:InitialiseOnStartup"))
{
    await app.InitialiseDatabaseAsync();
}

if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors(static builder => 
    builder.AllowAnyMethod()
        .AllowAnyHeader()
        .AllowAnyOrigin());

app.UseFileServer();

// Ảnh đã lưu về kho (bìa phim…). Công khai vì thẻ <img> không gửi được JWT; tên file không đoán được nội dung riêng tư.
var mediaRoot = Path.GetFullPath(app.Services.GetRequiredService<IOptions<MediaOptions>>().Value.RootDirectory);
Directory.CreateDirectory(mediaRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(mediaRoot),
    RequestPath = MediaOptions.RequestPath,
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "public,max-age=86400"
});

app.MapOpenApi("/openapi/runtime/{documentName}.json");
app.MapScalarApiReference(options =>
{
    options
        .WithOpenApiRoutePattern("/openapi/runtime/{documentName}.json")
        .AddHttpAuthentication("Bearer", auth => auth.WithToken("<paste-jwt-access-token>"))
        .AddPreferredSecuritySchemes("Bearer");
});

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();


app.MapDefaultEndpoints();
app.MapEndpoints(typeof(Program).Assembly);

// API không tồn tại phải trả 404, không rơi xuống trang index.html của giao diện quản trị (client nhận 200 + HTML).
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

app.Run();
