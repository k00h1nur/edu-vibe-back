using LMS.Application;
using LMS.Infrastructure;
using LMS.WebApi.Extensions;
using LMS.WebApi.Middleware;
using LMS.WebApi.Security;
using Serilog;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));
builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorizationPolicies();
builder.Services.AddSwagger();
builder.Services.AddHostedService<PermissionDiscoveryHostedService>();
// Must run AFTER PermissionDiscoveryHostedService — hosted services start in registration order.
builder.Services.AddHostedService<RolePermissionSeederHostedService>();
var redisConn = builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConn))
{
    builder.Services.AddStackExchangeRedisCache(o => o.Configuration = redisConn);
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<RequestResponseLoggingMiddleware>();
var uploadPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");

if (!Directory.Exists(uploadPath))
{
    Directory.CreateDirectory(uploadPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadPath),
    RequestPath = "/uploads"
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
 