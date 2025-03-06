using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

            // Таймер для обновления списка аукционов
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

                // Первое обновление аукционов
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

            string request = "GET_AUCTIONS";
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

                if (message.StartsWith("AUCTIONS"))
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
                                    string[] details = auction.Split(',');
                                    if (details.Length == 3)
                                    {
                                        AuctionList.Items.Add(new Auction
                                        {
                                            Name = details[0],
                                            OwnerUsername = details[1],
                                            StartPrice = details[2]
                                        });
                                    }
                                }
                            }
                        });
                    }
                }
            }
        }

        private void AuctionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AuctionList.SelectedItem is Auction selectedAuction)
            {
                var detailsWindow = new AuctionDetailsWindow(
                    selectedAuction.Name,
                    selectedAuction.OwnerUsername,
                    selectedAuction.StartPrice
                );
                detailsWindow.Show();
            }
        }


        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Открытие профиля пользователя {username} (заглушка)");
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _ = RequestAuctions();
        }

        private class Auction
        {
            public string Name { get; set; }
            public string OwnerUsername { get; set; }
            public string StartPrice { get; set; }
        }
    }
}