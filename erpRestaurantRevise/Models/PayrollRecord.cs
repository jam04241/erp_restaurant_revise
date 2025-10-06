using System;

namespace erpRestaurantRevise.Models
{
    public class PayrollRecord
    {
        public int PayrollID { get; set; }
        public int EmployeeID { get; set; }
        public string EmployeeName { get; set; }
        public double TotalHours { get; set; }
        public decimal BasicPay { get; set; }
        public decimal OvertimePay { get; set; }
        public decimal Deduction { get; set; }
        public decimal NetPay { get; set; }
        public DateTime DateIssued { get; set; }
    }
}
