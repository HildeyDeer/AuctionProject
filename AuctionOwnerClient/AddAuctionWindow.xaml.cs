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
        private readonly int ownerId; // Примем ownerId как целое число

        public AddAuctionWindow(NetworkStream stream, int ownerId) // Конструктор с передачей ownerId
        {
            InitializeComponent();
            this.stream = stream;
            this.ownerId = ownerId; // Присваиваем ownerId
        }

        private async void AddAuction_Click(object sender, RoutedEventArgs e)
        {
            string name = AuctionNameBox.Text.Trim();
            string description = AuctionDescriptionBox.Text.Trim();
            string priceText = AuctionStartPriceBox.Text.Trim();
            string category = AuctionCategoryBox.Text.Trim(); // Новый элемент для категории
            string endTime = AuctionEndTimeBox.Text.Trim(); // Новый элемент для времени окончания

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description) ||
                string.IsNullOrWhiteSpace(priceText) || string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(endTime))
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
                // Формируем запрос для добавления аукциона с ownerId
                string addMessage = $"ADD_AUCTION|{ownerId}|{name}|{description}|{formattedPrice}|{category}|{endTime}";
                byte[] data = Encoding.UTF8.GetBytes(addMessage);
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();

                // Получаем ответ от сервера
                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

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
