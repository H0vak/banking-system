using System;
using System.IO;
using System.Threading; // Нужно для эффекта паузы
using CreditScoringSystem;

var service = new ScoringService();
service.FixOldRecords();

while (true)
{
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("╔══════════════════════════════════════════╗");
    Console.WriteLine("║        BANK SYSTEM PRO v4.1 - МЕНЮ       ║");
    Console.WriteLine("╚══════════════════════════════════════════╝");
    Console.ResetColor();
    Console.WriteLine(" 1. [НОВАЯ ЗАЯВКА]   - Оформить кредит");
    Console.WriteLine(" 2. [БАЗА ДАННЫХ]    - Список всех должников");
    Console.WriteLine(" 3. [ВНЕСТИ ОПЛАТУ]  - Погашение кредита");
    Console.WriteLine(" 4. [ВЫХОД]");
    Console.Write("\n Выберите действие (1-4): ");

    string choice = Console.ReadLine();

    if (choice == "1") CreateLoan(service);
    else if (choice == "2") ShowHistory();
    else if (choice == "3") MakePayment(service);
    else if (choice == "4") break;
    else
    {
        // ОБРАБОТКА НЕПРАВИЛЬНОЙ КОМАНДЫ
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\n [ОШИБКА]: Неправильная команда! Введите цифру от 1 до 4.");
        Console.ResetColor();
    }

    Console.WriteLine("\n Нажмите любую клавишу для возврата в меню...");
    Console.ReadKey();
}

static void CreateLoan(ScoringService service)
{
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine("╔══════════════════════════════════════════╗");
    Console.WriteLine("║         АНКЕТА НОВОГО ЗАЕМЩИКА           ║");
    Console.WriteLine("╚══════════════════════════════════════════╝");
    Console.ResetColor();

    var app = new LoanApplication();

    // ID на базе времени
    app.Id = "ID-" + DateTime.Now.ToString("ssmmHH");

    // 1. ИМЯ
    Console.Write(" ▸ Введите ФИО: ");
    app.FullName = Console.ReadLine() ?? "Unknown";

    // 2. ВОЗРАСТ
    Console.Write(" ▸ Введите возраст: ");
    if (!int.TryParse(Console.ReadLine(), out int age)) { Console.WriteLine(" Ошибка: введите число!"); return; }
    app.Age = age;
    if (age < 18)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(" [!] Отказ: Вам нет 18.");
        Console.ResetColor();
        return;
    }

    // 3. ДОХОД
    Console.Write(" ▸ Ваш доход в месяц (грн): ");
    if (!decimal.TryParse(Console.ReadLine(), out decimal income)) { Console.WriteLine(" Ошибка: введите число!"); return; }
    app.MonthlyIncome = income;
    if (app.MonthlyIncome < 5000) { Console.WriteLine(" Отказ: Низкий доход."); return; }

    var (minLoan, maxLoan) = service.CalculateLimits(app.MonthlyIncome);
    Console.WriteLine("\n--------------------------------------------");
    Console.WriteLine($" СИСТЕМА: Ваш лимит от {minLoan:N0} до {maxLoan:N0} грн.");
    Console.WriteLine("--------------------------------------------\n");

    // 4. СУММА
    Console.Write(" ▸ Сумма кредита: ");
    if (!decimal.TryParse(Console.ReadLine(), out decimal amount)) { Console.WriteLine(" Ошибка: введите число!"); return; }
    app.RequestedAmount = amount;

    if (app.RequestedAmount < minLoan || app.RequestedAmount > maxLoan)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($" [!] ОШИБКА: Сумма вне лимита ({minLoan} - {maxLoan}).");
        Console.ResetColor();
        return;
    }

    // 5. СРОК
    Console.Write(" ▸ Срок (мес, 3-60): ");
    if (!int.TryParse(Console.ReadLine(), out int months)) { Console.WriteLine(" Ошибка: введите число!"); return; }
    app.LoanTermMonths = months;
    if (app.LoanTermMonths < 3 || app.LoanTermMonths > 60) { Console.WriteLine(" Ошибка: Неверный срок."); return; }

    // 6. РАБОТА
    Console.Write(" ▸ Официальная работа? (д/н): ");
    string input = Console.ReadLine()?.ToLower() ?? "";
    app.HasOfficialJob = (input == "y" || input == "д");

    // 7. РАСЧЕТ ДАТ И ПЛАТЕЖА
    app.MonthlyPayment = service.CalculateMonthlyPayment(amount, months);
    app.OrderDate = DateTime.Now;
    app.EndDate = DateTime.Now.AddMonths(months);
    app.PaidAmount = 0;

    // Эффект "работы" системы
    Console.Write("\n [СИСТЕМА]: Анализ данных...");
    for (int i = 0; i < 5; i++)
    {
        Thread.Sleep(300);
        Console.Write("■");
    }
    Console.WriteLine(" Готово!");

    // ИТОГ
    if (service.AssessCredit(app, out string result))
    {
        Console.BackgroundColor = ConsoleColor.DarkGreen;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"\n РЕШЕНИЕ: {result} ");
        Console.ResetColor();
        Console.WriteLine($" Уникальный ID договора: {app.Id}");
        service.SaveToDatabase(app);
        Console.WriteLine(" Заявка сохранена в database.txt");
    }
    else
    {
        Console.BackgroundColor = ConsoleColor.DarkRed;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"\n РЕШЕНИЕ: {result} ");
        Console.ResetColor();
    }
}

static void ShowHistory()
{
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║                         БАЗА ДАННЫХ КЛИЕНТОВ                             ║");
    Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════╝");
    Console.ResetColor();

    // Заголовок таблицы (Добавили Даты)
    Console.WriteLine(" ID      | Имя           | Сумма     | Платёж    | Выплачено | Старт      | Конец");
    Console.WriteLine("----------------------------------------------------------------------------------");

    if (File.Exists("database.txt"))
    {
        foreach (var line in File.ReadAllLines("database.txt"))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var p = line.Split('|');

            // ПРОВЕРКА: Если в строке меньше 7 элементов, значит запись старая
            if (p.Length < 7)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($" [!] Устаревший формат: {p[0]}...");
                Console.ResetColor();
                continue;
            }

            // ВЫВОД С ДАТАМИ (p[5] - старт, p[6] - конец)
            Console.WriteLine($"{p[0].PadRight(7)} | {p[1].PadRight(13)} | {p[2].PadRight(9)} | {p[3].PadRight(9)} | {p[4].PadRight(9)} | {p[5]} | {p[6]}");
        }
    }
    else Console.WriteLine(" База пуста.");
}

static void MakePayment(ScoringService service)
{
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("╔══════════════════════════════════════════╗");
    Console.WriteLine("║             ПРИЕМ ПЛАТЕЖА                ║");
    Console.WriteLine("╚══════════════════════════════════════════╝");
    Console.ResetColor();

    Console.Write(" ▸ Введите ID кредита: ");
    string id = Console.ReadLine();

    Console.Write(" ▸ Сумма оплаты: ");
    if (!decimal.TryParse(Console.ReadLine(), out decimal pay)) { Console.WriteLine(" Ошибка: введите число!"); return; }

    if (service.UpdatePayment(id, pay, out string msg))
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n [РЕЗУЛЬТАТ]: {msg}");
        Console.ResetColor();
    }
    else Console.WriteLine($"\n [!] Ошибка: {msg}");
}