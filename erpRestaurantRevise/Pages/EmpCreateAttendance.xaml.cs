using erpRestaurantRevise;
using erpRestaurantRevise.Pages;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace practice.Pages
{
    public partial class EmpCreateAttendance : Page
    {
        private connDB db = new connDB();
        private ObservableCollection<AttendanceRow> attendanceRows = new ObservableCollection<AttendanceRow>();

        public EmpCreateAttendance()
        {
            InitializeComponent();
            LoadEmployees();
            LoadPositions();
            dailyAttendanceDataGrid.ItemsSource = attendanceRows;
        }

        private void LoadEmployees()
        {
            try
            {
                attendanceRows.Clear();

                using (SqlConnection conn = db.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        SELECT e.employeeID,
                               e.firstName,
                               e.middleName,
                               e.lastName,
                               p.position
                        FROM Employee e
                        INNER JOIN EmployeePosition p ON e.positionID = p.positionID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new AttendanceRow
                            {
                                EmployeeID = Convert.ToInt32(reader["employeeID"]),
                                FullName = $"{reader["lastName"]}, {reader["firstName"]} {reader["middleName"]}",
                                PositionName = reader["position"].ToString()
                            };

                            attendanceRows.Add(row);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading employees: " + ex.Message);
            }
        }

        private void LoadPositions()
        {
            try
            {
                using (SqlConnection conn = db.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT positionID, position FROM EmployeePosition";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        var positions = new List<Position>();
                        positions.Add(new Position { PositionID = 0, PositionName = "All" }); // Default option

                        while (reader.Read())
                        {
                            positions.Add(new Position
                            {
                                PositionID = Convert.ToInt32(reader["positionID"]),
                                PositionName = reader["position"].ToString()
                            });
                        }

                        positionComboBox.ItemsSource = positions;
                        positionComboBox.SelectedIndex = 0; // Default to "All"
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading positions: " + ex.Message);
            }
        }


        private void SubmitRow_Click(object sender, RoutedEventArgs e)
        {
            if (dailyAttendanceDataGrid.SelectedItem is AttendanceRow row)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to submit attendance for {row.FullName}?",
                    "Confirm Submission",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;
                    

                try
                {
                    using (SqlConnection conn = db.GetConnection())
                    {
                        conn.Open();

                        // Default status
                        string attendanceStatus = "PResent";

                        //if (row.TimeIn.HasValue)
                        //{
                        //    attendanceStatus = row.TimeIn.Value <= new TimeSpan(8, 0, 0) ? "On Time" : "Late";
                        //}
                        //else
                        //{
                        //    attendanceStatus = "Absent";
                        //}


                        string query = @"
                    INSERT INTO Attendance (employeeID, dateToday, timeIn, timeOut, status)
                    VALUES (@employeeID, @dateToday, @timeIn, @timeOut, @status)";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@employeeID", row.EmployeeID);
                            cmd.Parameters.AddWithValue("@dateToday", DateTime.Today);

                            // TimeIn can be NULL
                            if (!row.TimeIn.HasValue)
                                cmd.Parameters.AddWithValue("@timeIn", DBNull.Value);
                            else
                                cmd.Parameters.AddWithValue("@timeIn", row.TimeIn.Value);

                            // TimeOut can be NULL
                            if (!row.TimeOut.HasValue)
                                cmd.Parameters.AddWithValue("@timeOut", DBNull.Value);
                            else
                                cmd.Parameters.AddWithValue("@timeOut", row.TimeOut.Value);

                            cmd.Parameters.AddWithValue("@status", attendanceStatus);

                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show($"✅ Attendance submitted for {row.FullName}");

                    // ✅ Clear fields after successful save
                    row.TimeIn = TimeSpan.Zero;
                    row.TimeOut = TimeSpan.Zero;

                    // Refresh DataGrid so cleared values appear
                   dailyAttendanceDataGrid.Items.Refresh();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("❌ Error saving attendance: " + ex.Message);
                }
            }
        }


        private void searchEmployeeBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                attendanceRows.Clear();
                using (SqlConnection conn = db.GetConnection())
                {
                    conn.Open();

                    string baseQuery = @"
                SELECT e.employeeID,
                       e.firstName,
                       e.middleName,
                       e.lastName,
                       p.position
                FROM Employee e
                INNER JOIN EmployeePosition p ON e.positionID = p.positionID
                WHERE 1=1"; // allows dynamic filters

                    // Parameters
                    var filters = new List<string>();
                    var cmd = new SqlCommand();
                    cmd.Connection = conn;

                    // Filter by Name
                    if (!string.IsNullOrWhiteSpace(searchEmployeeTxt.Text))
                    {
                        baseQuery += " AND (e.firstName LIKE @name OR e.lastName LIKE @name OR e.middleName LIKE @name)";
                        cmd.Parameters.AddWithValue("@name", "%" + searchEmployeeTxt.Text + "%");
                    }

                    // Filter by Position (ignore "All" = 0)
                    if (positionComboBox.SelectedValue != null && (int)positionComboBox.SelectedValue != 0)
                    {
                        baseQuery += " AND e.positionID = @positionID";
                        cmd.Parameters.AddWithValue("@positionID", (int)positionComboBox.SelectedValue);
                    }

                    cmd.CommandText = baseQuery;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new AttendanceRow
                            {
                                EmployeeID = Convert.ToInt32(reader["employeeID"]),
                                FullName = $"{reader["lastName"]}, {reader["firstName"]} {reader["middleName"]}",
                                PositionName = reader["position"].ToString()
                            };
                            attendanceRows.Add(row);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error searching employees: " + ex.Message);
            }
        }

    }
}
