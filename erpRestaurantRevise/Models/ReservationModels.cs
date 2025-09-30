using System;

namespace erpRestaurantRevise.Models
{
    public class Customer
    {
        public int CustomerID { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Contact { get; set; }
    }

    public class TableChair
    {
        public int TableID { get; set; }
        public int TableNumber { get; set; }        
        public int TableQuantity { get; set; }     
        public int ChairQuantity { get; set; }      
        public string Location { get; set; }
    }

    public class Reservation
    {
        public int ReservationID { get; set; }
        public Customer Customer { get; set; }
        public TableChair Table { get; set; }
        public int EmployeeID { get; set; }
        public DateTime DateReserve { get; set; }
        public TimeSpan TimeReserve { get; set; }
        public string Status { get; set; }
    }
}
