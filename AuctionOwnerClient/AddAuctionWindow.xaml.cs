using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AuctionOwnerClient
{
    public partial class AddAuctionWindow : Window
    {
        private readonly NetworkStream stream;
        private readonly string username;

        public AddAuctionWindow(NetworkStream stream, string username)
        {
            InitializeComponent();
            this.stream = stream;
            this.username = username;
        }

        private async void AddAuction_Click(object sender, RoutedEventArgs e)
        {
            string name = AuctionNameBox.Text.Trim();
            string description = AuctionDescriptionBox.Text.Trim();
            string priceText = AuctionStartPriceBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(priceText))
            {
                MessageBox.Show("Пожалуйста, заполните все поля.");
                return;
            }

            if (!double.TryParse(priceText, out double startPrice) || startPrice <= 0)
            {
                MessageBox.Show("Введите корректную стартовую цену (больше 0).");
                return;
            }

            string formattedPrice = startPrice.ToString("F2");

            try
            {
                string requestOwnerId = $"GET_OWNER_ID|{username}";
                byte[] requestData = Encoding.UTF8.GetBytes(requestOwnerId);
                await stream.WriteAsync(requestData, 0, requestData.Length);

                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                if (!response.StartsWith("OWNER_ID|"))
                {
                    MessageBox.Show("Ошибка: не удалось получить ID владельца.");
                    return;
                }

                string ownerId = response.Split('|')[1];

                string addMessage = $"ADD_AUCTION|{username}|{ownerId}|{name}|{description}|{formattedPrice}";
                byte[] data = Encoding.UTF8.GetBytes(addMessage);
                await stream.WriteAsync(data, 0, data.Length);

                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                if (response.StartsWith("SUCCESS"))
                {
                    MessageBox.Show("Аукцион успешно добавлен!");
                    this.Close();
                }
                else
                {
                    MessageBox.Show($"Ошибка: {response}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении аукциона: {ex.Message}");
            }
        }



        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Закрыть окно без добавления аукциона
        }
    }
}
