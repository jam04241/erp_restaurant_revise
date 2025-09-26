using practice.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace practice.Landing_Page
{
    /// <summary>
    /// Interaction logic for Mainpage.xaml
    /// </summary>
    public partial class Mainpage : Window
    {
        public Mainpage()
        {
            InitializeComponent();
        }

        private void DashboardBtn_Click(object sender, RoutedEventArgs e)
        {

            Navigate_Panel.Navigate(new Dashboard());
        }

        private void EmployeeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (employeePanel.Visibility == Visibility.Collapsed)
                employeePanel.Visibility = Visibility.Visible;

            else
                employeePanel.Visibility = Visibility.Collapsed;
                payrollPanel.Visibility = Visibility.Collapsed;
                customerManagePanel.Visibility = Visibility.Collapsed;
                attendancePanel.Visibility = Visibility.Collapsed;
        }

        private void CustomerManageBtn_Click(object sender, RoutedEventArgs e)
        {
            if (customerManagePanel.Visibility == Visibility.Collapsed)
                customerManagePanel.Visibility = Visibility.Visible;

            else
                customerManagePanel.Visibility = Visibility.Collapsed;
                payrollPanel.Visibility = Visibility.Collapsed;  
                attendancePanel.Visibility = Visibility.Collapsed;
                employeePanel.Visibility = Visibility.Collapsed;

        }

        private void PayrollBtn_Click(object sender, RoutedEventArgs e)
        {
            if (payrollPanel.Visibility == Visibility.Collapsed)
                payrollPanel.Visibility = Visibility.Visible;

            else
                payrollPanel.Visibility = Visibility.Collapsed;
                attendancePanel.Visibility = Visibility.Collapsed;
                customerManagePanel.Visibility = Visibility.Collapsed;
                employeePanel.Visibility = Visibility.Collapsed;
        }

        private void AttendanceBtn_Click(object sender, RoutedEventArgs e)
        {
            if (attendancePanel.Visibility == Visibility.Collapsed)
                attendancePanel.Visibility = Visibility.Visible;

            else
                attendancePanel.Visibility = Visibility.Collapsed;
                payrollPanel.Visibility = Visibility.Collapsed;
                customerManagePanel.Visibility = Visibility.Collapsed;
                employeePanel.Visibility = Visibility.Collapsed;
        }

        private void empaddBtn_Click(object sender, RoutedEventArgs e)
        {
            Navigate_Panel.Navigate(new EmpAdd());
        }

        private void createAttendanceBtn_Click(object sender, RoutedEventArgs e)
        {
            Navigate_Panel.Navigate(new EmpCreateAttendance());
        }

        private void attendanceRecordsBtn_Click(object sender, RoutedEventArgs e)
        {
            Navigate_Panel.Navigate(new EmpAttendanceRecord());
        }

        private void customerReservationBtn_Click(object sender, RoutedEventArgs e)
        {
            Navigate_Panel.Navigate(new ReserveManage());
        }

        private void empmanagedBtn_Click(object sender, RoutedEventArgs e)
        {
            Navigate_Panel.Navigate(new EmpRecord());
        }

        private void customerTableBtn_Click(object sender, RoutedEventArgs e)
        {
            Navigate_Panel.Navigate(new ReserveCreateTable());
        }

        private void addCustomersBtn_Click(object sender, RoutedEventArgs e)
        {
           Navigate_Panel.Navigate(new ReserveAdd());
        }

        private void reservationListBtn_Click(object sender, RoutedEventArgs e)
        {
            Navigate_Panel.Navigate(new ReservationList());
        }

        private void reservationRecordsBtn_Click(object sender, RoutedEventArgs e)
        {
            Navigate_Panel.Navigate(new ReservationRecord());
        }
    }
}
