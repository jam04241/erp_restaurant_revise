using erpRestaurantRevise;
using erpRestaurantRevise.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace practice.Pages
{
    public partial class EmpSalaryRecord : Page, INotifyPropertyChanged
    {
        private connDB db = new connDB();
        private ObservableCollection<PayrollRecord> _payrollRecords = new ObservableCollection<PayrollRecord>();
        private ObservableCollection<PayrollRecord> _filteredPayrollRecords = new ObservableCollection<PayrollRecord>();
        private string _searchText = "";
        private string _sortBy = "Newest First";

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<PayrollRecord> FilteredPayrollRecords
        {
            get => _filteredPayrollRecords;
            set
            {
                _filteredPayrollRecords = value;
                OnPropertyChanged(nameof(FilteredPayrollRecords));
            }
        }

        public EmpSalaryRecord()
        {
            InitializeComponent();
            DataContext = this;
            LoadAllPayrollRecords();

            // Set up event handlers
            SortComboBox.SelectionChanged += SortComboBox_SelectionChanged;
            SortComboBox.SelectedIndex = 0;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (searchTextBox.Text != "Name / Employee no.")
            {
                _searchText = searchTextBox.Text.ToLower();
                ApplyFilterAndSort();
            }
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SortComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                _sortBy = selectedItem.Content.ToString();
                ApplyFilterAndSort();
            }
        }

        private void ApplyFilterAndSort()
        {
            if (_payrollRecords == null) return;

            var filtered = _payrollRecords.Where(record =>
                string.IsNullOrEmpty(_searchText) ||
                record.EmployeeName.ToLower().Contains(_searchText) ||
                record.EmployeeID.ToString().Contains(_searchText) ||
                record.PayrollID.ToString().Contains(_searchText));

            IOrderedEnumerable<PayrollRecord> sorted;

            switch (_sortBy)
            {
                case "Newest First":
                    sorted = filtered.OrderByDescending(record => record.DateIssued).ThenByDescending(record => record.PayrollID);
                    break;
                case "Oldest First":
                    sorted = filtered.OrderBy(record => record.DateIssued).ThenBy(record => record.PayrollID);
                    break;
                case "Name (A-Z)":
                    sorted = filtered.OrderBy(record => record.EmployeeName);
                    break;
                case "Name (Z-A)":
                    sorted = filtered.OrderByDescending(record => record.EmployeeName);
                    break;
                case "Net Pay (High-Low)":
                    sorted = filtered.OrderByDescending(record => record.NetPay);
                    break;
                case "Net Pay (Low-High)":
                    sorted = filtered.OrderBy(record => record.NetPay);
                    break;
                default:
                    sorted = filtered.OrderByDescending(record => record.DateIssued).ThenByDescending(record => record.PayrollID);
                    break;
            }

            FilteredPayrollRecords = new ObservableCollection<PayrollRecord>(sorted);
        }

        private void LoadAllPayrollRecords()
        {
            try
            {
                string query = @"
                SELECT 
                    p.PayrollID,
                    p.EmployeeID,
                    e.firstName + ' ' + COALESCE(e.middleName + ' ', '') + e.lastName as EmployeeName,
                    (SELECT ISNULL(SUM(hourWorked), 0) 
                     FROM Attendance 
                     WHERE employeeID = p.EmployeeID 
                     AND dateToday BETWEEN p.payPeriodStart AND p.payPeriodEnd) as TotalHours,
                    p.BasicPay,
                    p.OvertimePay,
                    p.Deductions,
                    p.NetPay,
                    p.dateIssue
                FROM Payroll p
                INNER JOIN Employee e ON p.EmployeeID = e.EmployeeID
                ORDER BY p.dateIssue DESC, p.PayrollID DESC";

                DataTable dt = db.GetData(query);
                _payrollRecords.Clear();

                foreach (DataRow row in dt.Rows)
                {
                    var record = new PayrollRecord
                    {
                        PayrollID = Convert.ToInt32(row["PayrollID"]),
                        EmployeeID = Convert.ToInt32(row["EmployeeID"]),
                        EmployeeName = row["EmployeeName"].ToString(),
                        TotalHours = Convert.ToDouble(row["TotalHours"]),
                        BasicPay = Convert.ToDecimal(row["BasicPay"]),
                        OvertimePay = Convert.ToDecimal(row["OvertimePay"]),
                        Deduction = Convert.ToDecimal(row["Deductions"]),
                        NetPay = Convert.ToDecimal(row["NetPay"]),
                        DateIssued = Convert.ToDateTime(row["dateIssue"])
                    };

                    _payrollRecords.Add(record);
                }

                ApplyFilterAndSort(); // Apply initial filter and sort
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading payroll records: " + ex.Message);
            }
        }

        private void SearchRecords()
        {
            string searchText = searchTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(searchText) || searchText == "Name / Employee no.")
            {
                LoadAllPayrollRecords();
                return;
            }

            _searchText = searchText.ToLower();
            ApplyFilterAndSort();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchRecords();
        }

        // Add Enter key support for search
        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchRecords();
            }
        }

        // Clear search when textbox gets focus
        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (searchTextBox.Text == "Name / Employee no.")
            {
                searchTextBox.Text = "";
            }
        }

        // Reset placeholder text when textbox loses focus and is empty
        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(searchTextBox.Text))
            {
                searchTextBox.Text = "Name / Employee no.";
                _searchText = "";
                ApplyFilterAndSort();
            }
        }
    }
}