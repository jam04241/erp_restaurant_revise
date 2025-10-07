using erpRestaurantRevise;
using erpRestaurantRevise.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace practice.Pages
{
    public partial class EmpSalaryRecord : Page
    {
        private connDB db = new connDB();
        public ObservableCollection<PayrollRecord> PayrollRecords { get; set; }

        public EmpSalaryRecord()
        {
            InitializeComponent();
        }

        private void LoadAllPayrollRecords()
        {
            try
            {
                string query = @"
            SELECT 
                p.PayrollID,
                p.EmployeeID,
                e.firstName + ' ' + COALESCE(e.middleName + ' ', '') + e.lastName as EmployeeName,
                ISNULL(SUM(a.hourWorked), 0) as TotalHours,
                p.BasicPay,
                p.OvertimePay,
                p.Deductions,
                p.NetPay,
                p.dateIssue
            FROM Payroll p
            INNER JOIN Employee e ON p.EmployeeID = e.EmployeeID
            LEFT JOIN Attendance a ON p.EmployeeID = a.employeeID 
                AND a.dateToday BETWEEN p.payPeriodStart AND p.payPeriodEnd
            GROUP BY 
                p.PayrollID,
                p.EmployeeID,
                e.firstName,
                e.middleName,
                e.lastName,
                p.BasicPay,
                p.OvertimePay,
                p.Deductions,
                p.NetPay,
                p.dateIssue
            ORDER BY p.dateIssue DESC, p.PayrollID DESC";

                DataTable dt = db.GetData(query);
                PayrollRecords.Clear();

                foreach (DataRow row in dt.Rows)
                {
                    var record = new PayrollRecord
                    {
                        PayrollID = Convert.ToInt32(row["PayrollID"]),
                        EmployeeID = Convert.ToInt32(row["EmployeeID"]),
                        EmployeeName = row["EmployeeName"].ToString(),
                        TotalHours = Convert.ToDouble(row["TotalHours"]),
                        BasicPay = Convert.ToDecimal(row["BasicPay"]),
                        OvertimePay = Convert.ToDecimal(row["OvertimePay"]),
                        Deduction = Convert.ToDecimal(row["Deductions"]),
                        NetPay = Convert.ToDecimal(row["NetPay"]),
                        DateIssued = Convert.ToDateTime(row["dateIssue"])
                    };

                    PayrollRecords.Add(record);
                }

                payrollDataGrid.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading payroll records: " + ex.Message);
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchRecords();
        }

        private void SearchRecords()
        {
            string searchText = searchTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(searchText) || searchText == "Name / Employee no.")
            {
                LoadAllPayrollRecords();
                return;
            }

            try
            {
                string query = @"
            SELECT 
                p.PayrollID,
                p.EmployeeID,
                e.firstName + ' ' + COALESCE(e.middleName + ' ', '') + e.lastName as EmployeeName,
                ISNULL(SUM(a.hourWorked), 0) as TotalHours,
                p.BasicPay,
                p.OvertimePay,
                p.Deductions,
                p.NetPay,
                p.dateIssue
            FROM Payroll p
            INNER JOIN Employee e ON p.EmployeeID = e.EmployeeID
            LEFT JOIN Attendance a ON p.EmployeeID = a.employeeID 
                AND a.dateToday BETWEEN p.payPeriodStart AND p.payPeriodEnd
            WHERE e.firstName LIKE @SearchText OR 
                  e.lastName LIKE @SearchText OR 
                  e.firstName + ' ' + e.lastName LIKE @SearchText OR
                  CAST(p.EmployeeID AS VARCHAR(10)) LIKE @SearchText OR
                  CAST(p.PayrollID AS VARCHAR(10)) LIKE @SearchText
            GROUP BY 
                p.PayrollID,
                p.EmployeeID,
                e.firstName,
                e.middleName,
                e.lastName,
                p.BasicPay,
                p.OvertimePay,
                p.Deductions,
                p.NetPay,
                p.dateIssue
            ORDER BY p.dateIssue DESC, p.PayrollID DESC";

                DataTable dt = db.GetData(query, new SqlParameter("@SearchText", "%" + searchText + "%"));
                // ... rest of your search code
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error searching payroll records: " + ex.Message);
            }
        }

        // Add Enter key support for search
        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchRecords();
            }
        }

        // Clear search when textbox gets focus
        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (searchTextBox.Text == "Name / Employee no.")
            {
                searchTextBox.Text = "";
            }
        }

        // Reset placeholder text when textbox loses focus and is empty
        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(searchTextBox.Text))
            {
                searchTextBox.Text = "Name / Employee no.";
            }
        }
    }
}