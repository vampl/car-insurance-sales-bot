using System.Text;

using CarInsuranceSalesBot.Models;

namespace CarInsuranceSalesBot.Services;

public class SummaryBuilder
{
    public static string BuildPassportInfoString(MindeeDataExtractionResponse.Passport passport)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Here’s what I found");
        builder.AppendLine("📄 Passport Information:");

        if (!string.IsNullOrEmpty(passport.Surname.Value) &&
            !string.IsNullOrEmpty(passport.Name.Value) &&
            !string.IsNullOrEmpty(passport.Patronymic.Value))
            builder.AppendLine(
                $"👤 Full Name: {passport.Surname.Value} {passport.Name.Value} {passport.Patronymic.Value}");

        if (!string.IsNullOrEmpty(passport.RecordNo.Value))
            builder.AppendLine($"🆔 Record No: {passport.RecordNo.Value}");

        if (!string.IsNullOrEmpty(passport.Sex.Value))
            builder.AppendLine($"👫 Sex: {passport.Sex.Value}");

        if (!string.IsNullOrEmpty(passport.DateOfBirth.Value))
            builder.AppendLine($"🎂 Date of Birth: {passport.DateOfBirth.Value}");

        if (!string.IsNullOrEmpty(passport.DateOfExpiry.Value))
            builder.AppendLine($"📅 Issued On: {passport.DateOfExpiry.Value}");

        if (!string.IsNullOrEmpty(passport.Nationality.Value))
            builder.AppendLine($"🌍 Nationality: {passport.Nationality.Value}");

        return builder.ToString();
    }


    public static string BuildVehicleInfoString(MindeeDataExtractionResponse.VehicleId vehicleData)
    {
        var builder = new StringBuilder();
        builder.AppendLine("🚗 Vehicle Information:");

        if (!string.IsNullOrEmpty(vehicleData.VehicleIdentificationNumber.Value))
            builder.AppendLine($"🔢 Vehicle Identification Number: {vehicleData.VehicleIdentificationNumber.Value}");
        else if (!string.IsNullOrEmpty(vehicleData.RegistrationNumber.Value))
            builder.AppendLine($"🔢 Vehicle Identification Number: {vehicleData.RegistrationNumber.Value}");

        if (!string.IsNullOrEmpty(vehicleData.Make.Value))
            builder.AppendLine($"🏷️ Make: {vehicleData.Make.Value}");

        if (!string.IsNullOrEmpty(vehicleData.Type.Value))
            builder.AppendLine($"📌 Type: {vehicleData.Type.Value}");

        if (!string.IsNullOrEmpty(vehicleData.YearOfManufacture.Value))
            builder.AppendLine($"🗓 Year Of Manufacture: {vehicleData.YearOfManufacture.Value}");

        builder.AppendLine();
        builder.AppendLine("Is this correct?");

        return builder.ToString();
    }
}
