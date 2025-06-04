using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;

namespace WeighingSystem.Views
{
    public partial class ReferenceWindow : Window
    {
        private string connectionString = "Data Source=weighing_system.db;Version=3;";
        private string tableName;
        private string columnName;
        private List<ReferenceItem> items;

        public class ReferenceItem
        {
            public string Name { get; set; }
        }

        public ReferenceWindow(string table, string column, string title)
        {
            InitializeComponent();
            tableName = table;
            columnName = column;
            ReferenceLabel.Content = title;
            LoadReferenceData();
        }

        private void LoadReferenceData()
        {
            items = new List<ReferenceItem>();
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT {columnName} FROM {tableName}";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new ReferenceItem { Name = reader.GetString(0) });
                    }
                }
            }
            ReferenceDataGrid.ItemsSource = items;
        }

        private void AddReference_Click(object sender, RoutedEventArgs e)
        {
            items.Add(new ReferenceItem { Name = "" });
            ReferenceDataGrid.ItemsSource = null; // Обновляем привязку
            ReferenceDataGrid.ItemsSource = items;
            ReferenceDataGrid.SelectedIndex = items.Count - 1; // Выделяем новую строку
            ReferenceDataGrid.ScrollIntoView(ReferenceDataGrid.SelectedItem);
        }

        private void DeleteReference_Click(object sender, RoutedEventArgs e)
        {
            if (ReferenceDataGrid.SelectedItem != null)
            {
                var selectedItem = ReferenceDataGrid.SelectedItem as ReferenceItem;
                items.Remove(selectedItem);
                ReferenceDataGrid.ItemsSource = null;
                ReferenceDataGrid.ItemsSource = items;
            }
            else
            {
                MessageBox.Show("Выберите запись для удаления", "Предупреждение");
            }
        }

        private void SaveReference_Click(object sender, RoutedEventArgs e)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();

                // Очистка таблицы
                command.CommandText = $"DELETE FROM {tableName}";
                command.ExecuteNonQuery();

                // Добавление всех записей из списка
                foreach (var item in items)
                {
                    if (!string.IsNullOrWhiteSpace(item.Name))
                    {
                        command.CommandText = $"INSERT OR IGNORE INTO {tableName} ({columnName}) VALUES (@value)";
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@value", item.Name);
                        command.ExecuteNonQuery();
                    }
                }

                // Для складов нужно сохранить тип (source/destination)
                if (tableName == "Warehouses")
                {
                    command.CommandText = "UPDATE Warehouses SET type = @type WHERE name = @name";
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@type", ReferenceLabel.Content.ToString().Contains("отгрузки") ? "source" : "destination");
                    foreach (var item in items)
                    {
                        if (!string.IsNullOrWhiteSpace(item.Name))
                        {
                            command.Parameters.AddWithValue("@name", item.Name);
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
            MessageBox.Show("Справочник сохранен", "Успех");
            Close();
        }

        private void CloseReference_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}