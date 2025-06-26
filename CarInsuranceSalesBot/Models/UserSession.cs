namespace CarInsuranceSalesBot.Models;

public class UserSession(long userId)
{
    public long UserId { get; init; } = userId;

    public MemoryStream PassportImageStream { get; set; } = null!;

    public MemoryStream VehicleIdImageStream { get; set; } = null!;

    public MindeeDataExtractionResponse MindeeDataExtractionResponse { get; set; } = new();

    public required int Step { get; set; }
}