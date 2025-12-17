using Lab1.Models;

namespace Lab1.Controllers;

public static class AuthController
{
    public static HttpResponse LoginPage(HttpRequest request, ProjectState state)
    {
        var html = @"
<!DOCTYPE html>
<html lang=""ru"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Вход</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            max-width: 500px;
            margin: 50px auto;
            padding: 20px;
            background-color: #f5f5f5;
        }
        .form-container {
            background: white;
            padding: 30px;
            border-radius: 5px;
            box-shadow: 0 2px 5px rgba(0,0,0,0.1);
        }
        h1 {
            text-align: center;
            color: #333;
        }
        form {
            margin-top: 20px;
        }
        label {
            display: block;
            margin-bottom: 5px;
            color: #333;
        }
        input {
            width: 100%;
            padding: 10px;
            margin-bottom: 15px;
            border: 1px solid #ddd;
            border-radius: 3px;
            box-sizing: border-box;
        }
        button {
            width: 100%;
            padding: 10px;
            background-color: #333;
            color: white;
            border: none;
            border-radius: 3px;
            cursor: pointer;
            font-size: 16px;
        }
        button:hover {
            background-color: #555;
        }
        .nav {
            text-align: center;
            margin-top: 20px;
        }
        .nav a {
            color: #333;
            text-decoration: none;
        }
    </style>
</head>
<body>
    <div class=""form-container"">
        <h1>Вход в систему</h1>
        <form method=""post"" action=""/login"">
            <label for=""login"">Логин:</label>
            <input type=""text"" id=""login"" name=""login"" required>
            
            <label for=""password"">Пароль:</label>
            <input type=""password"" id=""password"" name=""password"" required>
            
            <button type=""submit"">Войти</button>
        </form>
        <div class=""nav"">
            <a href=""/"">На главную</a>
        </div>
    </div>
</body>
</html>";

        return new HttpResponse
        {
            StatusCode = 200,
            StatusText = "OK",
            Body = html
        };
    }

    public static HttpResponse Login(HttpRequest request, ProjectState state)
    {
        var formData = ParseFormData(request.Body);
        var login = formData.GetValueOrDefault("login", "");
        var password = formData.GetValueOrDefault("password", "");

        var user = state.Users.FirstOrDefault(u => u.Login == login && u.Password == password);

        if (user != null)
        {
            var sessionId = Guid.NewGuid().ToString();
            state.Sessions[sessionId] = user.Login;

            var html = $@"
<!DOCTYPE html>
<html lang=""ru"">
<head>
    <meta charset=""UTF-8"">
    <meta http-equiv=""refresh"" content=""2;url=/"">
    <title>Успешный вход</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            text-align: center;
            padding: 50px;
            background-color: #f5f5f5;
        }}
        .success {{
            background: white;
            padding: 30px;
            border-radius: 5px;
            box-shadow: 0 2px 5px rgba(0,0,0,0.1);
            display: inline-block;
        }}
    </style>
</head>
<body>
    <div class=""success"">
        <h1>Успешный вход!</h1>
        <p>Добро пожаловать, {user.Login}!</p>
        <p>Перенаправление на главную страницу...</p>
    </div>
</body>
</html>";

            return new HttpResponse
            {
                StatusCode = 200,
                StatusText = "OK",
                Headers = new Dictionary<string, string>
                {
                    ["Set-Cookie"] = $"sessionId={sessionId}; Path=/",
                    ["Content-Type"] = "text/html; charset=utf-8"
                },
                Body = html
            };
        }
        else
        {
            var html = @"
<!DOCTYPE html>
<html lang=""ru"">
<head>
    <meta charset=""UTF-8"">
    <title>Ошибка входа</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            text-align: center;
            padding: 50px;
            background-color: #f5f5f5;
        }
        .error {
            background: white;
            padding: 30px;
            border-radius: 5px;
            box-shadow: 0 2px 5px rgba(0,0,0,0.1);
            display: inline-block;
        }
        a {
            color: #333;
            text-decoration: none;
            display: inline-block;
            margin-top: 20px;
            padding: 10px 20px;
            background-color: #333;
            color: white;
            border-radius: 3px;
        }
    </style>
</head>
<body>
    <div class=""error"">
        <h1>Ошибка входа</h1>
        <p>Неверный логин или пароль</p>
        <a href=""/login"">Попробовать снова</a>
    </div>
</body>
</html>";

            return new HttpResponse
            {
                StatusCode = 401,
                StatusText = "Unauthorized",
                Body = html
            };
        }
    }

    public static HttpResponse Logout(HttpRequest request, ProjectState state)
    {
        if (request.Headers.TryGetValue("Cookie", out var cookieHeader))
        {
            var sessionId = ExtractSessionId(cookieHeader);
            if (!string.IsNullOrEmpty(sessionId))
            {
                state.Sessions.Remove(sessionId);
            }
        }

        var html = @"
<!DOCTYPE html>
<html lang=""ru"">
<head>
    <meta charset=""UTF-8"">
    <meta http-equiv=""refresh"" content=""2;url=/"">
    <title>Выход</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            text-align: center;
            padding: 50px;
            background-color: #f5f5f5;
        }
        .message {
            background: white;
            padding: 30px;
            border-radius: 5px;
            box-shadow: 0 2px 5px rgba(0,0,0,0.1);
            display: inline-block;
        }
    </style>
</head>
<body>
    <div class=""message"">
        <h1>Вы вышли из системы</h1>
        <p>Перенаправление на главную страницу...</p>
    </div>
</body>
</html>";

        return new HttpResponse
        {
            StatusCode = 200,
            StatusText = "OK",
            Headers = new Dictionary<string, string>
            {
                ["Set-Cookie"] = "sessionId=; Path=/; Expires=Thu, 01 Jan 1970 00:00:00 GMT",
                ["Content-Type"] = "text/html; charset=utf-8"
            },
            Body = html
        };
    }

    private static Dictionary<string, string> ParseFormData(string body)
    {
        var result = new Dictionary<string, string>();
        var pairs = body.Split('&');
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=');
            if (parts.Length == 2)
            {
                // Заменяем + на пробелы перед декодированием (application/x-www-form-urlencoded формат)
                var key = parts[0].Replace('+', ' ');
                var value = parts[1].Replace('+', ' ');
                result[Uri.UnescapeDataString(key)] = Uri.UnescapeDataString(value);
            }
        }
        return result;
    }

    private static string? ExtractSessionId(string cookieHeader)
    {
        var cookies = cookieHeader.Split(';');
        foreach (var cookie in cookies)
        {
            var parts = cookie.Trim().Split('=');
            if (parts.Length == 2 && parts[0].Trim() == "sessionId")
            {
                return parts[1].Trim();
            }
        }
        return null;
    }
}

