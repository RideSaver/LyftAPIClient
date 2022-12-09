// using LyftClient.HTTPClient;
using LyftClient.Services;
<<<<<<< Updated upstream
=======
using LyftClient.HTTPClient;
using InternalAPI;
using Grpc.Core;
using Grpc.Net.Client;
using ByteString = Google.Protobuf.ByteString;
>>>>>>> Stashed changes

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddMvc();
builder.Services.AddDistributedRedisCache(options =>
{
    options.Configuration = "lyft-redis:6379";
    options.InstanceName = "";
});
<<<<<<< Updated upstream
=======

builder.Services.AddSingleton<IHttpClientInstance, HttpClientInstance>();
>>>>>>> Stashed changes
builder.Services.AddGrpc();
builder.Services.AddGrpcClient<Services.ServicesClient>(o =>
{
    o.Address = new Uri($"https://services.api:7042");
});

var app = builder.Build();

app.UseHttpsRedirection();

<<<<<<< Updated upstream
app.UseAuthorization();

app.MapControllers();

// HttpClientInstance.InitializeClient();

app.MapGrpcService<EstimatesService>();
app.MapGrpcService<RequestsService>();
=======
app.UseRouting();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGrpcService<EstimatesService>();
    endpoints.MapGrpcService<RequestsService>();
});
>>>>>>> Stashed changes

app.Run();
