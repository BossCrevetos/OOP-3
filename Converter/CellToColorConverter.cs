using OOP_3.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace OOP_3.Converters
{
    public class CellToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CellState state)
            {
                return state switch
                {
                    CellState.Empty => Brushes.LightBlue,
                    CellState.Ship => Brushes.Gray,
                    CellState.Miss => Brushes.White,
                    CellState.Hit => Brushes.Red,
                    CellState.Sunk => Brushes.DarkRed,
                    _ => Brushes.LightBlue
                };
            }
            return Brushes.LightBlue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}