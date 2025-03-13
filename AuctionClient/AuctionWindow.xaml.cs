using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        public AuctionWindow(string username)
        {
            InitializeComponent();
            this.username = username;
            ProfileButton.Content = $"Профиль ({username})";
            ConnectToServer();
        }

        private async void ConnectToServer()
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(AuctionServer, AuctionPort);
                stream = client.GetStream();

                // Запускаем обновление аукционов и прием сообщений
                _ = Task.Run(HandleAuctionUpdates);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}");
            }
        }

        private async Task HandleAuctionUpdates()
        {
            if (stream == null) return;

            byte[] buffer = new byte[4096];
            string request = "GET_AUCTIONS";
            byte[] requestData = Encoding.UTF8.GetBytes(request);

            while (true)
            {
                // Отправка запроса на получение аукционов
                await stream.WriteAsync(requestData, 0, requestData.Length);
                await stream.FlushAsync();

                // Чтение данных от сервера
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break; // Соединение разорвано

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                if (message.StartsWith("AUCTIONS"))
                {
                    string[] parts = message.Split('|');
                    Dispatcher.Invoke(() =>
                    {
                        AuctionList.Items.Clear();
                        if (parts.Length > 1 && parts[1] != "EMPTY")
                        {
                            foreach (string auction in parts[1].Split(';'))
                            {
                                string[] details = auction.Split(',');
                                if (details.Length == 5)
                                {
                                    AuctionList.Items.Add(new Auction
                                    {
                                        Name = details[0],
                                        OwnerUsername = details[1],
                                        StartPrice = details[2] + " $",
                                        Category = details[3],
                                        EndTime = details[4]
                                    });
                                }
                            }
                        }
                    });
                }

                // Задержка 30 секунд перед следующим запросом
                await Task.Delay(30000);
            }
        }

        private async Task ManualRequestAuctions()
        {
            if (stream == null) return;

            string request = "GET_AUCTIONS";
            byte[] data = Encoding.UTF8.GetBytes(request);
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();

            byte[] buffer = new byte[4096];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0) return; // Соединение разорвано

            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            if (message.StartsWith("AUCTIONS"))
            {
                string[] parts = message.Split('|');
                Dispatcher.Invoke(() =>
                {
                    AuctionList.Items.Clear();
                    if (parts.Length > 1 && parts[1] != "EMPTY")
                    {
                        foreach (string auction in parts[1].Split(';'))
                        {
                            string[] details = auction.Split(',');
                            if (details.Length == 5)
                            {
                                AuctionList.Items.Add(new Auction
                                {
                                    Name = details[0],
                                    OwnerUsername = details[1],
                                    StartPrice = details[2] + " $",
                                    Category = details[3],
                                    EndTime = details[4]
                                });
                            }
                        }
                    }
                });
            }
        }


        private async void AuctionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AuctionList.SelectedItem is Auction selectedAuction)
            {
                string request = $"GET_AUCTION_DETAILS|{selectedAuction.Name}";
                byte[] data = Encoding.UTF8.GetBytes(request);

                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();

                byte[] buffer = new byte[4096];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                if (response.StartsWith("AUCTION_DETAILS"))
                {
                    string[] parts = response.Split('|');
                    if (parts.Length == 8)
                    {
                        string name = parts[1];
                        string owner = parts[2];
                        string startPrice = parts[3];
                        string description = parts[4];
                        string category = parts[5];
                        string endTime = parts[6];
                        string status = parts[7];

                        AuctionDetailsWindow detailsWindow = new AuctionDetailsWindow(
                            name, owner, startPrice, description, category, endTime, username
                        );
                        detailsWindow.Show();
                    }
                }
                else
                {
                    MessageBox.Show("Ошибка при получении данных аукциона", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (stream == null) return;

            string request = $"USER_DETAILS|{username}";
            byte[] data = Encoding.UTF8.GetBytes(request);
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();

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

                    ProfileWindow profileWindow = new ProfileWindow(username, email, address, cardNumber, profileImage, client);
                    profileWindow.Show();
                }
                else
                {
                    MessageBox.Show("Ошибка загрузки данных профиля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _ = ManualRequestAuctions();
        }

        private async void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (stream == null) return;

            string selectedCategory = ((ComboBoxItem)CategoryFilter.SelectedItem).Content.ToString();

            if (selectedCategory == "Все")
            {
                await ManualRequestAuctions();
                return;
            }

            string request = $"FILTER_BY_CATEGORY|{selectedCategory}";
            byte[] data = Encoding.UTF8.GetBytes(request);
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();

            // Читаем ответ от сервера
            byte[] buffer = new byte[4096];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0) return; // Если соединение разорвано

            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            if (message.StartsWith("AUCTIONS"))
            {
                string[] parts = message.Split('|');
                Dispatcher.Invoke(() =>
                {
                    AuctionList.Items.Clear();
                    if (parts.Length > 1 && parts[1] != "EMPTY")
                    {
                        foreach (string auction in parts[1].Split(';'))
                        {
                            string[] details = auction.Split(',');
                            if (details.Length == 5)
                            {
                                AuctionList.Items.Add(new Auction
                                {
                                    Name = details[0],
                                    OwnerUsername = details[1],
                                    StartPrice = details[2] + " $",
                                    Category = details[3],
                                    EndTime = details[4]
                                });
                            }
                        }
                    }
                });
            }
        }

        private class Auction
        {
            public string Name { get; set; }
            public string OwnerUsername { get; set; }
            public string StartPrice { get; set; }
            public string Category { get; set; }
            public string EndTime { get; set; }
            public string Status { get; set; }
        }
    }
}
