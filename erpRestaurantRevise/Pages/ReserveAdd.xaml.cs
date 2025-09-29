using erpRestaurantRevise;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
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

namespace practice.Pages
{
    /// <summary>
    /// Interaction logic for ReserveAdd.xaml
    /// </summary>
    public partial class ReserveAdd : Page
    {
        private connDB db = new connDB();

        public ReserveAdd()
        {
            InitializeComponent();

        }

        private void submitBtn_Click(object sender, RoutedEventArgs e)
        {
            using (SqlConnection conn = db.GetConnection())
            {
                try
                {

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }

            }
        }
    }
}
