using LyftClient.Services;
using InternalAPI;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Security.Cryptography.X509Certificates;
using LyftClient.Interface;
using LyftClient.Internal;
using LyftClient.Filters;
using Microsoft.ApplicationInsights.Extensibility;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddMvc();
builder.Services.AddDistributedRedisCache(options =>
{
    options.Configuration = "https://lyft-redis:6379";
    options.InstanceName = "";
});

builder.Services.AddGrpc();
builder.Services.AddHealthChecks();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<IAccessTokenService, AccessTokenService>();
builder.Services.AddSingleton<IServicesService, ServicesService>();
builder.Services.AddSingleton<ITelemetryInitializer, FilterHealthchecksTelemetryInitializer>();

builder.Services.AddHostedService<ServicesService>();

builder.Services.Configure<ListenOptions>(options =>
{
    options.UseHttps(new X509Certificate2(Path.Combine("/certs/tls.crt"), Path.Combine("/certs/tls.key")));
});

builder.Services.AddGrpcClient<Services.ServicesClient>(o =>
{
    var httpHandler = new HttpClientHandler();
    httpHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    o.Address = new Uri($"https://services.api:443");
});

builder.Services.AddGrpcClient<Users.UsersClient>(o =>
{
    o.Address = new Uri($"https://identity.api:443");
});

var app = builder.Build();
app.UseRouting();

app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/healthz");

app.UseEndpoints(endpoints =>
{
    endpoints.MapGrpcService<EstimatesService>();
    endpoints.MapGrpcService<RequestsService>();
});

app.Run();
