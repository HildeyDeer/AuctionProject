using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class AuctionClient
{
    private const string AuthServer = "127.0.0.1";
    private const int AuthPort = 4000;
    private const string AuctionServer = "127.0.0.1";
    private const int AuctionPort = 5000;  // Подключаемся к Load Balancer

    private static async Task Main()
    {
        Console.Write("Введите логин: ");
        string username = Console.ReadLine();
        Console.Write("Введите пароль: ");
        string password = Console.ReadLine();

        if (!await Authenticate(username, password))
        {
            Console.WriteLine("Ошибка входа!");
            return;
        }

        using TcpClient client = new TcpClient();
        await client.ConnectAsync(AuctionServer, AuctionPort);
        NetworkStream stream = client.GetStream();
        Console.WriteLine("Подключено к аукциону!");

        Task listenTask = ListenForMessages(stream);

        while (true)
        {
            string message = Console.ReadLine();
            if (string.IsNullOrEmpty(message)) continue;

            if (message.StartsWith("/bid"))
            {
                string[] parts = message.Split(' ');
                if (parts.Length < 3) continue;
                string auctionId = parts[1];
                string amount = parts[2];

                string bidMessage = $"BID|{auctionId}|{amount}";
                byte[] data = Encoding.UTF8.GetBytes(bidMessage);
                await stream.WriteAsync(data, 0, data.Length);
            }
            else if (message.StartsWith("/chat"))
            {
                string chatMessage = $"CHAT|{username}: {message.Substring(5)}";
                byte[] data = Encoding.UTF8.GetBytes(chatMessage);
                await stream.WriteAsync(data, 0, data.Length);
            }
        }
    }

    private static async Task<bool> Authenticate(string username, string password)
    {
        using TcpClient authClient = new TcpClient();
        await authClient.ConnectAsync(AuthServer, AuthPort);
        NetworkStream authStream = authClient.GetStream();

        string loginRequest = $"REGISTER|{username}|{password}";
        byte[] data = Encoding.UTF8.GetBytes(loginRequest);
        await authStream.WriteAsync(data, 0, data.Length);

        byte[] responseBuffer = new byte[1024];
        int bytesRead = await authStream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
        string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);
        return response.StartsWith("SUCCESS");
    }

    private static async Task ListenForMessages(NetworkStream stream)
    {
        byte[] buffer = new byte[1024];
        while (true)
        {
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0) break;

            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"\n[Сообщение]: {message}");
        }
    }
}