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
            payrollDataGrid.ItemsSource = payrollList;
        }

        private void GeneratePayroll_Click(object sender, RoutedEventArgs e)
        {
            if (!startDatePicker.SelectedDate.HasValue || !endDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select a date range.");
                return;
            }

            DateTime startDate = startDatePicker.SelectedDate.Value;
            DateTime endDate = endDatePicker.SelectedDate.Value;

            try
            {
                // 1️⃣ Check if payroll already exists for this date range
                string checkPayrollQuery = @"
            SELECT COUNT(*) as PayrollCount 
            FROM Payroll 
            WHERE PayPeriodStart = @StartDate AND PayPeriodEnd = @EndDate";

                DataTable checkData = new DataTable();
                using (SqlConnection conn = db.GetConnection())
                using (SqlCommand cmd = new SqlCommand(checkPayrollQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        checkData.Load(reader);
                    }
                }

                if (checkData.Rows.Count > 0 && Convert.ToInt32(checkData.Rows[0]["PayrollCount"]) > 0)
                {
                    MessageBox.Show($"Payroll for the date range {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd} has already been generated.");
                    return;
                }

                // 2️⃣ Get only ACTIVE employees
                string employeesQuery = @"
            SELECT EmployeeID, firstName, middleName, lastName 
            FROM Employee 
            WHERE IsActive = 1";  // Only active employees

                DataTable employeesData = new DataTable();
                using (SqlConnection conn = db.GetConnection())
                using (SqlCommand cmd = new SqlCommand(employeesQuery, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        employeesData.Load(reader);
                    }
                }

                if (employeesData.Rows.Count == 0)
                {
                    MessageBox.Show("No active employees found in the system.");
                    return;
                }

                int processedEmployees = 0;
                int successfulPayrolls = 0;

                // Clear existing payroll list
                payrollList.Clear();

                // 3️⃣ Process payroll for each ACTIVE employee
                foreach (DataRow employeeRow in employeesData.Rows)
                {
                    processedEmployees++;
                    int employeeId = Convert.ToInt32(employeeRow["EmployeeID"]);
                    string fullName = $"{employeeRow["firstName"]} {employeeRow["middleName"]} {employeeRow["lastName"]}".Replace("  ", " ").Trim();

                    try
                    {
                        // Compute total work hours, overtime, AND late minutes
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
                        ), 0) AS TotalOvertime,
                        ISNULL(SUM(
                            CASE 
                                WHEN status = 'Late' AND timeIn IS NOT NULL THEN 
                                    DATEDIFF(MINUTE, '08:00', CAST(timeIn AS TIME))
                                ELSE 0
                            END
                        ), 0) AS TotalLateMinutes
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
                        int totalLateMinutes = 0;

                        if (attendanceData.Rows.Count > 0)
                        {
                            totalHours = Convert.ToDouble(attendanceData.Rows[0]["TotalHours"]);
                            overtimeHours = Convert.ToDouble(attendanceData.Rows[0]["TotalOvertime"]);
                            totalLateMinutes = Convert.ToInt32(attendanceData.Rows[0]["TotalLateMinutes"]);
                        }

                        // Get EmployeePosition rates
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
                            // Skip employees without position assignment
                            continue;
                        }

                        if (posData.Rows[0]["hourlyRate"] == DBNull.Value)
                        {
                            // Skip employees without hourly rate
                            continue;
                        }

                        decimal hourlyRate = Convert.ToDecimal(posData.Rows[0]["hourlyRate"]);
                        decimal overtimeRate = Convert.ToDecimal(posData.Rows[0]["overtime"]);

                        // Calculate expected hours based on work days in date range
                        decimal expectedHours = 0;
                        for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
                        {
                            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                            {
                                expectedHours += 8; // 8 hours per work day
                            }
                        }

                        // Calculate payroll
                        decimal regularHours = Math.Min(Convert.ToDecimal(totalHours), expectedHours);
                        decimal actualOvertimeHours = Convert.ToDecimal(overtimeHours);

                        decimal basicPay = regularHours * hourlyRate;
                        decimal overtimePay = actualOvertimeHours * overtimeRate;

                        // Calculate deduction for hours NOT worked (if total hours < expected hours)
                        decimal hoursShort = expectedHours - Convert.ToDecimal(totalHours);
                        decimal shortageDeduction = hoursShort > 0 ? hoursShort * hourlyRate : 0;

                        // Calculate late deduction based on minutes late
                        decimal minuteRate = hourlyRate / 60; // Hourly rate per minute
                        decimal lateDeduction = minuteRate * totalLateMinutes;

                        // Total deduction = shortage + late
                        decimal totalDeduction = shortageDeduction + lateDeduction;

                        decimal netPay = (basicPay + overtimePay) - totalDeduction;
                        DateTime dateIssued = DateTime.Now;

                        // Insert into Payroll table
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
                            cmd.Parameters.AddWithValue("@Deductions", totalDeduction);
                            cmd.Parameters.AddWithValue("@NetPay", netPay);
                            cmd.Parameters.AddWithValue("@DateIssue", dateIssued);

                            conn.Open();
                            newPayrollID = (int)cmd.ExecuteScalar();
                        }

                        // Add to DataGrid
                        payrollList.Add(new PayrollRecord
                        {
                            PayrollID = newPayrollID,
                            EmployeeID = employeeId,
                            EmployeeName = fullName,
                            TotalHours = totalHours,
                            BasicPay = basicPay,
                            OvertimePay = overtimePay,
                            Deduction = totalDeduction,
                            NetPay = netPay,
                            DateIssued = dateIssued
                        });

                        successfulPayrolls++;
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with other employees
                        Console.WriteLine($"Error processing payroll for employee {employeeId}: {ex.Message}");
                    }
                }

                // Refresh the DataGrid
                payrollDataGrid.Items.Refresh();

                MessageBox.Show($"Payroll generation completed!\n" +
                               $"Processed: {processedEmployees} active employees\n" +
                               $"Successful: {successfulPayrolls} payrolls generated\n" +
                               $"Date Range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error generating payroll: " + ex.Message);
            }
        }
    }
}