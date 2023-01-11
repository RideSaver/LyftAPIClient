using LyftAPI.Client.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;

namespace LyftClient.Models
{
    [DataContract(Name = "RideDetail")]
    public partial class RideDetail
    {
        [JsonConstructorAttribute]
        protected RideDetail() { }
        /// <summary>
        /// Gets or Sets Status
        /// </summary>
        [DataMember(Name = "status", EmitDefaultValue = false)]
        public RideStatusEnum? Status { get; set; }

        /// <summary>
        /// Gets or Sets RideType
        /// </summary>
        [DataMember(Name = "ride_type", EmitDefaultValue = false)]
        public RideTypeEnumWithOther? RideType { get; set; }
        /// <summary>
        /// The array of actors who may cancel the ride at this point
        /// </summary>
        /// <value>The array of actors who may cancel the ride at this point</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum CanCancelEnum
        {
            /// <summary>
            /// Enum Driver for value: driver
            /// </summary>
            [EnumMember(Value = "driver")]
            Driver = 1,

            /// <summary>
            /// Enum Passenger for value: passenger
            /// </summary>
            [EnumMember(Value = "passenger")]
            Passenger = 2,

            /// <summary>
            /// Enum Dispatcher for value: dispatcher
            /// </summary>
            [EnumMember(Value = "dispatcher")]
            Dispatcher = 3

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RideDetail" /> class.
        /// </summary>
        /// <param name="rideId">The unique ID of this ride.</param>
        /// <param name="status">status.</param>
        /// <param name="rideType">rideType.</param>
        /// <param name="passenger">passenger.</param>
        /// <param name="driver">driver.</param>
        /// <param name="vehicle">vehicle.</param>
        /// <param name="origin">origin.</param>
        /// <param name="destination">destination.</param>
        /// <param name="pickup">pickup.</param>
        /// <param name="dropoff">dropoff.</param>
        /// <param name="location">location.</param>
        /// <param name="primetimePercentage">The Prime Time percentage applied to the base price.</param>
        /// <param name="price">price.</param>
        /// <param name="lineItems">The break down of cost.</param>
        /// <param name="canCancel">canCancel.</param>
        /// <param name="canceledBy">The role of user who canceled the ride (if applicable).</param>
        /// <param name="cancellationPrice">cancellationPrice.</param>
        /// <param name="rating">The rating the user left for this ride, from 1 to 5.</param>
        /// <param name="feedback">The written feedback the user left for this ride.</param>
        /// <param name="routeUrl">The web view showing the passenger, driver, and route for this ride. This field will only be present for rides created through this API, or that have been shared through the \&quot;Share my Route\&quot; feature .</param>
        /// <param name="requestedAt">The ride requested timestamp in date and time.</param>
        public RideDetail(string rideId = default(string), RideStatusEnum? status = default(RideStatusEnum?), RideTypeEnumWithOther? rideType = default(RideTypeEnumWithOther?), PassengerDetail passenger = default(PassengerDetail), DriverDetail driver = default(DriverDetail), VehicleDetail vehicle = default(VehicleDetail), RideLocation origin = default(RideLocation), RideLocation destination = default(RideLocation), PickupDropoffLocation pickup = default(PickupDropoffLocation), PickupDropoffLocation dropoff = default(PickupDropoffLocation), RideLocation location = default(RideLocation), string primetimePercentage = default(string), Cost price = default(Cost), List<LineItem> lineItems = default(List<LineItem>), List<CanCancelEnum> canCancel = default(List<CanCancelEnum>), string canceledBy = default(string), CancellationCost cancellationPrice = default(CancellationCost), int rating = default(int), string feedback = default(string), string routeUrl = default(string), DateTimeOffset requestedAt = default(DateTimeOffset))
        {
            this.RideId = rideId;
            this.Status = status;
            this.RideType = rideType;
            this.Passenger = passenger;
            this.Driver = driver;
            this.Vehicle = vehicle;
            this.Origin = origin;
            this.Destination = destination;
            this.Pickup = pickup;
            this.Dropoff = dropoff;
            this.Location = location;
            this.PrimetimePercentage = primetimePercentage;
            this.Price = price;
            this.LineItems = lineItems;
            this.CanCancel = canCancel;
            this.CanceledBy = canceledBy;
            this.CancellationPrice = cancellationPrice;
            this.Rating = rating;
            this.Feedback = feedback;
            this.RouteUrl = routeUrl;
            this.RequestedAt = requestedAt;
        }

        /// <summary>
        /// The unique ID of this ride
        /// </summary>
        /// <value>The unique ID of this ride</value>
        [DataMember(Name = "ride_id", EmitDefaultValue = false)]
        public string? RideId { get; set; }

        /// <summary>
        /// Gets or Sets Passenger
        /// </summary>
        [DataMember(Name = "passenger", EmitDefaultValue = false)]
        public PassengerDetail? Passenger { get; set; }

        /// <summary>
        /// Gets or Sets Driver
        /// </summary>
        [DataMember(Name = "driver", EmitDefaultValue = false)]
        public DriverDetail? Driver { get; set; }

        /// <summary>
        /// Gets or Sets Vehicle
        /// </summary>
        [DataMember(Name = "vehicle", EmitDefaultValue = false)]
        public VehicleDetail? Vehicle { get; set; }

        /// <summary>
        /// Gets or Sets Origin
        /// </summary>
        [DataMember(Name = "origin", EmitDefaultValue = false)]
        public RideLocation? Origin { get; set; }

        /// <summary>
        /// Gets or Sets Destination
        /// </summary>
        [DataMember(Name = "destination", EmitDefaultValue = false)]
        public RideLocation? Destination { get; set; }

        /// <summary>
        /// Gets or Sets Pickup
        /// </summary>
        [DataMember(Name = "pickup", EmitDefaultValue = false)]
        public PickupDropoffLocation? Pickup { get; set; }

        /// <summary>
        /// Gets or Sets Dropoff
        /// </summary>
        [DataMember(Name = "dropoff", EmitDefaultValue = false)]
        public PickupDropoffLocation? Dropoff { get; set; }

        /// <summary>
        /// Gets or Sets Location
        /// </summary>
        [DataMember(Name = "location", EmitDefaultValue = false)]
        public RideLocation? Location { get; set; }

        /// <summary>
        /// The Prime Time percentage applied to the base price
        /// </summary>
        /// <value>The Prime Time percentage applied to the base price</value>
        [DataMember(Name = "primetime_percentage", EmitDefaultValue = false)]
        public string? PrimetimePercentage { get; set; }

        /// <summary>
        /// Gets or Sets Price
        /// </summary>
        [DataMember(Name = "price", EmitDefaultValue = false)]
        public Cost? Price { get; set; }

        /// <summary>
        /// The break down of cost
        /// </summary>
        /// <value>The break down of cost</value>
        [DataMember(Name = "line_items", EmitDefaultValue = false)]
        public List<LineItem>? LineItems { get; set; }

        /// <summary>
        /// Gets or Sets CanCancel
        /// </summary>
        [DataMember(Name = "can_cancel", EmitDefaultValue = false)]
        public List<RideDetail.CanCancelEnum>? CanCancel { get; set; }

        /// <summary>
        /// The role of user who canceled the ride (if applicable)
        /// </summary>
        /// <value>The role of user who canceled the ride (if applicable)</value>
        [DataMember(Name = "canceled_by", EmitDefaultValue = false)]
        public string? CanceledBy { get; set; }

        /// <summary>
        /// Gets or Sets CancellationPrice
        /// </summary>
        [DataMember(Name = "cancellation_price", EmitDefaultValue = false)]
        public CancellationCost? CancellationPrice { get; set; }

        /// <summary>
        /// The rating the user left for this ride, from 1 to 5
        /// </summary>
        /// <value>The rating the user left for this ride, from 1 to 5</value>
        [DataMember(Name = "rating", EmitDefaultValue = false)]
        public int? Rating { get; set; }

        /// <summary>
        /// The written feedback the user left for this ride
        /// </summary>
        /// <value>The written feedback the user left for this ride</value>
        [DataMember(Name = "feedback", EmitDefaultValue = false)]
        public string? Feedback { get; set; }

        /// <summary>
        /// The web view showing the passenger, driver, and route for this ride. This field will only be present for rides created through this API, or that have been shared through the \&quot;Share my Route\&quot; feature 
        /// </summary>
        /// <value>The web view showing the passenger, driver, and route for this ride. This field will only be present for rides created through this API, or that have been shared through the \&quot;Share my Route\&quot; feature </value>
        [DataMember(Name = "route_url", EmitDefaultValue = false)]
        public string? RouteUrl { get; set; }

        /// <summary>
        /// The ride requested timestamp in date and time
        /// </summary>
        /// <value>The ride requested timestamp in date and time</value>
        [DataMember(Name = "requested_at", EmitDefaultValue = false)]
        public DateTimeOffset? RequestedAt { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class RideDetail {\n");
            sb.Append("  RideId: ").Append(RideId).Append("\n");
            sb.Append("  Status: ").Append(Status).Append("\n");
            sb.Append("  RideType: ").Append(RideType).Append("\n");
            sb.Append("  Passenger: ").Append(Passenger).Append("\n");
            sb.Append("  Driver: ").Append(Driver).Append("\n");
            sb.Append("  Vehicle: ").Append(Vehicle).Append("\n");
            sb.Append("  Origin: ").Append(Origin).Append("\n");
            sb.Append("  Destination: ").Append(Destination).Append("\n");
            sb.Append("  Pickup: ").Append(Pickup).Append("\n");
            sb.Append("  Dropoff: ").Append(Dropoff).Append("\n");
            sb.Append("  Location: ").Append(Location).Append("\n");
            sb.Append("  PrimetimePercentage: ").Append(PrimetimePercentage).Append("\n");
            sb.Append("  Price: ").Append(Price).Append("\n");
            sb.Append("  LineItems: ").Append(LineItems).Append("\n");
            sb.Append("  CanCancel: ").Append(CanCancel).Append("\n");
            sb.Append("  CanceledBy: ").Append(CanceledBy).Append("\n");
            sb.Append("  CancellationPrice: ").Append(CancellationPrice).Append("\n");
            sb.Append("  Rating: ").Append(Rating).Append("\n");
            sb.Append("  Feedback: ").Append(Feedback).Append("\n");
            sb.Append("  RouteUrl: ").Append(RouteUrl).Append("\n");
            sb.Append("  RequestedAt: ").Append(RequestedAt).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="input">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object input)
        {
            return this.Equals(input as RideDetail);
        }

        /// <summary>
        /// Returns true if RideDetail instances are equal
        /// </summary>
        /// <param name="input">Instance of RideDetail to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(RideDetail input)
        {
            if (input == null)
            {
                return false;
            }
            return
                (
                    this.RideId == input.RideId ||
                    (this.RideId != null &&
                    this.RideId.Equals(input.RideId))
                ) &&
                (
                    this.Status == input.Status ||
                    this.Status.Equals(input.Status)
                ) &&
                (
                    this.RideType == input.RideType ||
                    this.RideType.Equals(input.RideType)
                ) &&
                (
                    this.Passenger == input.Passenger ||
                    (this.Passenger != null &&
                    this.Passenger.Equals(input.Passenger))
                ) &&
                (
                    this.Driver == input.Driver ||
                    (this.Driver != null &&
                    this.Driver.Equals(input.Driver))
                ) &&
                (
                    this.Vehicle == input.Vehicle ||
                    (this.Vehicle != null &&
                    this.Vehicle.Equals(input.Vehicle))
                ) &&
                (
                    this.Origin == input.Origin ||
                    (this.Origin != null &&
                    this.Origin.Equals(input.Origin))
                ) &&
                (
                    this.Destination == input.Destination ||
                    (this.Destination != null &&
                    this.Destination.Equals(input.Destination))
                ) &&
                (
                    this.Pickup == input.Pickup ||
                    (this.Pickup != null &&
                    this.Pickup.Equals(input.Pickup))
                ) &&
                (
                    this.Dropoff == input.Dropoff ||
                    (this.Dropoff != null &&
                    this.Dropoff.Equals(input.Dropoff))
                ) &&
                (
                    this.Location == input.Location ||
                    (this.Location != null &&
                    this.Location.Equals(input.Location))
                ) &&
                (
                    this.PrimetimePercentage == input.PrimetimePercentage ||
                    (this.PrimetimePercentage != null &&
                    this.PrimetimePercentage.Equals(input.PrimetimePercentage))
                ) &&
                (
                    this.Price == input.Price ||
                    (this.Price != null &&
                    this.Price.Equals(input.Price))
                ) &&
                (
                    this.LineItems == input.LineItems ||
                    this.LineItems != null &&
                    input.LineItems != null &&
                    this.LineItems.SequenceEqual(input.LineItems)
                ) &&
                (
                    this.CanCancel == input.CanCancel ||
                    this.CanCancel != null &&
                    input.CanCancel != null &&
                    this.CanCancel.SequenceEqual(input.CanCancel)
                ) &&
                (
                    this.CanceledBy == input.CanceledBy ||
                    (this.CanceledBy != null &&
                    this.CanceledBy.Equals(input.CanceledBy))
                ) &&
                (
                    this.CancellationPrice == input.CancellationPrice ||
                    (this.CancellationPrice != null &&
                    this.CancellationPrice.Equals(input.CancellationPrice))
                ) &&
                (
                    this.Rating == input.Rating ||
                    this.Rating.Equals(input.Rating)
                ) &&
                (
                    this.Feedback == input.Feedback ||
                    (this.Feedback != null &&
                    this.Feedback.Equals(input.Feedback))
                ) &&
                (
                    this.RouteUrl == input.RouteUrl ||
                    (this.RouteUrl != null &&
                    this.RouteUrl.Equals(input.RouteUrl))
                ) &&
                (
                    this.RequestedAt == input.RequestedAt ||
                    (this.RequestedAt != null &&
                    this.RequestedAt.Equals(input.RequestedAt))
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = 41;
                if (this.RideId != null)
                {
                    hashCode = (hashCode * 59) + this.RideId.GetHashCode();
                }
                hashCode = (hashCode * 59) + this.Status.GetHashCode();
                hashCode = (hashCode * 59) + this.RideType.GetHashCode();
                if (this.Passenger != null)
                {
                    hashCode = (hashCode * 59) + this.Passenger.GetHashCode();
                }
                if (this.Driver != null)
                {
                    hashCode = (hashCode * 59) + this.Driver.GetHashCode();
                }
                if (this.Vehicle != null)
                {
                    hashCode = (hashCode * 59) + this.Vehicle.GetHashCode();
                }
                if (this.Origin != null)
                {
                    hashCode = (hashCode * 59) + this.Origin.GetHashCode();
                }
                if (this.Destination != null)
                {
                    hashCode = (hashCode * 59) + this.Destination.GetHashCode();
                }
                if (this.Pickup != null)
                {
                    hashCode = (hashCode * 59) + this.Pickup.GetHashCode();
                }
                if (this.Dropoff != null)
                {
                    hashCode = (hashCode * 59) + this.Dropoff.GetHashCode();
                }
                if (this.Location != null)
                {
                    hashCode = (hashCode * 59) + this.Location.GetHashCode();
                }
                if (this.PrimetimePercentage != null)
                {
                    hashCode = (hashCode * 59) + this.PrimetimePercentage.GetHashCode();
                }
                if (this.Price != null)
                {
                    hashCode = (hashCode * 59) + this.Price.GetHashCode();
                }
                if (this.LineItems != null)
                {
                    hashCode = (hashCode * 59) + this.LineItems.GetHashCode();
                }
                if (this.CanCancel != null)
                {
                    hashCode = (hashCode * 59) + this.CanCancel.GetHashCode();
                }
                if (this.CanceledBy != null)
                {
                    hashCode = (hashCode * 59) + this.CanceledBy.GetHashCode();
                }
                if (this.CancellationPrice != null)
                {
                    hashCode = (hashCode * 59) + this.CancellationPrice.GetHashCode();
                }
                hashCode = (hashCode * 59) + this.Rating.GetHashCode();
                if (this.Feedback != null)
                {
                    hashCode = (hashCode * 59) + this.Feedback.GetHashCode();
                }
                if (this.RouteUrl != null)
                {
                    hashCode = (hashCode * 59) + this.RouteUrl.GetHashCode();
                }
                if (this.RequestedAt != null)
                {
                    hashCode = (hashCode * 59) + this.RequestedAt.GetHashCode();
                }
                return hashCode;
            }
        }

        /// <summary>
        /// To validate all properties of the instance
        /// </summary>
        /// <param name="validationContext">Validation context</param>
        /// <returns>Validation Result</returns>
        public IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }
}
