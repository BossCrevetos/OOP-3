using OOP_3.Server.Models;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace OOP_3.Server.Services
{
    public class GameServer
    {
        private TcpListener _listener;
        private List<Player> _players = new List<Player>();
        private GameSession _currentSession;
        private bool _isRunning = false;

        public GameServer(string ip = "127.0.0.1", int port = 8888)
        {
            _listener = new TcpListener(IPAddress.Parse(ip), port);
            _currentSession = new GameSession();
        }

        public async void Start()
        {
            _listener.Start();
            _isRunning = true;
            Console.WriteLine($"✅ Сервер запущен на {_listener.LocalEndpoint}");
            Console.WriteLine("⏳ Ожидаем подключения игроков...");

            while (_isRunning)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client);
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                        Console.WriteLine($"❌ Ошибка: {ex.Message}");
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            Player player = new Player { Client = client };

            lock (_players)
            {
                _players.Add(player);
                player.Name = $"Игрок {_players.Count}";
            }

            Console.WriteLine($"🎮 Подключился {player.Name} ({player.Id})");

            // Отправляем Connected
            player.SendMessage(new GameMessage
            {
                Type = "Connected",
                Data = $"Добро пожаловать, {player.Name}!",
                PlayerId = player.Id
            });

            Console.WriteLine($"📤 Отправлено Connected для {player.Name}");

            // Если второй игрок
            if (_players.Count == 2)
            {
                BroadcastMessage(new GameMessage
                {
                    Type = "SecondPlayerConnected",
                    Data = "Второй игрок подключился! Расставьте корабли и нажмите 'Готов'"
                });
                Console.WriteLine("🎮 Оба игрока подключены. Ожидаем готовности...");
            }

            byte[] buffer = new byte[4096];
            while (client.Connected && _isRunning)
            {
                try
                {
                    int bytesRead = await player.Stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"📨 {player.Name} raw: {message}");
                        var gameMessage = JsonSerializer.Deserialize<GameMessage>(message);
                        await ProcessClientMessage(player, gameMessage);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка у {player.Name}: {ex.Message}");
                    break;
                }
            }

            lock (_players)
            {
                _players.Remove(player);
            }
            client.Close();
            Console.WriteLine($"🔌 Отключился {player.Name}");
        }

        private async Task ProcessClientMessage(Player player, GameMessage message)
        {
            if (message == null)
            {
                Console.WriteLine($"❌ {player.Name}: Получено пустое сообщение");
                return;
            }

            Console.WriteLine($"📨 {player.Name}: {message.Type}");

            switch (message.Type)
            {
                case "PlaceShips":
                    try
                    {
                        var shipsData = JsonSerializer.Deserialize<AllShipsData>(message.Data);
                        if (shipsData != null && shipsData.Ships != null)
                        {
                            player.Board = new GameBoard();
                            int totalCells = 0;
                            foreach (var shipData in shipsData.Ships)
                            {
                                var ship = new Ship();
                                foreach (var cell in shipData.Cells)
                                {
                                    ship.Cells.Add(new ShipCell { X = cell.X, Y = cell.Y });
                                }
                                player.Board.AddShip(ship);
                                totalCells += ship.Cells.Count;
                            }
                            Console.WriteLine($"🚢 {player.Name} добавил {shipsData.Ships.Count} кораблей ({totalCells} клеток)");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Ошибка обработки кораблей: {ex.Message}");
                    }
                    break;

                case "Ready":
                    player.IsReady = true;
                    Console.WriteLine($"✅ {player.Name} готов. Готовы: {_players.Count(p => p.IsReady)}/{_players.Count}");

                    if (_players.All(p => p.IsReady) && _players.Count == 2)
                    {
                        Console.WriteLine("🎮 Все игроки готовы! Запускаем игру...");
                        StartGame();
                    }
                    else
                    {
                        player.SendMessage(new GameMessage
                        {
                            Type = "WaitingForOpponent",
                            Data = "Ожидаем готовности противника..."
                        });
                    }
                    break;

                case "Shot":
                    await ProcessShot(player, message);
                    break;

                default:
                    Console.WriteLine($"❌ {player.Name}: Неизвестный тип сообщения: {message.Type}");
                    break;
            }
        }

        private async Task ProcessShot(Player shooter, GameMessage shotMessage)
        {
            if (!_currentSession.IsPlayerTurn(shooter.Id))
            {
                shooter.SendMessage(new GameMessage { Type = "Error", Data = "Не ваш ход!" });
                return;
            }

            var targetPlayer = _players.FirstOrDefault(p => p.Id != shooter.Id);
            if (targetPlayer == null) return;

            Console.WriteLine($"🎯 {shooter.Name} стреляет в ({shotMessage.X},{shotMessage.Y})");

            if (targetPlayer.Board.IsValidShot(shotMessage.X.GetValueOrDefault(), shotMessage.Y.GetValueOrDefault()))
            {
                var shotResult = targetPlayer.Board.ProcessShot(
                    shotMessage.X.GetValueOrDefault(),
                    shotMessage.Y.GetValueOrDefault());

                BroadcastMessage(new GameMessage
                {
                    Type = "ShotResult",
                    X = shotMessage.X,
                    Y = shotMessage.Y,
                    PlayerId = shooter.Id,
                    Data = shotResult.IsHit ? (shotResult.IsSunk ? "Sunk" : "Hit") : "Miss"
                });

                Console.WriteLine($"🎯 Результат: {(shotResult.IsHit ? (shotResult.IsSunk ? "ПОТОПИЛ!" : "ПОПАЛ!") : "МИМО")}");

                if (targetPlayer.Board.AllShipsSunk())
                {
                    _currentSession.EndGame(shooter.Id);
                    BroadcastMessage(new GameMessage
                    {
                        Type = "GameOver",
                        Data = $"{shooter.Name} победил!",
                        PlayerId = shooter.Id
                    });
                    Console.WriteLine($"🏆 Победитель: {shooter.Name}");

                    await Task.Delay(3000);
                    ResetGame();
                }
                else
                {
                    // Если попал - ход остается, если промахнулся - передаем
                    if (!shotResult.IsHit)
                    {
                        _currentSession.SwitchTurn();
                        Console.WriteLine($"🔄 Передача хода");
                    }
                    else
                    {
                        Console.WriteLine($"🎯 {shooter.Name} попадает и получает дополнительный ход");
                    }

                    UpdateTurns();
                }
            }
            else
            {
                shooter.SendMessage(new GameMessage { Type = "Error", Data = "Неверный выстрел!" });
            }
        }

        private void UpdateTurns()
        {
            var currentPlayer = _players.FirstOrDefault(p => p.Id == _currentSession.CurrentPlayerId);
            if (currentPlayer == null) return;

            foreach (var player in _players)
            {
                bool isPlayerTurn = player.Id == _currentSession.CurrentPlayerId;
                player.SendMessage(new GameMessage
                {
                    Type = isPlayerTurn ? "YourTurn" : "WaitTurn",
                    Data = isPlayerTurn ? "Ваш ход! Стреляйте" : "Ход противника..."
                });
                Console.WriteLine($"📤 {player.Name}: {(isPlayerTurn ? "YourTurn" : "WaitTurn")}");
            }
        }

        private void StartGame()
        {
            Console.WriteLine("🎮 Начинаем игру!");

            _currentSession.Player1Id = _players[0].Id;
            _currentSession.Player2Id = _players[1].Id;
            _currentSession.CurrentPlayerId = _players[0].Id; // Первый игрок ходит первым
            _currentSession.IsGameActive = true;

            BroadcastMessage(new GameMessage
            {
                Type = "GameStart",
                Data = "Игра началась!"
            });

            UpdateTurns();
        }

        private void ResetGame()
        {
            Console.WriteLine("🔄 Перезапуск игры...");

            foreach (var player in _players)
            {
                player.Board = new GameBoard();
                player.IsReady = false;
            }

            _currentSession = new GameSession();

            BroadcastMessage(new GameMessage
            {
                Type = "GameReset",
                Data = "Новая игра! Расставьте корабли заново."
            });

            Console.WriteLine("⏳ Ожидаем готовности...");
        }

        private void BroadcastMessage(GameMessage message)
        {
            lock (_players)
            {
                foreach (var player in _players.Where(p => p.Client?.Connected == true))
                {
                    player.SendMessage(message);
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
            foreach (var player in _players)
            {
                player.Client?.Close();
            }
            _players.Clear();
            Console.WriteLine("🛑 Сервер остановлен");
        }
    }

    public class AllShipsData
    {
        public List<ShipData> Ships { get; set; }
    }

    public class ShipData
    {
        public List<CellData> Cells { get; set; }
    }

    public class CellData
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}