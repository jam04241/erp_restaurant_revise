using erpRestaurantRevise;
using erpRestaurantRevise.Models;
using System;
using System.Collections.ObjectModel;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace practice.Pages
{
    // Session manager for tracking logged-in employee
    public static class SessionManager
    {
        public static int LoggedInEmployeeID { get; set; }
    }

    public partial class EmpCreateAttendance : Page
    {
        private connDB db = new connDB();
        private ObservableCollection<DailyAttendanceRecord> attendanceList = new ObservableCollection<DailyAttendanceRecord>();

        public EmpCreateAttendance()
        {
            InitializeComponent();
            LoadEmployees();
            attendanceRecordDataGrid.ItemsSource = attendanceList;
        }

        private void LoadEmployees()
        {
            attendanceList.Clear();

            using (SqlConnection conn = db.GetConnection())
            {
                conn.Open();

                string query = @"
                    SELECT e.employeeID,
                           e.firstName,
                           e.middleName,
                           e.lastName,
                           a.timeIn,
                           a.timeOut,
                           a.status,
                           a.hourWorked
                    FROM Employee e
                    LEFT JOIN Attendance a
                        ON e.employeeID = a.employeeID
                       AND a.dateToday = CAST(GETDATE() AS date)
                    ORDER BY e.lastName, e.firstName;";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int empId = reader.GetInt32(0);
                        if (empId == SessionManager.LoggedInEmployeeID)
                            continue; // Exclude logged-in employee

                        var record = new DailyAttendanceRecord
                        {
                            EmployeeID = empId,
                            FullName = $"{reader.GetString(1)} {reader.GetString(2)} {reader.GetString(3)}",
                            TimeIn = reader.IsDBNull(4) ? (TimeSpan?)null : reader.GetTimeSpan(4),
                            TimeOut = reader.IsDBNull(5) ? (TimeSpan?)null : reader.GetTimeSpan(5),
                            Status = reader.IsDBNull(6) ? null : reader.GetString(6),
                            HourWorked = reader.IsDBNull(7) ? 0 : reader.GetDecimal(7)
                        };
                        attendanceList.Add(record);
                    }
                }
            }
        }

        private void TimeIn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DailyAttendanceRecord record)
            {
                // Prevent multiple time-ins
                if (!record.CanTimeIn)
                {
                    MessageBox.Show("Employee has already timed in for today.", "Warning", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                using (SqlConnection conn = db.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        MERGE Attendance AS target
                        USING (SELECT @empId AS employeeID, CAST(GETDATE() AS date) AS dateToday) AS source
                        ON target.employeeID = source.employeeID AND target.dateToday = source.dateToday
                        WHEN MATCHED THEN 
                            UPDATE SET timeIn = @timeIn, status = 'Present'
                        WHEN NOT MATCHED THEN
                            INSERT (employeeID, dateToday, timeIn, status)
                            VALUES (@empId, CAST(GETDATE() AS date), @timeIn, 'Present');";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@empId", record.EmployeeID);
                        cmd.Parameters.AddWithValue("@timeIn", DateTime.Now.TimeOfDay);
                        cmd.ExecuteNonQuery();
                    }
                }

                record.TimeIn = DateTime.Now.TimeOfDay;
                record.Status = "Present";
            }
        }

        private void TimeOut_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DailyAttendanceRecord record)
            {
                using (SqlConnection conn = db.GetConnection())
                {
                    conn.Open();

                    TimeSpan? timeIn = null;
                    string fetchQuery = @"
                        SELECT timeIn 
                        FROM Attendance 
                        WHERE employeeID = @empId AND dateToday = CAST(GETDATE() AS date);";

                    using (SqlCommand fetchCmd = new SqlCommand(fetchQuery, conn))
                    {
                        fetchCmd.Parameters.AddWithValue("@empId", record.EmployeeID);
                        object result = fetchCmd.ExecuteScalar();
                        if (result != DBNull.Value && result != null)
                            timeIn = (TimeSpan)result;
                    }

                    if (timeIn.HasValue)
                    {
                        TimeSpan timeOut = DateTime.Now.TimeOfDay;
                        double hoursWorked = Math.Round((timeOut - timeIn.Value).TotalHours, 2);

                        string updateQuery = @"
                            UPDATE Attendance
                            SET timeOut = @timeOut, hourWorked = @hoursWorked
                            WHERE employeeID = @empId AND dateToday = CAST(GETDATE() AS date);";

                        using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@empId", record.EmployeeID);
                            cmd.Parameters.AddWithValue("@timeOut", timeOut);
                            cmd.Parameters.AddWithValue("@hoursWorked", (decimal)hoursWorked);
                            cmd.ExecuteNonQuery();
                        }

                        record.TimeOut = timeOut;
                        record.HourWorked = (decimal)hoursWorked;
                    }
                    else
                    {
                        MessageBox.Show("Cannot time out without a valid time in.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        private void Absent_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DailyAttendanceRecord record)
            {
                using (SqlConnection conn = db.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        MERGE Attendance AS target
                        USING (SELECT @empId AS employeeID, CAST(GETDATE() AS date) AS dateToday) AS source
                        ON target.employeeID = source.employeeID AND target.dateToday = source.dateToday
                        WHEN MATCHED THEN 
                            UPDATE SET status = 'Absent', timeIn = NULL, timeOut = NULL, hourWorked = 0
                        WHEN NOT MATCHED THEN
                            INSERT (employeeID, dateToday, status, hourWorked)
                            VALUES (@empId, CAST(GETDATE() AS date), 'Absent', 0);";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@empId", record.EmployeeID);
                        cmd.ExecuteNonQuery();
                    }
                }

                record.Status = "Absent";
                record.TimeIn = null;
                record.TimeOut = null;
                record.HourWorked = 0;
            }
        }
    }
}
