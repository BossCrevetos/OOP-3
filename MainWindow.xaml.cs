using OOP_3.ViewModels;
using System.Windows;

namespace OOP_3
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
