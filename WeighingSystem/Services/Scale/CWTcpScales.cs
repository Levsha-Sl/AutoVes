using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace WeighingSystem.Services.Scale
{
    public class CWTcpScales : IDisposable
    {
        //TODO Сделать обработку ошибок, и так же опрокидывания их выше.
        private readonly string ipAddress;
        private readonly int port;
        private TcpClient client;
        private NetworkStream stream;

        public CWTcpScales(string ipAddress, int port)
        {
            this.ipAddress = ipAddress;
            this.port = port;
        }

        public bool Initialize()
        {
            try
            {
                Connect();
                SendCommand("Start", "");
                Disconnect();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка инициализации весов: {ex.Message}");
                return false;
            }
        }

        private void Connect()
        {
            client = new TcpClient();
            client.Connect(ipAddress, port);
            stream = client.GetStream();
        }

        private void Disconnect()
        {
            stream?.Close();
            client?.Close();
            stream?.Dispose();
            client?.Dispose();
            stream = null;
            client = null;
        }

        public void SetZero()
        {
            //TODO при ошибки обнуления служба отправит ответ
            try
            {
                Connect();
                SendCommand("Set", "Zero");
                Disconnect();
            }
            catch (Exception ex) {
                Console.WriteLine($"Ошибка в SetZero: {ex.Message}");
            }
        }

        public int? GetWeight()
        {
            try
            {
                Connect();
                SendCommand("Get", "Massa");
                string response = ReadResponse();
                //Console.WriteLine($"Ответ от сервера: {response}");
                Disconnect();

                var jsonResponse = JsonSerializer.Deserialize<ScaleResponse>(response);
                //Console.WriteLine($"Распарсенный JSON: Event={jsonResponse.Event}, Value={jsonResponse.Value}, ErrCode={jsonResponse.ErrCode}");
                if (jsonResponse.Event == 0)
                {
                    return (int)jsonResponse.Value; // Теперь Value уже int
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в GetWeight: {ex.Message}");
                return null;
            }
        }

        private void SendCommand(string command, string param)
        {
            var message = new { Command = command, Param = param };
            string json = JsonSerializer.Serialize(message);
            byte[] data = Encoding.UTF8.GetBytes(json);
            stream.Write(data, 0, data.Length);
        }

        private string ReadResponse()
        {
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }

        public void Dispose()
        {
            Disconnect();
        }
    }

    public class ScaleResponse
    {
        public int Event { get; set; }
        public int ErrCode { get; set; }
        public string Message { get; set; }
        public double Value { get; set; }
        public bool? IsStabil { get; set; }
        public string AxleMassa { get; set; }
        public string AxleDistance { get; set; }
    }
}