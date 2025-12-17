using Lab1.Models;

namespace Lab1.Controllers;

public static class StatusController
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
    <title>Статус магазина</title>
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
        .section {{
            background: white;
            padding: 20px;
            margin-bottom: 20px;
            border-radius: 5px;
            box-shadow: 0 2px 5px rgba(0,0,0,0.1);
        }}
        table {{
            width: 100%;
            border-collapse: collapse;
            margin-top: 10px;
        }}
        th, td {{
            padding: 10px;
            text-align: left;
            border-bottom: 1px solid #ddd;
        }}
        th {{
            background-color: #333;
            color: white;
        }}
    </style>
</head>
<body>
    <div class=""header"">
        <h1>Статус магазина</h1>
        <div class=""nav"">
            <a href=""/"">Главная</a>
            <a href=""/status"">Статус</a>
            {authButton}
        </div>
    </div>
";

        if (currentUser != null)
        {
            html += $@"
    <div class=""section"">
        <h2>Текущий пользователь: {currentUser.Login}</h2>
        <p>Баланс: {currentUser.Balance:C}</p>
        <p>Роль: {(currentUser.IsAdmin ? "Администратор" : "Пользователь")}</p>
        <p>Заказов: {currentUser.Orders.Count}</p>
        {GetAdminBalanceForm(isAdmin, state)}
    </div>
";
        }

        html += @"
    <div class=""section"">
        <h2>Товары в магазине</h2>
        <table>
            <tr>
                <th>ID</th>
                <th>Название</th>
                <th>Описание</th>
                <th>Цена</th>
                <th>Остаток</th>
            </tr>
";

        foreach (var product in state.Products)
        {
            html += $@"
            <tr>
                <td>{product.Id}</td>
                <td>{product.Name}</td>
                <td>{product.Description}</td>
                <td>{product.Price:C}</td>
                <td>{product.Stock}</td>
            </tr>
";
        }

        html += @"
        </table>
    </div>
";

        if (isAdmin)
        {
            html += @"
    <div class=""section"">
        <h2>Все пользователи</h2>
        <table>
            <tr>
                <th>Логин</th>
                <th>Роль</th>
                <th>Баланс</th>
                <th>Заказов</th>
            </tr>
";

            foreach (var user in state.Users)
            {
                html += $@"
            <tr>
                <td>{user.Login}</td>
                <td>{(user.IsAdmin ? "Администратор" : "Пользователь")}</td>
                <td>{user.Balance:C}</td>
                <td>{user.Orders.Count}</td>
            </tr>
";
            }

            html += @"
        </table>
    </div>
";
        }

        html += @"
    <div class=""section"">
        <h2>Заказы</h2>
";

        if (isAdmin)
        {
            html += @"
        <table>
            <tr>
                <th>ID</th>
                <th>Пользователь</th>
                <th>Товаров</th>
                <th>Сумма</th>
                <th>Дата</th>
            </tr>
";

            foreach (var order in state.Orders)
            {
                html += $@"
            <tr>
                <td>{order.Id}</td>
                <td>{order.UserLogin}</td>
                <td>{order.Items.Sum(i => i.Quantity)}</td>
                <td>{order.TotalPrice:C}</td>
                <td>{order.CreatedAt:yyyy-MM-dd HH:mm}</td>
            </tr>
";
            }

            html += @"
        </table>
";
        }
        else if (currentUser != null)
        {
            var userOrders = state.Orders.Where(o => o.UserLogin == currentUser.Login).ToList();
            if (userOrders.Any())
            {
                html += @"
        <table>
            <tr>
                <th>ID</th>
                <th>Товаров</th>
                <th>Сумма</th>
                <th>Дата</th>
            </tr>
";

                foreach (var order in userOrders)
                {
                    html += $@"
            <tr>
                <td>{order.Id}</td>
                <td>{order.Items.Sum(i => i.Quantity)}</td>
                <td>{order.TotalPrice:C}</td>
                <td>{order.CreatedAt:yyyy-MM-dd HH:mm}</td>
            </tr>
";
                }

                html += @"
        </table>
";
            }
            else
            {
                html += "<p>У вас пока нет заказов.</p>";
            }
        }
        else
        {
            html += "<p>Войдите, чтобы увидеть свои заказы.</p>";
        }

        html += @"
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

    private static string GetUserOptions(ProjectState state)
    {
        return string.Join("\n", state.Users.Select(u => $"                    <option value=\"{u.Login}\">{u.Login}</option>"));
    }

    private static string GetAdminBalanceForm(bool isAdmin, ProjectState state)
    {
        if (!isAdmin)
            return "";

        var userOptions = GetUserOptions(state);
        return $@"
        <div style=""margin-top: 20px; padding: 15px; background-color: #f0f0f0; border-radius: 5px;"">
            <h3>Пополнить баланс пользователя</h3>
            <form method=""post"" action=""/action"">
                <input type=""hidden"" name=""action"" value=""add_balance"">
                <select name=""target_login"" required style=""padding: 8px; margin-right: 10px; width: 200px;"">
                    <option value="""" disabled selected>Выберите пользователя</option>
{userOptions}
                </select>
                <input type=""number"" name=""amount"" placeholder=""Сумма пополнения"" step=""0.01"" min=""0.01"" required style=""padding: 8px; margin-right: 10px; width: 200px;"">
                <button type=""submit"" style=""padding: 8px 15px; background-color: #27ae60; color: white; border: none; border-radius: 3px; cursor: pointer;"">Пополнить баланс</button>
            </form>
        </div>
";
    }
}

