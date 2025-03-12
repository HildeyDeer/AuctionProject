using System;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace AuctionClient
{
    public partial class AuctionActiveWindow : Window
    {
        private const string AuctionServer = "127.0.0.1";
        private const int AuctionPort = 5001;
        private TcpClient client;
        private NetworkStream stream;
        private string username;
        private string auctionName;
        private int currentBid;
        private DispatcherTimer countdownTimer;
        private DateTime auctionEndTime;
        private bool isOwner;
        private string lastBidder; // Имя последнего, кто сделал ставку
        private string auctionDescription;
        private string auctionImageUrl;

        public AuctionActiveWindow(string username, string auctionName, string owner, string startPrice, string endTime, bool isOwner, string description, string imageUrl)
        {
            InitializeComponent();
            this.username = username;
            this.auctionName = auctionName;
            this.currentBid = int.Parse(startPrice.Replace(" $", ""));
            this.isOwner = isOwner;
            this.auctionDescription = description;
            this.auctionImageUrl = imageUrl;

            AuctionTitle.Text = auctionName;
            AuctionOwner.Text = $"Владелец: {owner}";
            AuctionDescription.Text = description;
            AuctionImage.Source = new BitmapImage(new Uri(imageUrl));

            _ = Task.Run(ConnectToServer);

            auctionEndTime = DateTime.Parse(endTime);
            StartCountdownTimer();

            ProfileButton.Content = $"Профиль ({username})";
        }


        private async Task ConnectToServer()
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(AuctionServer, AuctionPort);
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
                        if (parts.Length == 4 && parts[1] == auctionName && double.TryParse(parts[3], out double newBid))
                        {
                            currentBid = (int)newBid;
                            lastBidder = parts[2]; // Получаем имя последнего поставившего
                            CurrentBidText.Text = $"Текущая ставка: {currentBid} $ от {lastBidder}";
                        }
                    }
                    else if (message.StartsWith("CHAT"))
                    {
                        string chatMessage = message.Substring(5);  // Получаем текст сообщения без префикса CHAT|
                        string[] parts = chatMessage.Split(':'); // Разбиваем на имя пользователя и сообщение по ':'

                        if (parts.Length == 2)
                        {
                            string senderUsername = parts[0].Trim(); // Имя отправителя
                            string chatText = parts[1].Trim(); // Текст сообщения

                            // Форматируем сообщение с префиксом для владельца
                            string formattedMessage = senderUsername == username
                                ? $"{senderUsername} (Вы): {chatText}" // Если это наш собственный чат, добавляем "(Вы)"
                                : $"{senderUsername}{(isOwner ? " (Владелец)" : "")}: {chatText}"; // Префикс владельца

                            ChatBox.Items.Add(formattedMessage); // Добавляем сообщение в чат
                            ChatBox.ScrollIntoView(ChatBox.Items[ChatBox.Items.Count - 1]); // Прокручиваем вниз
                        }
                    }
                });
            }
        }



        private async void SendChatButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ChatInput.Text) || stream == null) return;

            string chatMessage = $"CHAT|{username}|{(isOwner ? "OWNER" : "USER")}|{ChatInput.Text}";
            byte[] data = Encoding.UTF8.GetBytes(chatMessage);
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync(); // Сбрасываем поток

            ChatInput.Clear(); // Очищаем поле ввода после отправки
        }

        // Обновляем ставки
        private async void PlaceBidButton_Click(object sender, RoutedEventArgs e)
        {
            if (DateTime.Now >= auctionEndTime)
            {
                MessageBox.Show("Аукцион уже завершён!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(BidAmount.Text, out int bidValue) || bidValue <= currentBid)
            {
                MessageBox.Show("Ставка должна быть выше текущей!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            lastBidder = username; // Фиксируем последнего сделавшего ставку

            string bidMessage = $"BID|{auctionName}|{username}|{bidValue}";
            byte[] data = Encoding.UTF8.GetBytes(bidMessage);
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();

            BidAmount.Clear();
        }

        // Обработчик нажатия кнопки профиля
        private async void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (stream == null) return;

            // Формируем и отправляем запрос на сервер
            string request = $"USER_DETAILS|{username}";
            byte[] data = Encoding.UTF8.GetBytes(request);
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();

            // Получаем ответ
            byte[] buffer = new byte[2048];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            if (response.StartsWith("USER_DETAILS"))
            {
                string[] parts = response.Split('|');
                if (parts.Length >= 6)
                {
                    string email = parts[3];
                    string address = parts[4];
                    string cardNumber = parts[5];
                    string profileImage = parts.Length > 6 ? parts[6] : "";

                    // Открываем окно профиля, передавая все данные
                    ProfileWindow profileWindow = new ProfileWindow(username, email, address, cardNumber, profileImage, client);
                    profileWindow.Show();
                }
                else
                {
                    MessageBox.Show("Ошибка загрузки данных профиля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Метод завершения аукциона
        private async void EndAuction()
        {
            countdownTimer.Stop();
            TimerText.Text = $"Аукцион завершён - победитель {lastBidder}";

            // Блокируем кнопку ставок и поле ввода
            PlaceBidButton.IsEnabled = false;
            BidAmount.IsEnabled = false;

            // Отправляем запрос на закрытие аукциона в базу
            string closeAuctionMessage = $"CLOSE_AUCTION|{auctionName}";
            byte[] data = Encoding.UTF8.GetBytes(closeAuctionMessage);

            if (stream != null)
            {
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();
            }

            // Если текущий пользователь - победитель, открываем окно выигрыша
            if (username == lastBidder)
            {
                WinnerWindow winnerWindow = new WinnerWindow(username, auctionName, currentBid);
                winnerWindow.Show();
                Close();
            }
        }



    }
}
