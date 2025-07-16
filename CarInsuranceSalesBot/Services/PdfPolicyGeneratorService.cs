using CarInsuranceSalesBot.Models;

using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace CarInsuranceSalesBot.Services;

public class PdfPolicyGenerationService
{
    public MemoryStream GeneratePolicy(MindeeDataExtractionResponse data)
    {
        var document = new PdfDocument();
        PdfPage? page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        var font = new XFont("Arial", 10);
        var boldFont = new XFont("Arial", 10, XFontStyle.Bold);
        var headerFont = new XFont("Arial", 14, XFontStyle.Bold);

        int y = 40;

        WriteLine("AutoGuard Car Insurance Policy", headerFont, 25);
        WriteLine($"Policy Number: {Guid.NewGuid()}");
        WriteLine($"Effective Date: {DateTime.UtcNow:MMMM d, yyyy}");
        WriteLine($"Expiration Date: {DateTime.UtcNow.AddYears(1).AddDays(-1):MMMM d, yyyy}");
        y += 10;

        // 1. Policyholder Information
        WriteLine("1. Policyholder Information", headerFont, 20);
        WriteLine(
            $"Name: {data.ExtractedPassportData.Surname.Value} {data.ExtractedPassportData.Name.Value} {
                data.ExtractedPassportData.Patronymic.Value}");
        y += 10;

        // 2. Vehicle Information
        WriteLine("2. Vehicle Information", headerFont, 20);
        WriteLine($"Make: {data.ExtractedVehicleIdData.Make.Value}", font);
        WriteLine($"VIN: {data.ExtractedVehicleIdData.RegistrationNumber.Value}");
        WriteLine("Usage: Personal");
        y += 10;

        // 3. Coverages and Limits
        WriteLine("3. Coverages and Limits", headerFont, 20);
        WriteLine("Coverage Type              Limit of Liability          Deductible", boldFont);
        WriteLine("Bodily Injury Liability    $100,000 / $300,000         N/A");
        WriteLine("Property Damage Liability  $100,000                    N/A");
        WriteLine("Collision Coverage         Actual Cash Value           $500");
        WriteLine("Comprehensive Coverage     Actual Cash Value           $250");
        WriteLine("Uninsured Motorist         $100,000 / $300,000         N/A");
        WriteLine("Medical Payments           $5,000                      N/A");
        WriteLine("Roadside Assistance        Included                    N/A");
        WriteLine("Rental Reimbursement       $30/day, up to 30 days      N/A");
        y += 10;

        // 4. Premium Summary
        WriteLine("4. Premium Summary", headerFont, 20);
        WriteLine("Annual Premium: $1,240.00");
        WriteLine("Monthly Payment: $103.33");
        WriteLine("Payment Method: Auto-debit");
        y += 10;

        // 5. Terms and Conditions
        WriteLine("5. Terms and Conditions", headerFont, 20);
        WriteLine("Policy Term: This policy is valid for 12 months and must be renewed annually.");
        WriteLine("Cancellation: Either party may cancel the policy with written notice of at least 10 days.");
        WriteLine("Claim Filing: All claims must be reported within 30 days of the incident.");
        y += 10;

        // 6. Exclusions
        WriteLine("6. Exclusions", headerFont, 20);
        WriteLine("This policy does not cover:");
        WriteLine("- Intentional damage caused by the policyholder.");
        WriteLine("- Commercial use of the insured vehicle.");
        WriteLine("- Racing or off-road activity.");
        WriteLine("- Mechanical or electrical breakdowns.");
        WriteLine("- Losses occurring while driving under the influence.");
        y += 10;

        // 7. Insurer Info
        WriteLine("7. Insurer Information", headerFont, 20);
        WriteLine("Company");
        WriteLine("Street, City, ZIP");
        WriteLine("Customer Service: 1-800-INSURED");
        WriteLine("Claims Dept: claims@insurance.example.com");

        // Save to MemoryStream
        MemoryStream stream = new();
        document.Save(stream, false);
        stream.Position = 0;

        return stream;

        void WriteLine(string text, XFont? textFont = null, int spacing = 15)
        {
            textFont ??= new XFont("Arial", 10);

            gfx.DrawString(text, textFont, XBrushes.Black, new XPoint(40, y));
            y += spacing;
        }
    }
}
