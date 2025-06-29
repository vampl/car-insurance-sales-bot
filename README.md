
# 🚗 Car Insurance Sales Bot

A Telegram bot written in C# (.NET) that guides users through a car insurance purchase process. It accepts passport and vehicle ID images, extracts relevant data using OCR (via Mindee API), confirms details, and generates a downloadable insurance policy as a PDF.

---

## 📦 Project Structure

```
car-insurance-sales-bot/
├── DiceusTestAssigment.sln                  # Solution file
└── CarInsuranceSalesBot/
    ├── Program.cs                           # Entry point
    ├── appsettings.json                     # Configuration (API keys, etc.)
    ├── Models/
    │   ├── MindeeDataExtractionResponse.cs
    │   └── UserSession.cs
    ├── Options/
    │   ├── MindeeOptions.cs
    │   └── TelegramBotOptions.cs
    ├── Services/
    │   ├── BotService.cs                    # Core bot logic and routing
    │   ├── MindeeOcrService.cs             # OCR integration
    │   ├── PdfPolicyGeneratorService.cs    # PDF generation
    │   ├── TelegramHelperExtensions.cs     # Utility extensions
    │   └── UserSessionManager.cs           # Tracks user progress
```

---

## ⚙️ Setup Instructions

### 1. Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- A [Telegram Bot Token](https://t.me/BotFather)
- [Mindee API Key](https://www.mindee.com/)

### 2. Clone the Repository

```bash
git clone https://github.com/vampl/car-insurance-sales-bot.git
cd car-insurance-sales-bot/CarInsuranceSalesBot
```

### 3. Configure `appsettings.json`

```json
{
  "TelegramBot": {
    "Token": "YOUR_TELEGRAM_BOT_TOKEN"
  },
  "Mindee": {
    "ApiKey": "YOUR_MINDEE_API_KEY"
  }
}
```

### 4. Run the Bot

```bash
dotnet run
```

---

## 🧠 Bot Workflow

1. **Greeting**
    - `/start` triggers a welcome message and a prompt to upload a passport image.

2. **Document Uploads**
    - The user uploads:
        - A **passport photo**
        - A **vehicle ID document**

3. **OCR Processing**
    - `MindeeOcrService` extracts key fields from the uploaded images.
    - Extracted fields include:
        - **Passport**: Full name, sex, date of birth, nationality, issue date
        - **Vehicle ID**: Registration number, first registration, make & model, color

4. **Confirmation & Price**
    - The bot displays the parsed data and asks for user confirmation.
    - Once confirmed, the bot provides an insurance price.

5. **Policy Generation**
    - On payment confirmation (simulated), the bot uses `PdfPolicyGeneratorService` to generate a PDF policy and sends it to the user.

6. **Error Handling**
    - If OCR fails or data is incomplete, the user is asked to re-upload documents.

---

## 💬 Example Interaction

```plaintext
👤 User: /start
🤖 Bot: Hello! Please send your passport photo.
👤 User: [uploads passport]
🤖 Bot: ✅ Passport received. Processing...
🤖 Bot: Now send your vehicle ID.
👤 User: [uploads vehicle ID]
🤖 Bot: ✅ Vehicle ID received.
🤖 Bot: Here’s what I found:
    👤 Name: ТКАЧЕНКО МАР'ЯНА ІВАНІВНА
    🚗 Vehicle: TOYOTA CAMRY, Color: ЧОРНИЙ
🤖 Bot: Confirm?
👤 User: ✅ Yes
🤖 Bot: Price: 100 USD. Proceed?
👤 User: ✅ Yes
🤖 Bot: 🎉 Here is your policy: `insurance_policy.pdf`
```

---

## 📌 Notes

- Mindee OCR is used for reliable structured data extraction.
- Telegram Bot API is used via `Telegram.Bot` NuGet package.
- PDF creation handled via standard .NET libraries.

---

## 🧑‍💻 Author

**Vitalii Barabash**

---

## 📄 License

This project is licensed under the MIT License.
