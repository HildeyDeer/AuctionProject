using System;
using System.Windows;

namespace AuctionClient
{
    public partial class AuctionDetailsWindow : Window
    {
        private string auctionName;
        private string owner;
        private string startPrice;

        public AuctionDetailsWindow(string auctionName, string owner, string startPrice)
        {
            InitializeComponent();
            this.auctionName = auctionName;
            this.owner = owner;
            this.startPrice = startPrice;

            AuctionNameText.Text = auctionName;
            OwnerText.Text = owner;
            StartPriceText.Text = startPrice;
        }

        private void JoinAuction_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Вы присоединились к аукциону: {auctionName}");
            Close();
        }
    }
}
