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

        // 4. Save Button Click Handler: Commits changes and updates the database
        private void SaveRow_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            DataGridRow row = FindVisualParent<DataGridRow>(btn);
            if (row == null) return;

            // The correct method to commit all edits in WPF DataGrid
            if (attendanceRecordDataGrid.CommitEdit())
            {
                // Stop editing the current row
                attendanceRecordDataGrid.CurrentItem = null;

                // Get the modified item
                AttendanceRecord record = row.Item as AttendanceRecord;
                if (record == null) return;

                // Call a method to update the database
                UpdateAttendanceRecord(record);
            }

            // After saving, revert the columns back to read-only
            attendanceRecordDataGrid.Columns[2].IsReadOnly = true; // Time In
            attendanceRecordDataGrid.Columns[3].IsReadOnly = true; // Time Out
            attendanceRecordDataGrid.Columns[5].IsReadOnly = true; // Status

            // Change the button back to "Edit"
            btn.Content = "Edit";
            btn.Click -= SaveRow_Click;
            btn.Click += EditRow_Click;
        }

        // 5. Helper method to update the database
        private void UpdateAttendanceRecord(AttendanceRecord record)
        {
            // Note: We use attendanceID in the WHERE clause as it is the unique identifier for the attendance record.
            string updateQuery = @"
                UPDATE [erp_restaurant].[dbo].[Attendance]
                SET [timeIn] = @TimeIn,
                    [timeOut] = @TimeOut,
                    [status] = @Status
                WHERE [employeeID] = @employeeID";

            try
            {
                using (SqlConnection connection = db.GetConnection())
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                    {
                        // Ensure data types are handled correctly (TimeSpan maps to SQL TIME/DATETIME)
                        command.Parameters.AddWithValue("@TimeIn", record.timeIn);
                        command.Parameters.AddWithValue("@TimeOut", record.timeOut);
                        command.Parameters.AddWithValue("@Status", record.status);
                        //command.Parameters.AddWithValue("@AttendanceID", record.attendanceID);

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            // Recalculate HourWorked (This is a complex business logic step, 
                            // typically done server-side or after a successful update.)
                            // For simplicity, we assume HourWorked is recalculated and stored by another process 
                            // or that the current display is sufficient.

                            // A real-world application would recalculate HourWorked here or refresh the grid.
                            MessageBox.Show("Record updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("No records were updated (ID not found).", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update attendance record: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
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