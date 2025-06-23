using CarInsuranceSalesBot.Models;

using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace CarInsuranceSalesBot.Services;

public class PdfPolicyGenerationService
{
    public MemoryStream GeneratePolicy(MindeeDataExtractionResponse data)
    {
        PdfDocument document = new();
        PdfPage? page = document.AddPage();
        XGraphics gfx = XGraphics.FromPdfPage(page);
        XFont font = new("Arial", 12);

        int y = 40;
        gfx.DrawString(
            "Car Insurance Certificate",
            new XFont("Arial", 18, XFontStyle.Bold),
            XBrushes.Black,
            new XPoint(40, y));
        y += 40;

        gfx.DrawString(
            $"Name: {data.ExtractedPassportData.Surname.Value} {data.ExtractedPassportData.Name.Value} {
                data.ExtractedPassportData.Patronymic.Value}",
            font,
            XBrushes.Black,
            new XPoint(40, y));
        y += 20;
        gfx.DrawString(
            $"Passport #: {data.ExtractedPassportData.RecordNo.Value}",
            font,
            XBrushes.Black,
            new XPoint(40, y));
        y += 30;

        gfx.DrawString(
            $"Vehicle: {data.ExtractedVehicleIdData.Make.Value} {data.ExtractedVehicleIdData.Type.Value} {
                data.ExtractedVehicleIdData.CommercialDescription.Value}",
            font,
            XBrushes.Black,
            new XPoint(40, y));
        y += 20;
        gfx.DrawString(
            $"VIN: {data.ExtractedVehicleIdData.RegistrationNumber.Value}",
            font,
            XBrushes.Black,
            new XPoint(40, y));

        y += 30;
        gfx.DrawString($"Issued On: {DateTime.UtcNow:yyyy-MM-dd}", font, XBrushes.Black, new XPoint(40, y));
        y += 20;
        gfx.DrawString("Policy Amount: 100 USD", font, XBrushes.Black, new XPoint(40, y));

        MemoryStream stream = new();

        document.Save(stream, false);
        stream.Position = 0;

        return stream;
    }
}
