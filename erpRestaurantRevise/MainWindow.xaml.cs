using practice.Landing_Page;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace erpRestaurantRevise
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // 🔹 Hash password with SHA256 (same as stored in DB)
        private byte[] HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        // 🔹 Validate login from DB
        private bool Login(string email, string password)
        {
            string connectionString = ConfigurationManager
                                        .ConnectionStrings["MyDbConnection"]
                                        .ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string query = @"
                    SELECT COUNT(*) 
                    FROM Employee 
                    WHERE email = @Email 
                      AND passwordHash = @PasswordHash";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.Add("@Email", SqlDbType.NVarChar).Value = email;
                    cmd.Parameters.Add("@PasswordHash", SqlDbType.VarBinary, 32).Value = HashPassword(password);

                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        // 🔹 Login button click
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
            else
            {
                MessageBox.Show("Login successful!", "Success",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            Mainpage main = new Mainpage();
            main.Show();
            this.Close();
        }
    }
}
