using System;
using System.Data.SQLite;
using System.Threading.Tasks;
using WeighingSystem.Services.Scale;

namespace WeighingSystem.Services
{
    public class ScaleService
    {
        private readonly string connectionString = "Data Source=weighing_system.db;Version=3;";
        private CWTcpScales scales;
        private bool isRunning;
        private int pollInterval;

        public bool isConnect = false;

        public ScaleService()
        {
            LoadSettings();
            scales = new CWTcpScales(ipAddress, port);
            isConnect = scales.Initialize();
        }

        private string ipAddress;
        private int port;

        private void LoadSettings()
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT ip_address, port, poll_interval FROM ScaleSettings WHERE id = 1";
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        ipAddress = reader.GetString(0);
                        port = reader.GetInt32(1);
                        pollInterval = reader.GetInt32(2);
                    }
                    else
                    {
                        ipAddress = "127.0.0.1";
                        port = 11600;
                        pollInterval = 200;
                        command.CommandText = "INSERT INTO ScaleSettings (id, ip_address, port, poll_interval) VALUES (1, @ip, @port, @interval)";
                        command.Parameters.AddWithValue("@ip", ipAddress);
                        command.Parameters.AddWithValue("@port", port);
                        command.Parameters.AddWithValue("@interval", pollInterval);
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public void Reload()
        {
            scales = new CWTcpScales(ipAddress, port);
            isConnect = scales.Initialize();
        }

        public void SetZero()
        {
            try
            {
                scales.SetZero();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обнулении весов: {ex.Message}");
            }
        }

        public async Task StartPolling(Func<int?, Task> onWeightReceived)
        {
            isRunning = true;
            while (isRunning)
            {
                try
                {
                    int? weight = scales.GetWeight();
                    //Console.WriteLine($"Получен вес: {weight} кг"); // Отладка
                    await onWeightReceived(weight);
                    //Console.WriteLine("Callback вызван"); // Отладка
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при опросе весов: {ex.Message}");
                    await onWeightReceived(null);
                }
                await Task.Delay(pollInterval);
            }
        }

        public void StopPolling()
        {
            isRunning = false;
            scales?.Dispose();
        }
    }
}