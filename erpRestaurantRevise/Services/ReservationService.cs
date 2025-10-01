using erpRestaurantRevise.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;

namespace erpRestaurantRevise.Services
{
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

        public static void AddTable(TableChair table)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var insertQuery = @"
                    INSERT INTO TableChair (tableNumber, tableQuantity, chairQuantity, location)
                    VALUES (@tableNumber, @tableQuantity, @chairQuantity, @location);
                    SELECT SCOPE_IDENTITY();";
                SqlCommand cmd = new SqlCommand(insertQuery, conn);
                cmd.Parameters.AddWithValue("@tableNumber", table.TableNumber);
                cmd.Parameters.AddWithValue("@tableQuantity", table.TableQuantity);
                cmd.Parameters.AddWithValue("@chairQuantity", table.ChairQuantity);
                cmd.Parameters.AddWithValue("@location", table.Location);

                table.TableID = Convert.ToInt32(cmd.ExecuteScalar());
                Tables.Add(table);
            }
        }

        public static void UpdateTable(int tableID, TableChair updatedTable)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var query = @"
                    UPDATE TableChair
                    SET TableNumber=@tableNumber,
                        TableQuantity=@tableQuantity,
                        ChairQuantity=@chairQuantity,
                        Location=@location
                    WHERE TableID=@tableID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@tableID", tableID);
                cmd.Parameters.AddWithValue("@tableNumber", updatedTable.TableNumber);
                cmd.Parameters.AddWithValue("@tableQuantity", updatedTable.TableQuantity);
                cmd.Parameters.AddWithValue("@chairQuantity", updatedTable.ChairQuantity);
                cmd.Parameters.AddWithValue("@location", updatedTable.Location);
                cmd.ExecuteNonQuery();
            }

            // Update in-memory collection
            var existing = Tables.FirstOrDefault(t => t.TableID == tableID);
            if (existing != null)
            {
                existing.TableNumber = updatedTable.TableNumber;
                existing.TableQuantity = updatedTable.TableQuantity;
                existing.ChairQuantity = updatedTable.ChairQuantity;
                existing.Location = updatedTable.Location;
            }
        }

        public static void DeleteTable(int tableID)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var query = "DELETE FROM TableChair WHERE TableID=@tableID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@tableID", tableID);
                cmd.ExecuteNonQuery();
            }

            var table = Tables.FirstOrDefault(t => t.TableID == tableID);
            if (table != null)
                Tables.Remove(table);
        }

        // ---------------- Load Reservations ----------------
        public static void LoadReservations()
        {
            Reservations.Clear();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var query = @"
                    SELECT r.reservationID, r.customerID, r.tableID, r.employeeID, r.dateReserve, r.timeReserve, r.status,
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

        // ---------------- Add Customer + Reservation ----------------
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

                // Insert Reservation
                var insertReservation = @"INSERT INTO Reservation (customerID, tableID, employeeID, dateReserve, timeReserve, status)
                                          VALUES (@customerID,@tableID,@employeeID,@dateReserve,@timeReserve,@status);
                                          SELECT SCOPE_IDENTITY();";
                SqlCommand cmd2 = new SqlCommand(insertReservation, conn);
                cmd2.Parameters.AddWithValue("@customerID", customerId);
                cmd2.Parameters.AddWithValue("@tableID", reservation.Table?.TableID ?? (object)DBNull.Value);
                cmd2.Parameters.AddWithValue("@employeeID", reservation.EmployeeID);
                cmd2.Parameters.AddWithValue("@dateReserve", reservation.DateReserve);
                cmd2.Parameters.AddWithValue("@timeReserve", reservation.TimeReserve);
                cmd2.Parameters.AddWithValue("@status", reservation.Status);

                reservation.ReservationID = Convert.ToInt32(cmd2.ExecuteScalar());
                reservation.Customer = customer;

                Customers.Add(customer);
                Reservations.Add(reservation);
            }
        }

        // ---------------- Confirm Reservation ----------------
        public static void ConfirmReservation(int reservationID, int tableID, string status)
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

        // ---------------- Delete Reservation ----------------
        public static void DeleteReservation(int reservationID)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM Reservation WHERE reservationID=@id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", reservationID);
                cmd.ExecuteNonQuery();
            }

            var reservation = Reservations.FirstOrDefault(r => r.ReservationID == reservationID);
            if (reservation != null)
                Reservations.Remove(reservation);
        }

        // ---------------- Get Pending/Upcoming ----------------
        public static List<Reservation> GetPendingReservations()
        {
            return Reservations.Where(r => r.Table == null).ToList();
        }

        public static List<Reservation> GetUpcomingReservations()
        {
            return Reservations.Where(r => r.Table != null).ToList();
        }

        // ---------------- Get Available Tables ----------------
        public static List<TableChair> GetAvailableTables()
        {
            var assignedTableIds = Reservations.Where(r => r.Table != null).Select(r => r.Table.TableID).ToList();
            return Tables.Where(t => !assignedTableIds.Contains(t.TableID)).ToList();
        }
    }
}
