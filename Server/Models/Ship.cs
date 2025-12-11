using System.Collections.Generic;

namespace OOP_3.Server.Models
{
    public class Ship
    {
        public List<ShipCell> Cells { get; set; } = new List<ShipCell>();
    }

    public class ShipCell
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsHit { get; set; }
    }
}