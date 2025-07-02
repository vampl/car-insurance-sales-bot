# 🚗 Car Insurance Sales Bot

A Telegram bot built with .NET that guides users through the car insurance purchase process. Users upload their passport and vehicle ID images, and the bot uses OCR (via Mindee API) to extract key data, confirm details, and generate a downloadable insurance policy PDF.

---

## 📁 Project Structure

```
car-insurance-sales-bot/
├── DiceusTestAssigment.sln               # Solution file
├── docker-compose.yml                    # Docker Compose setup
└── CarInsuranceSalesBot/
    ├── Program.cs                        # Entry point
    ├── Models/
    ├── Options/
    ├── Services/
    └── ...
```

---

## ⚙️ Getting Started

### ✅ Requirements

- [Docker](https://www.docker.com/)
- A [Telegram Bot Token](https://t.me/BotFather)
- A [Mindee API Key](https://platform.mindee.com/)

### 🚀 Run the Bot

1. **Clone the repository**:

   ```bash
   git clone https://github.com/vampl/car-insurance-sales-bot.git
   cd car-insurance-sales-bot
   ```

2. **Start the bot using Docker Compose**:

   ```bash
   docker compose run \
     -e TelegramBotToken=YOUR_TELEGRAM_BOT_TOKEN \
     -e Mindee__ApiKey=YOUR_MINDEE_API_KEY \
     bot
   ```

   > Replace `YOUR_TELEGRAM_BOT_TOKEN` and `YOUR_MINDEE_API_KEY` with your real credentials.

✅ Done! Your bot should now be running and available on Telegram.

---

## 🤖 Bot Workflow

1. **Start**
   - User sends `/start`
   - Bot asks for a passport photo

2. **Document Upload**
   - User sends:
     - Passport photo
     - Vehicle ID document

3. **OCR & Data Extraction**
   - Mindee API extracts:
     - **Passport**: Name, birth date, nationality, etc.
     - **Vehicle**: Make, model, registration, color, etc.

4. **Confirmation**
   - Bot shows extracted data and asks for user confirmation

5. **Pricing & Policy**
   - Bot provides a quote
   - On confirmation, it generates and sends a policy PDF

6. **Error Handling**
   - If OCR fails or data is incomplete, the bot will prompt for re-upload

---

## 💬 Example Interaction

```plaintext
👤 /start  
🤖 👋 Hello! I’ll help you purchase car insurance.  
    Please send a photo of your passport.  
👤 [uploads passport]  
🤖 ✅ Got your passport!  
    Extracted details:  
    👤 Name: ТКАЧЕНКО МАР'ЯНА ІВАНІВНА  
    🆔 Passport No: ХХ123456  
🤖 Is this correct? (Yes / No)  
👤 yes  
🤖 Great! Now send a photo of your vehicle ID.  
👤 [uploads vehicle ID]  
🤖 ✅ Got your vehicle ID!  
    🚗 Make: TOYOTA  
    🏷️ Model: CAMRY  
    🎨 Color: ЧОРНИЙ  
🤖 Is this correct? (Yes / No)  
👤 confirm  
🤖 Here's a summary of your data:  
    👤 ТКАЧЕНКО МАР'ЯНА ІВАНІВНА  
    🚗 TOYOTA CAMRY, Color: ЧОРНИЙ  
🤖 Proceed with insurance for 100 USD?  
👤 ok  
🤖 🎉 Done! Here is your policy: `insurance_policy.pdf`

```

---

## 📌 Notes

- OCR is powered by [Mindee](https://www.mindee.com/)
- Telegram integration via [`Telegram.Bot`](https://github.com/TelegramBots/Telegram.Bot)
- PDF generation uses native .NET libraries
- Configuration is passed via environment variables (no config files required)

---

## 👨‍💻 Author

**Vitalii Barabash**

[GitHub](https://github.com/vampl)

---

## 📄 License

MIT License. See `LICENSE` for details.
