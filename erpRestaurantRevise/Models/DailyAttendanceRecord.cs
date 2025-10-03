using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace erpRestaurantRevise.Models
{
    public class DailyAttendanceRecord
    {
        public int EmployeeID { get; set; }
        public string FullName { get; set; }
        public TimeSpan? TimeIn { get; set; }
        public TimeSpan? TimeOut { get; set; }
        public string Status { get; set; }
    }
}
