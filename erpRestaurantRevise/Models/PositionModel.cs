using erpRestaurantRevise.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace erpRestaurantRevise.Pages
{
    public class PositionModel
    {
        public int PositionID { get; set; }
        public string Position { get; set; }
        public decimal BaseSalary { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal Overtime { get; set; }
    }

}