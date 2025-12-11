namespace OOP_3.Server.Models
{
    public class ServerCell
    {
        public int X { get; set; }
        public int Y { get; set; }
        public ServerCellState State { get; set; } = ServerCellState.Empty;
    }

    public enum ServerCellState
    {
        Empty,
        Ship,
        Miss,
        Hit,
        Sunk
    }
}