using erpRestaurantRevise.Models;
using erpRestaurantRevise.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace practice.Pages
{
    public partial class ReservationRecord : Page
    {
        private List<Reservation> _allRecords;

        public ReservationRecord()
        {
            InitializeComponent();
            LoadRecords();
            InitializeFilters();
        }

        private void LoadRecords()
        {
            try
            {
                // Ensure reservations are loaded from DB first
                ReservationService.LoadReservations();

                _allRecords = ReservationService.Reservations
                    .Where(r => r.Status == "Done" || r.Status == "Cancelled" || r.Status == "Confirmed" || r.Status == "Pending" || r.Status == "Completed")
                    .ToList();

                ApplyFilters();
                UpdateSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading reservation records: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeFilters()
        {
            // Set default date range to last 30 days
            StartDatePicker.SelectedDate = DateTime.Now.AddDays(-30);
            EndDatePicker.SelectedDate = DateTime.Now;

            // Set up event handlers
            SearchTextBox.TextChanged += SearchTextBox_TextChanged;
            StartDatePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
            EndDatePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
            StatusFilterComboBox.SelectionChanged += StatusFilterComboBox_SelectionChanged;
        }

        private void ApplyFilters()
        {
            if (_allRecords == null) return;

            var filteredRecords = _allRecords.AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                string searchText = SearchTextBox.Text.ToLower();
                filteredRecords = filteredRecords.Where(r =>
                    r.CustomerName.ToLower().Contains(searchText) ||
                    (r.Customer != null &&
                     (r.Customer.FirstName.ToLower().Contains(searchText) ||
                      r.Customer.MiddleName.ToLower().Contains(searchText) ||
                      r.Customer.LastName.ToLower().Contains(searchText))));
            }

            // Apply date range filter
            if (StartDatePicker.SelectedDate.HasValue)
            {
                filteredRecords = filteredRecords.Where(r => r.DateReserve >= StartDatePicker.SelectedDate.Value);
            }

            if (EndDatePicker.SelectedDate.HasValue)
            {
                filteredRecords = filteredRecords.Where(r => r.DateReserve <= EndDatePicker.SelectedDate.Value);
            }

            // Apply status filter
            string selectedStatus = (StatusFilterComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (selectedStatus != null && selectedStatus != "All Status")
            {
                filteredRecords = filteredRecords.Where(r => r.Status == selectedStatus);
            }

            // Convert to display format for DataGrid
            var displayRecords = filteredRecords.Select(r => new
            {
                CustomerFullName = r.Customer != null ?
                    $"{r.Customer.FirstName} {r.Customer.MiddleName} {r.Customer.LastName}" : "Unknown Customer",
                r.DateReserve,
                r.TimeReserve,
                TableNumber = r.Table != null ? r.Table.TableNumber.ToString() : "N/A",
                r.NumberOfGuests,
                r.Status
            }).ToList();

            RecordsDataGrid.ItemsSource = displayRecords;
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            if (_allRecords == null) return;

            var currentRecords = RecordsDataGrid.ItemsSource as IEnumerable<dynamic> ??
                                _allRecords.Select(r => new
                                {
                                    CustomerFullName = r.Customer != null ?
                                        $"{r.Customer.FirstName} {r.Customer.MiddleName} {r.Customer.LastName}" : "Unknown Customer",
                                    r.DateReserve,
                                    r.TimeReserve,
                                    TableNumber = r.Table != null ? r.Table.TableNumber.ToString() : "N/A",
                                    r.NumberOfGuests,
                                    r.Status
                                });

            int total = currentRecords.Count();
            int confirmed = currentRecords.Count(r => r.Status == "Confirmed");
            int pending = currentRecords.Count(r => r.Status == "Pending");
            int cancelled = currentRecords.Count(r => r.Status == "Cancelled");
            int completed = currentRecords.Count(r => r.Status == "Completed" || r.Status == "Done");

            TotalReservationsText.Text = total.ToString();
            ConfirmedText.Text = confirmed.ToString();
            PendingText.Text = pending.ToString();
            CancelledText.Text = cancelled.ToString();
        }

        // Event Handlers
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        // Clear Filters Function
        private void ClearFilters()
        {
            SearchTextBox.Text = string.Empty;
            StartDatePicker.SelectedDate = DateTime.Now.AddDays(-30);
            EndDatePicker.SelectedDate = DateTime.Now;
            StatusFilterComboBox.SelectedIndex = 0;

            ApplyFilters();
        }

        // Export to Excel Function
        private void ExportToExcel()
        {
            try
            {
                var records = RecordsDataGrid.ItemsSource as IEnumerable<dynamic>;
                if (records == null || !records.Any())
                {
                    MessageBox.Show("No records to export.", "Information",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // TODO: Implement Excel export logic here
                MessageBox.Show($"Exporting {records.Count()} records to Excel...\n\nThis feature requires Excel export libraries to be installed.", "Export",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to Excel: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Refresh Data Function
        private void RefreshData()
        {
            LoadRecords();
        }

        // Color-code rows
        private void RecordsDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (e.Row.Item == null) return;

            dynamic rowData = e.Row.Item;
            string status = rowData?.Status;

            // Use theme colors
            if (status == "Done" || status == "Completed")
                e.Row.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
            else if (status == "Cancelled")
                e.Row.Background = new SolidColorBrush(Color.FromRgb(229, 57, 53)); // Red
            else if (status == "Confirmed")
                e.Row.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Blue
            else if (status == "Pending")
                e.Row.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
            else
                e.Row.Background = new SolidColorBrush(Color.FromRgb(13, 45, 44)); // Default dark

            e.Row.Foreground = new SolidColorBrush(Colors.White);
        }

        // Button click handlers
        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            ClearFilters();
        }

        private void ExportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshData();
        }

        // Method to get reservation statistics for reports
        public (int total, int confirmed, int pending, int cancelled, int completed) GetReservationStatistics()
        {
            if (_allRecords == null) return (0, 0, 0, 0, 0);

            return (
                total: _allRecords.Count,
                confirmed: _allRecords.Count(r => r.Status == "Confirmed"),
                pending: _allRecords.Count(r => r.Status == "Pending"),
                cancelled: _allRecords.Count(r => r.Status == "Cancelled"),
                completed: _allRecords.Count(r => r.Status == "Completed" || r.Status == "Done")
            );
        }
    }
}