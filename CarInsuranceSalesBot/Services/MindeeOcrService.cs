using CarInsuranceSalesBot.Models;
using CarInsuranceSalesBot.Options;

using Mindee;
using Mindee.Http;
using Mindee.Input;
using Mindee.Parsing.Common;
using Mindee.Product.Generated;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CarInsuranceSalesBot.Services;

public class MindeeOcrService
{
    private readonly MindeeClient _client;

    public MindeeOcrService(MindeeOptions options)
    {
        _client = new MindeeClient(options.ApiKey);
    }

    public async Task<MindeeDataExtractionResponse.Passport> ExtractPassportAsync(MemoryStream passportStream)
    {
        CustomEndpoint passportEndpoint = new(endpointName: "passport", accountName: "vampl", "v1");

        Task<AsyncPredictResponse<GeneratedV1>>? passportOcrTask =
            _client.EnqueueAndParseAsync<GeneratedV1>(
                new LocalInputSource(passportStream, "passport.jpg"),
                passportEndpoint);

        JObject root = JObject.Parse((await passportOcrTask).RawResponse);
        JToken prediction =
            root["document"]?["inference"]?["prediction"] ??
            throw new InvalidOperationException("No data in response scheme");

        MindeeDataExtractionResponse.Passport passportData =
            prediction.ToObject<MindeeDataExtractionResponse.Passport>() ??
            throw new InvalidOperationException(
                "DeserializatipassportEndpointon returned nullable passport");

        return passportData;
    }

    public async Task<MindeeDataExtractionResponse.VehicleId> ExtractVehicleIdAsync(MemoryStream vehicleIdStream)
    {
        CustomEndpoint passportEndpoint = new(endpointName: "vehicle_registration_certeficate", accountName: "vampl", "v1");

        Task<AsyncPredictResponse<GeneratedV1>>? vehicleIdOcrTask =
            _client.EnqueueAndParseAsync<GeneratedV1>(
                new LocalInputSource(vehicleIdStream, "vehicleId.jpg"),
                passportEndpoint);

        JObject root = JObject.Parse((await vehicleIdOcrTask).RawResponse);
        JToken prediction =
            root["document"]?["inference"]?["prediction"] ??
            throw new InvalidOperationException("No data in response scheme");

        MindeeDataExtractionResponse.VehicleId passportData =
            prediction.ToObject<MindeeDataExtractionResponse.VehicleId>() ??
            throw new InvalidOperationException(
                "DeserializatipassportEndpointon returned nullable passport");

        return passportData;
    }
}
