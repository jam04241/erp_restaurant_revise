using erpRestaurantRevise;
using erpRestaurantRevise.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace practice.Pages
{
    public partial class EmpAttendanceRecord : Page
    {
        private connDB db = new connDB();
        private ObservableCollection<AttendanceRecord> attendanceData = new ObservableCollection<AttendanceRecord>();

        public EmpAttendanceRecord()
        {
            InitializeComponent();
            attendanceRecordDataGrid.ItemsSource = attendanceData;
            LoadAttendanceData();
        }

        // LOAD Attendance Data
        private void LoadAttendanceData()
        {
            try
            {
                attendanceData.Clear();

                using (SqlConnection conn = db.GetConnection())
                {
                    conn.Open();

                    string selectQuery = @"
                        SELECT 
                            A.employeeID,
                            CONCAT_WS(' ', E.firstName, E.middleName, E.lastName) AS FullName,
                            A.timeIn,
                            A.timeOut,
                            A.hourWorked,
                            A.status,
                            A.dateToday
                        FROM Attendance A
                        INNER JOIN erp_restaurant.dbo.Employee AS E
                            ON A.employeeID = E.employeeID
                        ORDER BY A.dateToday DESC, A.timeIn DESC;";

                    using (SqlCommand cmd = new SqlCommand(selectQuery, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            AttendanceRecord row = new AttendanceRecord
                            {
                                employeeID = reader.IsDBNull(reader.GetOrdinal("employeeID"))
                                    ? (int?)null
                                    : reader.GetInt32(reader.GetOrdinal("employeeID")),

                                fullName = reader.IsDBNull(reader.GetOrdinal("FullName"))
                                    ? string.Empty
                                    : reader.GetString(reader.GetOrdinal("FullName")),

                                timeIn = reader.IsDBNull(reader.GetOrdinal("timeIn"))
                                    ? (TimeSpan?)null
                                    : reader.GetTimeSpan(reader.GetOrdinal("timeIn")),

                                timeOut = reader.IsDBNull(reader.GetOrdinal("timeOut"))
                                    ? (TimeSpan?)null
                                    : reader.GetTimeSpan(reader.GetOrdinal("timeOut")),

                                hourWorked = reader.IsDBNull(reader.GetOrdinal("hourWorked"))
                                    ? (decimal?)null
                                    : reader.GetDecimal(reader.GetOrdinal("hourWorked")),

                                status = reader.IsDBNull(reader.GetOrdinal("status"))
                                    ? string.Empty
                                    : reader.GetString(reader.GetOrdinal("status"))
                            };

                            attendanceData.Add(row);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}");
            }
        }

        // UPDATE Attendance
        private void UpdateAttendanceRecord(AttendanceRecord record)
        {
            try
            {
                using (SqlConnection conn = db.GetConnection())
                {
                    conn.Open();

                    string updateQuery = @"
                        UPDATE Attendance
                        SET timeIn = @timeIn,
                            timeOut = @timeOut,
                            hourWorked = @hourWorked,
                            status = @status
                        WHERE employeeID = @employeeID 
                          AND dateToday = @dateToday";

                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@employeeID", record.employeeID ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@dateToday", DateTime.Today);

                        cmd.Parameters.AddWithValue("@timeIn", record.timeIn.HasValue ? (object)record.timeIn.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@timeOut", record.timeOut.HasValue ? (object)record.timeOut.Value : DBNull.Value);

                        if (record.timeIn.HasValue && record.timeOut.HasValue)
                        {
                            TimeSpan worked = record.timeOut.Value - record.timeIn.Value;
                            record.hourWorked = (decimal)worked.TotalHours;
                        }

                        cmd.Parameters.AddWithValue("@hourWorked", record.hourWorked.HasValue ? (object)record.hourWorked.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@status", string.IsNullOrEmpty(record.status) ? "Absent" : record.status);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("✅ Attendance updated successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Failed to update attendance record: {ex.Message}");
            }
        }

        // EDIT Button
        private void EditRow_Click(object sender, RoutedEventArgs e)
        {
            if (attendanceRecordDataGrid.SelectedItem is AttendanceRecord selected)
            {
                Window editWindow = new Window
                {
                    Title = "Edit Attendance",
                    Width = 350,
                    Height = 250,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize
                };

                Grid grid = new Grid { Margin = new Thickness(15) };
                for (int i = 0; i < 4; i++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Time In
                grid.Children.Add(new Label { Content = "Time In:", Margin = new Thickness(0, 5, 0, 0) });
                TextBox timeInBox = new TextBox { Text = selected.timeIn?.ToString(@"hh\:mm") ?? "" };
                Grid.SetRow(timeInBox, 0);
                Grid.SetColumn(timeInBox, 1);
                grid.Children.Add(timeInBox);

                // Time Out
                grid.Children.Add(new Label { Content = "Time Out:", Margin = new Thickness(0, 5, 0, 0) });
                Grid.SetRow(grid.Children[grid.Children.Count - 1], 1);
                TextBox timeOutBox = new TextBox { Text = selected.timeOut?.ToString(@"hh\:mm") ?? "" };
                Grid.SetRow(timeOutBox, 1);
                Grid.SetColumn(timeOutBox, 1);
                grid.Children.Add(timeOutBox);

                // Status
                grid.Children.Add(new Label { Content = "Status:", Margin = new Thickness(0, 5, 0, 0) });
                Grid.SetRow(grid.Children[grid.Children.Count - 1], 2);
                TextBox statusBox = new TextBox { Text = selected.status };
                Grid.SetRow(statusBox, 2);
                Grid.SetColumn(statusBox, 1);
                grid.Children.Add(statusBox);

                // Save Button
                Button saveBtn = new Button { Content = "Save", Width = 80, Margin = new Thickness(0, 10, 0, 0) };
                saveBtn.Click += (s, ev) =>
                {
                    if (TimeSpan.TryParse(timeInBox.Text, out TimeSpan tin)) selected.timeIn = tin;
                    if (TimeSpan.TryParse(timeOutBox.Text, out TimeSpan tout)) selected.timeOut = tout;
                    selected.status = statusBox.Text;

                    UpdateAttendanceRecord(selected);
                    LoadAttendanceData();
                    editWindow.Close();
                };

                Grid.SetRow(saveBtn, 3);
                grid.Children.Add(saveBtn);

                editWindow.Content = grid;
                editWindow.ShowDialog();
            }
        }

        // DELETE Button
        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (attendanceRecordDataGrid.SelectedItem is AttendanceRecord selected)
            {
                if (MessageBox.Show($"Are you sure you want to delete record for {selected.fullName}?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection conn = db.GetConnection())
                        {
                            conn.Open();
                            string deleteQuery = @"DELETE FROM Attendance WHERE employeeID = @employeeID AND dateToday = @dateToday";
                            using (SqlCommand cmd = new SqlCommand(deleteQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@employeeID", selected.employeeID);
                                cmd.Parameters.AddWithValue("@dateToday", DateTime.Today);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        attendanceData.Remove(selected);
                        MessageBox.Show("✅ Record deleted successfully!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"❌ Failed to delete: {ex.Message}");
                    }
                }
            }
        }
    }
}
