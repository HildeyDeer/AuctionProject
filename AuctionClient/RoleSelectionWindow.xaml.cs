using System.Windows;

namespace AuctionClient
{
    public partial class RoleSelectionWindow : Window
    {
        public RoleSelectionWindow()
        {
            InitializeComponent();
        }

        private void OpenUserLogin(object sender, RoutedEventArgs e)
        {
            LoginWindow userLogin = new LoginWindow();
            userLogin.Show();
            Close();
        }

        private void OpenOwnerLogin(object sender, RoutedEventArgs e)
        {
            AuctionOwnerClient.LoginWindow ownerLogin = new AuctionOwnerClient.LoginWindow();
            ownerLogin.Show();
            Close();
        }
    }
}
