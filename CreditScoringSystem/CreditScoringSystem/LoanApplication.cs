using System;

namespace CreditScoringSystem
{
    public class LoanApplication
    {
        public string Id { get; set; } = string.Empty; // Уникальный номер кредита
        public string FullName { get; set; } = string.Empty;
        public int Age { get; set; }
        public decimal MonthlyIncome { get; set; }
        public decimal RequestedAmount { get; set; }
        public decimal MonthlyPayment { get; set; } // Ежемесячный платеж
        public decimal PaidAmount { get; set; } // Сколько уже вернул
        public int LoanTermMonths { get; set; }
        public bool HasOfficialJob { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}