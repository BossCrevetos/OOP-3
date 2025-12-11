    using System.Collections.Generic;
    using System.Linq;

    namespace OOP_3.Models
    {
        public class GameBoard
        {
            public List<Cell> Cells { get; set; } = new List<Cell>();

            public GameBoard()
            {
                for (int i = 0; i < 10; i++)
                    for (int j = 0; j < 10; j++)
                        Cells.Add(new Cell { X = i, Y = j });
            }

            public Cell GetCell(int x, int y)
            {
                return Cells.FirstOrDefault(c => c.X == x && c.Y == y);
            }
        }
    }