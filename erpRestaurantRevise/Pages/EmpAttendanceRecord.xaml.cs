using erpRestaurantRevise;
using erpRestaurantRevise.Pages;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel; // Add this
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace practice.Pages
{
        public partial class EmpAttendanceRecord : Page
        {
            private connDB db = new connDB();
            
            private AttendanceRecord selectedRecord = null;



        // 1. Data Model Class (Matches the columns returned by the JOIN query)


        // Declare attendanceData here
        private ObservableCollection<AttendanceRecord> attendanceData = new ObservableCollection<AttendanceRecord>();

            public EmpAttendanceRecord()
            {
                InitializeComponent();

                attendanceRecordDataGrid.ItemsSource = attendanceData;

                LoadAttendanceData();
        }

        // 2. Data Loading Method (Uses the revised JOIN query)
        private void LoadAttendanceData()
        {
            try
            {
                attendanceData.Clear(); // Clears old rows

                using (SqlConnection conn = db.GetConnection())
                {
                    conn.Open();

                    string selectQuery = @"
            SELECT
                A.attendanceID, 
                A.employeeID,
                CONCAT_WS(' ', E.firstName, E.middleName, E.lastName) AS FullName, 
                A.timeIn,
                A.timeOut,
                A.hourWorked,
                A.status,
                A.dateToday
            FROM
                Attendance A
            INNER JOIN
                erp_restaurant.dbo.Employee AS E
                ON A.employeeID = E.employeeID
            ORDER BY
                A.dateToday DESC, 
                A.timeIn DESC;";

                    using (SqlCommand cmd = new SqlCommand(selectQuery, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            AttendanceRecord row = new AttendanceRecord
                            {
                                attendanceID = reader.GetInt32(reader.GetOrdinal("attendanceID")),
                                employeeID = reader.IsDBNull(reader.GetOrdinal("employeeID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("employeeID")),
                                fullName = reader.GetString(reader.GetOrdinal("FullName")),
                                timeIn = reader.IsDBNull(reader.GetOrdinal("timeIn")) ? (TimeSpan?)null : reader.GetTimeSpan(reader.GetOrdinal("timeIn")),
                                timeOut = reader.IsDBNull(reader.GetOrdinal("timeOut")) ? (TimeSpan?)null : reader.GetTimeSpan(reader.GetOrdinal("timeOut")),
                                hourWorked = reader.IsDBNull(reader.GetOrdinal("hourWorked")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("hourWorked")),
                                status = reader.GetString(reader.GetOrdinal("status")),
                            };
                            attendanceData.Add(row); // This automatically updates the DataGrid
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}");
            }
        }

        // MODAL FOR EDIT ATTENDANCE
        private AttendanceRecord ShowEditAttendanceDialog(AttendanceRecord record)
        {
            // Create the modal window
            Window editWindow = new Window
            {
                Title = "Edit Attendance",
                Width = 300,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(10) };

            // Time In / Time Out TextBoxes
            TextBox timeInBox = new TextBox
            {
                Text = record.timeIn?.ToString() ?? "",
                Margin = new Thickness(0, 5, 0, 5)
            };

            TextBox timeOutBox = new TextBox
            {
                Text = record.timeOut?.ToString() ?? "",
                Margin = new Thickness(0, 5, 0, 5)
            };

            // Add labels + textboxes to panel
            panel.Children.Add(new TextBlock { Text = "Time In (HH:mm:ss):" });
            panel.Children.Add(timeInBox);
            panel.Children.Add(new TextBlock { Text = "Time Out (HH:mm:ss):" });
            panel.Children.Add(timeOutBox);

            // Save button
            Button saveButton = new Button
            {
                Content = "Save",
                Width = 80,
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                IsDefault = true
            };

            saveButton.Click += (s, e) =>
            {
                // Optional: validate format
                if (!TimeSpan.TryParse(timeInBox.Text, out TimeSpan _))
                {
                    MessageBox.Show("Invalid Time In format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!TimeSpan.TryParse(timeOutBox.Text, out TimeSpan _))
                {
                    MessageBox.Show("Invalid Time Out format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                editWindow.DialogResult = true;
                editWindow.Close();
            };

            panel.Children.Add(saveButton);
            editWindow.Content = panel;

            // Show modal
            if (editWindow.ShowDialog() == true)
            {
                // Return a new AttendanceRecord with updated values
                record.timeIn = TimeSpan.Parse(timeInBox.Text);
                record.timeOut = TimeSpan.Parse(timeOutBox.Text);
                return record;
            }

            return null;
        }

        // 3. Edit Button Click Handler: Enables editing on the row and changes the button to "Save"

        private void EditRow_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            DataGridRow row = FindVisualParent<DataGridRow>(btn);
            if (row == null) return;

            AttendanceRecord record = row.Item as AttendanceRecord;
            if (record == null) return;

            AttendanceRecord updated = ShowEditAttendanceDialog(record);
            if (updated != null)
            {
                // Update DB
                UpdateAttendanceRecord(updated);

                // Refresh DataGrid
                attendanceRecordDataGrid.Items.Refresh();
            }
        }

        // 5. Helper method to update the database
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
                WHERE 
                    attendanceID = @attendanceID";  // to update today's record only

                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@attendanceID", record.attendanceID);

                        // Times
                        if (record.timeIn.HasValue)
                            cmd.Parameters.AddWithValue("@timeIn", record.timeIn.Value);
                        else
                            cmd.Parameters.AddWithValue("@timeIn", DBNull.Value);

                        if (record.timeOut.HasValue)
                            cmd.Parameters.AddWithValue("@timeOut", record.timeOut.Value);
                        else
                            cmd.Parameters.AddWithValue("@timeOut", DBNull.Value);

                        // Hours worked calculation
                        if (record.timeIn.HasValue && record.timeOut.HasValue)
                        {
                            TimeSpan worked = record.timeOut.Value - record.timeIn.Value;
                            record.hourWorked = (decimal)worked.TotalHours;
                        }

                        cmd.Parameters.AddWithValue("@hourWorked", record.hourWorked ?? (object)DBNull.Value);

                        // ✅ Automatic status logic
                        string status = "absent"; // default

                        if (record.timeIn.HasValue)
                        {
                            if (record.timeIn.Value <= new TimeSpan(8, 1, 0))
                                status = "present";  // on time (DB only allows 'present')
                            else
                                status = "late";     // after 8:01 AM
                        }

                        cmd.Parameters.AddWithValue("@status", status);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("✅ Attendance updated successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Failed to update attendance record: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        // Generic helper to find a parent element in the visual tree (required for button-in-DataGrid)
        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null && !(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as T;
        }

    }
}