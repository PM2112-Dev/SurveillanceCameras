using SurveillanceCameras.Infrastructure.Data;
using Scalar.AspNetCore;
using SurveillanceCameras.Application;
using SurveillanceCameras.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabaseAsync();
}
else
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

app.MapFallbackToFile("index.html");

app.Run();
