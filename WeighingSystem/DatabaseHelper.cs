using System;
using System.Data.SQLite;
using System.IO;

namespace WeighingSystem
{
    public static class DatabaseHelper
    {
        private static string DbPath => "weighing_system.db";

        static DatabaseHelper()
        {
            InitializeDatabase();
        }

        private static void InitializeDatabase()
        {
            if (!File.Exists(DbPath))
            {
                SQLiteConnection.CreateFile(DbPath);
            }

            using (var connection = new SQLiteConnection($"Data Source={DbPath};Version=3;"))
            {
                connection.Open();
                var command = connection.CreateCommand();

                // Создание таблиц
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Weighings (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        vehicle TEXT NOT NULL,
                        cargo_type TEXT NOT NULL,
                        source_warehouse TEXT,
                        destination_warehouse TEXT,
                        counterparty TEXT,
                        driver TEXT,
                        gross_weight REAL NOT NULL,
                        weighing_time TEXT DEFAULT CURRENT_TIMESTAMP
                    );

                    CREATE TABLE IF NOT EXISTS Users (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        username TEXT NOT NULL UNIQUE,
                        password_hash TEXT NOT NULL,
                        role TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS Roles (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        role_name TEXT NOT NULL UNIQUE,
                        can_generate_reports INTEGER NOT NULL,
                        can_edit_settings INTEGER NOT NULL
                    );";
                command.ExecuteNonQuery();
            }
        }

        public static SQLiteConnection GetConnection()
        {
            return new SQLiteConnection($"Data Source={DbPath};Version=3;");
        }
    }
}