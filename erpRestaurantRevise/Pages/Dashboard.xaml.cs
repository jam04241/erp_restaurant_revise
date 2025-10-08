using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Legends;
using erpRestaurantRevise;
using erpRestaurantRevise.Pages;

namespace practice.Pages
{
    // Position data class for DataGrid binding
    public class PositionData
    {
        public string Position { get; set; }
        public decimal BaseSalary { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal Overtime { get; set; }
    }

    public partial class Dashboard : Page
    {
        private Frame _navigate_Panel;
        private connDB db = new connDB();

        public Dashboard(Frame navigate_Panel)
        {
            InitializeComponent();
            _navigate_Panel = navigate_Panel;

            // Set current date
            txtCurrentDate.Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy");

            // Load all analytics data
            LoadDashboardData();
        }

        private void positionTableBtn_Click(object sender, RoutedEventArgs e)
        {
            _navigate_Panel.Navigate(new EmpAddPosition());
        }

        private void LoadDashboardData()
        {
            try
            {
                LoadKPICards();
                LoadAttendanceChart();
                LoadReservationChart();
                LoadPayrollChart();
                LoadPositionData();
                LoadPositionSalaryChart();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard data: {ex.Message}\n\nPlease check your database connection string.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region KPI Cards

        private void LoadKPICards()
        {
            using (SqlConnection conn = db.GetConnection())
            {
                conn.Open();

                // Total Employees
                string empQuery = @"
                    SELECT 
                        COUNT(*) as Total,
                        SUM(CASE WHEN status = 'Active' THEN 1 ELSE 0 END) as Active
                    FROM Employee";

                using (SqlCommand cmd = new SqlCommand(empQuery, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        txtTotalEmployees.Text = reader["Total"].ToString();
                        txtActiveEmployees.Text = $"Active: {reader["Active"]}";
                    }
                }

                // Today's Attendance
                string attQuery = @"
                    SELECT 
                        COUNT(*) as Total,
                        SUM(CASE WHEN status IN ('Present', 'Late') THEN 1 ELSE 0 END) as Present
                    FROM Attendance
                    WHERE CAST(dateToday AS DATE) = CAST(GETDATE() AS DATE)";

                using (SqlCommand cmd = new SqlCommand(attQuery, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int total = reader["Total"] != DBNull.Value ? Convert.ToInt32(reader["Total"]) : 0;
                        int present = reader["Present"] != DBNull.Value ? Convert.ToInt32(reader["Present"]) : 0;
                        double rate = total > 0 ? (present * 100.0 / total) : 0;

                        txtAttendanceRate.Text = $"{rate:0}%";
                        txtPresentToday.Text = $"Present: {present}/{total}";
                    }
                }

                // Monthly Payroll
                string payQuery = @"
                    SELECT ISNULL(SUM(netPay), 0) as TotalPayroll
                    FROM Payroll
                    WHERE MONTH(dateIssue) = MONTH(GETDATE()) 
                        AND YEAR(dateIssue) = YEAR(GETDATE())";

                using (SqlCommand cmd = new SqlCommand(payQuery, conn))
                {
                    object result = cmd.ExecuteScalar();
                    decimal totalPayroll = result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                    txtMonthlyPayroll.Text = $"₱{totalPayroll:N2}";
                }

                // Today's Reservations
                string resQuery = @"
                    SELECT 
                        COUNT(*) as Today,
                        (SELECT COUNT(*) FROM Reservation 
                         WHERE dateReserve BETWEEN CAST(GETDATE() AS DATE) AND DATEADD(DAY, 7, CAST(GETDATE() AS DATE))
                         AND status IN ('Confirmed', 'Pending')) as Upcoming
                    FROM Reservation
                    WHERE CAST(dateReserve AS DATE) = CAST(GETDATE() AS DATE)";

                using (SqlCommand cmd = new SqlCommand(resQuery, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        txtTodayReservations.Text = reader["Today"].ToString();
                        txtUpcomingReservations.Text = $"Upcoming: {reader["Upcoming"]}";
                    }
                }
            }
        }

        #endregion

        #region Attendance Chart

        private void LoadAttendanceChart()
        {
            var plotModel = new PlotModel
            {
                Background = OxyColors.Transparent,
                PlotAreaBorderColor = OxyColors.White,
                TextColor = OxyColors.White
            };

            // Add legend for OxyPlot 2.2.0
            var legend = new Legend
            {
                LegendTextColor = OxyColors.White,
                LegendPosition = LegendPosition.TopRight,
                LegendPlacement = LegendPlacement.Inside
            };
            plotModel.Legends.Add(legend);

            using (SqlConnection conn = db.GetConnection())
            {
                conn.Open();

                string query = @"
                    SELECT 
                        DATENAME(WEEKDAY, dateToday) as DayName,
                        DATEPART(WEEKDAY, dateToday) as DayNum,
                        COUNT(*) as Total,
                        SUM(CASE WHEN status IN ('Present', 'Late') THEN 1 ELSE 0 END) as Present
                    FROM Attendance
                    WHERE dateToday >= DATEADD(DAY, -6, CAST(GETDATE() AS DATE))
                    GROUP BY DATENAME(WEEKDAY, dateToday), DATEPART(WEEKDAY, dateToday)
                    ORDER BY DayNum";

                var presentSeries = new BarSeries { Title = "Present", FillColor = OxyColor.FromRgb(74, 255, 136) };
                var absentSeries = new BarSeries { Title = "Absent", FillColor = OxyColor.FromRgb(255, 74, 74) };
                var categoryAxis = new CategoryAxis { Position = AxisPosition.Left, TextColor = OxyColors.White };

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string dayName = reader["DayName"].ToString().Substring(0, 3);
                        int total = Convert.ToInt32(reader["Total"]);
                        int present = Convert.ToInt32(reader["Present"]);

                        categoryAxis.Labels.Add(dayName);
                        presentSeries.Items.Add(new BarItem { Value = present });
                        absentSeries.Items.Add(new BarItem { Value = total - present });
                    }
                }

                plotModel.Axes.Add(categoryAxis);
                plotModel.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    TextColor = OxyColors.White,
                    MinimumPadding = 0.1,
                    MaximumPadding = 0.1
                });

