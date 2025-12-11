using OOP_3.Models;
using OOP_3.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.Json;

namespace OOP_3.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private GameBoard _myBoard;
        private GameBoard _enemyBoard;
        private string _gameStatus;
        private NetworkService _networkService;
        private bool _isMyTurn;
        private bool _isConnected;
        private List<Ship> _ships = new List<Ship>();
        private bool _isPlacingShips = true;
        private int[] _shipSizes = { 4, 3, 3, 2, 2, 2, 1, 1, 1, 1 };
        private int _currentShipIndex = 0;
        private bool _isHorizontal = true;

        public GameBoard MyBoard
        {
            get => _myBoard;
            set { _myBoard = value; OnPropertyChanged(); }
        }

        public GameBoard EnemyBoard
        {
            get => _enemyBoard;
            set { _enemyBoard = value; OnPropertyChanged(); }
        }

        public string GameStatus
        {
            get => _gameStatus;
            set { _gameStatus = value; OnPropertyChanged(); }
        }

        public bool IsMyTurn
        {
            get => _isMyTurn;
            set { _isMyTurn = value; OnPropertyChanged(); }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set { _isConnected = value; OnPropertyChanged(); }
        }

        public bool IsPlacingShips
        {
            get => _isPlacingShips;
            set { _isPlacingShips = value; OnPropertyChanged(); }
        }

        public ICommand CellClickCommand { get; }
        public ICommand ReadyCommand { get; }
        public ICommand ConnectCommand { get; }
        public ICommand AutoPlaceCommand { get; }
        public ICommand RotateShipCommand { get; }

        public MainViewModel()
        {
            MyBoard = new GameBoard();
            EnemyBoard = new GameBoard();
            GameStatus = "Нажмите 'Подключиться к серверу'";
            IsConnected = false;

            CellClickCommand = new RelayCommand<Cell>(OnCellClick);
            ReadyCommand = new RelayCommand<object>(OnReady);
            ConnectCommand = new RelayCommand<object>(OnConnect);
            AutoPlaceCommand = new RelayCommand<object>(OnAutoPlace);
            RotateShipCommand = new RelayCommand<object>(OnRotateShip);

            // Начинаем с расстановки
            GameStatus = $"Расставьте корабли. Текущий корабль: {_shipSizes[_currentShipIndex]} палуб. Ориентация: {(_isHorizontal ? "Вертикально" : "Горизонтально")}. R - повернуть";
        }

        private void OnConnect(object obj)
        {
            if (IsConnected)
            {
                MessageBox.Show("Уже подключено к серверу!");
                return;
            }

            GameStatus = "Подключаемся к серверу...";
            InitializeNetwork();
        }

        private void InitializeNetwork()
        {
            _networkService = new NetworkService();
            _networkService.ConnectToServer();
            _networkService.MessageReceived += OnNetworkMessageReceived;
        }

        private void OnNetworkMessageReceived(GameMessage message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"📨 Получено: {message.Type}");

                switch (message.Type)
                {
                    case "Connected":
                        GameStatus = $"✅ Подключено! {message.Data}";
                        IsConnected = true;
                        break;

                    case "SecondPlayerConnected":
                        GameStatus = "Второй игрок подключился! Расставьте корабли и нажмите 'Готов'";
                        break;

                    case "WaitingForOpponent":
                        GameStatus = "Ожидаем готовности противника...";
                        break;

                    case "GameStart":
                        GameStatus = "Игра началась! Ожидаем хода...";
                        IsPlacingShips = false;
                        break;

                    case "YourTurn":
                        GameStatus = "Ваш ход! Стреляйте по полю противника";
                        IsMyTurn = true;
                        break;

                    case "WaitTurn":
                        GameStatus = "Ход противника...";
                        IsMyTurn = false;
                        break;

                    case "ShotResult":
                        Console.WriteLine($"🎯 Результат выстрела: {message.Data} в ({message.X},{message.Y})");
                        ProcessShotResult(message);
                        break;

                    case "GameOver":
                        GameStatus = message.Data;
                        IsMyTurn = false;
                        var result = MessageBox.Show($"{message.Data}\n\nХотите сыграть еще?", "Игра окончена", MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.Yes)
                        {
                            ResetGame();
                            _networkService.SendMessage(new GameMessage
                            {
                                Type = "Ready",
                                Data = "Готов",
                                PlayerId = _networkService.PlayerId
                            });
                        }
                        break;

                    case "GameReset":
                        ResetGame();
                        GameStatus = "Новая игра! Расставьте корабли";
                        break;

                    case "Error":
                        MessageBox.Show(message.Data, "Ошибка");
                        break;
                }
            });
        }

        private void ProcessShotResult(GameMessage message)
        {
            Console.WriteLine($"🎯 Обработка выстрела: {message.Data} в ({message.X},{message.Y})");

            // Если это выстрел противника по нам
            if (message.PlayerId != _networkService.PlayerId)
            {
                var cell = MyBoard.GetCell(message.X, message.Y);
                if (cell != null)
                {
                    if (message.Data == "Hit" || message.Data == "Sunk")
                    {
                        cell.State = CellState.Hit;

                        // Если потоплен, ищем весь корабль
                        if (message.Data == "Sunk")
                        {
                            FindAndMarkSunkShip(message.X, message.Y);
                            MessageBox.Show("Ваш корабль потоплен!");
                        }
                    }
                    else
                    {
                        cell.State = CellState.Miss;
                    }
                }
            }
            // Если это наш выстрел по противнику
            else
            {
                var cell = EnemyBoard.GetCell(message.X, message.Y);
                if (cell != null)
                {
                    if (message.Data == "Hit" || message.Data == "Sunk")
                    {
                        cell.State = CellState.Hit;

                        if (message.Data == "Hit")
                        {
                            // При попадании даем еще ход
                            IsMyTurn = true;
                            GameStatus = "Попадание! Стреляйте еще раз!";
                        }
                        else if (message.Data == "Sunk")
                        {
                            MessageBox.Show("Потопил корабль противника!", "Успех");
                        }
                    }
                    else
                    {
                        cell.State = CellState.Miss;
                    }
                }
            }
        }

        private void FindAndMarkSunkShip(int x, int y)
        {
            // Ищем все клетки этого корабля
            var ship = _ships.FirstOrDefault(s => s.Cells.Any(c => c.X == x && c.Y == y));
            if (ship != null)
            {
                foreach (var cell in ship.Cells)
                {
                    cell.State = CellState.Sunk;
                }
            }
        }

        private void OnCellClick(Cell cell)
        {
            if (cell == null) return;

            if (!IsConnected)
            {
                MessageBox.Show("Сначала подключитесь к серверу!");
                return;
            }

            if (IsPlacingShips)
            {
                PlaceShip(cell);
            }
            else if (IsMyTurn && IsConnected)
            {
                // Вражеская доска - можем стрелять только в пустые клетки или корабли
                // (если отображаются корабли, их скрываем в реальной игре)
                if (cell.State == CellState.Empty || cell.State == CellState.Ship)
                {
                    Console.WriteLine($"🎯 Отправляем выстрел в ({cell.X},{cell.Y})");

                    _networkService.SendMessage(new GameMessage
                    {
                        Type = "Shot",
                        X = cell.X,
                        Y = cell.Y,
                        PlayerId = _networkService.PlayerId
                    });

                    // ВАЖНО: Не сбрасываем IsMyTurn здесь - ждем ответа от сервера
                    GameStatus = "Выстрел сделан! Ожидаем результат...";
                }
                else
                {
                    MessageBox.Show("Сюда уже стреляли!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("Сейчас не ваш ход!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void PlaceShip(Cell startCell)
        {
            if (_currentShipIndex >= _shipSizes.Length)
            {
                IsPlacingShips = false;
                GameStatus = "Все корабли расставлены! Нажмите 'Готов'";
                return;
            }

            int shipSize = _shipSizes[_currentShipIndex];

            if (CanPlaceShip(startCell, shipSize, _isHorizontal))
            {
                var shipCells = new List<Cell>();
                for (int i = 0; i < shipSize; i++)
                {
                    var cell = MyBoard.GetCell(
                        startCell.X + (_isHorizontal ? i : 0),
                        startCell.Y + (_isHorizontal ? 0 : i)
                    );
                    if (cell != null)
                    {
                        cell.State = CellState.Ship;
                        shipCells.Add(cell);
                    }
                }

                _ships.Add(new Ship { Cells = shipCells });
                _currentShipIndex++;

                if (_currentShipIndex < _shipSizes.Length)
                {
                    GameStatus = $"Корабль размещен! Следующий: {_shipSizes[_currentShipIndex]} палуб. " +
                                $"Ориентация: {(_isHorizontal ? "Вертикально" : "Горизонтально")}. R - повернуть";
                }
                else
                {
                    IsPlacingShips = false;
                    GameStatus = "Все корабли расставлены! Нажмите 'Готов'";
                }
            }
            else
            {
                MessageBox.Show($"Нельзя разместить {shipSize}-палубный корабль {(_isHorizontal ? "вертикально" : "горизонтально")} здесь!",
                    "Невозможно разместить", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private bool CanPlaceShip(Cell startCell, int size, bool horizontal)
        {
            for (int i = 0; i < size; i++)
            {
                int x = startCell.X + (horizontal ? i : 0);
                int y = startCell.Y + (horizontal ? 0 : i);

                if (x >= 10 || y >= 10)
                    return false;

                var cell = MyBoard.GetCell(x, y);
                if (cell == null || cell.State != CellState.Empty)
                    return false;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;

                        if (nx >= 0 && nx < 10 && ny >= 0 && ny < 10)
                        {
                            var neighbor = MyBoard.GetCell(nx, ny);
                            if (neighbor != null && neighbor.State == CellState.Ship)
                                return false;
                        }
                    }
                }
            }
            return true;
        }

        private void OnRotateShip(object obj)
        {
            _isHorizontal = !_isHorizontal;
            GameStatus = $"Ориентация: {(_isHorizontal ? "Вертикально" : "Горизонтально")}. " +
                         $"Текущий корабль: {_shipSizes[_currentShipIndex]} палуб";
        }

        private void OnAutoPlace(object obj)
        {
            // Сброс текущих кораблей
            foreach (var cell in MyBoard.Cells)
                cell.State = CellState.Empty;

            _ships.Clear();
            _currentShipIndex = 0;

            var random = new Random();

            foreach (int size in _shipSizes)
            {
                bool placed = false;
                int attempts = 0;

                while (!placed && attempts < 100)
                {
                    int x = random.Next(0, 10);
                    int y = random.Next(0, 10);
                    bool horizontal = random.Next(0, 2) == 0;

                    var startCell = MyBoard.GetCell(x, y);
                    if (startCell != null && CanPlaceShip(startCell, size, horizontal))
                    {
                        var shipCells = new List<Cell>();
                        for (int i = 0; i < size; i++)
                        {
                            var cell = MyBoard.GetCell(
                                x + (horizontal ? i : 0),
                                y + (horizontal ? 0 : i)
                            );
                            if (cell != null)
                            {
                                cell.State = CellState.Ship;
                                shipCells.Add(cell);
                            }
                        }
                        _ships.Add(new Ship { Cells = shipCells });
                        _currentShipIndex++;
                        placed = true;
                    }
                    attempts++;
                }

                if (!placed)
                {
                    MessageBox.Show("Не удалось автоматически расставить корабли. Попробуйте вручную.",
                        "Ошибка авторасстановки", MessageBoxButton.OK, MessageBoxImage.Error);
                    foreach (var cell in MyBoard.Cells)
                        cell.State = CellState.Empty;
                    _ships.Clear();
                    _currentShipIndex = 0;
                    GameStatus = $"Расставьте корабли. Текущий корабль: {_shipSizes[_currentShipIndex]} палуб. Ориентация: {(_isHorizontal ? "Вертикально" : "Горизонтально")}. R - повернуть";
                    return;
                }
            }

            IsPlacingShips = false;
            GameStatus = "Корабли расставлены автоматически! Нажмите 'Готов'";
        }

        private void OnReady(object obj)
        {
            if (!IsConnected)
            {
                MessageBox.Show("Сначала подключитесь к серверу!");
                return;
            }

            if (_currentShipIndex < _shipSizes.Length)
            {
                MessageBox.Show("Расставьте все корабли сначала!", "Не готов", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Отправляем ВСЕ корабли ОДНИМ сообщением
            Console.WriteLine($"🚢 Отправляем {_ships.Count} кораблей на сервер...");

            var allShipsData = new
            {
                Ships = _ships.Select(ship => new
                {
                    Cells = ship.Cells.Select(c => new { X = c.X, Y = c.Y }).ToList()
                }).ToList()
            };

            _networkService.SendMessage(new GameMessage
            {
                Type = "PlaceShips",
                Data = JsonSerializer.Serialize(allShipsData),
                PlayerId = _networkService.PlayerId
            });

            // Отправляем что готов
            _networkService.SendMessage(new GameMessage
            {
                Type = "Ready",
                Data = "Готов",
                PlayerId = _networkService.PlayerId
            });

            GameStatus = "Готов к игре! Ожидаем противника...";
        }

        private void ResetGame()
        {
            MyBoard = new GameBoard();
            EnemyBoard = new GameBoard();
            _ships.Clear();
            _currentShipIndex = 0;
            IsPlacingShips = true;
            IsMyTurn = false;
            _isHorizontal = true;

            GameStatus = $"Расставьте корабли. Текущий корабль: {_shipSizes[_currentShipIndex]} палуб. Ориентация: {(_isHorizontal ? "Вертикально" : "Горизонтально")}. R - повернуть";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}