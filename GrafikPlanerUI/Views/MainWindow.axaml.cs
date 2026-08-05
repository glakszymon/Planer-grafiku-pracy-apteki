using Avalonia.Controls;
using GrafikPlanerCore;
using GrafikPlanerData;

namespace GrafikPlanerUI.Views;

public partial class MainWindow : Window
{

    public MainWindow()
    {
        InitializeComponent();
        
        DbService db = new DbService();
        db.InitializeDatabase();
    }
}