using System;
using System.Data.SQLite;
using System.IO;
using System.Collections.Generic;
using System.Data;

namespace WeighingSystem
{
    public static class DatabaseHelper
    {
        private static string DbPath => "weighing_system.db";

        public static bool IsCreateFile()
        {
            return File.Exists(DbPath);
        }

        public static void InitializeDatabase()
        {
            SQLiteConnection.CreateFile(DbPath);
        }

        public static SQLiteConnection GetConnection()
        {
            return new SQLiteConnection($"Data Source={DbPath};Version=3;");
        }

        public static int ExecuteNonQuery(SQLiteConnection connection, string commandText, Dictionary<string, object> parameters = null, bool lastInsertRowId = false)
        {
            var result = -2;
            using (SQLiteCommand command = new SQLiteCommand(commandText, connection))
            {
                if (parameters != null)
                {
                    AddParameters(command, parameters);
                }

                result = command.ExecuteNonQuery();

                if (lastInsertRowId)
                {
                    command.CommandText = "SELECT last_insert_rowid();";
                    result = Convert.ToInt32(command.ExecuteScalar());
                }
            }
            return result;
        }


        public static int SQLiteCommand(string commandText, Dictionary<string, object> parameters = null, bool lastInsertRowId = false)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                return ExecuteNonQuery(connection, commandText, parameters, lastInsertRowId);
            }
        }

        private static bool SQLiteCommandRead(string commandText, Action<SQLiteDataReader> processRow, Dictionary<string, object> parameters = null)
        {
            var result = false;
            using (var connection = GetConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = commandText;

                if (parameters != null)
                {
                    AddParameters(command, parameters);
                }

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result = true;
                        processRow(reader);
                        while (reader.Read()) processRow(reader);
                    }
                    else
                    {
                        result = false;
                    }
                }
            }
            return result;
        }

        public static bool SQLiteCommand(string commandText, Action<SQLiteDataReader> processRow)
        {
            return SQLiteCommandRead(commandText, processRow);
        }

        public static bool SQLiteCommand(string commandText, Dictionary<string, object> parameters, Action<SQLiteDataReader> processRow)
        {
            return SQLiteCommandRead(commandText, processRow, parameters);
        }

        private static void AddParameters(SQLiteCommand command, Dictionary<string, object> parameters)
        {
            foreach (var param in parameters)
            {
                SQLiteParameter parameter = new SQLiteParameter
                {
                    ParameterName = param.Key,
                    Value = param.Value ?? DBNull.Value // Если значение null, передаем DBNull.Value
                };

                // Автоматическое определение типа данных
                if (param.Value != null)
                {
                    parameter.DbType = GetDbType(param.Value.GetType());
                }

                command.Parameters.Add(parameter);
            }
        }

        // Метод для определения DbType на основе типа .NET
        private static DbType GetDbType(Type type)
        {
            if (type == typeof(int) || type == typeof(int?))
                return DbType.Int32;
            if (type == typeof(string))
                return DbType.String;
            if (type == typeof(DateTime) || type == typeof(DateTime?))
                return DbType.DateTime;
            if (type == typeof(bool) || type == typeof(bool?))
                return DbType.Boolean;
            if (type == typeof(decimal) || type == typeof(decimal?))
                return DbType.Decimal;
            if (type == typeof(double) || type == typeof(double?))
                return DbType.Double;
            if (type == typeof(float) || type == typeof(float?))
                return DbType.Single;
            if (type == typeof(byte[]))
                return DbType.Binary;
            if (type == typeof(Guid) || type == typeof(Guid?))
                return DbType.Guid;

            // По умолчанию возвращаем String
            return DbType.String;
        }
    }
}