// using LyftClient.HTTPClient;
using LyftClient.Services;
using InternalAPI;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddMvc();
builder.Services.AddDistributedRedisCache(options =>
{
    options.Configuration = "lyft-redis:6379";
    options.InstanceName = "";
});

builder.Services.AddHttpClient();
builder.Services.AddTransient<IAccessTokenService, AccessTokenService>();

builder.Services.Configure<ListenOptions>(options =>
{
    options.UseHttps("/certs/certificate.pfx");
});

builder.Services.AddGrpc();
builder.Services.AddGrpcClient<Services.ServicesClient>(o =>
{
    o.Address = new Uri($"https://services.api:443");
});

builder.Services.AddGrpcClient<Users.UsersClient>(o =>
{
    o.Address = new Uri($"https://identity-service.api:443");
});

var app = builder.Build();
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGrpcService<EstimatesService>();
    endpoints.MapGrpcService<RequestsService>();
});


app.UseHttpsRedirection();
app.Run();
