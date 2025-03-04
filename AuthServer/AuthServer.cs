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
        TcpListener listener = new TcpListener(IPAddress.Any, Port);
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
        string password = parts.Length > 2 ? parts[2] : "";
        string permissionKey = parts.Length > 3 ? parts[3] : "";

        using var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;");
        conn.Open();
        using var cmd = new SQLiteCommand(conn);

        if (command == "REGISTER")
        {
            try
            {
                cmd.CommandText = "INSERT INTO Users (Username, Password) VALUES (@user, @pass)";
                cmd.Parameters.AddWithValue("@user", username);
                cmd.Parameters.AddWithValue("@pass", password);
                cmd.ExecuteNonQuery();
                return "SUCCESS|Регистрация успешна";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при регистрации: {ex.Message}");
                return "ERROR|Ошибка при регистрации";
            }
        }
        else if (command == "LOGIN")
        {
            cmd.CommandText = "SELECT Id FROM Users WHERE Username = @user AND Password = @pass";
            cmd.Parameters.AddWithValue("@user", username);
            cmd.Parameters.AddWithValue("@pass", password);
            object result = cmd.ExecuteScalar();
            return result != null ? "SUCCESS|Вход успешен" : "ERROR|Неверный логин или пароль";
        }
        else if (command == "OWNER_LOGIN")
        {
            cmd.CommandText = "SELECT Id FROM Owners WHERE Username = @user AND Password = @pass AND PermissionKey = @key";
            cmd.Parameters.AddWithValue("@user", username);
            cmd.Parameters.AddWithValue("@pass", password);
            cmd.Parameters.AddWithValue("@key", permissionKey);
            object result = cmd.ExecuteScalar();
            return result != null ? "SUCCESS|Вход владельца успешен" : "ERROR|Неверные данные владельца";
        }

        return "ERROR|Неизвестная команда";
    }
}
