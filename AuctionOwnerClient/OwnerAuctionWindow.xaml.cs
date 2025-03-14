using System;
using System.ComponentModel;
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
        private DispatcherTimer refreshTimer;

        public OwnerAuctionWindow(int ownerId)
        {
            InitializeComponent();
            this.ownerId = ownerId;
            ConnectToServer();

            // Таймер автообновления списка
            refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            refreshTimer.Tick += async (s, e) => await RequestOwnAuctions();
            refreshTimer.Start();
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

        private async void AuctionActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Auction auction)
            {
                // Новый статус в зависимости от текущего состояния
                string newStatus = auction.Status == "Pending" ? "Closed" : "Pending";

                // Формируем запрос в нужном формате
                string request = $"UPDATE_AUCTION_STATUS|{auction.Name}|{newStatus}";
                byte[] data = Encoding.UTF8.GetBytes(request);

                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();

                await RequestOwnAuctions(); // Обновляем список аукционов после смены статуса
            }
        }

        private void AddAuction_Click(object sender, RoutedEventArgs e)
        {
            // Открытие окна добавления аукциона с передачей ownerId
            AddAuctionWindow addAuctionWindow = new AddAuctionWindow(stream, ownerId);
            addAuctionWindow.ShowDialog();
        }
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RequestOwnAuctions();
        }

        public class Auction : INotifyPropertyChanged
        {
            public string Name { get; set; }
            public double StartPrice { get; set; }
            public string Category { get; set; }
            public string EndTime { get; set; }

            private string status;
            public string Status
            {
                get => status;
                set
                {
                    if (status != value)
                    {
                        status = value;
                        OnPropertyChanged(nameof(Status));
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}

