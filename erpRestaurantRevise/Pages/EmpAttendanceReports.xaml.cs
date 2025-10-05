using erpRestaurantRevise.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;

namespace erpRestaurantRevise.Pages
{
    public partial class EmpAttendanceReports : Page
    {
        private connDB db = new connDB();

        public EmpAttendanceReports()
        {
            InitializeComponent();
            LoadAttendanceRecords();
        }

        private void LoadAttendanceRecords()
        {
            List<DailyAttendanceRecord> records = new List<DailyAttendanceRecord>();

            try
            {
                using (SqlConnection conn = db.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        SELECT 
                            e.employeeID,
                            (e.firstName + ' ' + e.middleName + ' ' + e.lastName) AS FullName,
                            a.timeIn,
                            a.timeOut,
                            a.status,
                            a.hourWorked
                        FROM Employee e
                        LEFT JOIN Attendance a ON e.employeeID = a.employeeID
                        ORDER BY e.lastName, e.firstName;";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var record = new DailyAttendanceRecord
                            {
                                EmployeeID = reader.GetInt32(0),
                                FullName = reader.GetString(1),
                                TimeIn = reader.IsDBNull(2) ? (TimeSpan?)null : reader.GetTimeSpan(2),
                                TimeOut = reader.IsDBNull(3) ? (TimeSpan?)null : reader.GetTimeSpan(3),
                                Status = reader.IsDBNull(4) ? "Absent" : reader.GetString(4),
                                HourWorked = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5)
                            };

                            // 🕒 Automatically compute hours if not stored
                            if (record.TimeIn.HasValue && record.TimeOut.HasValue && record.HourWorked == 0)
                            {
                                var hours = (record.TimeOut.Value - record.TimeIn.Value).TotalHours;
                                record.HourWorked = (decimal)Math.Round(hours, 2);
                            }

                            records.Add(record);
                        }
                    }
                }

                AttendanceDataGrid.ItemsSource = records;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading attendance data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
