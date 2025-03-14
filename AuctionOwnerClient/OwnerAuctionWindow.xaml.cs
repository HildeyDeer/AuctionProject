
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AuctionOwnerClient
{
    public partial class OwnerAuctionWindow : Window
    {
        private const string AuctionServer = "127.0.0.1";
        private const int AuctionPort = 5001;
        private TcpClient client;
        private NetworkStream stream;
        private int ownerId;

        public OwnerAuctionWindow(int ownerId)
        {
            InitializeComponent();
            this.ownerId = ownerId;
            ConnectToServer();
        }

        private async void ConnectToServer()
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(AuctionServer, AuctionPort);
                stream = client.GetStream();

                await RequestOwnAuctions();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}");
            }
        }

        private async Task RequestOwnAuctions()
        {
            if (stream == null) return;

            string request = $"GET_OWN_AUCTIONS|{ownerId}";
            byte[] data = Encoding.UTF8.GetBytes(request);
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();

            byte[] buffer = new byte[4096];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            if (response.StartsWith("OWN_AUCTIONS"))
            {
                string[] parts = response.Split('|');
                Dispatcher.Invoke(() =>
                {
                    AuctionList.Items.Clear();
                    if (parts.Length > 1 && parts[1] != "EMPTY")
                    {
                        foreach (string auction in parts[1].Split(';'))
                        {
                            string[] details = auction.Split(',');
                            if (details.Length >= 5)
                            {
                                AuctionList.Items.Add(new Auction
                                {
                                    Name = details[0],
                                    StartPrice = double.Parse(details[1]),
                                    Category = details[2],
                                    EndTime = details[3],
                                    Status = details[4]
                                });
                            }
                        }
                    }
                });
            }
        }

        private async void CloseAuction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Auction auction)
            {
                await ChangeAuctionStatus(auction.Name, "Closed");
            }
        }

        private async void RestoreAuction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Auction auction)
            {
                await ChangeAuctionStatus(auction.Name, "Pending");
            }
        }

        private async Task ChangeAuctionStatus(string auctionName, string newStatus)
        {
            if (stream == null) return;

            string request = $"UPDATE_AUCTION_STATUS|{auctionName}|{newStatus}";
            byte[] data = Encoding.UTF8.GetBytes(request);
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();

            // Ожидание ответа от сервера
            byte[] buffer = new byte[4096];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            if (response.StartsWith("SUCCESS"))
            {
                ShowNotification($"Статус аукциона '{auctionName}' изменён на '{newStatus}'");

                // Ожидание 1.5 секунды перед обновлением списка
                //await Task.Delay(1500);

                await RequestOwnAuctions();
            }
            else if (response.StartsWith("ERROR"))
            {
                MessageBox.Show(response.Split('|')[1], "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void ShowNotification(string message)
        {
            Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    var notification = new TextBlock
                    {
                        Text = message,
                        Background = System.Windows.Media.Brushes.DarkGray,
                        Foreground = System.Windows.Media.Brushes.White,
                        Padding = new Thickness(10),
                        Opacity = 0.9,
                        FontSize = 14
                    };

                    var border = new Border
                    {
                        Background = System.Windows.Media.Brushes.Black,
                        CornerRadius = new CornerRadius(5),
                        Padding = new Thickness(5),
                        Margin = new Thickness(10),
                        Child = notification
                    };

                    NotificationPanel.Children.Add(border);

                    // Удаление уведомления через 3 секунды
                    Task.Delay(3000).ContinueWith(_ =>
                    {
                        Dispatcher.Invoke(() => NotificationPanel.Children.Remove(border));
                    });
                });
            });
        }

        private void AddAuction_Click(object sender, RoutedEventArgs e)
        {
            AddAuctionWindow addAuctionWindow = new AddAuctionWindow(stream, ownerId);
            addAuctionWindow.ShowDialog();
        }

        private void AuctionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AuctionList.SelectedItem is Auction selectedAuction)
            {
                // Создание окна с деталями аукциона
                AuctionDetailsWindow detailsWindow = new AuctionDetailsWindow(
                    selectedAuction.Name,
                    selectedAuction.StartPrice,
                    "Описание недоступно",  // Если сервер не передаёт описание, можно оставить заглушку
                    selectedAuction.Category,
                    selectedAuction.EndTime,
                    true // Так как это OwnerAuctionWindow, владелец — всегда текущий пользователь
                );

                detailsWindow.ShowDialog();
            }
        }

        private async void RefreshAuction_Click(object sender, RoutedEventArgs e)
        {
            await RequestOwnAuctions();
        }

        private class Auction
        {
            public string Name { get; set; }
            public double StartPrice { get; set; }
            public string Category { get; set; }
            public string EndTime { get; set; }
            public string Status { get; set; }
        }
    }
}



