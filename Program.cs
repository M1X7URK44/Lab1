using Lab1.Models;

namespace Lab1;

class Program
{
    static void Main(string[] args)
    {
        var state = new ProjectState();
        var router = new Router();
        var server = new HttpServer(8080, state, router);

        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            server.Stop();
            Environment.Exit(0);
        };

        server.Start();
    }
}

