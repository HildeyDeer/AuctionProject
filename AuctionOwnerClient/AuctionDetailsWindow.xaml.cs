using AuctionClient;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace AuctionOwnerClient
{
    public partial class AuctionDetailsWindow : Window
    {
        private string auctionName;
        private double startPrice;
        private string category;
        private string endTime;
        private string description;
        private bool isOwner;
        private string ownerName;
        private string imageUrl; // Добавлено поле для ссылки на изображение

        public AuctionDetailsWindow(string name, double price, string description, string category, string endTime, bool isOwner, string ownerName, string imageUrl)
        {
            InitializeComponent();

            this.auctionName = name;
            this.startPrice = price;
            this.category = category;
            this.endTime = endTime;
            this.description = description;
            this.isOwner = isOwner;
            this.ownerName = ownerName;
            this.imageUrl = imageUrl; // Сохраняем ссылку на изображение

            AuctionName.Text = name;
            AuctionPrice.Text = price.ToString("F2") + " $";
            AuctionCategory.Text = category;
            AuctionEndTime.Text = endTime;
            AuctionDescription.Text = description;
            AuctionOwner.Text = "Вы";

            LoadAuctionImage(imageUrl);
        }

        private void LoadAuctionImage(string imageUrl)
        {
            try
            {
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    if (File.Exists(imageUrl))
                    {
                        // Если файл существует, загружаем изображение
                        Uri imageUri = new Uri($"file:///{imageUrl.Replace("\\", "/")}");
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = imageUri;
                        bitmap.EndInit();
                        AuctionImage.Source = bitmap;
                    }
                    else
                    {
                        SetPlaceholderImage();
                    }
                }
                else
                {
                    SetPlaceholderImage();
                }
            }
            catch (Exception)
            {
                //SetPlaceholderImage();
            }
        }

        private void SetPlaceholderImage()
        {
            // Здесь можно установить изображение-заполнитель
            //Uri placeholderUri = new Uri("C:\\Users\\user\\Pictures");
            //BitmapImage placeholderImage = new BitmapImage(placeholderUri);
            //AuctionImage.Source = placeholderImage;
        }

        private void JoinAuction_Click(object sender, RoutedEventArgs e)
        {
            if (isOwner)
            {
                var ownerWindow = new AuctionOwnerWindow(
                    auctionName,
                    ownerName,
                    description,
                    imageUrl, // Передаём ссылку на изображение
                    endTime
                );

                ownerWindow.Show();
                Close();
            }
            else
            {
                MessageBox.Show("У вас нет прав для управления этим аукционом.", "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
