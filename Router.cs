using Lab1.Controllers;
using Lab1.Models;

namespace Lab1;

public class Router
{
    private readonly Dictionary<string, Func<HttpRequest, ProjectState, HttpResponse>> _routes;

    public Router()
    {
        _routes = new Dictionary<string, Func<HttpRequest, ProjectState, HttpResponse>>();
        RegisterRoutes();
    }

    private void RegisterRoutes()
    {
        _routes["GET /"] = HomeController.Index;
        _routes["GET /status"] = StatusController.Index;
        _routes["GET /login"] = AuthController.LoginPage;
        _routes["POST /login"] = AuthController.Login;
        _routes["POST /logout"] = AuthController.Logout;
        _routes["POST /action"] = ActionController.HandleAction;
    }

    public HttpResponse HandleRequest(HttpRequest request, ProjectState state)
    {
        var routeKey = $"{request.Method} {request.Path}";
        
        if (_routes.TryGetValue(routeKey, out var handler))
        {
            return handler(request, state);
        }

        // Проверка статических файлов (Views)
        if (request.Method == "GET" && request.Path.StartsWith("/"))
        {
            var viewPath = request.Path.TrimStart('/');
            if (string.IsNullOrEmpty(viewPath) || viewPath == "index.html")
            {
                return HomeController.Index(request, state);
            }
        }

        return new HttpResponse
        {
            StatusCode = 404,
            StatusText = "Not Found",
            Body = "<html><body><h1>404 Not Found</h1></body></html>"
        };
    }
}

