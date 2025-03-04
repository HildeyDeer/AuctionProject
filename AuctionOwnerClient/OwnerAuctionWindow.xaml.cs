using AuctionOwnerClient;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AuctionOwnerClient
{
    public partial class OwnerAuctionWindow : Window
    {
        private const string AuctionServer = "127.0.0.1";
        private const int AuctionPort = 5001;
        private TcpClient owner;
        private NetworkStream stream;
        private string username;

        public OwnerAuctionWindow(string username)
        {
            InitializeComponent();
            this.username = username;
            ConnectToServer();
        }

        private async void ConnectToServer()
        {
            try
            {
                owner = new TcpClient();
                await owner.ConnectAsync(AuctionServer, AuctionPort);
                stream = owner.GetStream();
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
        private async void LoadAuctions()
        {
            try
            {
                // 🔹 **Сначала получаем ID владельца**
                string requestOwnerId = $"GET_OWNER_ID|{username}";
                byte[] requestData = Encoding.UTF8.GetBytes(requestOwnerId);
                await stream.WriteAsync(requestData, 0, requestData.Length);

                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                if (!response.StartsWith("OWNER_ID|"))
                {
                    MessageBox.Show("Ошибка: не удалось получить ID владельца.");
                    return;
                }

                string ownerId = response.Split('|')[1];

                // 🔹 **Запрашиваем аукционы только для владельца**
                string request = $"GET_AUCTIONS|{ownerId}";
                byte[] data = Encoding.UTF8.GetBytes(request);
                await stream.WriteAsync(data, 0, data.Length);

                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                AuctionList.Items.Clear();

                if (response == "EMPTY")
                {
                    return;
                }
                if (response.StartsWith("ERROR"))
                {
                    MessageBox.Show($"Ошибка загрузки аукционов: {response}");
                    return;
                }

                string[] auctions = response.Split(';');
                foreach (string auction in auctions)
                {
                    if (!string.IsNullOrWhiteSpace(auction))
                    {
                        AuctionList.Items.Add(auction);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки аукционов: {ex.Message}");
            }
        }

        private void AddAuction_Click(object sender, RoutedEventArgs e)
        {
            AddAuctionWindow addAuctionWindow = new AddAuctionWindow(stream, username);
            addAuctionWindow.ShowDialog();
            LoadAuctions(); // Перезагружаем аукционы после добавления
        }


        private async void DeleteAuction_Click(object sender, RoutedEventArgs e)
        {
            string deleteMessage = $"DELETE_AUCTION|{username}|1"; // Заглушка ID
            byte[] data = Encoding.UTF8.GetBytes(deleteMessage);
            await stream.WriteAsync(data, 0, data.Length);
        }

        private async void StartAuction_Click(object sender, RoutedEventArgs e)
        {
            string startMessage = $"START_AUCTION|{username}|1"; // Заглушка ID
            byte[] data = Encoding.UTF8.GetBytes(startMessage);
            await stream.WriteAsync(data, 0, data.Length);
        }

        private async void SendChat_Click(object sender, RoutedEventArgs e)
        {
            string message = ChatMessageBox.Text;
            if (string.IsNullOrWhiteSpace(message)) return;

            string chatMessage = $"CHAT|{username}|OWNER|{message}";
            byte[] data = Encoding.UTF8.GetBytes(chatMessage);
            await stream.WriteAsync(data, 0, data.Length);
            ChatMessageBox.Clear();
        }
        private async void RefreshAuctions_Click(object sender, RoutedEventArgs e)
        {
            // Загружаем обновленный список аукционов
            LoadAuctions();
        }
    }
}