using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace erpRestaurantRevise.Pages
{
    public class AttendanceRow
    {
        public int EmployeeID { get; set; }
        public string FullName { get; set; }
        public string PositionName { get; set; }

        // For attendance input
        public TimeSpan? TimeIn { get; set; }
        public TimeSpan? TimeOut { get; set; }
        public double HourWorked { get; set; }
    }
}
