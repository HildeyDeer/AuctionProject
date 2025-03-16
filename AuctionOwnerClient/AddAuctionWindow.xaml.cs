using Microsoft.Win32;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace AuctionOwnerClient
{
    public partial class AddAuctionWindow : Window
    {
        private readonly NetworkStream stream;
        private readonly int ownerId;
        private string imageFilePath = ""; // Хранит путь к файлу изображения

        public AddAuctionWindow(NetworkStream stream, int ownerId)
        {
            InitializeComponent();
            this.stream = stream;
            this.ownerId = ownerId;
        }

        private void BrowseImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Выберите изображение",
                Filter = "Изображения (*.jpg, *.png)|*.jpg;*.png"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                imageFilePath = openFileDialog.FileName;
                AuctionImagePathBox.Text = imageFilePath;

                try
                {
                    // Предварительный просмотр
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imageFilePath); // Используем путь к файлу
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    AuctionImagePreview.Source = bitmap;
                    AuctionImagePreview.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}");
                }
            }
        }

        private async void AddAuction_Click(object sender, RoutedEventArgs e)
        {
            string name = AuctionNameBox.Text.Trim();
            string description = AuctionDescriptionBox.Text.Trim();
            string priceText = AuctionStartPriceBox.Text.Trim();
            string category = AuctionCategoryBox.Text.Trim();
            string endTime = AuctionEndTimeBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description) ||
                string.IsNullOrWhiteSpace(priceText) || string.IsNullOrWhiteSpace(category) ||
                string.IsNullOrWhiteSpace(endTime) || string.IsNullOrWhiteSpace(imageFilePath))
            {
                MessageBox.Show("Пожалуйста, заполните все поля и выберите изображение.");
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
                string addMessage = $"ADD_AUCTION|{ownerId}|{name}|{description}|{formattedPrice}|{category}|{endTime}|{imageFilePath}";
                byte[] data = Encoding.UTF8.GetBytes(addMessage);
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();

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
            this.Close();
        }
    }
}
