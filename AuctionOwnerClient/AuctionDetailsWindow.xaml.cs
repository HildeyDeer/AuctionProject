using AuctionClient;
using System;
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

        public AuctionDetailsWindow(string name, double price, string description, string category, string endTime, bool isOwner, string ownerName)
        {
            InitializeComponent();

            this.auctionName = name;
            this.startPrice = price;
            this.category = category;
            this.endTime = endTime;
            this.description = description;
            this.isOwner = isOwner;
            this.ownerName = ownerName;

            AuctionName.Text = name;
            AuctionPrice.Text = price.ToString("F2") + " $";
            AuctionCategory.Text = category;
            AuctionEndTime.Text = endTime;
            AuctionDescription.Text = description;

            // Устанавливаем владельца аукциона как "Вы"
            AuctionOwner.Text = "Вы";

            // Заглушка для фото
            AuctionImage.Source = new BitmapImage(new Uri("https://geauction.com/wp-content/uploads/2018/07/5-Auction-Tips-for-Beginners2.jpg"));
        }


        private void JoinAuction_Click(object sender, RoutedEventArgs e)
        {
            if (isOwner)
            {
                // Создаём окно владельца и передаём нужные параметры
                var ownerWindow = new AuctionOwnerWindow(
                    auctionName,
                    ownerName,  // Владелец — это текущий пользователь
                    description,
                    "https://geauction.com/wp-content/uploads/2018/07/5-Auction-Tips-for-Beginners2.jpg",
                    endTime // Заглушка для фото
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
