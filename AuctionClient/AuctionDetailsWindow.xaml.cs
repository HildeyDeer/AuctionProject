using System.Windows;

namespace AuctionClient
{
    public partial class AuctionDetailsWindow : Window
    {
        private string auctionId;
        private string auctionName;

        public AuctionDetailsWindow(string name, string owner, string price, string description)
        {
            InitializeComponent();

            AuctionName.Text = name;
            AuctionOwner.Text = owner;
            AuctionPrice.Text = price + " $";
            AuctionDescription.Text = description;

            // Заглушка для фото
            AuctionImage.Source = new System.Windows.Media.Imaging.BitmapImage(new System.Uri("https://via.placeholder.com/200x150"));
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
