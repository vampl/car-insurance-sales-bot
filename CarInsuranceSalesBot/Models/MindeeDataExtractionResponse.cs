using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CarInsuranceSalesBot.Models;

public class MindeeDataExtractionResponse
{
    public class PredictionField
    {
        [JsonProperty("value")]
        public required string Value { get; set; }
    }
    
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class Passport
    {
        public required PredictionField RecordNo { get; set; }

        public required PredictionField Surname { get; set; }

        public required PredictionField Name { get; set; }

        public required PredictionField Patronymic { get; set; }

        public required PredictionField Sex { get; set; }

        public required PredictionField DateOfBirth { get; set; }

        public required PredictionField DateOfExpiry { get; set; }
        
        public required PredictionField Nationality { get; set; }
    }
    
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public class VehicleId
    {
        public required PredictionField RegistrationNumber { get; set; }

        public required PredictionField RegistrationData { get; set; }

        public required PredictionField DateOfFirstRegistration { get; set; }

        public required PredictionField DateOfFirstRegistrationInUkraine { get; set; }

        public required PredictionField Make { get; set; }

        public required PredictionField Type { get; set; }

        public required PredictionField CommercialDescription { get; set; }

        public required PredictionField ColorOfVehicle { get; set; }
    }

    public Passport ExtractedPassportData { get; set; } = null!;

    public VehicleId ExtractedVehicleIdData { get; set; } = null!;
}
