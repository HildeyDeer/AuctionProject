using System.Windows;

namespace AuctionClient
{
    public partial class AuctionDetailsWindow : Window
    {
        private string auctionId;
        private string auctionName;
        private string username;
        private string auctionOwner;

        public AuctionDetailsWindow(string name, string owner, string price, string description, string category, string endTime, string username)
        {
            InitializeComponent();

            this.username = username;  // Сохраняем имя пользователя
            this.auctionOwner = owner; // Сохраняем владельца аукциона

            AuctionName.Text = name;
            AuctionOwner.Text = owner;
            AuctionPrice.Text = price + " $";
            AuctionDescription.Text = description;
            AuctionCategory.Text = category;
            AuctionEndTime.Text = endTime;

            // Заглушка для фото
            AuctionImage.Source = new System.Windows.Media.Imaging.BitmapImage(new System.Uri("https://geauction.com/wp-content/uploads/2018/07/5-Auction-Tips-for-Beginners2.jpg"));
        }

        private void JoinAuction_Click(object sender, RoutedEventArgs e)
        {
            bool isOwner = username == auctionOwner;
            AuctionActiveWindow activeAuction = new AuctionActiveWindow(
                username, AuctionName.Text, AuctionOwner.Text, AuctionPrice.Text, AuctionEndTime.Text, isOwner,
                AuctionDescription.Text, AuctionImage.Source.ToString()
            );
            activeAuction.Show();
            Close();
        }
    }
}
