using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using erpRestaurantRevise;


namespace practice.Pages
{
    public partial class EmpAdd : Page
    {
        private connDB db = new connDB();

        public EmpAdd()
        {
            InitializeComponent();
            LoadPositions();
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
                        while (reader.Read())
                        {
                            positions.Add(new Position
                            {
                                PositionID = Convert.ToInt32(reader["positionID"]),
                                PositionName = reader["position"].ToString()
                            });
                        }

                        positionComboBox.ItemsSource = positions;
                        positionComboBox.DisplayMemberPath = "PositionName";  // what user sees
                        positionComboBox.SelectedValuePath = "PositionID";   // what is used as value
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading positions: " + ex.Message);
            }
        }

        private void CreateEmployee_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (positionComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Please select a position.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Default password (can be changed later)
                string defaultPassword = "1234";
                byte[] passwordHash;
                using (SHA256 sha256 = SHA256.Create())
                {
                    passwordHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(defaultPassword));
                }

                using (SqlConnection conn = db.GetConnection())
                {
                    conn.Open();

                    string query = @"INSERT INTO Employee
                            (firstName, middleName, lastName, contact, status, sex, cityProvince, barangay, street, email, positionID, passwordHash)
                             VALUES (@firstName, @middleName, @lastName, @contactNo, @status, @sex, @cityProvince, @barangay, @street, @email, @positionID, @passwordHash)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@firstName", firstnameField.Text.Trim());
                        cmd.Parameters.AddWithValue("@middleName", middlenameField.Text.Trim());
                        cmd.Parameters.AddWithValue("@lastName", lastnameField.Text.Trim());
                        cmd.Parameters.AddWithValue("@contactNo", contactField.Text.Trim());
                        cmd.Parameters.AddWithValue("@status", (statusComboBox.SelectedItem as ComboBoxItem)?.Content.ToString());
                        cmd.Parameters.AddWithValue("@sex", (sexComboBox.SelectedItem as ComboBoxItem)?.Content.ToString());
                        cmd.Parameters.AddWithValue("@cityProvince", cityprovinceField.Text.Trim());
                        cmd.Parameters.AddWithValue("@barangay", brgyField.Text.Trim());
                        cmd.Parameters.AddWithValue("@street", streetField.Text.Trim());
                        cmd.Parameters.AddWithValue("@email", emailField.Text.Trim());

                        // Only use PositionID
                        cmd.Parameters.AddWithValue("@positionID", positionComboBox.SelectedValue);
                        cmd.Parameters.AddWithValue("@passwordHash", passwordHash);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Employee created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Clear fields
                firstnameField.Clear();
                middlenameField.Clear();
                lastnameField.Clear();
                contactField.Clear();
                cityprovinceField.Clear();
                brgyField.Clear();
                streetField.Clear();
                emailField.Clear();
                statusComboBox.SelectedIndex = -1;
                sexComboBox.SelectedIndex = -1;
                positionComboBox.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Helper class for binding positions
    public class Position
    {
        public int PositionID { get; set; }
        public string PositionName { get; set; }
    }
}
