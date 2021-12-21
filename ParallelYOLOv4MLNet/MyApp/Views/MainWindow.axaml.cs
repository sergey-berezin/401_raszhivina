using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using MyApp.ViewModels;


namespace MyApp.Views
{
    public partial class MainWindow : Window
    {
        MainWindowViewModel model = new MainWindowViewModel();
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            DataContext = model;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void MenuItem_Click_Open(object sender, RoutedEventArgs e)
        {
            model.Open();
        }
        
        private void MenuItem_Click_Start(object sender, RoutedEventArgs e)
        {
            model.Detection();
        }

        private void MenuItem_Click_Cencel(object sender, RoutedEventArgs e)
        {
            model.Cencel();
        }    

        private void MenuItem_Click_Clear(object sender, RoutedEventArgs e)
        {
            model.Clear();
        } 

    }
}