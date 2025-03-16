using System;
using System.Data.SQLite;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

class AuthServer
{
    private const int Port = 4000;
    private const string DbPath = "C:\\Users\\user\\Downloads\\Project TEST\\Project TEST\\AuctionServer\\AuctionDB.db";
    private const string AdminKeyFilePath = "C:\\Users\\user\\Downloads\\Project TEST\\Project TEST\\AuthServer\\pass.txt"; // Путь к файлу ключей
    private const string DefaultPassword = "admin123"; // Стандартный пароль

    public static async Task Main()
    {
        TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), Port);
        listener.Start();
        Console.WriteLine($"Auth Server запущен на порту {Port}");

        // Если файл ключей пуст, зашифровать и записать стандартный пароль
        if (new FileInfo(AdminKeyFilePath).Length == 0)
        {
            string encryptedPassword = EncryptPassword(DefaultPassword);
            File.WriteAllText(AdminKeyFilePath, encryptedPassword);
        }

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
            string password = parts[2]; // Пароль, который нужно зашифровать
            string email = parts[3];
            string address = parts[4];
            string cardNumber = parts[5];
            string profileImage = parts.Length > 6 ? parts[6] : "";

            // Шифруем пароль перед записью в базу данных
            string encryptedPassword = EncryptPassword(password);

            try
            {
                cmd.CommandText = @"INSERT INTO Users (Username, Password, Email, Address, CardNumber, ProfileImage) 
                            VALUES (@user, @pass, @mail, @addr, @card, @image)";
                cmd.Parameters.AddWithValue("@user", username);
                cmd.Parameters.AddWithValue("@pass", encryptedPassword); // Используем зашифрованный пароль
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
        else if (command == "LOGIN" && parts.Length == 3)
        {
            string password = parts[2];

            // Шифруем введенный пароль перед сравнением с зашифрованным паролем в базе данных
            string encryptedPassword = EncryptPassword(password);

            cmd.CommandText = "SELECT Id FROM Users WHERE Username = @user AND Password = @pass";
            cmd.Parameters.AddWithValue("@user", username);
            cmd.Parameters.AddWithValue("@pass", encryptedPassword); // Сравниваем зашифрованные пароли
            object result = cmd.ExecuteScalar();

            return result != null ? "SUCCESS|Вход успешен" : "ERROR|Неверный логин или пароль";
        }


        else if (command == "OWNER_LOGIN" && parts.Length == 4)
        {
            string password = parts[2];  // Пароль, который вводит пользователь
            string permissionKey = parts[3];  // Ключ разрешения

            // Шифруем введенный ключ разрешения
            string encryptedPermissionKey = EncryptPermissionKey(permissionKey);

            // Шифруем введенный пароль перед сравнением с зашифрованным паролем в базе данных
            string encryptedPassword = EncryptPassword(password);

            // Проверяем, существует ли запись в базе данных с зашифрованным ключом разрешения
            cmd.CommandText = "SELECT Id FROM Owners WHERE Username = @user AND Password = @pass AND PermissionKey = @permKey";
            cmd.Parameters.AddWithValue("@user", username);
            cmd.Parameters.AddWithValue("@pass", encryptedPassword);  // Используем пароль как есть
            cmd.Parameters.AddWithValue("@permKey", encryptedPermissionKey);  // Используем зашифрованный ключ разрешения

            object result = cmd.ExecuteScalar();

            return result != null ? $"SUCCESS|{result}" : "ERROR|Неверные данные владельца";
        }

        else if (command == "OWNER_REGISTER" && parts.Length == 4)
        {
            string password = parts[2]; // Введенный пользователем ключ разрешения
            string permissionKey = parts[3];        // Ключ разрешения владельца

            // Чтение зашифрованного ключа администратора из файла
            string encryptedAdminKey = File.ReadAllText(AdminKeyFilePath);

            string encryptedPassword = EncryptPassword(password);

            // Сравнение введенного ключа разрешения с зашифрованным ключом администратора
            if (EncryptPermissionKey(permissionKey) != encryptedAdminKey)
            {
                return "ERROR|Неверный ключ администратора";
            }

            // Шифруем ключ разрешения перед сохранением в БД
            string encryptedPermissionKey = EncryptPermissionKey(permissionKey);

            // Проверка на уникальность имени владельца
            cmd.CommandText = "SELECT COUNT(*) FROM Owners WHERE Username = @user";
            cmd.Parameters.AddWithValue("@user", username);
            int userCount = Convert.ToInt32(cmd.ExecuteScalar());

            if (userCount > 0)
            {
                return "ERROR|Пользователь с таким именем уже существует";
            }

            cmd.CommandText = "INSERT INTO Owners (Username, Password, PermissionKey) VALUES (@user, @pass, @permKey)";
            cmd.Parameters.AddWithValue("@user", username); // Имя владельца
            cmd.Parameters.AddWithValue("@pass", encryptedPassword); // Имя владельца
            cmd.Parameters.AddWithValue("@permKey", encryptedPermissionKey); // Записываем зашифрованный ключ разрешения

            try
            {
                cmd.ExecuteNonQuery();
                return "SUCCESS|Владелец зарегистрирован";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                return "ERROR|Ошибка при регистрации владельца";
            }
        }

        else if (command == "CHANGE_PASSWORD" && parts.Length == 3)
        {
            string username1 = parts[1];
            string newPassword = parts[2];

            // Шифруем новый пароль
            string encryptedNewPassword = EncryptPassword(newPassword);

            // Проверяем, существует ли пользователь с таким именем и паролем
            cmd.CommandText = "SELECT Password FROM Users WHERE Username = @user";
            cmd.Parameters.AddWithValue("@user", username);
            object result = cmd.ExecuteScalar();

            if (result != null)
            {
                string currentEncryptedPassword = result.ToString();

                // Проверка, что введенный пароль совпадает с зашифрованным паролем в базе данных
                if (currentEncryptedPassword == encryptedNewPassword)
                {
                    // Если пароли не совпадают, возвращаем ошибку
                    return "ERROR|Неверный текущий пароль";
                }

                // Если пароли совпадают, обновляем пароль в базе данных
                cmd.CommandText = "UPDATE Users SET Password = @pass WHERE Username = @user";
                cmd.Parameters.AddWithValue("@pass", encryptedNewPassword); // Обновляем зашифрованный пароль
                cmd.ExecuteNonQuery();

                return "SUCCESS|Пароль успешно изменен";
            }
            else
            {
                return "ERROR|Пользователь не найден";
            }
        }

        return "ERROR|Неизвестная команда";
    }

    private static string EncryptPassword(string password)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Encoding.UTF8.GetBytes("12345678123456781234567812345678"); // 32 байта для AES-256
            aesAlg.IV = Encoding.UTF8.GetBytes("1234567812345678"); // 16 байтов для IV
            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (StreamWriter sw = new StreamWriter(cs))
                {
                    sw.Write(password);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    private static string EncryptPermissionKey(string permissionKey)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Encoding.UTF8.GetBytes("12345678123456781234567812345678"); // 32 байта для AES-256
            aesAlg.IV = Encoding.UTF8.GetBytes("1234567812345678"); // 16 байтов для IV
            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (StreamWriter sw = new StreamWriter(cs))
                {
                    sw.Write(permissionKey);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }
}
