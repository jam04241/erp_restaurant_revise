using erpRestaurantRevise;
using erpRestaurantRevise.Models;
using erpRestaurantRevise.Services;
using Microsoft.Data.SqlClient;
using System;
using System.Windows;
using System.Windows.Controls;

namespace practice.Pages
{
    public partial class ReserveAdd : Page
    {
        private connDB db = new connDB();

        public ReserveAdd()
        {
            InitializeComponent();
        }

        private void submitBtn_Click(object sender, RoutedEventArgs e)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(firstnameField.Text) ||
                string.IsNullOrWhiteSpace(middlenameField.Text) ||
                string.IsNullOrWhiteSpace(lastnameField.Text) ||
                string.IsNullOrWhiteSpace(emailField.Text) ||
                string.IsNullOrWhiteSpace(contactField.Text) ||
                string.IsNullOrWhiteSpace(dateField.Text) ||
                string.IsNullOrWhiteSpace(timeField.Text) ||
                string.IsNullOrWhiteSpace(guestField.Text))
            {
                MessageBox.Show("Please fill in all required fields.");
                return;
            }

            try
            {
                // Parse number of guests
                if (!int.TryParse(guestField.Text.Trim(), out int numberOfGuests) || numberOfGuests <= 0)
                {
                    MessageBox.Show("Please enter a valid number of guests.");
                    return;
                }

                // Create customer object
                Customer customer = new Customer
                {
                    FirstName = firstnameField.Text.Trim(),
                    MiddleName = middlenameField.Text.Trim(),
                    LastName = lastnameField.Text.Trim(),
                    Email = emailField.Text.Trim(),
                    Contact = contactField.Text.Trim()
                };

                // Create reservation object
                Reservation reservation = new Reservation
                {
                    EmployeeID = CurrentSession.EmployeeID, // use logged-in employee
                    DateReserve = DateTime.Parse(dateField.Text.Trim()),
                    TimeReserve = TimeSpan.Parse(timeField.Text.Trim()),
                    Status = "Pending",
                    Table = null, // table assignment will be done in ReserveManage
                    NumberOfGuests = numberOfGuests // <-- NEW
                };

                // Save to database
                ReservationService.AddReservation(customer, reservation);

                MessageBox.Show("Customer and reservation added successfully!");

                // Clear form
                firstnameField.Clear();
                middlenameField.Clear();
                lastnameField.Clear();
                emailField.Clear();
                contactField.Clear();
                dateField.Clear();
                timeField.Clear();
                guestField.Clear();
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid date or time format.");
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show("Database error: " + sqlEx.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
    }
}
