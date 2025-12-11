namespace OOP_3.Models
{
    public class Ship
    {
        public List<Cell> Cells { get; set; } = new List<Cell>();
        public bool IsSunk => Cells.All(c => c.State == CellState.Hit);
        public int Size => Cells.Count;
    }
}