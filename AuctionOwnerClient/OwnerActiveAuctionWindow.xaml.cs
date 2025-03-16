using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace AuctionClient
{
    public partial class AuctionOwnerWindow : Window
    {
        private TcpClient client;
        private NetworkStream stream;
        private string auctionName;
        private string ownerName;
        private DispatcherTimer countdownTimer;
        private DateTime auctionEndTime;
        private string lastBidder = "Победителя нет";
        private int currentBid = 0;

        public AuctionOwnerWindow(string auctionName, string ownerName, string description, string imageUrl, string endTime)
        {
            InitializeComponent();
            this.auctionName = auctionName;
            this.ownerName = ownerName;
            AuctionTitle.Text = auctionName;
            AuctionDescription.Text = description;
            AuctionImage.Source = new BitmapImage(new Uri(imageUrl));

            auctionEndTime = DateTime.Parse(endTime);
            StartCountdownTimer();

            _ = Task.Run(async () =>
            {
                await ConnectToServer();
                Dispatcher.Invoke(() => AuctionOwner.Text = $"Владелец: {ownerName}");
            });
        }

        private async Task ConnectToServer()
        {
            try
            {
                client = new TcpClient("127.0.0.1", 5001);
                stream = client.GetStream();
                _ = Task.Run(ListenForMessages);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}");
            }
        }

        private void StartCountdownTimer()
        {
            countdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            countdownTimer.Tick += (s, e) =>
            {
                TimeSpan remaining = auctionEndTime - DateTime.Now;
                if (remaining.TotalSeconds > 0)
                {
                    TimerText.Text = $"До окончания: {remaining.Minutes}м {remaining.Seconds}с";
                }
                else
                {
                    EndAuction();
                }
            };
            countdownTimer.Start();
        }

        private async Task ListenForMessages()
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Dispatcher.Invoke(() =>
                {
                    if (message.StartsWith("BID_UPDATE"))
                    {
                        string[] parts = message.Split('|');
                        if (parts.Length == 4 && parts[1] == auctionName)
                        {
                            lastBidder = parts[2]; // Запоминаем последнего сделавшего ставку
                            currentBid = int.Parse(parts[3]);
                            CurrentBidText.Text = $"Текущая ставка: {currentBid} $ от {lastBidder}";
                        }
                    }
                    else if (message.StartsWith("CHAT"))
                    {
                        string chatMessage = message.Substring(5);
                        string[] parts = chatMessage.Split(':', 2);

                        if (parts.Length == 2)
                        {
                            string senderInfo = parts[0].Trim();
                            string chatText = parts[1].Trim();

                            string formattedSender = senderInfo.Replace(" (Владелец)", "") == ownerName
                                ? $"{ownerName} (Вы)"
                                : senderInfo;

                            string formattedMessage = $"{formattedSender}: {chatText}";

                            ChatBox.Items.Add(formattedMessage);
                            ChatBox.ScrollIntoView(ChatBox.Items[ChatBox.Items.Count - 1]);
                        }
                    }
                });
            }
        }

        private async void SendChatButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ChatInput.Text) || stream == null) return;

            string chatMessage = $"CHAT|{ownerName}|OWNER|{ChatInput.Text}";
            byte[] data = Encoding.UTF8.GetBytes(chatMessage);
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();

            ChatInput.Clear();
        }

        private async void EndAuction()
        {
            countdownTimer.Stop();
            TimerText.Text = $"Аукцион завершён - {(currentBid > 0 ? $"победитель {lastBidder}" : "победителя нет")}";

            // Отправляем команду на закрытие аукциона
            string closeAuctionMessage = $"CLOSE_AUCTION|{auctionName}";
            byte[] data = Encoding.UTF8.GetBytes(closeAuctionMessage);

            if (stream != null)
            {
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();
            }
        }
    }
}
