using System;
using System.Windows;
using System.IO;
using System.Windows.Media.Imaging;

namespace AuctionClient
{
    public partial class AuctionDetailsWindow : Window
    {
        private string auctionId;
        private string auctionName;
        private string username;
        private string auctionOwner;
        private string imageUrl; // Добавлен параметр для ссылки на изображение

        public AuctionDetailsWindow(string name, string owner, string price, string description, string category, string endTime, string username, string imageUrl)
        {
            InitializeComponent();

            this.username = username;  // Сохраняем имя пользователя
            this.auctionOwner = owner; // Сохраняем владельца аукциона
            this.imageUrl = imageUrl;  // Сохраняем URL изображения

            AuctionName.Text = name;
            AuctionOwner.Text = owner;
            AuctionPrice.Text = price + " $";
            AuctionDescription.Text = description;
            AuctionCategory.Text = category;
            AuctionEndTime.Text = endTime;

            // Загружаем изображение
            LoadAuctionImage(imageUrl);
        }

        private void LoadAuctionImage(string imageUrl)
        {
            try
            {
                if (!string.IsNullOrEmpty(imageUrl) && File.Exists(imageUrl))
                {
                    // Если путь к изображению существует, загружаем его
                    Uri imageUri = new Uri($"file:///{imageUrl.Replace("\\", "/")}");
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = imageUri;
                    bitmap.EndInit();
                    AuctionImage.Source = bitmap;
                }
                else
                {
                    // Если файл не найден или URL пустой, используем изображение-заполнитель
                    SetPlaceholderImage();
                }
            }
            catch (Exception)
            {
                // В случае ошибки загружаем изображение-заполнитель
                SetPlaceholderImage();
            }
        }

        private void SetPlaceholderImage()
        {
            // Устанавливаем изображение-заполнитель (можно добавить свой путь к заглушке)
            Uri placeholderUri = new Uri("https://example.com/placeholder-image.jpg");
            BitmapImage placeholderImage = new BitmapImage(placeholderUri);
            AuctionImage.Source = placeholderImage;
        }

        private void JoinAuction_Click(object sender, RoutedEventArgs e)
        {
            bool isOwner = username == auctionOwner;
            AuctionActiveWindow activeAuction = new AuctionActiveWindow(
                username, AuctionName.Text, AuctionOwner.Text, AuctionPrice.Text, AuctionEndTime.Text, isOwner,
                AuctionDescription.Text, imageUrl // Передаем ссылку на изображение
            );
            activeAuction.Show();
            Close();
        }
    }
}
