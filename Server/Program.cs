using LyftClient.Services;
using LyftClient.HTTPClient;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

builder.Services.AddMvc();  
builder.Services.AddDistributedRedisCache(options => {  
    options.Configuration = "localhost:6379";  
    options.InstanceName = "";  
});
builder.Services.AddSingleton<IHttpClientInstance, HttpClientInstance>();
builder.Services.AddGrpc();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication().AddJwtBearer();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGrpcService<EstimatesService>();
    endpoints.MapGrpcService<RequestsService>();
});

app.Run();
