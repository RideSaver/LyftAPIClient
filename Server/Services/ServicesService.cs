using InternalAPI;
using ByteString = Google.Protobuf.ByteString;

namespace LyftClient.Services
{
        public class ServicesService : InternalAPI.Services.ServicesClient
        {
            private readonly InternalAPI.Services.ServicesClient _services;

            public ServicesService(InternalAPI.Services.ServicesClient services)
            {
                _services = services;
                RegisterServiceRequest();
            }

            public void RegisterServiceRequest()
            {
                var request = new RegisterServiceRequest
                {
                    Id = ByteString.CopyFrom(Guid.Parse("2B2225AD-9D0E-45E0-85FB-378FE2B521E0").ToByteArray()),
                    Name = "Lyft",
                    ClientName = "Lyft",
                };
                request.Features.Add(ServiceFeatures.ProfessionalDriver);
                _services.RegisterService(request);
                request.Features.Clear();

                request = new RegisterServiceRequest
                {
                    Id = ByteString.CopyFrom(Guid.Parse("52648E86-B617-44FD-B753-295D5CE9D9DC").ToByteArray()),
                    Name = "LyftShared",
                    ClientName = "Lyft",
                };
                request.Features.Add(ServiceFeatures.Shared);
                _services.RegisterService(request);
                request.Features.Clear();

                request = new RegisterServiceRequest
                {
                    Id = ByteString.CopyFrom(Guid.Parse("BB331ADE-E379-4F12-9AB0-A68AF94D5813").ToByteArray()),
                    Name = "LyftXL",
                    ClientName = "Lyft",
                };
                request.Features.Add(ServiceFeatures.ProfessionalDriver);
                _services.RegisterService(request);
                request.Features.Clear();

                request = new RegisterServiceRequest
                {
                    Id = ByteString.CopyFrom(Guid.Parse("B47A0993-DE35-4F86-8DD8-C6462F16F5E8").ToByteArray()),
                    Name = "LyftLUX",
                    ClientName = "Lyft",
                };
                request.Features.Add(ServiceFeatures.ProfessionalDriver);
                _services.RegisterService(request);
                request.Features.Clear();
            }
        }
    }
