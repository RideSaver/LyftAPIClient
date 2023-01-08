using LyftAPI.Client.Model;
using InternalAPI;
using System;
using System.Text;

namespace LyftApiClient.Server.Models
{
    public class EstimateCache
    {
        public Cost? Cost { get; set; }
        public GetEstimatesRequest? GetEstimatesRequest { get; set; }
        public Guid ProductId { get; set; }
        public CurrencyModel? CancelationCost { get; set; }
        public Guid RequestId { get; set; }
        public Guid CancelationToken { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class EstimateCache {\n");
            sb.Append(" ProductId: ").Append(ProductId).Append("\n");
            sb.Append(" RequestId: ").Append(RequestId).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }
    }
}
