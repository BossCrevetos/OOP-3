namespace OOP_3.Models
{
    public class Cell
    {
        public int X { get; set; }
        public int Y { get; set; }
        public CellState State { get; set; } = CellState.Empty;
    }

    public enum CellState
    {
        Empty,
        Ship,
        Hit,
        Miss,
        Sunk
    }
}