using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AuctionClient
{
    public partial class LoginWindow : Window
    {
        private const string AuthServer = "127.0.0.1";
        private const int AuthPort = 4000;

        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text;
            string password = PasswordBox.Password;

            if (await AuthenticateUser(username, password))
            {
                AuctionWindow auctionWindow = new AuctionWindow(username);
                auctionWindow.Show();
                Close();
            }
            else
            {
                MessageBox.Show("Ошибка входа!");
            }
        }

        private async void Register_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text;
            string password = PasswordBox.Password;

            if (await Register(username, password))
            {
                MessageBox.Show("Регистрация успешна!");
            }
            else
            {
                MessageBox.Show("Ошибка регистрации!");
            }
        }

        private async Task<bool> AuthenticateUser(string username, string password)
        {
            using TcpClient client = new TcpClient();
            await client.ConnectAsync(AuthServer, AuthPort);
            NetworkStream stream = client.GetStream();

            string request = $"LOGIN|{username}|{password}";
            byte[] data = Encoding.UTF8.GetBytes(request);
            await stream.WriteAsync(data, 0, data.Length);

            byte[] responseBuffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
            string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);

            return response.StartsWith("SUCCESS");
        }

        private async Task<bool> Register(string username, string password)
        {
            using TcpClient client = new TcpClient();
            await client.ConnectAsync(AuthServer, AuthPort);
            NetworkStream stream = client.GetStream();

            string request = $"REGISTER|{username}|{password}";
            byte[] data = Encoding.UTF8.GetBytes(request);
            await stream.WriteAsync(data, 0, data.Length);

            byte[] responseBuffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
            string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);
            return response.StartsWith("SUCCESS");
        }
    }
}
