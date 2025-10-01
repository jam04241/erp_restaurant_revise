using erpRestaurantRevise;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace erpRestaurantRevise.Pages
{
    public partial class EmpAddPosition : Page
    {
        private ObservableCollection<PositionModel> positions = new ObservableCollection<PositionModel>();
        private connDB db = new connDB();

        public EmpAddPosition()
        {
            InitializeComponent();
            LoadPositions();
        }

        // FUNCTION TO LOAD THE POSITIONS FROM THE DATABASE
        private void LoadPositions()
        {
            positions.Clear();

            using (SqlConnection conn = db.GetConnection())
            {
                conn.Open();
                string query = "SELECT positionID, position, baseSalary, hourlyRate, overtime FROM EmployeePosition";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        positions.Add(new PositionModel
                        {
                            PositionID = reader.GetInt32(0),
                            Position = reader.GetString(1),
                            BaseSalary = reader.GetDecimal(2),
                            HourlyRate = reader.GetDecimal(3),
                            Overtime = reader.GetDecimal(4)
                        });
                    }
                }
            }

            positionDataGrid.ItemsSource = positions;
        }

        // FUNCTION TO CREATE A NEW POSITION IN THE DATABASE
        private void CreateBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string positionName = txtPositionName.Text.Trim();
                decimal baseSalary = decimal.TryParse(txtBaseSalary.Text, out var bs) ? bs : 0;
                decimal hourlyRate = decimal.TryParse(txtHourlyRate.Text, out var hr) ? hr : 0;
                decimal overtime = decimal.TryParse(txtOvertime.Text, out var ot) ? ot : 0;

                if (string.IsNullOrEmpty(positionName))
                {
                    MessageBox.Show("Please enter a position name.", "Validation Error",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (SqlConnection conn = db.GetConnection())
                {
                    conn.Open();
                    string query = @"INSERT INTO EmployeePosition (position, baseSalary, hourlyRate, overtime) 
                             VALUES (@position, @baseSalary, @hourlyRate, @overtime)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@position", positionName);
                        cmd.Parameters.AddWithValue("@baseSalary", baseSalary);
                        cmd.Parameters.AddWithValue("@hourlyRate", hourlyRate);
                        cmd.Parameters.AddWithValue("@overtime", overtime);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Position created successfully!", "Success",
                                MessageBoxButton.OK, MessageBoxImage.Information);

                // Clear fields
                txtPositionName.Clear();
                txtBaseSalary.Clear();
                txtHourlyRate.Clear();
                txtOvertime.Clear();

                // Reload DataGrid
                LoadPositions();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while adding position: " + ex.Message,
                                "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        // FUNCTION TO DELETE THE POSITION FROM THE DATABASE
        private void DeletePosition_Click(object sender, RoutedEventArgs e)
        {
            if (positionDataGrid.SelectedItem is PositionModel pos)
            {
                var confirm = MessageBox.Show($"Delete position '{pos.Position}'?",
                                              "Confirm Delete",
                                              MessageBoxButton.YesNo,
                                              MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes)
                    return;

                using (SqlConnection conn = db.GetConnection())
                {
                    conn.Open();
                    string query = "DELETE FROM EmployeePosition WHERE positionID = @id";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", pos.PositionID);
                        cmd.ExecuteNonQuery();
                    }
                }

                positions.Remove(pos);
                MessageBox.Show("Deleted successfully!");
            }
        }
        // FUNCTON TO UPDATE THE POSITION IN THE DATABASE
        private void UpdatePositionInDatabase(PositionModel updated)
        {
            using (SqlConnection conn = db.GetConnection())
            {
                conn.Open();
                string query = @"UPDATE EmployeePosition 
                         SET position = @position, 
                             baseSalary = @baseSalary, 
                             hourlyRate = @hourlyRate, 
                             overtime = @overtime
                         WHERE positionID = @id";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@position", updated.Position);
                    cmd.Parameters.AddWithValue("@baseSalary", updated.BaseSalary);
                    cmd.Parameters.AddWithValue("@hourlyRate", updated.HourlyRate);
                    cmd.Parameters.AddWithValue("@overtime", updated.Overtime);
                    cmd.Parameters.AddWithValue("@id", updated.PositionID);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        // MODAL FOR EDITIING THE POSITION
        private PositionModel ShowEditPositionDialog(PositionModel position)
        {
            Window editWindow = new Window
            {
                Title = "Edit Position",
                Width = 350,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(10) };

            // TextBoxes
            TextBox positionBox = new TextBox { Text = position.Position, Margin = new Thickness(0, 5, 0, 5) };
            TextBox baseSalaryBox = new TextBox { Text = position.BaseSalary.ToString(), Margin = new Thickness(0, 5, 0, 5) };
            TextBox hourlyRateBox = new TextBox { Text = position.HourlyRate.ToString(), Margin = new Thickness(0, 5, 0, 5) };
            TextBox overtimeBox = new TextBox { Text = position.Overtime.ToString(), Margin = new Thickness(0, 5, 0, 5) };

            // Labels + Controls
            panel.Children.Add(new TextBlock { Text = "Position:" });
            panel.Children.Add(positionBox);

            panel.Children.Add(new TextBlock { Text = "Base Salary:" });
            panel.Children.Add(baseSalaryBox);

            panel.Children.Add(new TextBlock { Text = "Hourly Rate:" });
            panel.Children.Add(hourlyRateBox);

            panel.Children.Add(new TextBlock { Text = "Overtime Rate:" });
            panel.Children.Add(overtimeBox);

            // Save button
            Button saveButton = new Button
            {
                Content = "Save",
                Width = 80,
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                IsDefault = true
            };
            saveButton.Click += (s, e) => { editWindow.DialogResult = true; editWindow.Close(); };

            panel.Children.Add(saveButton);
            editWindow.Content = panel;

            if (editWindow.ShowDialog() == true)
            {
                return new PositionModel
                {
                    PositionID = position.PositionID,
                    Position = positionBox.Text,
                    BaseSalary = decimal.TryParse(baseSalaryBox.Text, out var bs) ? bs : 0,
                    HourlyRate = decimal.TryParse(hourlyRateBox.Text, out var hr) ? hr : 0,
                    Overtime = decimal.TryParse(overtimeBox.Text, out var ot) ? ot : 0
                };
            }

            return null;
        }


        private void EditPosition_Click(object sender, RoutedEventArgs e)
        {
            if (positionDataGrid.SelectedItem is PositionModel selected)
            {
                var updated = ShowEditPositionDialog(selected);

                if (updated != null)
                {
                    UpdatePositionInDatabase(updated);
                    LoadPositions(); // refresh DataGrid
                    MessageBox.Show("Position updated successfully!");
                }
            }
        }


    }
}
