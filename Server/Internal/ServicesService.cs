using InternalAPI;
using LyftClient.Interface;
using ByteString = Google.Protobuf.ByteString;

namespace LyftClient.Internal
{
    public class ServicesService : InternalAPI.Services.ServicesClient, IServicesService, IHostedService
    {
        private readonly InternalAPI.Services.ServicesClient _services;
        private readonly ILogger<ServicesService> _logger;

        public ServicesService(InternalAPI.Services.ServicesClient services, ILogger<ServicesService> logger)
        {
            _services = services;
            _logger = logger;
        }

        public async Task RegisterServiceRequest()
        {
            _logger.LogInformation("[LyftClient:ServicesService:RegisterServiceRequest] Registering services...");

            var lyft = new RegisterServiceRequest
            {
                Id = ByteString.CopyFrom(Guid.Parse("2B2225AD-9D0E-45E0-85FB-378FE2B521E0").ToByteArray()),
                Name = "Lyft",
                ClientName = "Lyft",
            };

            lyft.Features.Add(ServiceFeatures.ProfessionalDriver);
            await _services.RegisterServiceAsync(lyft);
            _logger.LogInformation("[LyftClient:ServicesService:RegisterServiceRequest] Service [Lyft] has been registered.");

            var lyftShared = new RegisterServiceRequest
            {
                Id = ByteString.CopyFrom(Guid.Parse("52648E86-B617-44FD-B753-295D5CE9D9DC").ToByteArray()),
                Name = "LyftShared",
                ClientName = "Lyft",
            };

            lyftShared.Features.Add(ServiceFeatures.Shared);
            await _services.RegisterServiceAsync(lyftShared);
            _logger.LogInformation("[LyftClient:ServicesService:RegisterServiceRequest] Service [LyftSHARED] has been registered.");

            var lyftXL = new RegisterServiceRequest
            {
                Id = ByteString.CopyFrom(Guid.Parse("BB331ADE-E379-4F12-9AB0-A68AF94D5813").ToByteArray()),
                Name = "LyftXL",
                ClientName = "Lyft",
            };

            lyftXL.Features.Add(ServiceFeatures.ProfessionalDriver);
            await _services.RegisterServiceAsync(lyftXL);
            _logger.LogInformation("[LyftClient:ServicesService:RegisterServiceRequest] Service [LyftXL] has been registered.");

            var lyftLUX = new RegisterServiceRequest
            {
                Id = ByteString.CopyFrom(Guid.Parse("B47A0993-DE35-4F86-8DD8-C6462F16F5E8").ToByteArray()),
                Name = "LyftLUX",
                ClientName = "Lyft",
            };
            _logger.LogInformation("[LyftClient:ServicesService:RegisterServiceRequest] Service [LyftLUX] has been registered.");

            lyftLUX.Features.Add(ServiceFeatures.ProfessionalDriver);
            await _services.RegisterServiceAsync(lyftLUX);

            _logger.LogInformation("[LyftClient:ServicesService:RegisterServiceRequest] Services Registeration complete.");
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await RegisterServiceRequest();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
