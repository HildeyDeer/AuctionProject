using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


class AuctionServer
{
    private const int Port = 5001;  // Порт для аукциона
    private const string DbPath = "C:\\Users\\user\\Downloads\\Project TEST\\Project TEST\\AuctionServer\\AuctionDB.db"; // Файл базы данных SQLite
    private static List<TcpClient> clients = new();

    public static async Task Main()
    {
        InitDatabase(); // Создание базы данных при старте
        TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), Port);
        listener.Start();
        Console.WriteLine($"Auction Server запущен на порту {Port}");

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            clients.Add(client);
            _ = Task.Run(() => HandleClient(client));
        }
    }

    private static void InitDatabase()
    {
        using var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;");
        conn.Open();
        using var cmd = new SQLiteCommand(conn);

        // Таблица пользователей
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT UNIQUE NOT NULL,
                        Password TEXT NOT NULL,
                        Email TEXT UNIQUE NOT NULL,
                        Address TEXT,
                        CardNumber TEXT,
                        ProfileImage TEXT,
                        Balance REAL DEFAULT 0.0
                    );";
        cmd.ExecuteNonQuery();


        // Таблица владельцев аукционов
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Owners (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                Username TEXT UNIQUE NOT NULL,
                                Password TEXT NOT NULL,
                                PermissionKey TEXT NOT NULL
                            );";
        cmd.ExecuteNonQuery();

        // Таблица аукционов
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Auctions (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        OwnerId INTEGER NOT NULL,
                        Name TEXT NOT NULL,
                        Description TEXT,
                        StartPrice REAL NOT NULL,
                        Category TEXT NOT NULL,
                        EndTime DATETIME NOT NULL,
                        Status TEXT DEFAULT 'Pending',
                        ImageUrl TEXT,
                        FOREIGN KEY (OwnerId) REFERENCES Owners(Id)
                    );";
        cmd.ExecuteNonQuery();

    }

    private static async Task HandleClient(TcpClient client)
    {
        using NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        while (true)
        {
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0) break;

            string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"Получено: {request}");
            string response = ProcessRequest(request);

            byte[] responseData = Encoding.UTF8.GetBytes(response);
            await stream.WriteAsync(responseData, 0, responseData.Length); // Отправляем ответ только отправителю

            // Рассылаем всем, кроме отправителя
            if (request.StartsWith("CHAT") || request.StartsWith("BID") || request.StartsWith("ADD_AUCTION"))
            {
                BroadcastMessage(response, client);
            }
        }

        clients.Remove(client);
    }



    private static string ProcessRequest(string request)
    {
        string[] parts = request.Split('|');
        string command = parts[0];

        // Обработка команды GET_AUCTIONS отдельно
        if (command == "GET_AUCTIONS")
        {
            // Проверяем, что не передаются параметры
            if (parts.Length != 1)
            {
                return "ERROR|Неверный формат запроса";
            }

            using var conn1 = new SQLiteConnection($"Data Source={DbPath};Version=3;");
            conn1.Open();
            using var cmd1 = new SQLiteCommand(conn1);

            cmd1.CommandText = @"SELECT A.Name, O.Username, A.StartPrice, A.Category, A.EndTime, A.ImageUrl 
                     FROM Auctions A 
                     JOIN Owners O ON A.OwnerId = O.Id 
                     WHERE A.Status = 'Pending'";

            using var reader = cmd1.ExecuteReader();
            List<string> auctions = new();

            while (reader.Read())
            {
                string name = reader.GetString(0);
                string owner = reader.GetString(1);
                double startPrice = reader.GetDouble(2);
                string category = reader.GetString(3);
                string endTime = reader.GetString(4);
                string imageUrl = reader.IsDBNull(5) ? "" : reader.GetString(5); // Проверка на NULL

                auctions.Add($"{name},{owner},{startPrice},{category},{endTime},{imageUrl}");
            }

            return auctions.Count > 0 ? $"AUCTIONS|{string.Join(";", auctions)}" : "AUCTIONS|EMPTY";

        }

        //// Для других команд
        if (parts.Length < 2)
        {
            return "ERROR|Неверный формат запроса";
        }

        using var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;");
        conn.Open();
        using var cmd = new SQLiteCommand(conn);

        // Запрос деталей аукциона (по названию)
        if (command == "GET_AUCTION_DETAILS" && parts.Length == 2)
        {
            string auctionName = parts[1];

            cmd.CommandText = @"SELECT A.Name, O.Username, A.StartPrice, A.Description, A.Category, A.EndTime, A.Status, A.ImageUrl
                    FROM Auctions A 
                    JOIN Owners O ON A.OwnerId = O.Id 
                    WHERE A.Name = @name";
            cmd.Parameters.AddWithValue("@name", auctionName);

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                string name = reader.GetString(0);
                string owner = reader.GetString(1);
                double startPrice = reader.GetDouble(2);
                string description = reader.IsDBNull(3) ? "Нет описания" : reader.GetString(3);
                string category = reader.GetString(4);
                string endTime = reader.GetString(5);
                string status = reader.GetString(6);
                string imageUrl = reader.IsDBNull(7) ? "" : reader.GetString(7);

                return $"AUCTION_DETAILS|{name}|{owner}|{startPrice}|{description}|{category}|{endTime}|{status}|{imageUrl}";
            }

            return "ERROR|Аукцион не найден";

        }

        else if (command == "TOP_UP" && parts.Length == 3)
        {
            string username = parts[1];
            if (!decimal.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount) || amount <= 0)
            {
                return "ERROR|Некорректная сумма";
            }

            try
            {
                // Получение текущего баланса
                cmd.CommandText = "SELECT Balance FROM Users WHERE Username = @user";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@user", username);
                object result = cmd.ExecuteScalar();

                if (result == null)
                {
                    return "ERROR|Пользователь не найден";
                }

                // Обновление баланса
                cmd.CommandText = "UPDATE Users SET Balance = Balance + @amount WHERE Username = @user";
                cmd.Parameters.AddWithValue("@amount", amount);
                cmd.ExecuteNonQuery();

                return "SUCCESS|Баланс пополнен";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при пополнении баланса: {ex.Message}");
                return "ERROR|Ошибка при пополнении баланса";
            }
        }

        else if (command == "GET_BALANCE" && parts.Length == 2)
        {
            string username = parts[1];

            try
            {
                // Получаем баланс пользователя
                cmd.CommandText = "SELECT Balance FROM Users WHERE Username = @user";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@user", username);
                object result = cmd.ExecuteScalar();

                if (result == null)
                {
                    return "ERROR|Пользователь не найден";
                }

                // Преобразуем результат в число с плавающей точкой
                if (!decimal.TryParse(result.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal balance))
                {
                    return "ERROR|Ошибка обработки баланса";
                }

                return $"BALANCE|{balance:F2}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении баланса: {ex.Message}");
                return "ERROR|Ошибка при получении баланса";
            }
        }


        if (command == "BID" && parts.Length == 4)
        {
            string auctionName = parts[1];
            string username = parts[2];
            double bidAmount = double.Parse(parts[3]);

            // Проверяем, существует ли аукцион
            cmd.CommandText = "SELECT StartPrice FROM Auctions WHERE Name = @name";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@name", auctionName);
            object startPriceResult = cmd.ExecuteScalar();

            if (startPriceResult == null)
            {
                return "ERROR|Аукцион не найден";
            }

            double currentPrice = Convert.ToDouble(startPriceResult);

            // Проверяем, что ставка выше текущей
            if (bidAmount <= currentPrice)
            {
                return "ERROR|Ставка должна быть выше текущей";
            }

            // Проверяем баланс пользователя (но не списываем!)
            cmd.CommandText = "SELECT Balance FROM Users WHERE Username = @username";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@username", username);
            object balanceResult = cmd.ExecuteScalar();

            if (balanceResult == null)
            {
                return "ERROR|Пользователь не найден";
            }

            double currentBalance = Convert.ToDouble(balanceResult);

            // Проверяем, что у пользователя достаточно средств
            if (currentBalance < bidAmount)
            {
                return "ERROR|Недостаточно средств на балансе";
            }

            // Обновляем текущую ставку в аукционе
            cmd.CommandText = "UPDATE Auctions SET StartPrice = @bid WHERE Name = @name";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@bid", bidAmount);
            cmd.Parameters.AddWithValue("@name", auctionName);
            cmd.ExecuteNonQuery();

            // Отправляем обновлённые данные
            return $"BID_UPDATE|{auctionName}|{username}|{bidAmount}";
        }

        if (command == "DEDUCT_BALANCE" && parts.Length == 3)
        {
            string username = parts[1];
            double bidAmount = double.Parse(parts[2]);

            // Проверяем, существует ли пользователь
            cmd.CommandText = "SELECT Balance FROM Users WHERE Username = @username";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@username", username);
            object balanceResult = cmd.ExecuteScalar();

            if (balanceResult == null)
            {
                return "ERROR|Пользователь не найден";
            }

            double currentBalance = Convert.ToDouble(balanceResult);

            // Проверяем, что у пользователя достаточно средств перед списанием
            if (currentBalance < bidAmount)
            {
                return "ERROR|Недостаточно средств на балансе";
            }

            // Списываем баланс только после завершения аукциона
            cmd.CommandText = "UPDATE Users SET Balance = Balance - @bidAmount WHERE Username = @username";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@bidAmount", bidAmount);
            cmd.Parameters.AddWithValue("@username", username);
            cmd.ExecuteNonQuery();

            return "SUCCESS|Баланс успешно списан";
        }



        if (command == "GET_OWN_AUCTIONS" && parts.Length == 2)
        {
            if (!int.TryParse(parts[1], out int ownerId))
            {
                return "ERROR|Некорректный ID владельца";
            }

            using var conn2 = new SQLiteConnection($"Data Source={DbPath};Version=3;");
            conn2.Open();
            using var cmd2 = new SQLiteCommand(conn2);

            cmd2.CommandText = @"SELECT Name, Description, StartPrice, Category, EndTime, Status, ImageUrl
                     FROM Auctions 
                     WHERE OwnerId = @ownerId";
            cmd2.Parameters.AddWithValue("@ownerId", ownerId);

            using var reader2 = cmd2.ExecuteReader();
            List<string> ownAuctions = new();

            while (reader2.Read())
            {
                string name = reader2.GetString(0);
                string description = reader2.IsDBNull(1) ? "" : reader2.GetString(1);
                double startPrice = reader2.GetDouble(2);
                string category = reader2.GetString(3);
                string endTime = reader2.GetString(4);
                string status = reader2.GetString(5);
                string imageUrl = reader2.IsDBNull(6) ? "" : reader2.GetString(6);

                ownAuctions.Add($"{name},{description},{startPrice},{category},{endTime},{status},{imageUrl}");
            }

            return ownAuctions.Count > 0 ? $"OWN_AUCTIONS|{string.Join(";", ownAuctions)}" : "OWN_AUCTIONS|EMPTY";

        }



        if (command == "ADD_AUCTION" && parts.Length == 8) // Теперь ожидаем 8 параметров
        {
            if (!int.TryParse(parts[1], out int ownerId))
            {
                return "ERROR|Некорректный ID владельца";
            }

            string name = parts[2];
            string description = parts[3];
            if (!double.TryParse(parts[4], out double startPrice))
            {
                return "ERROR|Некорректная цена";
            }

            string category = parts[5];
            string endTime = parts[6];
            string imageUrl = parts[7]; // Новый параметр

            cmd.CommandText = "INSERT INTO Auctions (OwnerId, Name, Description, StartPrice, Category, EndTime, ImageUrl) " +
                              "VALUES (@ownerId, @name, @description, @startPrice, @category, @endTime, @imageUrl)";
            cmd.Parameters.AddWithValue("@ownerId", ownerId);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@description", description);
            cmd.Parameters.AddWithValue("@startPrice", startPrice);
            cmd.Parameters.AddWithValue("@category", category);
            cmd.Parameters.AddWithValue("@endTime", endTime);
            cmd.Parameters.AddWithValue("@imageUrl", imageUrl);

            try
            {
                cmd.ExecuteNonQuery();
                return $"SUCCESS|Аукцион {name} добавлен";
            }
            catch (Exception ex)
            {
                return $"ERROR|Ошибка БД: {ex.Message}";
            }
        }


        // Обновление статуса аукциона
        if (command == "UPDATE_AUCTION_STATUS" && parts.Length == 3)
        {
            string auctionName = parts[1];
            string newStatus = parts[2];

            cmd.CommandText = "UPDATE Auctions SET Status = @status WHERE Name = @name";
            cmd.Parameters.AddWithValue("@status", newStatus);
            cmd.Parameters.AddWithValue("@name", auctionName);

            int rowsUpdated = cmd.ExecuteNonQuery();
            if (rowsUpdated > 0)
            {
                //string statusUpdateMessage = $"AUCTION_STATUS_UPDATED|{auctionName}|{newStatus}";
                //BroadcastMessage(statusUpdateMessage); // Оповещаем клиентов о смене статуса
                return $"SUCCESS|Статус аукциона {auctionName} обновлен на {newStatus}";
            }
            else
            {
                return "ERROR|Аукцион не найден";
            }
        }




        // Обработка сообщений в чате (с префиксом владельца, если это владелец)
        if (command == "CHAT" && parts.Length == 4)
        {
            string username = parts[1];
            bool isOwner = parts[2] == "OWNER"; // Проверка, является ли отправитель владельцем
            string message = parts[3];

            // Корректная передача данных с префиксом "Владелец" для владельца
            string chatMessage = isOwner ? $"CHAT|{username} (Владелец): {message}" : $"CHAT|{username}: {message}";

            // Рассылка сообщения всем клиентам
            //BroadcastMessage(chatMessage);
            return chatMessage;
        }
        if (command == "GET_OWNER_NAME" && parts.Length == 2)
        {
            if (int.TryParse(parts[1], out int ownerId))
            {
                cmd.CommandText = @"SELECT Username 
                            FROM Owners 
                            WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", ownerId);

                object ownerNameObj = cmd.ExecuteScalar();
                if (ownerNameObj != null)
                {
                    string ownerName = ownerNameObj.ToString();
                    return $"OWNER_NAME|{ownerName}";
                }
                else
                {
                    return "ERROR|Владелец не найден";
                }
            }
            else
            {
                return "ERROR|Некорректный ID";
            }
        }

        if (command == "DELETE_AUCTION" && parts.Length == 2)
        {
            string auctionName = parts[1];

            cmd.CommandText = "DELETE FROM Auctions WHERE Name = @name";
            cmd.Parameters.AddWithValue("@name", auctionName);

            int rowsDeleted = cmd.ExecuteNonQuery();
            if (rowsDeleted > 0)
            {
                return $"SUCCESS|Аукцион {auctionName} удален";
            }
            else
            {
                return "ERROR|Аукцион не найден";
            }
        }



        if (command == "FILTER_BY_CATEGORY" && parts.Length == 2)
        {
            string category = parts[1];

            using var conn2 = new SQLiteConnection($"Data Source={DbPath};Version=3;");
            conn2.Open();
            using var cmd2 = new SQLiteCommand(conn2);

            cmd2.CommandText = @"SELECT A.Name, O.Username, A.StartPrice, A.Category, A.EndTime, A.ImageUrl 
                     FROM Auctions A 
                     JOIN Owners O ON A.OwnerId = O.Id 
                     WHERE A.Status = 'Pending' AND A.Category = @category";
            cmd2.Parameters.AddWithValue("@category", category);

            using var reader2 = cmd2.ExecuteReader();
            List<string> filteredAuctions = new();

            while (reader2.Read())
            {
                string name = reader2.GetString(0);
                string owner = reader2.GetString(1);
                double startPrice = reader2.GetDouble(2);
                string categoryName = reader2.GetString(3);
                string endTime = reader2.GetString(4);
                string imageUrl = reader2.IsDBNull(5) ? "" : reader2.GetString(5);

                filteredAuctions.Add($"{name},{owner},{startPrice},{categoryName},{endTime},{imageUrl}");
            }

            return filteredAuctions.Count > 0 ? $"AUCTIONS|{string.Join(";", filteredAuctions)}" : "AUCTIONS|EMPTY";

        }

        // Закрытие аукциона
        if (command == "CLOSE_AUCTION" && parts.Length == 2)
        {
            string auctionName = parts[1];

            cmd.CommandText = "UPDATE Auctions SET Status = 'Closed' WHERE Name = @name";
            cmd.Parameters.AddWithValue("@name", auctionName);
            int rowsAffected = cmd.ExecuteNonQuery();

            if (rowsAffected > 0)
            {
                return $"SUCCESS|Аукцион {auctionName} закрыт";
            }
            else
            {
                return "ERROR|Аукцион не найден";
            }
        }

        // Получение данных пользователя
        if (command == "USER_DETAILS" && parts.Length == 2)
        {
            string username = parts[1];

            cmd.CommandText = @"SELECT Username, Password, Email, Address, CardNumber, ProfileImage, Balance 
                        FROM Users WHERE Username = @username";
            cmd.Parameters.AddWithValue("@username", username);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                string user = reader.GetString(0);
                string password = reader.GetString(1);  // Желательно хранить хэш
                string email = reader.GetString(2);
                string address = reader.IsDBNull(3) ? "" : reader.GetString(3);
                string cardNumber = reader.IsDBNull(4) ? "" : reader.GetString(4);
                string profileImage = reader.IsDBNull(5) ? "" : reader.GetString(5);
                double balance = reader.IsDBNull(6) ? 0.0 : reader.GetDouble(6);

                return $"USER_DETAILS|{user}|{password}|{email}|{address}|{cardNumber}|{profileImage}|{balance}";
            }

            return "ERROR|Пользователь не найден";
        }




        return "ERROR|Неизвестная команда";
    }


    private static async void BroadcastMessage(string message, TcpClient excludeClient = null)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        List<TcpClient> disconnectedClients = new();

        foreach (var client in clients)
        {
            if (client == excludeClient) continue;

            try
            {
                await client.GetStream().WriteAsync(data, 0, data.Length);
            }
            catch
            {
                disconnectedClients.Add(client);
            }
        }

        foreach (var client in disconnectedClients)
        {
            clients.Remove(client);
        }
    }


}