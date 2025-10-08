using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace erpRestaurantRevise.Models
{
    // -- Employee Model --
    public class EmployeeRecord
    {
        public int EmployeeID { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Sex { get; set; }
        public string Contact { get; set; }
        public string Status { get; set; }
        public string PositionName { get; set; }
    }
}
