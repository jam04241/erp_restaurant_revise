using System;
using System.ComponentModel;

namespace erpRestaurantRevise.Models
{
    public class DailyAttendanceRecord : INotifyPropertyChanged
    {
        public int EmployeeID { get; set; }
        public string FullName { get; set; }

        private TimeSpan? _timeIn;
        public TimeSpan? TimeIn
        {
            get => _timeIn;
            set
            {
                if (_timeIn != value)
                {
                    _timeIn = value;
                    OnPropertyChanged(nameof(TimeIn));
                    OnPropertyChanged(nameof(CanTimeIn));
                    OnPropertyChanged(nameof(CanTimeOut));
                }
            }
        }

        private TimeSpan? _timeOut;
        public TimeSpan? TimeOut
        {
            get => _timeOut;
            set
            {
                if (_timeOut != value)
                {
                    _timeOut = value;
                    OnPropertyChanged(nameof(TimeOut));
                    OnPropertyChanged(nameof(CanTimeOut));
                }
            }
        }

        private string _status;
        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(CanTimeIn));
                    OnPropertyChanged(nameof(CanTimeOut));
                }
            }
        }

        private decimal? _hourWorked;
        public decimal? HourWorked
        {
            get => _hourWorked;
            set
            {
                if (_hourWorked != value)
                {
                    _hourWorked = value;
                    OnPropertyChanged(nameof(HourWorked));
                }
            }
        }

        private DateTime? _dateToday;
        public DateTime? DateToday
        {
            get => _dateToday;
            set
            {
                if (_dateToday != value)
                {
                    _dateToday = value;
                    OnPropertyChanged(nameof(DateToday));
                }
            }
        }

        public bool CanTimeIn => !TimeIn.HasValue && Status != "Absent";
        public bool CanTimeOut => TimeIn.HasValue && !TimeOut.HasValue && Status != "Absent";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
