using System.Windows;

namespace Chess.View.Window
{
    public partial class MainMenuWindow : System.Windows.Window
    {
        public MainMenuWindow()
        {
            InitializeComponent();
        }

        private void Chess_Click(object sender, RoutedEventArgs e)
        {
            var w = new MainWindow(false);
            w.Show();
            this.Close();
        }

        private void Chess960_Click(object sender, RoutedEventArgs e)
        {
            var w = new MainWindow(true);
            w.Show();
            this.Close();
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}