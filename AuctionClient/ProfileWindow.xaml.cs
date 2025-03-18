using System;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;

namespace AuctionClient
{
    public partial class ProfileWindow : Window
    {
        private string username;
        private string cardNumber;
        private TcpClient client;
        private NetworkStream stream;

        public ProfileWindow(string username, string email, string address, string cardNumber, string profileImage, string balance, TcpClient client)
        {
            InitializeComponent();
            this.username = username;
            this.cardNumber = cardNumber;
            this.client = client ?? throw new ArgumentNullException(nameof(client), "Соединение с сервером отсутствует");

            LoginText.Text = username;
            EmailText.Text = email;
            AddressText.Text = address;
            CardText.Text = cardNumber;
            BalanceText.Text = $"{balance} $";

            if (!string.IsNullOrEmpty(profileImage))
            {
                LoadProfileImage(profileImage);
            }
        }

        private void LoadProfileImage(string imageUrl)
        {
            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imageUrl, UriKind.Absolute);
                bitmap.EndInit();
                ProfileImage.Source = bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки изображения: " + ex.Message);
            }
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (client == null || !client.Connected)
            {
                MessageBox.Show("Нет соединения с сервером!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ChangePasswordWindow changePasswordWindow = new ChangePasswordWindow(username);
            changePasswordWindow.ShowDialog();
        }

        private async void TopUpBalanceButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(cardNumber))
            {
                MessageBox.Show("Привязанная карта отсутствует", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Окно ввода суммы
            string amountStr = Microsoft.VisualBasic.Interaction.InputBox(
                $"С вашей карты {cardNumber} будет произведено пополнение.\nВведите сумму:",
                "Пополнение баланса",
                "0"
            );

            if (decimal.TryParse(amountStr, out decimal amount) && amount > 0)
            {
                if (client == null || !client.Connected)
                {
                    MessageBox.Show("Нет соединения с сервером!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Отправка запроса на сервер
                NetworkStream stream = client.GetStream();
                string request = $"TOP_UP|{username}|{amount}";
                byte[] data = Encoding.UTF8.GetBytes(request);
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();

                // Получение ответа
                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                if (response.StartsWith("SUCCESS"))
                {
                    // Обновление баланса
                    BalanceText.Text = $"{decimal.Parse(BalanceText.Text.Replace(" $", "")) + amount} $";
                    MessageBox.Show("Баланс успешно пополнен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Ошибка при пополнении баланса", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Некорректная сумма", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
