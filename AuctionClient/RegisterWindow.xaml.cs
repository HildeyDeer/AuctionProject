using Microsoft.Win32;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace AuctionClient
{
    public partial class RegisterWindow : Window
    {
        private const string AuthServer = "127.0.0.1";
        private const int AuthPort = 4000;

        public RegisterWindow()
        {
            InitializeComponent();
        }

        private async void Register_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text;
            string password = PasswordBox.Password;
            string email = EmailBox.Text;
            string address = AddressBox.Text;
            string cardNumber = CardNumberBox.Text;
            string profileImage = ProfileImagePath.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(address) ||
                string.IsNullOrWhiteSpace(cardNumber))
            {
                MessageBox.Show("Заполните все поля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (await RegisterUser(username, password, email, address, cardNumber, profileImage))
            {
                MessageBox.Show("Регистрация успешна!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            else
            {
                MessageBox.Show("Ошибка регистрации!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<bool> RegisterUser(string username, string password, string email,
                                              string address, string cardNumber, string profileImage)
        {
            using TcpClient client = new TcpClient();
            await client.ConnectAsync(AuthServer, AuthPort);
            NetworkStream stream = client.GetStream();

            string request = $"REGISTER|{username}|{password}|{email}|{address}|{cardNumber}|{profileImage}";
            byte[] data = Encoding.UTF8.GetBytes(request);
            await stream.WriteAsync(data, 0, data.Length);

            byte[] responseBuffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
            string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);

            return response.StartsWith("SUCCESS");
        }

        private void SelectProfileImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Выберите фото профиля",
                Filter = "Изображения (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ProfileImagePath.Text = openFileDialog.FileName;

                // Отображаем изображение в окне
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(openFileDialog.FileName);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                ProfilePreview.Source = bitmap;
                ProfilePreview.Visibility = Visibility.Visible;
            }
        }

    }
}
