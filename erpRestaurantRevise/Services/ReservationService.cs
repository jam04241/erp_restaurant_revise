using erpRestaurantRevise.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;

namespace erpRestaurantRevise.Services
{
    public enum ReservationStatus
    {
        Pending,
        Confirmed,
        Cancelled,
        Done
    }

    public static class ReservationService
    {
        private static string connectionString = ConfigurationManager.ConnectionStrings["MyDbConnection"].ConnectionString;

        public static ObservableCollection<Customer> Customers { get; private set; } = new ObservableCollection<Customer>();
        public static ObservableCollection<TableChair> Tables { get; private set; } = new ObservableCollection<TableChair>();
        public static ObservableCollection<Reservation> Reservations { get; private set; } = new ObservableCollection<Reservation>();

        // ---------------- Load Customers ----------------
        public static void LoadCustomers()
        {
            Customers.Clear();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var query = "SELECT * FROM Customer";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Customers.Add(new Customer
                    {
                        CustomerID = Convert.ToInt32(reader["customerID"]),
                        FirstName = reader["firstName"].ToString(),
                        MiddleName = reader["middleName"].ToString(),
                        LastName = reader["lastName"].ToString(),
                        Email = reader["email"].ToString(),
                        Contact = reader["contact"].ToString()
                    });
                }
            }
        }

        // ---------------- Load Tables ----------------
        public static void LoadTables()
        {
            Tables.Clear();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var query = "SELECT * FROM TableChair";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Tables.Add(new TableChair
                    {
                        TableID = reader["tableID"] != DBNull.Value ? Convert.ToInt32(reader["tableID"]) : 0,
                        TableNumber = reader["tableNumber"] != DBNull.Value ? Convert.ToInt32(reader["tableNumber"]) : 0,
                        TableQuantity = reader["tableQuantity"] != DBNull.Value ? Convert.ToInt32(reader["tableQuantity"]) : 0,
                        ChairQuantity = reader["chairQuantity"] != DBNull.Value ? Convert.ToInt32(reader["chairQuantity"]) : 0,
                        Location = reader["location"] != DBNull.Value ? reader["location"].ToString() : ""
                    });
                }
            }
        }

        // ---------------- Load Reservations ----------------
        public static void LoadReservations()
        {
            Reservations.Clear();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var query = @"
                            SELECT r.reservationID, r.customerID, r.tableID, r.employeeID, r.dateReserve, r.timeReserve, r.status, r.numberOfGuests,
                                   c.firstName, c.middleName, c.lastName, c.email, c.contact,
                                   t.tableNumber, t.tableQuantity, t.chairQuantity, t.location
                            FROM Reservation r
                            INNER JOIN Customer c ON r.customerID = c.customerID
                            LEFT JOIN TableChair t ON r.tableID = t.tableID";

                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Reservations.Add(new Reservation
                    {
                        ReservationID = Convert.ToInt32(reader["reservationID"]),
                        EmployeeID = Convert.ToInt32(reader["employeeID"]),
                        DateReserve = Convert.ToDateTime(reader["dateReserve"]),
                        TimeReserve = (TimeSpan)reader["timeReserve"],
                        NumberOfGuests = reader["numberOfGuests"] != DBNull.Value ? Convert.ToInt32(reader["numberOfGuests"]) : 1,
                        Status = reader["status"].ToString(),
                        Customer = new Customer
                        {
                            CustomerID = Convert.ToInt32(reader["customerID"]),
                            FirstName = reader["firstName"].ToString(),
                            MiddleName = reader["middleName"].ToString(),
                            LastName = reader["lastName"].ToString(),
                            Email = reader["email"].ToString(),
                            Contact = reader["contact"].ToString()
                        },
                        Table = reader["tableID"] != DBNull.Value
                            ? new TableChair
                            {
                                TableID = Convert.ToInt32(reader["tableID"]),
                                TableNumber = Convert.ToInt32(reader["tableNumber"]),
                                TableQuantity = Convert.ToInt32(reader["tableQuantity"]),
                                ChairQuantity = Convert.ToInt32(reader["chairQuantity"]),
                                Location = reader["location"].ToString()
                            }
                            : null
                    });
                }
            }
        }

        // ---------------- Add Reservation ----------------
        public static void AddReservation(Customer customer, Reservation reservation)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Insert Customer
                var insertCustomer = @"INSERT INTO Customer (firstName, middleName, lastName, email, contact)
                                       VALUES (@firstName,@middleName,@lastName,@email,@contact);
                                       SELECT SCOPE_IDENTITY();";
                SqlCommand cmd = new SqlCommand(insertCustomer, conn);
                cmd.Parameters.AddWithValue("@firstName", customer.FirstName);
                cmd.Parameters.AddWithValue("@middleName", customer.MiddleName);
                cmd.Parameters.AddWithValue("@lastName", customer.LastName);
                cmd.Parameters.AddWithValue("@email", customer.Email);
                cmd.Parameters.AddWithValue("@contact", customer.Contact);
                int customerId = Convert.ToInt32(cmd.ExecuteScalar());
                customer.CustomerID = customerId;

                var insertReservation = @"INSERT INTO Reservation 
                          (customerID, tableID, employeeID, dateReserve, timeReserve, status, numberOfGuests)
                          VALUES (@customerID, @tableID, @employeeID, @dateReserve, @timeReserve, @status, @numberOfGuests);
                          SELECT SCOPE_IDENTITY();";
                SqlCommand cmd2 = new SqlCommand(insertReservation, conn);
                cmd2.Parameters.AddWithValue("@customerID", customerId);
                cmd2.Parameters.AddWithValue("@tableID", reservation.Table?.TableID ?? (object)DBNull.Value);
                cmd2.Parameters.AddWithValue("@employeeID", reservation.EmployeeID);
                cmd2.Parameters.AddWithValue("@dateReserve", reservation.DateReserve);
                cmd2.Parameters.AddWithValue("@timeReserve", reservation.TimeReserve);
                cmd2.Parameters.AddWithValue("@status", reservation.Status);
                cmd2.Parameters.AddWithValue("@numberOfGuests", reservation.NumberOfGuests);

                reservation.ReservationID = Convert.ToInt32(cmd2.ExecuteScalar());
                reservation.Customer = customer;

                Customers.Add(customer);
                Reservations.Add(reservation);
            }
        }

        // ---------------- Confirm Reservation ----------------
        public static void ConfirmReservationSimple(int reservationID, int tableID, string status)
        {
            var reservation = Reservations.FirstOrDefault(r => r.ReservationID == reservationID);
            if (reservation != null)
            {
                reservation.Table = Tables.FirstOrDefault(t => t.TableID == tableID);
                reservation.Status = status;

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                UPDATE Reservation
                SET tableID = @tableID,
                    status = @status
                WHERE reservationID = @id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@tableID", tableID);
                        cmd.Parameters.AddWithValue("@status", status);
                        cmd.Parameters.AddWithValue("@id", reservationID);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        // ---------------- Cancel Reservation ----------------
        public static void CancelReservationWithReason(int reservationID, string reason)
        {
            var reservation = Reservations.FirstOrDefault(r => r.ReservationID == reservationID);
            if (reservation != null)
            {
                reservation.Status = "Cancelled";
                reservation.Table = null; // Free the table

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"UPDATE Reservation 
                             SET tableID = NULL, status = 'Cancelled', cancelReason = @reason
                             WHERE reservationID = @id";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", reservationID);
                    cmd.Parameters.AddWithValue("@reason", string.IsNullOrEmpty(reason) ? "No reason provided" : reason);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ---------------- Mark Reservation Done ----------------
        public static void MarkReservationDone(int reservationID)
        {
            var reservation = Reservations.FirstOrDefault(r => r.ReservationID == reservationID);
            if (reservation != null)
            {
                int? tableId = reservation.Table?.TableID;

                reservation.Status = ReservationStatus.Done.ToString();
                reservation.Table = null;

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE Reservation SET status=@status, tableID=NULL WHERE reservationID=@id";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@status", ReservationStatus.Done.ToString());
                    cmd.Parameters.AddWithValue("@id", reservationID);
                    cmd.ExecuteNonQuery();

                    // free table explicitly (optional, handled by GetAvailableTables)
                    if (tableId.HasValue)
                    {
                        string updateTable = "UPDATE TableChair SET tableQuantity = tableQuantity WHERE tableID=@tableID";
                        SqlCommand cmd2 = new SqlCommand(updateTable, conn);
                        cmd2.Parameters.AddWithValue("@tableID", tableId.Value);
                        cmd2.ExecuteNonQuery();
                    }
                }
            }
        }

        // ---------------- Get Reservations ----------------
        public static List<Reservation> GetPendingReservations()
        {
            return Reservations.Where(r => r.Status == ReservationStatus.Pending.ToString()).ToList();
        }

        public static List<Reservation> GetUpcomingReservations()
        {
            return Reservations.Where(r => r.Status == ReservationStatus.Confirmed.ToString()).ToList();
        }

        public static List<Reservation> GetDoneReservations()
        {
            return Reservations.Where(r => r.Status == ReservationStatus.Done.ToString()).ToList();
        }

        // ---------------- Get Available Tables ----------------
        public static List<TableChair> GetAvailableTables()
        {
            var assignedTableIds = Reservations
                .Where(r => r.Table != null && r.Status == ReservationStatus.Confirmed.ToString())
                .Select(r => r.Table.TableID)
                .ToList();

            return Tables.Where(t => !assignedTableIds.Contains(t.TableID)).ToList();
        }
    }
}
