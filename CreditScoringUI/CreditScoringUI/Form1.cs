using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CreditScoringSystem;

namespace CreditScoringUI
{
    public partial class Form1 : Form
    {
        ScoringService service = new ScoringService();

        public Form1()
        {
            InitializeComponent();
            service.FixOldRecords();
            RefreshGrid();

            // Прив'язуєм живі розрахунки до полів вводу
            numericUpDown2.ValueChanged += (s, e) => UpdateLiveCalculations();
            numericUpDown3.ValueChanged += (s, e) => UpdateLiveCalculations();
            numericUpDown4.ValueChanged += (s, e) => UpdateLiveCalculations();
        }

        // 1. ЖИВИЙ РОЗРАХУНОК (Виправлений під лейбли)
        private void UpdateLiveCalculations()
        {
            decimal income = numericUpDown2.Value;
            decimal amount = numericUpDown3.Value;
            int months = (int)numericUpDown4.Value;

            // Рахуємо ліміти і виводимо в lblLimits (той, що під доходом)
            var (min, max) = service.CalculateLimits(income);
            lblLimits.Text = $"Доступно: {min:N0} - {max:N0} грн";

            // Фарбуем червоним, якщо сума не підходить
            lblLimits.ForeColor = (amount > max || (amount < min && amount > 0)) ? Color.Red : Color.DarkGreen;

            // Рахуємо прогноз платежа і виводим в label8 (самий низ)
            if (amount > 0 && months > 0)
            {
                decimal monthly = service.CalculateMonthlyPayment(amount, months);
                label8.Text = $" {monthly:N2} грн/міс";
                label8.ForeColor = Color.Blue;
            }

        }

        // 2. ОНОВЛЕННЯ ТАБЛИЦІ
        private void RefreshGrid()
        {
            string path = "database.txt";
            if (!File.Exists(path)) return;

            var lines = File.ReadAllLines(path);
            var displayList = new List<object>();

            foreach (var line in lines)
            {
                var p = line.Split('|');
                if (p.Length >= 7)
                {
                    decimal total = decimal.Parse(p[2]);
                    decimal paid = decimal.Parse(p[4]);
                    displayList.Add(new
                    {
                        ID = p[0],
                        Имя = p[1],
                        Сумма = total,
                        Выплачено = paid,
                        Остаток = total - paid,
                        Статус = paid >= total ? "✅ Погашен" : "⏳ Активний"
                    });
                }
            }
            dataGridView1.DataSource = null;
            dataGridView1.DataSource = displayList;

            // Фарбуем строчки в таблиці
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["Статус"].Value != null && row.Cells["Статус"].Value.ToString().Contains("Погашен"))
                    row.DefaultCellStyle.BackColor = Color.LightGreen;
            }
        }

        // 3. КНОПКА "РОЗРАХУВАТИ"
        private void button1_Click(object sender, EventArgs e)
        {
            var app = new LoanApplication
            {
                Id = "ID-" + DateTime.Now.ToString("ssmmHH"),
                FullName = textBox1.Text,
                Age = (int)numericUpDown1.Value,
                MonthlyIncome = numericUpDown2.Value,
                RequestedAmount = numericUpDown3.Value,
                LoanTermMonths = (int)numericUpDown4.Value,
                HasOfficialJob = checkBox1.Checked,
                OrderDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths((int)numericUpDown4.Value)
            };

            app.MonthlyPayment = service.CalculateMonthlyPayment(app.RequestedAmount, app.LoanTermMonths);

            if (service.AssessCredit(app, out string reason))
            {
                service.SaveToDatabase(app);
                MessageBox.Show("Заявка схвалена і збережена!", "Успіх");
                RefreshGrid();
            }
            else MessageBox.Show("Відмова: " + reason, "Результат перевірки");
        }

        // 4. КНОПКА "ОПЛАТИТИ"
        private void btnPay_Click(object sender, EventArgs e)
        {
            // Берем ID із текстового поля і суму з numPaymentAmount
            if (service.UpdatePayment(txtPaymentId.Text, numPaymentAmount.Value, out string msg))
            {
                MessageBox.Show(msg, "Оплата");
                RefreshGrid();
                txtPaymentId.Clear();
                numPaymentAmount.Value = 0;
            }
            else MessageBox.Show(msg, "Помилка");



        }

    }
}