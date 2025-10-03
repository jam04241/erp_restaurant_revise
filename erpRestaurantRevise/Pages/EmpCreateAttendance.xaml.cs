using erpRestaurantRevise;
using erpRestaurantRevise.Models;
using System;
using System.Collections.ObjectModel;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace practice.Pages
{
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
                    SELECT employeeID, firstName, middleName, lastName
                    FROM Employee";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        attendanceList.Add(new DailyAttendanceRecord
                        {
                            EmployeeID = reader.GetInt32(0),
                            FullName = $"{reader.GetString(1)} {reader.GetString(2)} {reader.GetString(3)}",
                            TimeIn = null,
                            TimeOut = null,
                            Status = "Absent"
                        });
                    }
                }
            }
        }



        private void SubmitAttendance_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn?.Tag is DailyAttendanceRecord record)
            {
                // Safely get TimeIn/TimeOut (default to 00:00)
                string timeInDisplay = record.TimeIn.HasValue
                    ? record.TimeIn.Value.ToString("HH:mm")
                    : "00:00";

                string timeOutDisplay = record.TimeOut.HasValue
                    ? record.TimeOut.Value.ToString("HH:mm")
                    : "00:00";

                // Ask for confirmation
                MessageBoxResult confirm = MessageBox.Show(
                    $"Submit attendance for {record.FullName}?\n\n" +
                    $"Time In: {timeInDisplay}\n" +
                    $"Time Out: {timeOutDisplay}",
                    "Confirm Submission",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                    return;

                // Compute Status
                if (!record.TimeIn.HasValue || record.TimeIn.Value == TimeSpan.Zero)
                {
                    record.Status = "Absent";
                }
                else if (record.TimeIn.Value > new TimeSpan(8, 0, 0)) // later than 8:00
                {
                    record.Status = "Late";
                }
                else
                {
                    record.Status = "On Time";
                }

                // Save to DB
                using (SqlConnection conn = db.GetConnection())
                {
                    conn.Open();
                    string query = @"
                INSERT INTO Attendance (employeeID, dateToday, timeIn, timeOut, status, hourWorked)
                VALUES (@empID, @dateToday, @timeIn, @timeOut, @status, @hourWorked)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@empID", record.EmployeeID);
                        cmd.Parameters.AddWithValue("@dateToday", DateTime.Today);

                        // If null, store 00:00 instead of DBNull
                        cmd.Parameters.AddWithValue("@timeIn", (object)record.TimeIn ?? TimeSpan.Zero);
                        cmd.Parameters.AddWithValue("@timeOut", (object)record.TimeOut ?? TimeSpan.Zero);

                        cmd.Parameters.AddWithValue("@status", record.Status);

                        double hoursWorked = 0;
                        if (record.TimeIn.HasValue && record.TimeOut.HasValue &&
                            record.TimeIn.Value != TimeSpan.Zero &&
                            record.TimeOut.Value != TimeSpan.Zero)
                        {
                            hoursWorked = (record.TimeOut.Value - record.TimeIn.Value).TotalHours;
                        }
                        cmd.Parameters.AddWithValue("@hourWorked", hoursWorked);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show($"Attendance submitted for {record.FullName} ({record.Status})");
            }
        }


    }
}
