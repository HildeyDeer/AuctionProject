using System.Windows;

namespace AuctionClient
{
    public partial class AuctionDetailsWindow : Window
    {
        private string auctionId;
        private string auctionName;

        public AuctionDetailsWindow(string name, string owner, string price, string description, string category, string endTime)
        {
            InitializeComponent();

            AuctionName.Text = name;
            AuctionOwner.Text = owner;
            AuctionPrice.Text = price + " $";
            AuctionDescription.Text = description;
            AuctionCategory.Text = "Категория: " + category;
            AuctionEndTime.Text = "Окончание: " + endTime;

            // Заглушка для фото
            AuctionImage.Source = new System.Windows.Media.Imaging.BitmapImage(new System.Uri("https://geauction.com/wp-content/uploads/2018/07/5-Auction-Tips-for-Beginners2.jpg"));
        }


        private void JoinAuction_Click(object sender, RoutedEventArgs e)
        {
            // Закрываем текущее окно
            //this.Close();

            // Открываем окно активного аукциона
            //AuctionActiveWindow activeAuction = new AuctionActiveWindow(auctionId, auctionName);
            //activeAuction.Show();
        }
    }
}
