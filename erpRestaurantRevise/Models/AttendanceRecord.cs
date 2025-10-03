using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace erpRestaurantRevise.Models
{
    public class AttendanceRecord
    {
        public int attendanceID { get; set; }   // ✅ new hidden key
        public int? employeeID { get; set; }
        public string fullName { get; set; }
        public TimeSpan? timeIn { get; set; }
        public TimeSpan? timeOut { get; set; }
        public decimal? hourWorked { get; set; }
        public string status { get; set; }
    }

}
