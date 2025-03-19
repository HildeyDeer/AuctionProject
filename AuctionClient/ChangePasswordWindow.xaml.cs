using System;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AuctionClient
{
    public partial class ChangePasswordWindow : Window
    {
        private string username;
        private TcpClient client;
        private NetworkStream stream;

        private const string AuthServerIp = "127.0.0.1"; // IP адрес AuthServer
        private const int AuthServerPort = 4000; // Порт сервера аутентификации

        public ChangePasswordWindow(string username)
        {
            InitializeComponent();
            this.username = username ?? throw new ArgumentNullException(nameof(username), "Имя пользователя не может быть null");
            ConnectToAuthServer(); // Подключаемся к серверу аутентификации при создании окна
        }

        private void ConnectToAuthServer()
        {
            try
            {
                // Создание TCP клиента для подключения к серверу аутентификации
                client = new TcpClient(AuthServerIp, AuthServerPort);
                stream = client.GetStream();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка подключения к серверу: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SavePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            string newPassword = NewPasswordBox.Password;
            string confirmPassword = ConfirmNewPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                MessageBox.Show("Введите новый пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPassword != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (client == null || !client.Connected)
            {
                MessageBox.Show("Нет соединения с сервером!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Отправляем команду для смены пароля
                string request = $"CHANGE_PASSWORD|{username}|{newPassword}";
                byte[] data = Encoding.UTF8.GetBytes(request);
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();

                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                if (response.StartsWith("SUCCESS"))
                {
                    MessageBox.Show("Пароль успешно изменен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Ошибка смены пароля: " + response.Substring(response.IndexOf('|') + 1), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка связи с сервером: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        // Закрытие соединения при закрытии окна
        private void Window_Closed(object sender, EventArgs e)
        {
            client?.Close();
        }
    }
}
