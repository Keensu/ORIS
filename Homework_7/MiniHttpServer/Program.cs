using MiniHttpServer.shared;
using System.Net;
using System.Text;
using System.Text.Json;

bool _keepRunning = true;


Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
{
    e.Cancel = true;
    _keepRunning = false;
};

var settingsJson = string.Empty;

try
{
    settingsJson = File.ReadAllText("settings.json");
}

catch (FileNotFoundException fileEx)
{
    Console.WriteLine("Файл settings.json не существует" + fileEx.Message);
    Environment.Exit(1);
}

SettingsModel? settings = null;

try
{
    settings = JsonSerializer.Deserialize<SettingsModel>(settingsJson);
}

catch (Exception ex)
{
    Console.WriteLine("Файл settings.json некорректен: " + ex.Message);
    Environment.Exit(1);
}


var consoleTask = Task.Run(() =>
{
    while (_keepRunning)
    {
        var input = Console.ReadLine();
        if (input?.Trim().ToLower() == "/stop")
        {
            Console.WriteLine("Остановка сервера...");
            _keepRunning = false;
            break;
        }
        else if (!string.IsNullOrEmpty(input))
        {
            Console.WriteLine("Неизвестная команда");
        }
    }
});


var httpServer = new HttpServer(settings!);

httpServer.Start();

while (_keepRunning) { }

httpServer.Stop();





