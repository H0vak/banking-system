using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CreditScoringSystem
{
    public class ScoringService
    {
        // ВСТАВЛЯТЬ СЮДА (внутри класса ScoringService)
        public void FixOldRecords()
        {
            if (!File.Exists(FilePath)) return;

            var lines = File.ReadAllLines(FilePath).ToList();
            bool wasChanged = false;

            for (int i = 0; i < lines.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var parts = lines[i].Split('|');

                // Если в строке меньше 7 частей (палок '|'), значит она старая
                if (parts.Length < 7)
                {
                    // 1. Создаем короткий ID
                    string id = "OLD-" + Guid.NewGuid().ToString().Substring(0, 4);

                    // 2. Имя обычно в начале
                    string name = parts[0].Trim();

                    // 3. Пытаемся вытащить только число суммы (убираем лишний текст, если он есть)
                    string rawAmount = parts.Length > 1 ? parts[1] : "0";
                    string cleanAmount = new string(rawAmount.Where(c => char.IsDigit(c)).ToArray());
                    if (string.IsNullOrEmpty(cleanAmount)) cleanAmount = "0";

                    // 4. Дата обычно в конце
                    string date = parts[parts.Length - 1].Trim();

                    // Собираем строку в НОВОМ формате:
                    // ID | Имя | Сумма | Платеж | Выплачено | ДатаСтарта | ДатаКонца
                    lines[i] = $"{id}|{name}|{cleanAmount}|0|0|{date}|{date}";
                    wasChanged = true;
                }
            }

            if (wasChanged)
            {
                File.WriteAllLines(FilePath, lines);
                Console.WriteLine("\n[СИСТЕМА]: Старые записи в базе обновлены до нового формата!");
            }
        }

        private const double YearlyRate = 0.20;
        private const string FilePath = "database.txt";

        public (decimal min, decimal max) CalculateLimits(decimal income) => (income * 0.5m, income * 10m);

        // Метод для расчета платежа отдельно
        public decimal CalculateMonthlyPayment(decimal amount, int months)
        {
            double monthlyRate = YearlyRate / 12;
            double p = (double)amount;
            double payment = p * (monthlyRate * Math.Pow(1 + monthlyRate, months)) / (Math.Pow(1 + monthlyRate, months) - 1);
            return (decimal)Math.Round(payment, 2);
        }

        public void SaveToDatabase(LoanApplication app)
        {
            // Формат строки: ID | Имя | Сумма | Платеж | Выплачено | ДатаОформления | ДатаКонца
            string record = $"{app.Id}|{app.FullName}|{app.RequestedAmount}|{app.MonthlyPayment}|{app.PaidAmount}|{app.OrderDate:dd.MM.yyyy}|{app.EndDate:dd.MM.yyyy}\n";
            File.AppendAllText(FilePath, record);
        }

        // МЕТОД ДЛЯ ОБНОВЛЕНИЯ ОПЛАТЫ
        public bool UpdatePayment(string loanId, decimal amountToAdd, out string message)
        {
            if (!File.Exists(FilePath)) { message = "База пуста."; return false; }

            var lines = File.ReadAllLines(FilePath).ToList();
            bool found = false;
            message = "";

            for (int i = 0; i < lines.Count; i++)
            {
                var parts = lines[i].Split('|');
                if (parts[0] == loanId)
                {
                    decimal totalLoan = decimal.Parse(parts[2]);
                    decimal currentPaid = decimal.Parse(parts[4]);
                    decimal newPaid = currentPaid + amountToAdd;

                    // Обновляем строку с новой суммой выплаты
                    parts[4] = newPaid.ToString();
                    lines[i] = string.Join("|", parts);
                    found = true;

                    if (newPaid >= totalLoan)
                    {
                        var endDate = DateTime.Parse(parts[6]);
                        int earlyDays = (endDate - DateTime.Now).Days;
                        message = $"Кредит полностью погашен! Раньше срока на {Math.Max(0, earlyDays)} дней.";
                    }
                    else
                    {
                        message = $"Оплата принята. Осталось вернуть: {totalLoan - newPaid} грн.";
                    }
                    break;
                }
            }

            if (found) File.WriteAllLines(FilePath, lines);
            else message = "Кредит с таким ID не найден.";

            return found;
        }

        public bool AssessCredit(LoanApplication app, out string reason)
        {
            int score = 0;
            if (app.MonthlyIncome >= app.MonthlyPayment * 3) score += 50;
            else if (app.MonthlyIncome >= app.MonthlyPayment * 2) score += 30;
            if (app.HasOfficialJob) score += 40;
            if (app.Age >= 25 && app.Age <= 50) score += 20;

            if (score >= 60) { reason = $"Одобрено! Платеж: {app.MonthlyPayment} грн/мес."; return true; }
            reason = $"Отказ: Мало баллов ({score}).";
            return false;
        }
    }
}