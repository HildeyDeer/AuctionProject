using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AuctionClient
{
    public partial class OwnerRegisterWindow : Window
    {
        private const string AuthServer = "127.0.0.1";
        private const int AuthPort = 4000;

        public OwnerRegisterWindow()
        {
            InitializeComponent();
        }

        private async void RegisterOwner_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text;
            string password = PasswordBox.Password;
            string permissionKey = PermissionKeyBox.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(permissionKey))
            {
                MessageBox.Show("Заполните все поля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (await RegisterOwner(username, password, permissionKey))
            {
                MessageBox.Show("Регистрация успешна!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            else
            {
                MessageBox.Show("Ошибка регистрации!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<bool> RegisterOwner(string username, string password, string permissionKey)
        {
            using TcpClient client = new TcpClient();
            await client.ConnectAsync(AuthServer, AuthPort);
            NetworkStream stream = client.GetStream();

            string request = $"OWNER_REGISTER|{username}|{password}|{permissionKey}";
            byte[] data = Encoding.UTF8.GetBytes(request);
            await stream.WriteAsync(data, 0, data.Length);

            byte[] responseBuffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
            string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);

            return response.StartsWith("SUCCESS");
        }
    }
}
