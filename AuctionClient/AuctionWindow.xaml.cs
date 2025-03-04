using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace AuctionClient
{
    public partial class AuctionWindow : Window
    {
        private const string AuctionServer = "127.0.0.1";
        private const int AuctionPort = 5001; // Порт сервера аукциона
        private TcpClient client;
        private NetworkStream stream;
        private string username;

        public AuctionWindow(string username)
        {
            InitializeComponent();
            this.username = username;
            ConnectToServer();
        }

        private async void ConnectToServer()
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(AuctionServer, AuctionPort);
                stream = client.GetStream();
                ListenForMessages();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}");
            }
        }

        private async void ListenForMessages()
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Dispatcher.Invoke(() => ChatBox.Items.Add(message));
            }
        }

        private async void Bid_Click(object sender, RoutedEventArgs e)
        {
            string bidAmount = BidAmountBox.Text;
            if (string.IsNullOrWhiteSpace(bidAmount)) return;

            if (!double.TryParse(bidAmount, out double bidValue))
            {
                MessageBox.Show("Введите корректную сумму.");
                return;
            }

            // Отправка ставки (предположим, что ID аукциона известен и равен 1)
            string bidMessage = $"BID|1|{username}|{bidValue}";
            byte[] data = Encoding.UTF8.GetBytes(bidMessage);
            await stream.WriteAsync(data, 0, data.Length);
            BidAmountBox.Clear();
        }

        private async void SendChat_Click(object sender, RoutedEventArgs e)
        {
            string message = ChatMessageBox.Text;
            if (string.IsNullOrWhiteSpace(message)) return;

            string chatMessage = $"CHAT|{username}|USER|{message}";
            byte[] data = Encoding.UTF8.GetBytes(chatMessage);
            await stream.WriteAsync(data, 0, data.Length);
            ChatMessageBox.Clear();
        }
    }
}
