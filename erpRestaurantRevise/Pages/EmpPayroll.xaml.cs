using erpRestaurantRevise;
using erpRestaurantRevise.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace practice.Pages
{
    public partial class EmpPayroll : Page
    {
        private connDB db = new connDB();
        private ObservableCollection<PayrollRecord> payrollList = new ObservableCollection<PayrollRecord>();

        public EmpPayroll()
        {
            InitializeComponent();
            LoadEmployees();
            payrollDataGrid.ItemsSource = payrollList;
        }

        // Custom class to store EmployeeID + FullName in ComboBox
        private class EmployeeComboItem
        {
            public int EmployeeID { get; set; }
            public string FullName { get; set; }
            public override string ToString() => FullName;
        }

        private void LoadEmployees()
        {
            try
            {
                string query = "SELECT EmployeeID, firstName, middleName, lastName FROM Employee";
                DataTable dt = db.GetData(query);

                employeeComboBox.ItemsSource = null;

                var employeeList = new List<EmployeeComboItem>();

                foreach (DataRow row in dt.Rows)
                {
                    string fullName = $"{row["firstName"]} {row["middleName"]} {row["lastName"]}"
                                        .Replace("  ", " ").Trim();

                    employeeList.Add(new EmployeeComboItem
                    {
                        EmployeeID = Convert.ToInt32(row["EmployeeID"]),
                        FullName = fullName
                    });
                }

                employeeComboBox.ItemsSource = employeeList;

                if (employeeComboBox.Items.Count > 0)
                    employeeComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading employees: " + ex.Message);
            }
        }

        private void GeneratePayroll_Click(object sender, RoutedEventArgs e)
        {
            if (employeeComboBox.SelectedItem == null || !startDatePicker.SelectedDate.HasValue || !endDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select an employee and date range.");
                return;
            }

            var selectedItem = (EmployeeComboItem)employeeComboBox.SelectedItem;
            int employeeId = selectedItem.EmployeeID;
            string employeeName = selectedItem.FullName;
            DateTime startDate = startDatePicker.SelectedDate.Value;
            DateTime endDate = endDatePicker.SelectedDate.Value;

            try
            {
                // 1️⃣ Compute total work hours AND calculate overtime (hours over 8 per day)
                string attendanceQuery = @"
            SELECT 
                ISNULL(SUM(
                    CASE 
                        WHEN hourWorked IS NOT NULL THEN hourWorked
                        WHEN timeIn IS NOT NULL AND timeOut IS NOT NULL THEN DATEDIFF(MINUTE, timeIn, timeOut)/60.0
                        ELSE 0
                    END
                ), 0) AS TotalHours,
                ISNULL(SUM(
                    CASE 
                        WHEN hourWorked IS NOT NULL THEN 
                            CASE WHEN hourWorked > 8 THEN hourWorked - 8 ELSE 0 END
                        WHEN timeIn IS NOT NULL AND timeOut IS NOT NULL THEN 
                            CASE WHEN DATEDIFF(MINUTE, timeIn, timeOut)/60.0 > 8 THEN DATEDIFF(MINUTE, timeIn, timeOut)/60.0 - 8 ELSE 0 END
                        ELSE 0
                    END
                ), 0) AS TotalOvertime
            FROM Attendance
            WHERE EmployeeID = @EmployeeID
            AND dateToday BETWEEN @StartDate AND @EndDate";

                DataTable attendanceData = new DataTable();
                using (SqlConnection conn = db.GetConnection())
                using (SqlCommand cmd = new SqlCommand(attendanceQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeID", employeeId);
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        attendanceData.Load(reader);
                    }
                }

                double totalHours = 0;
                double overtimeHours = 0;

                if (attendanceData.Rows.Count > 0)
                {
                    totalHours = Convert.ToDouble(attendanceData.Rows[0]["TotalHours"]);
                    overtimeHours = Convert.ToDouble(attendanceData.Rows[0]["TotalOvertime"]);
                }

                // 2️⃣ Get EmployeePosition rates - REMOVED deduction
                string positionQuery = @"
            SELECT ep.hourlyRate, ep.overtime
            FROM Employee e
            INNER JOIN EmployeePosition ep ON e.positionID = ep.positionID
            WHERE e.EmployeeID = @EmployeeID";

                DataTable posData = new DataTable();
                using (SqlConnection conn = db.GetConnection())
                using (SqlCommand cmd = new SqlCommand(positionQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeID", employeeId);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        posData.Load(reader);
                    }
                }

                if (posData.Rows.Count == 0)
                {
                    MessageBox.Show("Employee position not found or not assigned.");
                    return;
                }

                if (posData.Rows[0]["hourlyRate"] == DBNull.Value)
                {
                    MessageBox.Show("Hourly rate is not set for this employee's position.");
                    return;
                }

                decimal hourlyRate = Convert.ToDecimal(posData.Rows[0]["hourlyRate"]);
                decimal overtimeRate = Convert.ToDecimal(posData.Rows[0]["overtime"]);
                decimal deduction = 0; // Set deduction to 0 since it's not in the table

                // 3️⃣ Calculate payroll
                decimal basicPay = Convert.ToDecimal(totalHours) * hourlyRate;
                decimal overtimePay = Convert.ToDecimal(overtimeHours) * overtimeRate;
                decimal netPay = (basicPay + overtimePay) - deduction;
                DateTime dateIssued = DateTime.Now;

                // 4️⃣ Insert into Payroll table
                string insertQuery = @"
            INSERT INTO Payroll (EmployeeID, PayPeriodStart, PayPeriodEnd, BasicPay, OvertimePay, Deductions, NetPay, DateIssue)
            VALUES (@EmployeeID, @PayPeriodStart, @PayPeriodEnd, @BasicPay, @OvertimePay, @Deductions, @NetPay, @DateIssue);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int newPayrollID = 0;
                using (SqlConnection conn = db.GetConnection())
                using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeID", employeeId);
                    cmd.Parameters.AddWithValue("@PayPeriodStart", startDate);
                    cmd.Parameters.AddWithValue("@PayPeriodEnd", endDate);
                    cmd.Parameters.AddWithValue("@BasicPay", basicPay);
                    cmd.Parameters.AddWithValue("@OvertimePay", overtimePay);
                    cmd.Parameters.AddWithValue("@Deductions", deduction);
                    cmd.Parameters.AddWithValue("@NetPay", netPay);
                    cmd.Parameters.AddWithValue("@DateIssue", dateIssued);

                    conn.Open();
                    newPayrollID = (int)cmd.ExecuteScalar();
                }

                // 5️⃣ Add to DataGrid
                payrollList.Add(new PayrollRecord
                {
                    PayrollID = newPayrollID,
                    EmployeeID = employeeId,
                    EmployeeName = employeeName,
                    TotalHours = totalHours,
                    BasicPay = basicPay,
                    OvertimePay = overtimePay,
                    Deduction = deduction,
                    NetPay = netPay,
                    DateIssued = dateIssued
                });

                MessageBox.Show("Payroll generated and saved successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error generating payroll: " + ex.Message);
            }
        }
    }
}