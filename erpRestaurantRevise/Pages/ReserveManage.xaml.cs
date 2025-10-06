using erpRestaurantRevise.Models;
using erpRestaurantRevise.Services;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace practice.Pages
{
    public partial class ReserveManage : Page
    {
        // Add this field to the ReserveManage class to fix CS0103
        private static readonly string connectionString = "MyDbConnection";
        public ReserveManage()
        {
            InitializeComponent();

            // Load all necessary data from database
            ReservationService.LoadTables();
            ReservationService.LoadCustomers();
            ReservationService.LoadReservations();

            LoadData();
        }

        private void LoadData()
        {
            // Bind Pending and Confirmed Reservations
            PendingReservationsList.ItemsSource = ReservationService.GetPendingReservations();
            ConfirmedReservationsGrid.ItemsSource = ReservationService.GetUpcomingReservations();

            // Update available tables count
            UpdateAvailableTablesCount();
        }

        private void UpdateAvailableTablesCount()
        {
            var totalTables = ReservationService.Tables.Count;
            var availableTables = ReservationService.GetAvailableTables().Count;
            AvailableTablesLabel.Content = $"{availableTables}/{totalTables}";
        }

        private void ConfirmReservation_Click(object sender, RoutedEventArgs e)
        {
            var reservation = (sender as Button)?.Tag as Reservation;
            if (reservation == null) return;

            var availableTables = ReservationService.GetAvailableTables();
            var updated = ShowAssignTableDialog(reservation, availableTables);

            if (updated != null && updated.Table != null)
            {
                updated.Status = "Confirmed";
                ReservationService.ConfirmReservationSimple(updated.ReservationID, updated.Table.TableID, updated.Status);
                LoadData();
            }
        }

        private void EditReservation_Click(object sender, RoutedEventArgs e)
        {
            var reservation = (sender as Button)?.Tag as Reservation;
            if (reservation == null) return;

            var availableTables = ReservationService.GetAvailableTables();
            var updated = ShowAssignTableDialog(reservation, availableTables);

            if (updated != null && updated.Table != null)
            {
                updated.Status = "Confirmed";
                ReservationService.ConfirmReservationSimple(updated.ReservationID, updated.Table.TableID, updated.Status);
                LoadData();
            }
        }

        public static void CancelReservationWithReason(int reservationID, string reason)
        {
            var reservation = ReservationService.Reservations.FirstOrDefault(r => r.ReservationID == reservationID);
            if (reservation != null)
            {
                reservation.Status = "Cancelled";
                reservation.Table = null; // Free the table

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"UPDATE Reservation 
                             SET tableID = NULL, status = 'Cancelled', cancelReason = @reason
                             WHERE reservationID = @id";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", reservationID);
                    cmd.Parameters.AddWithValue("@reason", string.IsNullOrEmpty(reason) ? "No reason provided" : reason);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void CancelPendingReservation_Click(object sender, RoutedEventArgs e)
        {
            var reservation = (sender as Button)?.Tag as Reservation;
            if (reservation == null) return;

            // Open a modal for cancellation reason
            Window cancelWindow = new Window
            {
                Title = "Cancel Reservation",
                Width = 350,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(10) };
            panel.Children.Add(new TextBlock { Text = $"Cancel reservation for {reservation.Customer.FirstName} {reservation.Customer.LastName}" });
            panel.Children.Add(new TextBlock { Text = "Reason:" });

            TextBox reasonBox = new TextBox { Height = 60, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true };
            panel.Children.Add(reasonBox);

            Button cancelButton = new Button
            {
                Content = "Cancel Reservation",
                Width = 120,
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = System.Windows.Media.Brushes.Red,
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(0, 10, 0, 0)
            };

            cancelButton.Click += (s, args) =>
            {
                if (MessageBox.Show("Are you sure you want to cancel this reservation?", "Confirm Cancel", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    ReservationService.CancelReservationWithReason(reservation.ReservationID, reasonBox.Text);
                    cancelWindow.DialogResult = true;
                    cancelWindow.Close();
                    LoadData(); // Refresh lists
                }
            };

            panel.Children.Add(cancelButton);
            cancelWindow.Content = panel;
            cancelWindow.ShowDialog();
        }

        private void DoneReservation_Click(object sender, RoutedEventArgs e)
        {
            var reservation = (sender as Button)?.Tag as Reservation;
            if (reservation == null) return;

            if (MessageBox.Show("Mark this reservation as done? This will free up the table.",
                "Confirm Done", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                ReservationService.MarkReservationDone(reservation.ReservationID);
                LoadData(); 
            }
        }

        public static void MarkReservationDone(int reservationID)
        {
            var reservation = ReservationService.Reservations.FirstOrDefault(r => r.ReservationID == reservationID);
            if (reservation != null)
            {
                reservation.Status = "Done";
                reservation.Table = null;

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE Reservation SET tableID=NULL, status='Done' WHERE reservationID=@id";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", reservationID);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static List<Reservation> GetPendingReservations()
        {
            return ReservationService.Reservations
                   .Where(r => r.Table == null && r.Status != "Cancelled" && r.Status != "Done")
                   .ToList();
        }

        // Only show confirmed reservations that are not cancelled or done
        public static List<Reservation> GetUpcomingReservations()
        {
            return ReservationService.Reservations
                   .Where(r => r.Table != null && r.Status != "Cancelled" && r.Status != "Done")
                   .ToList();
        }

        private Reservation ShowAssignTableDialog(Reservation reservation, System.Collections.Generic.List<TableChair> availableTables)
        {
            // Only tables that can fit the party
            var suitableTables = availableTables
                                 .Where(t => t.ChairQuantity >= reservation.NumberOfGuests)
                                 .ToList();

            Window assignWindow = new Window
            {
                Title = reservation.Table == null ? "Confirm Reservation" : "Edit Reservation",
                Width = 350,
                Height = 350,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(10) };

            panel.Children.Add(new TextBlock { Text = $"Customer: {reservation.Customer.FirstName} {reservation.Customer.LastName}" });
            panel.Children.Add(new TextBlock { Text = $"Date: {reservation.DateReserve.ToShortDateString()}" });
            panel.Children.Add(new TextBlock { Text = $"Number of Guests: {reservation.NumberOfGuests}" }); // NEW

            ComboBox tableBox = new ComboBox { Margin = new Thickness(0, 10, 0, 10) };
            foreach (var table in suitableTables)
            {
                tableBox.Items.Add(new ComboBoxItem
                {
                    Content = $"Table {table.TableNumber}  (Tables:{table.TableQuantity}, Chairs:{table.ChairQuantity})",
                    Tag = table.TableID
                });
            }

            panel.Children.Add(new TextBlock { Text = "Assign to Table:" });
            panel.Children.Add(tableBox);

            Button saveButton = new Button
            {
                Content = "Save",
                Width = 80,
                Margin = new Thickness(0, 15, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                IsDefault = true
            };
            saveButton.Click += (s, e) => { assignWindow.DialogResult = true; assignWindow.Close(); };

            panel.Children.Add(saveButton);
            assignWindow.Content = panel;

            if (assignWindow.ShowDialog() == true && tableBox.SelectedItem is ComboBoxItem selectedItem)
            {
                reservation.Table = availableTables.FirstOrDefault(t => t.TableID == (int)selectedItem.Tag);
                return reservation;
            }

            return null;
        }

        private void CancelReservation_Click(object sender, RoutedEventArgs e)
        {
            var reservation = (sender as Button)?.Tag as Reservation;
            if (reservation == null) return;

            // Implement cancellation logic here, or call ReservationService.CancelReservationWithReason
            ReservationService.CancelReservationWithReason(reservation.ReservationID, "Cancelled from Confirmed list");
            LoadData(); // Refresh the lists
        }
    }
}
