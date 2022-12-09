// using LyftClient.HTTPClient;
using LyftClient.Services;
using LyftClient.HTTPClient;
using InternalAPI;
using Grpc.Core;
using Grpc.Net.Client;
using ByteString = Google.Protobuf.ByteString;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddMvc();
builder.Services.AddDistributedRedisCache(options =>
{
    options.Configuration = "lyft-redis:6379";
    options.InstanceName = "";
});

builder.Services.AddSingleton<IHttpClientInstance, HttpClientInstance>();
builder.Services.AddGrpc();
builder.Services.AddGrpcClient<Services.ServicesClient>(o =>
{
    o.Address = new Uri($"https://services.api:7042");
});

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// HttpClientInstance.InitializeClient();

app.MapGrpcService<EstimatesService>();
app.MapGrpcService<RequestsService>();
app.UseRouting();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGrpcService<EstimatesService>();
    endpoints.MapGrpcService<RequestsService>();
});

app.Run();
