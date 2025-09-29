using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using erpRestaurantRevise;

namespace practice.Pages
{
    public partial class EmpRecord : Page
    {
        private connDB db = new connDB();

        private List<EmployeeRecord> allEmployees = new List<EmployeeRecord>();

        public EmpRecord()
        {
            InitializeComponent();
            LoadEmployees();
        }

        // ---------------- Load Employees ----------------
        private void LoadEmployees()
        {
            try
            {
                allEmployees.Clear();

                using (SqlConnection con = db.GetConnection())
                {
                    con.Open();
                    string query = @"SELECT e.employeeID, e.firstName, e.middleName, e.lastName, e.sex,
                                     e.contact, p.position AS PositionName
                                     FROM Employee e
                                     INNER JOIN EmployeePosition p ON e.positionID = p.positionID";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            allEmployees.Add(new EmployeeRecord
                            {
                                EmployeeID = Convert.ToInt32(reader["employeeID"]),
                                FirstName = reader["firstName"].ToString(),
                                MiddleName = reader["middleName"].ToString(),
                                LastName = reader["lastName"].ToString(),
                                Sex = reader["Sex"].ToString(),
                                Contact = reader["contact"].ToString(),
                                PositionName = reader["PositionName"].ToString()
                            });
                        }
                    }
                }

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading employees: " + ex.Message);
            }
        }

        // ---------------- Load Positions ----------------
        private List<string> LoadPositions()
        {
            var positions = new List<string>();
            try
            {
                using (SqlConnection con = db.GetConnection())
                {
                    con.Open();
                    string query = "SELECT position FROM EmployeePosition";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            positions.Add(reader["position"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading positions: " + ex.Message);
            }
            return positions;
        }

        // ---------------- Edit Button Click ----------------
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var employee = button.DataContext as EmployeeRecord;

            if (employee != null)
            {
                var updatedEmployee = ShowEditEmployeeDialog(employee);

                if (updatedEmployee != null)
                {
                    UpdateEmployeeFull(updatedEmployee);
                    LoadEmployees(); // ✅ Refresh with search/sort applied
                }
            }
        }

        // ---------------- Delete Button Click ----------------
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var employee = button.DataContext as EmployeeRecord;

            if (employee != null)
            {
                if (MessageBox.Show($"Are you sure you want to delete {employee.FirstName} {employee.LastName}?",
                    "Confirm Delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    DeleteEmployee(employee.EmployeeID);
                    LoadEmployees(); 
                }
            }
        }

        // -- Update Employee (All Fields) --
        private void UpdateEmployeeFull(EmployeeRecord employee)
        {
            try
            {
                using (SqlConnection con = db.GetConnection())
                {
                    con.Open();
                    string query = @"UPDATE Employee 
                                     SET firstName=@FirstName, middleName=@MiddleName, lastName=@LastName, 
                                         sex=@Sex, contact=@Contact, positionID=(SELECT positionID FROM EmployeePosition WHERE position=@Position)
                                     WHERE employeeID=@ID";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@FirstName", employee.FirstName);
                        cmd.Parameters.AddWithValue("@MiddleName", employee.MiddleName);
                        cmd.Parameters.AddWithValue("@LastName", employee.LastName);
                        cmd.Parameters.AddWithValue("@Sex", employee.Sex);
                        cmd.Parameters.AddWithValue("@Contact", employee.Contact);
                        cmd.Parameters.AddWithValue("@Position", employee.PositionName);
                        cmd.Parameters.AddWithValue("@ID", employee.EmployeeID);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating employee: " + ex.Message);
            }
        }

        // -- Delete Employee --
        private void DeleteEmployee(int employeeID)
        {
            try
            {
                using (SqlConnection con = db.GetConnection())
                {
                    con.Open();
                    string query = "DELETE FROM Employee WHERE employeeID = @ID";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@ID", employeeID);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting employee: " + ex.Message);
            }
        }

        // -- Multi-Field Edit Dialog --
        private EmployeeRecord ShowEditEmployeeDialog(EmployeeRecord employee)
        {
            Window editWindow = new Window
            {
                Title = "Edit Employee",
                Width = 350,
                Height = 450,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(10) };

            // TextBoxes
            TextBox firstNameBox = new TextBox { Text = employee.FirstName, Margin = new Thickness(0, 5, 0, 5) };
            TextBox middleNameBox = new TextBox { Text = employee.MiddleName, Margin = new Thickness(0, 5, 0, 5) };
            TextBox lastNameBox = new TextBox { Text = employee.LastName, Margin = new Thickness(0, 5, 0, 5) };
            TextBox sexBox = new TextBox { Text = employee.Sex, Margin = new Thickness(0, 5, 0, 5) };
            TextBox contactBox = new TextBox { Text = employee.Contact, Margin = new Thickness(0, 5, 0, 5) };

            // Position ComboBox
            ComboBox positionBox = new ComboBox { Margin = new Thickness(0, 5, 0, 5) };
            var positions = LoadPositions();
            foreach (var pos in positions)
                positionBox.Items.Add(pos);
            positionBox.SelectedItem = employee.PositionName;

            // Labels + Controls
            panel.Children.Add(new TextBlock { Text = "First Name:" });
            panel.Children.Add(firstNameBox);
            panel.Children.Add(new TextBlock { Text = "Middle Name:" });
            panel.Children.Add(middleNameBox);
            panel.Children.Add(new TextBlock { Text = "Last Name:" });
            panel.Children.Add(lastNameBox);
            panel.Children.Add(new TextBlock { Text = "Sex:" });
            panel.Children.Add(sexBox);
            panel.Children.Add(new TextBlock { Text = "Contact:" });
            panel.Children.Add(contactBox);
            panel.Children.Add(new TextBlock { Text = "Position:" });
            panel.Children.Add(positionBox);

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
                return new EmployeeRecord
                {
                    EmployeeID = employee.EmployeeID,
                    FirstName = firstNameBox.Text,
                    MiddleName = middleNameBox.Text,
                    LastName = lastNameBox.Text,
                    Sex = sexBox.Text,
                    Contact = contactBox.Text,
                    PositionName = positionBox.SelectedItem?.ToString()
                };
            }

            return null; 
        }

        // -- Search + Sort --
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void SortBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private void ApplyFilters()
        {
            string searchText = SearchBox.Text?.ToLower() ?? "";

            var filtered = allEmployees.Where(emp =>
                emp.FirstName.ToLower().Contains(searchText) ||
                emp.LastName.ToLower().Contains(searchText) ||
                emp.Contact.ToLower().Contains(searchText) ||
                emp.PositionName.ToLower().Contains(searchText) ||
                emp.Sex.ToLower().Contains(searchText)
            );

            if (SortBox.SelectedItem is ComboBoxItem selectedSort)
            {
                if (selectedSort.Content.ToString() == "Newest")
                    filtered = filtered.OrderByDescending(emp => emp.EmployeeID);
                else if (selectedSort.Content.ToString() == "Oldest")
                    filtered = filtered.OrderBy(emp => emp.EmployeeID);
            }

            employeeDataGrid.ItemsSource = filtered.ToList();
        }
    }

    // -- Employee Model --
    public class EmployeeRecord
    {
        public int EmployeeID { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Sex { get; set; }
        public string Contact { get; set; }
        public string PositionName { get; set; }
    }
}
