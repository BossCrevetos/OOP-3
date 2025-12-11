using OOP_3.Server.Services;

namespace OOP_3.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== МОРСКОЙ БОЙ - СЕРВЕР ===");

            var server = new GameServer();
            server.Start();

            Console.WriteLine("Сервер запущен. Нажмите 'q' для выхода...");
            while (Console.ReadKey().Key != ConsoleKey.Q) { }

            server.Stop();
        }
    }
}