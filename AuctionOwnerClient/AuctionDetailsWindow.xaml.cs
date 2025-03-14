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

        public AuctionDetailsWindow(string name, double price, string description, string category, string endTime, bool isOwner)
        {
            InitializeComponent();

            this.auctionName = name;
            this.startPrice = price;
            this.category = category;
            this.endTime = endTime;
            this.description = description;
            this.isOwner = isOwner;

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
            MessageBox.Show("Здесь можно реализовать логику для управления аукционом.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
    }
}
