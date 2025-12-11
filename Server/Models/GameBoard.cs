using System;
using System.Collections.Generic;
using System.Linq;

namespace OOP_3.Server.Models
{
    public class GameBoard
    {
        public List<ServerCell> Cells { get; set; } = new List<ServerCell>();
        private List<Ship> _ships = new List<Ship>();

        public GameBoard()
        {
            InitializeBoard();
        }

        private void InitializeBoard()
        {
            Cells.Clear();
            _ships.Clear();

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    Cells.Add(new ServerCell { X = i, Y = j });
                }
            }
        }

        public ServerCell GetCell(int x, int y)
        {
            return Cells.FirstOrDefault(c => c.X == x && c.Y == y);
        }

        public bool IsValidShot(int x, int y)
        {
            var cell = GetCell(x, y);
            return cell != null && (cell.State == ServerCellState.Empty || cell.State == ServerCellState.Ship);
        }

        public ShotResult ProcessShot(int x, int y)
        {
            var cell = GetCell(x, y);
            if (cell == null)
                return new ShotResult { IsHit = false, IsSunk = false };

            if (cell.State == ServerCellState.Ship)
            {
                cell.State = ServerCellState.Hit;

                // Находим корабль
                var hitShip = _ships.FirstOrDefault(ship =>
                    ship.Cells.Any(c => c.X == x && c.Y == y));

                bool isSunk = false;
                if (hitShip != null)
                {
                    isSunk = hitShip.Cells.All(c => GetCell(c.X, c.Y)?.State == ServerCellState.Hit);
                    if (isSunk)
                    {
                        // Помечаем все клетки потопленного корабля
                        foreach (var shipCell in hitShip.Cells)
                        {
                            var sunkCell = GetCell(shipCell.X, shipCell.Y);
                            if (sunkCell != null)
                                sunkCell.State = ServerCellState.Sunk;
                        }
                    }
                }

                return new ShotResult { IsHit = true, IsSunk = isSunk };
            }
            else if (cell.State == ServerCellState.Empty)
            {
                cell.State = ServerCellState.Miss;
                return new ShotResult { IsHit = false, IsSunk = false };
            }

            return new ShotResult { IsHit = false, IsSunk = false };
        }

        public bool AllShipsSunk()
        {
            return _ships.Count > 0 && _ships.All(ship =>
                ship.Cells.All(cell =>
                    GetCell(cell.X, cell.Y)?.State == ServerCellState.Hit ||
                    GetCell(cell.X, cell.Y)?.State == ServerCellState.Sunk
                )
            );
        }

        public void AddShip(Ship ship)
        {
            _ships.Add(ship);
            foreach (var cell in ship.Cells)
            {
                var boardCell = GetCell(cell.X, cell.Y);
                if (boardCell != null)
                    boardCell.State = ServerCellState.Ship;
            }
        }
    }

    public class ShotResult
    {
        public bool IsHit { get; set; }
        public bool IsSunk { get; set; }
    }
}