using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Data.SqlClient;
using erpRestaurantRevise;
namespace practice.Pages
{
    public partial class EmpAttendanceRecord : Page
    {
        private connDB db = new connDB();

        // 1. Data Model Class (Matches the columns returned by the JOIN query)
        public class AttendanceRecord
        {
            public int employeeID { get; set; }
            public string FullName { get; set; }
            public TimeSpan timeIn { get; set; }
            public TimeSpan timeOut { get; set; }
            public TimeSpan hourWorked { get; set; }
            public string status { get; set; }

            // We need a unique key from the attendance table for WHERE clause in UPDATE
            // We'll select it in the query but not display it in the DataGrid for simplicity.
            public int attendanceID { get; set; }
        }

        public EmpAttendanceRecord()
        {
            InitializeComponent();
            LoadAttendanceData();
        }

        // 2. Data Loading Method (Uses the revised JOIN query)
        private void LoadAttendanceData()
        {
            try
            {
                // SQL query to join Attendance and Employee tables and combine names
                string selectQuery = @"
                    SELECT TOP (1000)
                        A.[attendanceID], -- Hidden but needed for update
                        A.[employeeID],
                        CONCAT(E.[firstName], ' ', E.[middleName], ' ', E.[lastName]) AS [FullName],
                        A.[timeIn],
                        A.[timeOut],
                        A.[hourWorked],
                        A.[status]
                    FROM
                        [erp_restaurant].[dbo].[Attendance] AS A
                    INNER JOIN
                        [erp_restaurant].[dbo].[Employee] AS E
                        ON A.[employeeID] = E.[employeeID]
                    ORDER BY
                        A.[dateToday] DESC, A.[timeIn] DESC;";

                using (SqlConnection connection = db.GetConnection())
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(selectQuery, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    var records = new List<AttendanceRecord>();
                    while (reader.Read())
                    {
                        // Helper function to safely read DATETIME and return a TimeSpan
                        Func<int, TimeSpan> GetTimeSpanFromDb = (index) =>
                        {
                            if (reader.IsDBNull(index))
                            {
                                return TimeSpan.Zero;
                            }
                            // 🚨 CORRECTION: Read as DateTime and use the TimeOfDay property 🚨
                            return reader.GetDateTime(index).TimeOfDay;
                        };

                        records.Add(new AttendanceRecord
                        {
                            attendanceID = reader.GetInt32(0),
                            employeeID = reader.GetInt32(1),
                            FullName = reader.GetString(2).Trim(),

                            // Use the corrected reader method (index 3, 4, 5)
                            timeIn = GetTimeSpanFromDb(3),
                            timeOut = GetTimeSpanFromDb(4),
                            hourWorked = GetTimeSpanFromDb(5),

                            status = reader.IsDBNull(6) ? "Absent" : reader.GetString(6)
                        });
                    }

                    attendanceDataGrid.ItemsSource = records;
                }
            }
            catch (Exception ex)
            {
                // This messagebox will now show the SPECIFIC error if the above fix doesn't work.
                MessageBox.Show($"Failed to load attendance data: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 3. Edit Button Click Handler: Enables editing on the row and changes the button to "Save"
        private void EditRow_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            // Find the row container
            DataGridRow row = FindVisualParent<DataGridRow>(btn);
            if (row == null) return;

            // Set the row to be in editing mode
            // We'll rely on the DataGrid's general editing logic when CommitEdit is called.

            // To make the TimeIn/TimeOut/Status columns specifically editable, 
            // we override the DataGrid's IsReadOnly=True state by setting the column's IsReadOnly property to False.

            // Index 2 is Time In, Index 3 is Time Out, Index 4 is Status 
            // NOTE: The indices MUST match the XAML DataGrid.Columns order (0=EmpID, 1=FullName, 2=TimeIn, 3=TimeOut, 4=HourWorked, 5=Status, 6=Action)

            // We only want TimeIn, TimeOut, and Status to be editable (indices 2, 3, 5)
            attendanceDataGrid.Columns[2].IsReadOnly = false; // Time In
            attendanceDataGrid.Columns[3].IsReadOnly = false; // Time Out
            attendanceDataGrid.Columns[5].IsReadOnly = false; // Status

            // Force the row into editing mode and select the first editable cell (Time In)
            attendanceDataGrid.CurrentCell = new DataGridCellInfo(row.Item, attendanceDataGrid.Columns[2]);
            attendanceDataGrid.BeginEdit();


            // Change the button text/action
            btn.Content = "Save";
            btn.Click -= EditRow_Click;
            btn.Click += SaveRow_Click;
        }

        // 4. Save Button Click Handler: Commits changes and updates the database
        private void SaveRow_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            DataGridRow row = FindVisualParent<DataGridRow>(btn);
            if (row == null) return;

            // The correct method to commit all edits in WPF DataGrid
            if (attendanceDataGrid.CommitEdit())
            {
                // Stop editing the current row
                attendanceDataGrid.CurrentItem = null;

                // Get the modified item
                AttendanceRecord record = row.Item as AttendanceRecord;
                if (record == null) return;

                // Call a method to update the database
                UpdateAttendanceRecord(record);
            }

            // After saving, revert the columns back to read-only
            attendanceDataGrid.Columns[2].IsReadOnly = true; // Time In
            attendanceDataGrid.Columns[3].IsReadOnly = true; // Time Out
            attendanceDataGrid.Columns[5].IsReadOnly = true; // Status

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
                WHERE [attendanceID] = @AttendanceID";

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
                        command.Parameters.AddWithValue("@AttendanceID", record.attendanceID);

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