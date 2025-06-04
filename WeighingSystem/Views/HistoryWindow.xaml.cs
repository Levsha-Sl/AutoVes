using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Windows;

namespace WeighingSystem.Views
{
    public partial class HistoryWindow : Window
    {
        private string connectionString = "Data Source=weighing_system.db;Version=3;";

        public HistoryWindow()
        {
            InitializeComponent();
            LoadHistoryData();
        }

        private void LoadHistoryData()
        {
            var data = new List<object>();
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT w.weighing_time, v.license_plate, c.name, d.full_name, w.gross_weight, w.tare_weight, w.net_weight
                    FROM Weighings w
                    JOIN Vehicles v ON w.vehicle_id = v.id
                    JOIN CargoTypes c ON w.cargo_type_id = c.id
                    JOIN Drivers d ON w.driver_id = d.id";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        data.Add(new
                        {
                            Date = reader.GetString(0),
                            Vehicle = reader.GetString(1),
                            Cargo = reader.GetString(2),
                            Driver = reader.GetString(3),
                            Gross = reader.IsDBNull(4) ? "" : reader.GetDouble(4).ToString("F2") + " тн",
                            Tare = reader.IsDBNull(5) ? "" : reader.GetDouble(5).ToString("F2") + " тн",
                            Net = reader.IsDBNull(6) ? "" : reader.GetDouble(6).ToString("F2") + " тн"
                        });
                    }
                }
            }
            HistoryGrid.ItemsSource = data;
        }

        private void ApplyFilters_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Фильтры применены (заглушка)", "Фильтр");
        private void ExportPdf_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Экспорт в PDF (заглушка)", "Экспорт");
        private void ExportExcel_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Экспорт в Excel (заглушка)", "Экспорт");
    }
}