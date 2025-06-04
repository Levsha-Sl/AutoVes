using System;
using System.Collections.Generic;
using System.Windows;
using WeighingSystem.Services;

namespace WeighingSystem.Views
{
    public partial class SettingsWindow : Window
    {
        private DatabaseFirstCode databaseFirstCode;

        public SettingsWindow()
        {
            InitializeComponent();

            databaseFirstCode = new DatabaseFirstCode();

            LoadUsersData();
            LoadRolesData();
        }

        // Вкладка "Настройки весов"
        private void SaveScalesSettings_Click(object sender, RoutedEventArgs e)
        {
            string address = ServiceAddressTextBox.Text;
            string port = PortTextBox.Text;
            string pollRate = PollRateTextBox.Text;
            string discreteness = DiscretenessTextBox.Text;
            MessageBox.Show($"Настройки весов сохранены:\nАдрес: {address}\nПорт: {port}\nСкорость опроса: {pollRate}\nДискретность: {discreteness}", "Успех");
        }

        // Вкладка "База данных"
        private void BackupDb_Click(object sender, RoutedEventArgs e)
        {
            System.IO.File.Copy("weighing_system.db", $"weighing_system_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db", true);
            MessageBox.Show("Резервное копирование выполнено", "База данных");
        }

        private void FillDemoData_Click(object sender, RoutedEventArgs e)
        {
            if (databaseFirstCode.FillDemoData())
            {
                MessageBox.Show("Демо-данные добавлены", "База данных");
            }
        }

        private void ClearDb_Click(object sender, RoutedEventArgs e)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    DELETE FROM WeighingPhotos;
                    DELETE FROM Weighings;
                    DELETE FROM Vehicles;
                    DELETE FROM CargoTypes;
                    DELETE FROM Warehouses;
                    DELETE FROM Counterparties;
                    DELETE FROM Drivers;
                    DELETE FROM Users;";
                command.ExecuteNonQuery();
                MessageBox.Show("База данных очищена", "База данных");
            }
        }

        // Вкладка "Пользователи"
        private void LoadUsersData()
        {
            var users = new List<User>();
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT username, role FROM Users";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new User { Username = reader.GetString(0), Role = reader.GetString(1) });
                    }
                }
            }
            UsersGrid.ItemsSource = users;
        }

        private void SaveUsers_Click(object sender, RoutedEventArgs e)
        {
            var users = (List<User>)UsersGrid.ItemsSource;
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Users";
                command.ExecuteNonQuery();

                foreach (var user in users)
                {
                    command.CommandText = "INSERT INTO Users (username, password_hash, role) VALUES (@username, 'hash', @role)";
                    command.Parameters.AddWithValue("@username", user.Username);
                    command.Parameters.AddWithValue("@role", user.Role);
                    command.ExecuteNonQuery();
                    command.Parameters.Clear();
                }
            }
            MessageBox.Show("Изменения пользователей сохранены", "Пользователи");
        }

        // Вкладка "Роли" (заглушка, т.к. роли пока не хранятся в базе)
        private void LoadRolesData()
        {
            var roles = new List<Role>
            {
                new Role { RoleName = "operator", CanGenerateReports = true, CanEditSettings = false },
                new Role { RoleName = "admin", CanGenerateReports = true, CanEditSettings = true }
            };
            RolesGrid.ItemsSource = roles;
        }

        private void SaveRoles_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Изменения ролей сохранены (заглушка)", "Роли");
        }
    }

    public class User
    {
        public string Username { get; set; }
        public string Role { get; set; }
    }

    public class Role
    {
        public string RoleName { get; set; }
        public bool CanGenerateReports { get; set; }
        public bool CanEditSettings { get; set; }
    }
}