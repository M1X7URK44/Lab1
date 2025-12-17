using Lab1.Models;

namespace Lab1.Controllers;

public static class HomeController
{
    public static HttpResponse Index(HttpRequest request, ProjectState state)
    {
        var currentUser = GetCurrentUser(request, state);
        var isAdmin = currentUser?.IsAdmin ?? false;
        var authButton = currentUser == null 
            ? "<a href=\"/login\" style=\"float: right;\">Войти</a>"
            : $@"<form method=""post"" action=""/logout"" style=""display: inline; float: right;"">
                <button type=""submit"" style=""padding: 5px 10px; background-color: #555; color: white; border: none; border-radius: 3px; cursor: pointer;"">Выйти ({currentUser.Login})</button>
               </form>";

        var html = $@"
<!DOCTYPE html>
<html lang=""ru"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Интернет-магазин</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            max-width: 1200px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f5f5f5;
        }}
        .header {{
            background-color: #333;
            color: white;
            padding: 20px;
            border-radius: 5px;
            margin-bottom: 20px;
            position: relative;
        }}
        .nav {{
            margin-top: 10px;
        }}
        .nav a {{
            color: white;
            text-decoration: none;
            margin-right: 20px;
            padding: 5px 10px;
            background-color: #555;
            border-radius: 3px;
        }}
        .nav a:hover {{
            background-color: #777;
        }}
        .products {{
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
            gap: 20px;
            margin-top: 20px;
        }}
        .product-card {{
            background: white;
            padding: 20px;
            border-radius: 5px;
            box-shadow: 0 2px 5px rgba(0,0,0,0.1);
        }}
        .product-card h3 {{
            margin-top: 0;
            color: #333;
        }}
        .product-card .price {{
            font-size: 1.3em;
            font-weight: bold;
            color: #2c3e50;
            margin: 15px 0;
        }}
        .product-card .stock {{
            color: #27ae60;
            font-size: 0.9em;
            margin-bottom: 15px;
        }}
        .buy-form {{
            margin-top: 15px;
        }}
        .buy-form input {{
            width: 60px;
            padding: 5px;
            margin-right: 10px;
        }}
        .buy-form button {{
            padding: 8px 15px;
            background-color: #27ae60;
            color: white;
            border: none;
            border-radius: 3px;
            cursor: pointer;
        }}
        .buy-form button:hover {{
            background-color: #229954;
        }}
        .admin-form {{
            margin-top: 20px;
            padding: 20px;
            background: #f9f9f9;
            border-radius: 5px;
        }}
        .admin-form input, .admin-form textarea {{
            width: 100%;
            padding: 8px;
            margin-bottom: 10px;
            border: 1px solid #ddd;
            border-radius: 3px;
            box-sizing: border-box;
        }}
        .admin-form button {{
            padding: 10px 20px;
            background-color: #333;
            color: white;
            border: none;
            border-radius: 3px;
            cursor: pointer;
        }}
    </style>
</head>
<body>
    <div class=""header"">
        <h1>Добро пожаловать в интернет-магазин!</h1>
        <div class=""nav"">
            <a href=""/"">Главная</a>
            <a href=""/status"">Статус</a>
            {authButton}
        </div>
    </div>
";

        if (isAdmin)
        {
            html += @"
    <div class=""admin-form"">
        <h2>Добавить новый товар (Админ)</h2>
        <form method=""post"" action=""/action"">
            <input type=""hidden"" name=""action"" value=""add_product"">
            <input type=""text"" name=""name"" placeholder=""Название товара"" required>
            <textarea name=""description"" placeholder=""Описание"" rows=""3""></textarea>
            <input type=""number"" name=""price"" placeholder=""Цена"" step=""0.01"" required>
            <input type=""number"" name=""stock"" placeholder=""Количество на складе"" required>
            <button type=""submit"">Добавить товар</button>
        </form>
    </div>
";
        }

        html += @"
    <div>
        <h2>Каталог товаров</h2>
        <div class=""products"">
";

        foreach (var product in state.Products)
        {
            html += $@"
        <div class=""product-card"">
            <h3>{product.Name}</h3>
            <p>{product.Description}</p>
            <div class=""price"">{product.Price:C}</div>
            <div class=""stock"">В наличии: {product.Stock} шт.</div>
";

            if (currentUser != null && !currentUser.IsAdmin)
            {
                html += $@"
            <form method=""post"" action=""/action"" class=""buy-form"">
                <input type=""hidden"" name=""action"" value=""buy"">
                <input type=""hidden"" name=""product_id"" value=""{product.Id}"">
                <input type=""number"" name=""quantity"" value=""1"" min=""1"" max=""{product.Stock}"" required>
                <button type=""submit"">Купить</button>
            </form>
";
            }

            if (isAdmin)
            {
                html += $@"
            <form method=""post"" action=""/action"" class=""buy-form"">
                <input type=""hidden"" name=""action"" value=""update_product"">
                <input type=""hidden"" name=""product_id"" value=""{product.Id}"">
                <input type=""text"" name=""name"" placeholder=""Новое название"" value=""{product.Name}"">
                <textarea name=""description"" placeholder=""Новое описание"" rows=""2"">{product.Description}</textarea>
                <input type=""number"" name=""price"" placeholder=""Новая цена"" step=""0.01"" value=""{product.Price}"">
                <input type=""number"" name=""stock"" placeholder=""Новое количество"" value=""{product.Stock}"">
                <button type=""submit"" style=""background-color: #e67e22;"">Обновить</button>
            </form>
";
            }

            html += @"
        </div>
";
        }

        html += @"
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

    private static User? GetCurrentUser(HttpRequest request, ProjectState state)
    {
        if (request.Headers.TryGetValue("Cookie", out var cookieHeader))
        {
            var sessionId = ExtractSessionId(cookieHeader);
            if (!string.IsNullOrEmpty(sessionId) && state.Sessions.TryGetValue(sessionId, out var login))
            {
                return state.Users.FirstOrDefault(u => u.Login == login);
            }
        }
        return null;
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

