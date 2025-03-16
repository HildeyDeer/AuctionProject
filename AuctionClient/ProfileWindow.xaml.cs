using System;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;

namespace AuctionClient
{
    public partial class ProfileWindow : Window
    {
        private string username;
        private TcpClient client;
        private NetworkStream stream;

        public ProfileWindow(string username, string email, string address, string cardNumber, string profileImage, TcpClient client)
        {
            InitializeComponent();
            this.username = username;
            this.client = client ?? throw new ArgumentNullException(nameof(client), "Соединение с сервером отсутствует");

            LoginText.Text = username;
            EmailText.Text = email;
            AddressText.Text = address;
            CardText.Text = cardNumber;

            if (!string.IsNullOrEmpty(profileImage))
            {
                LoadProfileImage(profileImage);
            }
        }

        private void LoadProfileImage(string imageUrl)
        {
            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imageUrl, UriKind.Absolute);
                bitmap.EndInit();
                ProfileImage.Source = bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки изображения: " + ex.Message);
            }
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (client == null || !client.Connected)
            {
                MessageBox.Show("Нет соединения с сервером!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ChangePasswordWindow changePasswordWindow = new ChangePasswordWindow(username);
            changePasswordWindow.ShowDialog();
        }
    }
}
