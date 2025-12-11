using OOP_3.Models;
using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OOP_3.Services
{
    public class NetworkService
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private bool _isConnected = false;
        private readonly JsonSerializerOptions _jsonOptions;

        public string PlayerId { get; private set; }
        public bool IsConnected => _isConnected;

        public event Action<GameMessage> MessageReceived;

        public NetworkService()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        public async void ConnectToServer(string ip = "192.168.0.8", int port = 8888)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(ip, port);
                _stream = _client.GetStream();
                _isConnected = true;

                StartListening();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка подключения: {ex.Message}");
                _isConnected = false;
            }
        }

        private async void StartListening()
        {
            byte[] buffer = new byte[4096];

            while (_isConnected && _client?.Connected == true)
            {
                try
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string messageJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"📨 Получено сырое: {messageJson}");

                        try
                        {
                            var message = JsonSerializer.Deserialize<GameMessage>(messageJson, _jsonOptions);
                            Console.WriteLine($"📨 Десериализовано: {message?.Type}");

                            if (message.Type == "Connected")
                            {
                                PlayerId = message.PlayerId;
                                Console.WriteLine($"✅ PlayerId установлен: {PlayerId}");
                            }

                            MessageReceived?.Invoke(message);
                        }
                        catch (JsonException ex)
                        {
                            Console.WriteLine($"❌ Ошибка десериализации: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка чтения: {ex.Message}");
                    break;
                }
            }
        }

        public async void SendMessage(GameMessage message)
        {
            if (!_isConnected || _stream == null)
            {
                Console.WriteLine("❌ Не подключен к серверу!");
                return;
            }

            try
            {
                string json = JsonSerializer.Serialize(message, _jsonOptions);
                byte[] data = Encoding.UTF8.GetBytes(json);
                await _stream.WriteAsync(data, 0, data.Length);
                await _stream.FlushAsync();
                Console.WriteLine($"📤 Отправлено: {message.Type} {(message.X != null ? $"({message.X},{message.Y})" : "")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка отправки: {ex.Message}");
                Disconnect();
            }
        }

        public void Disconnect()
        {
            _isConnected = false;
            _stream?.Close();
            _client?.Close();
            Console.WriteLine("🔌 Отключено от сервера");
        }
    }
}   