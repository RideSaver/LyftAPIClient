using LyftAPI.Client.Model;
using InternalAPI;
using System;

namespace LyftApiClient.Server.Models
{
    public class EstimateCache
    {
        public Cost Cost { get; set; }
        public GetEstimatesRequest GetEstimatesRequest { get; set; }
        public Guid ProductId { get; set; }
    }
}