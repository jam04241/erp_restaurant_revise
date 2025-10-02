using erpRestaurantRevise.Models;
using erpRestaurantRevise.Services;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;

namespace practice.Pages
{
    public partial class ReservationRecord : Page
    {
        public ReservationRecord()
        {
            InitializeComponent();
            LoadRecords();
        }

        private void LoadRecords()
        {
            // Ensure reservations are loaded from DB first
            ReservationService.LoadReservations();

            var doneCancelled = ReservationService.Reservations
                .Where(r => r.Status == "Done" || r.Status == "Cancelled")
                .Select(r => new
                {
                    CustomerFullName = $"{r.Customer.FirstName} {r.Customer.MiddleName} {r.Customer.LastName}",
                    r.DateReserve,
                    r.TimeReserve,
                    TableNumber = r.Table != null ? r.Table.TableNumber.ToString() : "N/A",
                    r.NumberOfGuests,
                    r.Status
                })
                .ToList();

            RecordsDataGrid.ItemsSource = doneCancelled;
        }

        // Color-code rows
        private void RecordsDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            dynamic rowData = e.Row.Item;
            string status = rowData?.Status;

            if (status == "Done")
                e.Row.Background = new SolidColorBrush(Colors.DarkGreen);
            else if (status == "Cancelled")
                e.Row.Background = new SolidColorBrush(Colors.DarkRed);

            e.Row.Foreground = new SolidColorBrush(Colors.White);
        }
    }
}
