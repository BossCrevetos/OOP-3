using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OOP_3.Server.Models
{
    public class Player
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public TcpClient Client { get; set; }
        public GameBoard Board { get; set; } = new GameBoard();
        public bool IsReady { get; set; }

        public NetworkStream Stream => Client?.GetStream();

        public void SendMessage(GameMessage message)
        {
            try
            {
                if (Client?.Connected == true && Stream != null)
                {
                    string json = JsonSerializer.Serialize(message);
                    byte[] data = Encoding.UTF8.GetBytes(json);
                    Stream.Write(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка отправки {Name}: {ex.Message}");
            }
        }

        // Асинхронная версия
        public async Task SendMessageAsync(GameMessage message, JsonSerializerOptions options)
        {
            try
            {
                if (Client?.Connected == true && Stream != null)
                {
                    string json = JsonSerializer.Serialize(message, options);
                    byte[] data = Encoding.UTF8.GetBytes(json);
                    await Stream.WriteAsync(data, 0, data.Length);
                    await Stream.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка отправки {Name}: {ex.Message}");
            }
        }
    }
}