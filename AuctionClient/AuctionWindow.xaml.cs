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
        private const int AuctionPort = 5001;
        private TcpClient client;
        private NetworkStream stream;
        private string username;
        private DispatcherTimer auctionUpdateTimer;

        public AuctionWindow(string username)
        {
            InitializeComponent();
            this.username = username;
            ProfileButton.Content = $"Профиль ({username})";
            ConnectToServer();

            // Запуск таймера обновления списка аукционов
            auctionUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            auctionUpdateTimer.Tick += async (s, e) => await RequestAuctions();
            auctionUpdateTimer.Start();
        }

        private async void ConnectToServer()
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(AuctionServer, AuctionPort);
                stream = client.GetStream();
                ListenForMessages();

                // Первоначальное обновление списка аукционов
                await RequestAuctions();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}");
            }
        }

        private async Task RequestAuctions()
        {
            if (stream == null) return;

            string request = "GET_AUCTIONS|1";
            byte[] data = Encoding.UTF8.GetBytes(request);
            await stream.WriteAsync(data, 0, data.Length);
        }

        private async void ListenForMessages()
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                // Фильтрация сообщений
                if (message.StartsWith("CHAT") || message.StartsWith("BID_UPDATE"))
                {
                    Dispatcher.Invoke(() => ChatBox.Items.Add(message));
                }
                else if (message.StartsWith("AUCTIONS"))
                {
                    string[] parts = message.Split('|');
                    if (parts.Length > 1)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            AuctionList.Items.Clear();
                            if (parts[1] != "EMPTY")
                            {
                                foreach (string auction in parts[1].Split(';'))
                                {
                                    AuctionList.Items.Add(auction);
                                }
                            }
                        });
                    }
                }
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

            string bidMessage = $"BID|1|{username}|{bidValue}"; // ID аукциона замените на актуальный
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

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Открытие профиля пользователя {username} (заглушка)");
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _ = RequestAuctions();
        }

    }
}