                plotModel.Series.Add(presentSeries);
                plotModel.Series.Add(absentSeries);
            }

            AttendanceChart.Model = plotModel;
        }

        #endregion

        #region Reservation Chart

        private void LoadReservationChart()
        {
            var plotModel = new PlotModel
            {
                Background = OxyColors.Transparent,
                PlotAreaBorderColor = OxyColors.Transparent,
                TextColor = OxyColors.White
            };

            // Add legend for pie chart
            var legend = new Legend
            {
                LegendTextColor = OxyColors.White,
                LegendPosition = LegendPosition.RightMiddle,
                LegendPlacement = LegendPlacement.Outside
            };
            plotModel.Legends.Add(legend);

            var pieSeries = new PieSeries
            {
                StrokeThickness = 2.0,
                InsideLabelPosition = 0.8,
                AngleSpan = 360,
                StartAngle = 0,
                InsideLabelColor = OxyColors.White,
                TextColor = OxyColors.White
            };

            using (SqlConnection conn = db.GetConnection())
            {
                conn.Open();

                string query = @"
                    SELECT 
                        status,
                        COUNT(*) as Count
                    FROM Reservation
                    WHERE dateReserve >= DATEADD(MONTH, -1, GETDATE())
                    GROUP BY status";

                var colorMap = new Dictionary<string, OxyColor>
                {
                    { "Confirmed", OxyColor.FromRgb(74, 255, 136) },
                    { "Pending", OxyColor.FromRgb(255, 170, 74) },
                    { "Cancelled", OxyColor.FromRgb(255, 74, 74) },
                    { "Completed", OxyColor.FromRgb(74, 158, 255) }
                };

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string status = reader["status"].ToString();
                        int count = Convert.ToInt32(reader["Count"]);

                        pieSeries.Slices.Add(new PieSlice(status, count)
                        {
                            Fill = colorMap.ContainsKey(status) ? colorMap[status] : OxyColor.FromRgb(128, 128, 128),
                            IsExploded = false
                        });
                    }
                }
            }

            plotModel.Series.Add(pieSeries);
            ReservationChart.Model = plotModel;
        }

        #endregion

        #region Payroll Chart

        private void LoadPayrollChart()
        {
            var plotModel = new PlotModel
            {
                Background = OxyColors.Transparent,
                PlotAreaBorderColor = OxyColors.White,
                TextColor = OxyColors.White
            };

            var lineSeries = new LineSeries
            {
                Title = "Net Payroll",
                Color = OxyColor.FromRgb(255, 170, 74),
                StrokeThickness = 3,
                MarkerType = MarkerType.Circle,
                MarkerSize = 6,
                MarkerFill = OxyColor.FromRgb(255, 170, 74)
            };

            using (SqlConnection conn = db.GetConnection())
            {
                conn.Open();

                string query = @"
                    SELECT 
                        FORMAT(dateIssue, 'MMM') as Month,
                        MONTH(dateIssue) as MonthNum,
                        ISNULL(SUM(netPay), 0) as TotalPayroll
                    FROM Payroll
                    WHERE dateIssue >= DATEADD(MONTH, -5, GETDATE())
                    GROUP BY FORMAT(dateIssue, 'MMM'), MONTH(dateIssue), YEAR(dateIssue)
                    ORDER BY YEAR(dateIssue), MONTH(dateIssue)";

                var categoryAxis = new CategoryAxis { Position = AxisPosition.Bottom, TextColor = OxyColors.White };
                int index = 0;

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string month = reader["Month"].ToString();
                        double totalPayroll = Convert.ToDouble(reader["TotalPayroll"]);

                        categoryAxis.Labels.Add(month);
                        lineSeries.Points.Add(new DataPoint(index++, totalPayroll));
                    }
                }

                plotModel.Axes.Add(categoryAxis);
                plotModel.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Left,
                    TextColor = OxyColors.White,
                    StringFormat = "₱#,0",
                    MinimumPadding = 0.1,
                    MaximumPadding = 0.1
                });

                plotModel.Series.Add(lineSeries);
            }

            PayrollChart.Model = plotModel;
        }

        #endregion

        #region Position Salary Chart

        private void LoadPositionSalaryChart()
        {
            var plotModel = new PlotModel
            {
                Background = OxyColors.Transparent,
                PlotAreaBorderColor = OxyColors.White,
                TextColor = OxyColors.White
            };

            var barSeries = new BarSeries
            {
                Title = "Average Salary",
                FillColor = OxyColor.FromRgb(255, 170, 74),
                StrokeThickness = 1
            };

            using (SqlConnection conn = db.GetConnection())
            {
                conn.Open();

                string query = @"
                    SELECT 
                        ep.position,
                        AVG(ep.baseSalary) as AvgSalary
                    FROM EmployeePosition ep
                    INNER JOIN Employee e ON ep.positionID = e.positionID
                    WHERE e.status = 'Active'
                    GROUP BY ep.position
                    ORDER BY AvgSalary DESC";

                var categoryAxis = new CategoryAxis { Position = AxisPosition.Left, TextColor = OxyColors.White };

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string position = reader["position"].ToString();
                        double avgSalary = Convert.ToDouble(reader["AvgSalary"]);

                        categoryAxis.Labels.Add(position);
                        barSeries.Items.Add(new BarItem { Value = avgSalary });
                    }
                }

                plotModel.Axes.Add(categoryAxis);
                plotModel.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    TextColor = OxyColors.White,
                    StringFormat = "₱#,0",
                    MinimumPadding = 0.1,
                    MaximumPadding = 0.1
                });

                plotModel.Series.Add(barSeries);
            }

            PositionSalaryChart.Model = plotModel;
        }

        #endregion

        #region Position Data Grid

        private void LoadPositionData()
        {
            try
            {
                List<PositionData> positions = new List<PositionData>();

                using (SqlConnection conn = db.GetConnection())
                {
                    conn.Open();

                    string query = @"
                    SELECT 
                        position,
                        baseSalary,
                        hourlyRate,
                        overtime
                    FROM EmployeePosition 
                    ORDER BY baseSalary DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            positions.Add(new PositionData
                            {
                                Position = reader["position"].ToString(),
                                BaseSalary = Convert.ToDecimal(reader["baseSalary"]),
                                HourlyRate = Convert.ToDecimal(reader["hourlyRate"]),
                                Overtime = Convert.ToDecimal(reader["overtime"])
                            });
                        }
                    }
                }

                // Set the DataGrid items source
                dgPositions.ItemsSource = positions;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading position data: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}