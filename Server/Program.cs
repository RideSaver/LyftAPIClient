using LyftClient.Services;
using LyftClient.HTTPClient;
using Microsoft.EntityFrameworkCore.Internal;
using InternalAPI;
using Microsoft.Data.SqlClient;

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

// app.UseAuthentication().AddJwtBearer();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGrpcService<EstimatesService>();
    endpoints.MapGrpcService<RequestsService>();
});

// var servicesClient = new Services.ServicesClient(GrpcChannel.ForAddress($"services.api"));
// var request = new RegisterServiceRequest
// {
//     Id = "2B2225AD-9D0E-45E0-85FB-378FE2B521E0",
//     Name = "lyft LYFT",
//     ClientName = "lyft",
// };
// request.Features.Add(ServiceFeatures.ProfessionalDriver);
// servicesClient.RegisterService(request);
// request.Features.Clear();

// var request = new RegisterServiceRequest
// {
//     Id = "52648E86-B617-44FD-B753-295D5CE9D9DC",
//     Name = "lyft LINE",
//     ClientName = "lyft" 
// };
// request.Features.Add(ServiceFeatures.ProfessionalDriver);
// servicesClient.RegisterService(request);
// request.Features.Clear();

// var request = new RegisterServiceRequest
// {
//     Id = "B47A0993-DE35-4F86-8DD8-C6462F16F5E8",
//     Name = "lyft PLUS",
//     ClientName = "lyft" 
// };
// request.Features.Add(ServiceFeatures.ProfessionalDriver);
// servicesClient.RegisterService(request);
// request.Features.Clear();

// var request = new RegisterServiceRequest
// {
//     Id = "BB331ADE-E379-4F12-9AB0-A68AF94D5813",
//     Name = "lyft SUV",
//     ClientName = "lyft" 
// };
// request.Features.Add(ServiceFeatures.ProfessionalDriver);
// servicesClient.RegisterService(request);
// request.Features.Clear();

app.Run();
