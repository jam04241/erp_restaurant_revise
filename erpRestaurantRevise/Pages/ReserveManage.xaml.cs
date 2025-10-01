using erpRestaurantRevise.Models;
using erpRestaurantRevise.Services;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace practice.Pages
{
    public partial class ReserveManage : Page
    {
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
            ConfirmedReservationsList.ItemsSource = ReservationService.GetUpcomingReservations();

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
                ReservationService.ConfirmReservation(updated.ReservationID, updated.Table.TableID, updated.Status);
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
                ReservationService.ConfirmReservation(updated.ReservationID, updated.Table.TableID, updated.Status);
                LoadData();
            }
        }

        private void CancelReservation_Click(object sender, RoutedEventArgs e)
        {
            var reservation = (sender as Button)?.Tag as Reservation;
            if (reservation == null) return;

            if (MessageBox.Show("Are you sure you want to cancel this reservation?",
                "Confirm Cancel", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                ReservationService.DeleteReservation(reservation.ReservationID);
                LoadData();
            }
        }

        private Reservation ShowAssignTableDialog(Reservation reservation, System.Collections.Generic.List<TableChair> availableTables)
        {
            Window assignWindow = new Window
            {
                Title = reservation.Table == null ? "Confirm Reservation" : "Edit Reservation",
                Width = 350,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(10) };

            panel.Children.Add(new TextBlock { Text = $"Customer: {reservation.Customer.FirstName} {reservation.Customer.LastName}" });
            panel.Children.Add(new TextBlock { Text = $"Date: {reservation.DateReserve.ToShortDateString()}" });

            ComboBox tableBox = new ComboBox { Margin = new Thickness(0, 10, 0, 10) };
            foreach (var table in availableTables)
            {
                ComboBoxItem item = new ComboBoxItem
                {
                    Content = $"Table {table.TableNumber} (T:{table.TableQuantity}, C:{table.ChairQuantity})",
                    Tag = table.TableID,
                    IsSelected = reservation.Table != null && reservation.Table.TableID == table.TableID
                };
                tableBox.Items.Add(item);
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
                int selectedTableID = (int)selectedItem.Tag;
                reservation.Table = availableTables.FirstOrDefault(t => t.TableID == selectedTableID);
                return reservation;
            }

            return null;
        }
    }
}
