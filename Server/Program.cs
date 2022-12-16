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
    options.UseHttps(new X509Certificate2(Path.Combine("/certs/tls.crt"), Path.Combine("/certs/tls.key")));
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

app.UseHttpsRedirection();
app.MapGrpcService<EstimatesService>();
app.MapGrpcService<RequestsService>();
app.UseRouting();
app.Run();
