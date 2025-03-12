using System.Windows;

namespace AuctionClient
{
    public partial class WinnerWindow : Window
    {
        private string username;
        private string auctionName;
        private int bidAmount;

        public WinnerWindow(string username, string auctionName, int bidAmount)
        {
            InitializeComponent();
            this.username = username;
            this.auctionName = auctionName;
            this.bidAmount = bidAmount;

            WinnerText.Text = $"Поздравляем, {username}!";
            PrizeText.Text = $"Вы выиграли аукцион '{auctionName}' со ставкой {bidAmount} $!";
        }

        // Метод-обработчик кнопки возврата
        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            Close(); // Закрытие окна
        }
    }
}
