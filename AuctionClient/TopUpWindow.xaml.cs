using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AuctionClient
{
    public partial class TopUpWindow : Window
    {
        public decimal Amount { get; private set; }
        private string _cardNumber;

        public TopUpWindow(string cardNumber)
        {
            InitializeComponent();
            _cardNumber = cardNumber;
            CardInfoText.Text = $"С вашей карты {_cardNumber} будут списаны средства.";
            FeeInfoText.Text = "Пожалуйста, убедитесь в правильности суммы.";
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(AmountTextBox.Text, out decimal amount) && amount > 0)
            {
                Amount = amount;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Введите корректную сумму!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }


}
