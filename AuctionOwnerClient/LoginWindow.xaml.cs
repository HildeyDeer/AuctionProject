using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AuctionOwnerClient
{
    public partial class LoginWindow : Window
    {
        private const string AuthServer = "127.0.0.1";
        private const int AuthPort = 4000;

        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void OwnerLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text;
            string password = PasswordBox.Password;
            string permissionKey = PermissionBox.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(permissionKey))
            {
                MessageBox.Show("Пожалуйста, заполните все поля.");
                return;
            }

            int? ownerId = await AuthenticateOwner(username, password, permissionKey);
            if (ownerId.HasValue)
            {
                MessageBox.Show("Вход успешен!");
                OwnerAuctionWindow ownerAuctionWindow = new OwnerAuctionWindow(ownerId.Value);
                ownerAuctionWindow.Show();
                Close();
            }
            else
            {
                MessageBox.Show("Ошибка входа владельца!");
            }
        }

        private async Task<int?> AuthenticateOwner(string username, string password, string permissionKey)
        {
            try
            {
                using TcpClient client = new TcpClient();
                await client.ConnectAsync(AuthServer, AuthPort);
                using NetworkStream stream = client.GetStream();

                string request = $"OWNER_LOGIN|{username}|{password}|{permissionKey}";
                byte[] data = Encoding.UTF8.GetBytes(request);
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();

                byte[] responseBuffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
                string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);

                if (response.StartsWith("SUCCESS"))
                {
                    string[] parts = response.Split('|');
                    if (parts.Length > 1 && int.TryParse(parts[1], out int ownerId))
                    {
                        return ownerId;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка соединения: {ex.Message}");
            }
            return null;
        }
    }
}
