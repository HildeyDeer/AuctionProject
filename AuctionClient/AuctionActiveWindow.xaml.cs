﻿using System;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

        public AuctionActiveWindow(string username, string auctionName, string owner, string startPrice, string endTime, bool isOwner)
        {
            InitializeComponent();
            this.username = username;
            this.auctionName = auctionName;
            this.currentBid = int.Parse(startPrice.Replace(" $", ""));
            this.isOwner = isOwner;

            // Подключение к серверу
            _ = Task.Run(ConnectToServer);

            // Настройка таймера
            auctionEndTime = DateTime.Parse(endTime);
            StartCountdownTimer();

            // Устанавливаем имя пользователя в кнопке профиля
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
                TimerText.Text = remaining.TotalSeconds > 0 ? $"До окончания: {remaining.Minutes}м {remaining.Seconds}с" : "Аукцион завершён!";
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
                            CurrentBidText.Text = $"{currentBid} $";
                        }
                    }
                    else if (message.StartsWith("CHAT"))
                    {
                        ChatBox.Items.Add(message.Substring(5)); // Добавляем сообщение в ListBox
                        ChatBox.ScrollIntoView(ChatBox.Items[ChatBox.Items.Count - 1]); // Прокрутка вниз
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

        private async void PlaceBidButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(BidAmount.Text, out int bidValue) || bidValue <= currentBid)
            {
                MessageBox.Show("Ставка должна быть выше текущей!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string bidMessage = $"BID|{auctionName}|{username}|{bidValue}";
            byte[] data = Encoding.UTF8.GetBytes(bidMessage);
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();

            BidAmount.Clear();
        }

        // Обработчик нажатия кнопки профиля
        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Открытие профиля пользователя {username} (заглушка)");
        }
    }
}
