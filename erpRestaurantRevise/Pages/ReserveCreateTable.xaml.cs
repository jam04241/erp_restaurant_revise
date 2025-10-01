using erpRestaurantRevise.Models;
using erpRestaurantRevise.Services;
using Microsoft.Data.SqlClient;
using System;
using System.Windows;
using System.Windows.Controls;

namespace practice.Pages
{
    /// <summary>
    /// Interaction logic for ReserveCreateTable.xaml
    /// </summary>
    public partial class ReserveCreateTable : Page
    {
        public ReserveCreateTable()
        {
            InitializeComponent();

            // Load existing tables into DataGrid
            ReservationService.LoadTables();
            TablesDataGrid.ItemsSource = ReservationService.Tables;
        }

        // Create new table
        private void CreateTableButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(TableNoTextBox.Text.Trim(), out int tableNo))
                {
                    MessageBox.Show("Please enter a valid Table No.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(TableQuantityTextBox.Text.Trim(), out int tableQuantity) || tableQuantity <= 0)
                {
                    MessageBox.Show("Please enter a valid Table Quantity.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(ChairQuantityTextBox.Text.Trim(), out int chairQuantity) || chairQuantity <= 0)
                {
                    MessageBox.Show("Please enter a valid Chair Quantity.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string location = LocationTextBox.Text.Trim();
                if (string.IsNullOrEmpty(location))
                {
                    MessageBox.Show("Please enter a valid Location.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                TableChair table = new TableChair
                {
                    TableNumber = tableNo,
                    TableQuantity = tableQuantity,
                    ChairQuantity = chairQuantity,
                    Location = location
                };

                ReservationService.AddTable(table);
                MessageBox.Show("Table added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                TableNoTextBox.Clear();
                LocationTextBox.Clear();
                TableQuantityTextBox.Clear();
                ChairQuantityTextBox.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Edit selected row using modal-style dialog
        private void EditRowButton_Click(object sender, RoutedEventArgs e)
        {
            if (TablesDataGrid.SelectedItem is TableChair selectedTable)
            {
                TableChair updatedTable = ShowEditTableDialog(selectedTable);
                if (updatedTable != null)
                {
                    try 
                    {
                        ReservationService.UpdateTable(selectedTable.TableID, updatedTable);
                        TablesDataGrid.Items.Refresh();
                        MessageBox.Show("Record Updated Successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (SqlException sqlEx)
                    {
                        MessageBox.Show("Database error: " + sqlEx.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // Delete selected row
        private void DeleteRowButton_Click(object sender, RoutedEventArgs e)
        {
            if (TablesDataGrid.SelectedItem is TableChair selectedTable)
            {
                var result = MessageBox.Show($"Are you sure you want to delete Table {selectedTable.TableNumber}?",
                                             "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        ReservationService.DeleteTable(selectedTable.TableID);
                        TablesDataGrid.Items.Refresh();
                    }
                    catch (SqlException sqlEx)
                    {
                        MessageBox.Show("Database error: " + sqlEx.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // Modal-style edit dialog
        private TableChair ShowEditTableDialog(TableChair table)
        {
            Window editWindow = new Window
            {
                Title = "Edit Table",
                Width = 350,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(10) };

            // TextBoxes
            TextBox tableNoBox = new TextBox { Text = table.TableNumber.ToString(), Margin = new Thickness(0, 5, 0, 5) };
            TextBox tableQtyBox = new TextBox { Text = table.TableQuantity.ToString(), Margin = new Thickness(0, 5, 0, 5) };
            TextBox chairQtyBox = new TextBox { Text = table.ChairQuantity.ToString(), Margin = new Thickness(0, 5, 0, 5) };
            TextBox locationBox = new TextBox { Text = table.Location, Margin = new Thickness(0, 5, 0, 5) };

            // Labels + Controls
            panel.Children.Add(new TextBlock { Text = "Table No:" });
            panel.Children.Add(tableNoBox);
            panel.Children.Add(new TextBlock { Text = "Station:" });
            panel.Children.Add(locationBox);
            panel.Children.Add(new TextBlock { Text = "Table Quantity:" });
            panel.Children.Add(tableQtyBox);
            panel.Children.Add(new TextBlock { Text = "Chair Quantity:" });
            panel.Children.Add(chairQtyBox);
            

            // Save button
            Button saveButton = new Button
            {
                Content = "Save",
                Width = 80,
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                IsDefault = true
            };
            saveButton.Click += (s, e) => { editWindow.DialogResult = true; editWindow.Close(); };

            panel.Children.Add(saveButton);
            editWindow.Content = panel;

            if (editWindow.ShowDialog() == true)
            {
                return new TableChair
                {
                    TableID = table.TableID, // keep same ID
                    TableNumber = int.Parse(tableNoBox.Text.Trim()),
                    TableQuantity = int.Parse(tableQtyBox.Text.Trim()),
                    ChairQuantity = int.Parse(chairQtyBox.Text.Trim()),
                    Location = locationBox.Text.Trim()
                };
            }

            return null;
        }
    }
}
