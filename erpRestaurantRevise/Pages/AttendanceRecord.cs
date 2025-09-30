using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace erpRestaurantRevise.Pages
{
    public class AttendanceRecord
    {
        public int attendanceID { get; set; }
        public int? employeeID { get; set; }
        public string fullName { get; set; } // Add this property
        public DateTime dateToday { get; set; }
        public TimeSpan? timeIn { get; set; }
        public TimeSpan? timeOut { get; set; }
        public string status { get; set; }
        public decimal? hourWorked { get; set; }
    }
}
