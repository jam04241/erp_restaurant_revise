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
using System.Windows.Navigation;
using System.Windows.Shapes;
using erpRestaurantRevise;
using erpRestaurantRevise.Pages;


namespace practice.Pages
{
    /// <summary>
    /// Interaction logic for Dashboard.xaml
    /// </summary>
    public partial class Dashboard : Page
    {
        private Frame _navigate_Panel;

        public Dashboard(Frame navigate_Panel)
        {
            InitializeComponent();

            _navigate_Panel = navigate_Panel;
        }

        private void positionTableBtn_Click(object sender, RoutedEventArgs e)
        {
            _navigate_Panel.Navigate(new EmpAddPosition()); // from Landing_page
        }
    }
}
