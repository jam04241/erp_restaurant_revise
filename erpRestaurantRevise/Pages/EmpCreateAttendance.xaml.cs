using erpRestaurantRevise;
using erpRestaurantRevise.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace practice.Pages
{
    // Session manager for tracking logged-in employee
    public static class SessionManager
    {
        public static int LoggedInEmployeeID { get; set; }
    }

    public partial class EmpCreateAttendance : Page, INotifyPropertyChanged
    {
        private connDB db = new connDB();
        private ObservableCollection<DailyAttendanceRecord> _attendanceList = new ObservableCollection<DailyAttendanceRecord>();
        private ObservableCollection<DailyAttendanceRecord> _filteredAttendanceList = new ObservableCollection<DailyAttendanceRecord>();
        private string _searchText = "";
        private string _sortBy = "Newest First";

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<DailyAttendanceRecord> attendanceList
        {
            get => _filteredAttendanceList;
            set
            {
                _filteredAttendanceList = value;
                OnPropertyChanged(nameof(attendanceList));
            }
        }

        public string CurrentDate => DateTime.Now.ToString("MMMM dd, yyyy");

        public EmpCreateAttendance()
        {
            InitializeComponent();
            DataContext = this;
            LoadEmployees();

            // Set up event handlers
            SearchTextBox.TextChanged += SearchTextBox_TextChanged;
            SortComboBox.SelectionChanged += SortComboBox_SelectionChanged;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = SearchTextBox.Text.ToLower();
            ApplyFilterAndSort();
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SortComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                _sortBy = selectedItem.Content.ToString();
                ApplyFilterAndSort();
            }
        }

        private void ApplyFilterAndSort()
        {
            if (_attendanceList == null) return;

            var filtered = _attendanceList.Where(record =>
                string.IsNullOrEmpty(_searchText) ||
                record.FullName.ToLower().Contains(_searchText) ||
                record.EmployeeID.ToString().Contains(_searchText) ||
                record.Status?.ToLower().Contains(_searchText) == true);

            IOrderedEnumerable<DailyAttendanceRecord> sorted;

            switch (_sortBy)
            {
                case "Newest First":
                    sorted = filtered.OrderByDescending(record => record.EmployeeID);
                    break;
                case "Oldest First":
                    sorted = filtered.OrderBy(record => record.EmployeeID);
                    break;
                case "Name (A-Z)":
                    sorted = filtered.OrderBy(record => record.FullName);
                    break;
                case "Name (Z-A)":
                    sorted = filtered.OrderByDescending(record => record.FullName);
                    break;
                default:
                    sorted = filtered.OrderByDescending(record => record.EmployeeID);
                    break;
            }

            _filteredAttendanceList = new ObservableCollection<DailyAttendanceRecord>(sorted);
            OnPropertyChanged(nameof(attendanceList));
        }

        private void LoadEmployees()
        {
            _attendanceList.Clear();

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
                    WHERE e.IsActive = 1  -- Only show active employees
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
                        _attendanceList.Add(record);
                    }
                }
            }

            ApplyFilterAndSort(); // Apply initial filter and sort
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

                // Refresh the filtered list to reflect changes
                ApplyFilterAndSort();
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

                        // Refresh the filtered list to reflect changes
                        ApplyFilterAndSort();
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

                // Refresh the filtered list to reflect changes
                ApplyFilterAndSort();
            }
        }
    }
}