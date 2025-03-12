﻿using System;
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
        private DispatcherTimer auctionUpdateTimer;

        public AuctionWindow(string username)
        {
            InitializeComponent();
            this.username = username;
            ProfileButton.Content = $"Профиль ({username})";
            ConnectToServer();

            //Таймер для обновления списка аукционов

            _ = Task.Run(() => RequestAuctions());

            //auctionUpdateTimer = new DispatcherTimer
            //{
            //    Interval = TimeSpan.FromSeconds(2)
            //};
            //auctionUpdateTimer.Tick += async (s, e) => await RequestAuctions();
            //auctionUpdateTimer.Start();
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

            while (true)
            {
                await stream.WriteAsync(data, 0, data.Length);
                Console.WriteLine("Request sent at: " + DateTime.Now);

                // Задержка 30 секунд
                await Task.Delay(30000); // 30000 миллисекунд = 30 секунд
            }
        }

        private async Task ManualRequestAuctions()
        {

            if (stream == null) return;

            string request = "GET_AUCTIONS";
            byte[] data = Encoding.UTF8.GetBytes(request);
            await stream.WriteAsync(data, 0, data.Length);
        }

        private async void ListenForMessages()
        {
            byte[] buffer = new byte[4096];
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

                byte[] buffer = new byte[4096]; // Буфер для ответа
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

                        // Создаем окно деталей аукциона, передавая username
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
                await ManualRequestAuctions(); // Загружаем все аукционы
                return;
            }

            string request = $"FILTER_BY_CATEGORY|{selectedCategory}";
            byte[] data = Encoding.UTF8.GetBytes(request);
            await stream.WriteAsync(data, 0, data.Length);
        }


        private class Auction
        {
            public string Name { get; set; }
            public string OwnerUsername { get; set; }
            public string StartPrice { get; set; }
            public string Category { get; set; }
            public string EndTime { get; set; } // Время окончания
            public string Status { get; set; }  // Статус аукциона
        }
    }
}