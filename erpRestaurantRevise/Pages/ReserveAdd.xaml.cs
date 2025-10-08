using erpRestaurantRevise;
using erpRestaurantRevise.Models;
using erpRestaurantRevise.Services;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace practice.Pages
{
    public partial class ReserveAdd : Page
    {
        private connDB db = new connDB();
        private Frame _navigate_Panel;

        public ReserveAdd(Frame navigate_Panel)
        {
            InitializeComponent();
            _navigate_Panel = navigate_Panel;
            InitializeTimePicker();
        }

        private void InitializeTimePicker()
        {
            try
            {
                // Initialize hours (00-23) for military time
                var hours = new List<string>();
                for (int i = 0; i <= 23; i++)
                {
                    hours.Add(i.ToString("00"));
                }
                hourComboBox.ItemsSource = hours;

                // Set default to current hour or a reasonable time like 18:00 (6 PM)
                int currentHour = DateTime.Now.Hour;
                hourComboBox.SelectedItem = currentHour.ToString("00");

                // Initialize minutes (00, 15, 30, 45)
                var minutes = new List<string> { "00", "15", "30", "45" };
                minuteComboBox.ItemsSource = minutes;

                // Set default minutes to 00
                minuteComboBox.SelectedItem = "00";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing time picker: {ex.Message}");
            }
        }

        private void submitBtn_Click(object sender, RoutedEventArgs e)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(firstnameField.Text) ||
                string.IsNullOrWhiteSpace(middlenameField.Text) ||
                string.IsNullOrWhiteSpace(lastnameField.Text) ||
                string.IsNullOrWhiteSpace(emailField.Text) ||
                string.IsNullOrWhiteSpace(contactField.Text) ||
                datePicker.SelectedDate == null ||
                hourComboBox.SelectedItem == null ||
                minuteComboBox.SelectedItem == null ||
                string.IsNullOrWhiteSpace(guestField.Text))
            {
                MessageBox.Show("Please fill in all required fields (*).", "Missing Information",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Parse number of guests
                if (!int.TryParse(guestField.Text.Trim(), out int numberOfGuests) || numberOfGuests <= 0)
                {
                    MessageBox.Show("Please enter a valid number of guests (must be greater than 0).",
                                  "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate date is not in the past
                if (datePicker.SelectedDate.Value.Date < DateTime.Now.Date)
                {
                    MessageBox.Show("Reservation date cannot be in the past.", "Invalid Date",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Create TimeSpan from military time components
                int hour = int.Parse(hourComboBox.SelectedItem.ToString());
                int minute = int.Parse(minuteComboBox.SelectedItem.ToString());
                TimeSpan timeReserve = new TimeSpan(hour, minute, 0);

                // Validate time for restaurant hours (example: 8:00 AM to 10:00 PM)
                if (timeReserve < new TimeSpan(8, 0, 0) || timeReserve > new TimeSpan(22, 0, 0))
                {
                    MessageBox.Show("Reservation time must be between 08:00 and 22:00.", "Invalid Time",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    DateReserve = datePicker.SelectedDate.Value,
                    TimeReserve = timeReserve,
                    Status = "Pending",
                    Table = null, // table assignment will be done in ReserveManage
                    NumberOfGuests = numberOfGuests
                };

                // Save to database
                ReservationService.AddReservation(customer, reservation);

                MessageBox.Show($"Reservation added successfully!\n\nDate: {datePicker.SelectedDate.Value:MMM dd, yyyy}\nTime: {timeReserve:hh\\:mm}\nStatus: Pending",
                              "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Clear form
                ClearForm();
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show($"Database error: {sqlEx.Message}", "Database Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearForm()
        {
            firstnameField.Clear();
            middlenameField.Clear();
            lastnameField.Clear();
            emailField.Clear();
            contactField.Clear();
            guestField.Clear();
            datePicker.SelectedDate = DateTime.Now;

            // Reset time to current time or default
            int currentHour = DateTime.Now.Hour;
            hourComboBox.SelectedItem = currentHour.ToString("00");
            minuteComboBox.SelectedItem = "00";

            // Set focus to first field
            firstnameField.Focus();
        }

        private void backBtn_Click(object sender, RoutedEventArgs e)
        {
            _navigate_Panel.Navigate(new ReserveManage(_navigate_Panel));
        }
    }
}