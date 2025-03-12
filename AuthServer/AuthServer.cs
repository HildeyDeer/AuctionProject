using System;
using System.Data.SQLite;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class AuthServer
{
    private const int Port = 4000;  // Порт сервера аутентификации
    private const string DbPath = "C:\\Users\\user\\Downloads\\Project TEST\\Project TEST\\AuctionServer\\AuctionDB.db"; // Теперь используем объединённую базу

    public static async Task Main()
    {
        TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), Port);
        listener.Start();
        Console.WriteLine($"Auth Server запущен на порту {Port}");

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            _ = Task.Run(() => HandleClient(client));
        }
    }

    private static async Task HandleClient(TcpClient client)
    {
        using NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        string response = ProcessRequest(request);
        byte[] responseData = Encoding.UTF8.GetBytes(response);
        await stream.WriteAsync(responseData, 0, responseData.Length);
    }

    private static string ProcessRequest(string request)
    {
        string[] parts = request.Split('|');
        if (parts.Length < 2) return "ERROR|Неверный формат запроса";

        string command = parts[0];
        string username = parts[1];

        using var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;");
        conn.Open();
        using var cmd = new SQLiteCommand(conn);

        if (command == "REGISTER" && parts.Length >= 6)
        {
            string password = parts[2]; // Желательно хэшировать
            string email = parts[3];
            string address = parts[4];
            string cardNumber = parts[5];
            string profileImage = parts.Length > 6 ? parts[6] : "";

            try
            {
                cmd.CommandText = @"INSERT INTO Users (Username, Password, Email, Address, CardNumber, ProfileImage) 
                                VALUES (@user, @pass, @mail, @addr, @card, @image)";
                cmd.Parameters.AddWithValue("@user", username);
                cmd.Parameters.AddWithValue("@pass", password);
                cmd.Parameters.AddWithValue("@mail", email);
                cmd.Parameters.AddWithValue("@addr", address);
                cmd.Parameters.AddWithValue("@card", cardNumber);
                cmd.Parameters.AddWithValue("@image", profileImage);
                cmd.ExecuteNonQuery();

                return "SUCCESS|Регистрация успешна";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при регистрации: {ex.Message}");
                return "ERROR|Ошибка при регистрации";
            }
        }
        else if (command == "LOGIN" && parts.Length == 3) // Исправлено: проверяем только 2 параметра
        {
            string password = parts[2];

            cmd.CommandText = "SELECT Id FROM Users WHERE Username = @user AND Password = @pass";
            cmd.Parameters.AddWithValue("@user", username);
            cmd.Parameters.AddWithValue("@pass", password);
            object result = cmd.ExecuteScalar();

            return result != null ? "SUCCESS|Вход успешен" : "ERROR|Неверный логин или пароль";
        }

        return "ERROR|Неизвестная команда";
    }

}
