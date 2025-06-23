using CarInsuranceSalesBot.Models;
using CarInsuranceSalesBot.Options;

using Mindee;
using Mindee.Http;
using Mindee.Input;
using Mindee.Parsing.Common;
using Mindee.Product.Generated;

using Newtonsoft.Json;

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
        CustomEndpoint passportEndpoint = new("vampl", "passport", "v1");

        Task<AsyncPredictResponse<GeneratedV1>>? passportOcrTask =
            _client.EnqueueAndParseAsync<GeneratedV1>(
                new LocalInputSource(passportStream, "passport.jpg"),
                passportEndpoint);

        MindeeDataExtractionResponse.Passport passportData =
            JsonConvert.DeserializeObject<MindeeDataExtractionResponse.Passport>(
                (await passportOcrTask).Document.Inference.Prediction.ToString()) ??
            throw new InvalidOperationException("Deserialization returned nullable passport");

        return passportData;
    }

    public async Task<MindeeDataExtractionResponse.VehicleId> ExtractVehicleIdAsync(MemoryStream vehicleIdStream)
    {
        var vehicleEndpoint = new CustomEndpoint("vampl", "vehicle_id", "v1");

        Task<AsyncPredictResponse<GeneratedV1>> vehicleIdOcrTask =
            _client.EnqueueAndParseAsync<GeneratedV1>(
                new LocalInputSource(vehicleIdStream, "vehicleId.jpg"),
                vehicleEndpoint);

        MindeeDataExtractionResponse.VehicleId? vehicleIdData =
            JsonConvert.DeserializeObject<MindeeDataExtractionResponse.VehicleId>(
                (await vehicleIdOcrTask).Document.Inference.Prediction.ToString()) ??
            throw new InvalidOperationException("Deserialization returned nullable vehicle id");

        return vehicleIdData;
    }
}
