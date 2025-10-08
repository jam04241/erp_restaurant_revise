using erpRestaurantRevise.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;

namespace erpRestaurantRevise.Pages
{
    public partial class EmpAttendanceReports : Page
    {
        private connDB db = new connDB();
        private List<DailyAttendanceRecord> allRecords = new List<DailyAttendanceRecord>();

        public EmpAttendanceReports()
        {
            InitializeComponent();
            LoadAttendanceRecords();
        }

        private void LoadAttendanceRecords()
        {
            allRecords.Clear();

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
                            a.hourWorked,
                            a.dateToday
                        FROM Employee e
                        LEFT JOIN Attendance a ON e.employeeID = a.employeeID
                        ORDER BY a.dateToday DESC, e.lastName, e.firstName;";

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
                                // IMPORTANT: store null when DB has NULL so we can detect and compute
                                HourWorked = reader.IsDBNull(5) ? (decimal?)null : reader.GetDecimal(5),
                                DateToday = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6)
                            };

                            // If hourWorked is not provided, but time in/out exist, compute it now
                            if ((!record.HourWorked.HasValue || record.HourWorked.Value == 0m)
                                && record.TimeIn.HasValue && record.TimeOut.HasValue)
                            {
                                var hours = (record.TimeOut.Value - record.TimeIn.Value).TotalHours;
                                record.HourWorked = (decimal)Math.Round(hours, 2);
                            }

                            allRecords.Add(record);
                        }
                    }
                }

                // Default view: show only today's attendance (only rows that actually have dateToday == today)
                DateTime today = DateTime.Today;
                var todaysRecords = allRecords
                    .Where(r => r.DateToday.HasValue && r.DateToday.Value.Date == today)
                    .ToList();

                AttendanceDataGrid.ItemsSource = todaysRecords;

                // Update total hours display for the currently visible records
                UpdateTotalHours(todaysRecords);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading attendance data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private decimal ComputeHoursForRecord(DailyAttendanceRecord r)
        {
            if (r.HourWorked.HasValue && r.HourWorked.Value > 0m)
                return r.HourWorked.Value;

            if (r.TimeIn.HasValue && r.TimeOut.HasValue)
            {
                var h = (r.TimeOut.Value - r.TimeIn.Value).TotalHours;
                return (decimal)Math.Round(h, 2);
            }

            return 0m;
        }

        // Update the bottom TextBlock that shows the total hours of the current list
        private void UpdateTotalHours(IEnumerable<DailyAttendanceRecord> records)
        {
            try
            {
                decimal total = records.Sum(r => ComputeHoursForRecord(r));
                // Ensure the TotalHoursTextBlock exists in your XAML (we added it previously)
                if (TotalHoursTextBlock != null)
                    TotalHoursTextBlock.Text = $"Total Hours: {total:F2}";
            }
            catch
            {
                // Swallow any update/display errors to avoid crashing the page
            }
        }

        // 🔍 Search bar
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterRecords();
        }

        // 📅 Date filter
        private void DatePicker_Changed(object sender, SelectionChangedEventArgs e)
        {
            FilterRecords();
        }

        // 🧹 Clear filters -> reset to today's view
        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = string.Empty;
            StartDatePicker.SelectedDate = null;
            EndDatePicker.SelectedDate = null;

            DateTime today = DateTime.Today;
            var todaysRecords = allRecords
                .Where(r => r.DateToday.HasValue && r.DateToday.Value.Date == today)
                .ToList();

            AttendanceDataGrid.ItemsSource = todaysRecords;
            UpdateTotalHours(todaysRecords);
        }

        // 🧮 Filtering logic (default to today if no date filters are set)
        private void FilterRecords()
        {
            string search = SearchBox.Text.Trim().ToLower();
            DateTime? startDate = StartDatePicker.SelectedDate;
            DateTime? endDate = EndDatePicker.SelectedDate;

            DateTime today = DateTime.Today;

            var filtered = allRecords.Where(r =>
                // search match
                (string.IsNullOrEmpty(search) ||
                 r.FullName.ToLower().Contains(search) ||
                 r.EmployeeID.ToString().Contains(search)) &&
                // date filter: if both datepickers empty -> show today; else apply range checks
                (!startDate.HasValue && !endDate.HasValue
                    ? (r.DateToday.HasValue && r.DateToday.Value.Date == today)
                    : (!startDate.HasValue || (r.DateToday.HasValue && r.DateToday.Value.Date >= startDate.Value.Date)) &&
                      (!endDate.HasValue || (r.DateToday.HasValue && r.DateToday.Value.Date <= endDate.Value.Date)))
            ).ToList();

            AttendanceDataGrid.ItemsSource = filtered;
            UpdateTotalHours(filtered);
        }
    }
}
