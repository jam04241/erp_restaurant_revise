using erpRestaurantRevise.Models;
using Microsoft.Data.SqlClient;
using practice.Landing_Page;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace erpRestaurantRevise
{
    public partial class MainWindow : Window
    {
        private connDB db = new connDB();

        public MainWindow()
        {
            InitializeComponent();
        }

        private byte[] HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        private bool Login(string email, string password)
        {
            using (SqlConnection conn = db.GetConnection())
            {
                conn.Open();

                string query = @"
                    SELECT employeeID, firstName, lastName
                    FROM Employee
                    WHERE email = @Email 
                      AND passwordHash = @PasswordHash";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.Add("@PasswordHash", System.Data.SqlDbType.VarBinary, 32).Value = HashPassword(password);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Store employee info in session
                            CurrentSession.EmployeeID = Convert.ToInt32(reader["employeeID"]);
                            CurrentSession.EmployeeName = reader["firstName"].ToString() + " " + reader["lastName"].ToString();
                            CurrentSession.Email = email;
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            string email = usernameField.Text.Trim();
            string password = passwordField.Password.Trim();

            if (!Login(email, password))
            {
                MessageBox.Show("Invalid email or password.", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show($"Login successful! Welcome {CurrentSession.EmployeeName}", "Success",
                            MessageBoxButton.OK, MessageBoxImage.Information);

            Mainpage main = new Mainpage();
            main.Show();
            this.Close();
        }
    }
}
