using GameAndDot.Shared.Enums;
using GameAndDot.Shared.Models;
using System.Net.Sockets;
using System.Text.Json;

namespace GameAndDot.Maui;

public partial class GamePage : ContentPage
{
    private TcpClient? _client;
    private StreamWriter? _writer;
    private StreamReader? _reader;
    private string? _userName;
    private string _userColor = "#FF0000";
    private readonly Dictionary<string, string> _playerColors = [];
    private readonly List<(int X, int Y, string Color)> _points = [];

    const string Host = "127.0.0.1";
    const int Port = 8888;

    public GamePage()
    {
        InitializeComponent();


        var drawable = new GameDrawable(_points);
        DrawingView.Drawable = drawable;


        var tap = new TapGestureRecognizer();
        tap.Tapped += OnDrawingTapped;
        DrawingView.GestureRecognizers.Add(tap);
    }

    private async void OnEnterClicked(object sender, EventArgs e)
    {
        string? name = NameEntry.Text?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            await DisplayAlert("Ошибка", "Введите имя", "OK");
            return;
        }

        _userName = name;


        int seed = (_userName.GetHashCode()) + Environment.TickCount;
        Random rnd = new Random(seed);
        _userColor = $"#{rnd.Next(0x1000000):X6}";


        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(Host, Port);
            var stream = _client.GetStream();
            _reader = new StreamReader(stream);
            _writer = new StreamWriter(stream) { AutoFlush = true };
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка подключения", ex.Message, "OK");
            return;
        }


        PlayerNameLabel.Text = _userName;
        ColorLabel.Text = _userColor;
        try
        {
            ColorLabel.TextColor = Color.FromArgb(_userColor);
        }
        catch
        {
            ColorLabel.TextColor = Colors.Red;
        }


        LoginPanel.IsVisible = false;
        GamePanel.IsVisible = true;

        _ = Task.Run(ReceiveMessagesAsync);


        var msg = new EventMessege
        {
            Type = EventType.PlayerConnected,
            Username = _userName,
            Color = _userColor
        };
        string json = JsonSerializer.Serialize(msg);
        await SendMessageAsync(json);
    }

    private void OnDrawingTapped(object sender, TappedEventArgs e)
    {
        if (string.IsNullOrEmpty(_userName)) return;

        var pos = e.GetPosition(DrawingView);
        if (pos == null) return; 

        int x = (int)pos.Value.X;
        int y = (int)pos.Value.Y;


        _points.Add((x, y, _userColor));
        DrawingView.Invalidate(); 


        SendPointToServer(x, y);
    }

    private async Task SendMessageAsync(string message)
    {
        if (_writer != null)
        {
            await _writer.WriteLineAsync(message);
        }
    }

    private async Task ReceiveMessagesAsync()
    {
        while (_reader != null)
        {
            try
            {
                string? json = await _reader.ReadLineAsync();
                if (string.IsNullOrEmpty(json)) continue;

                var msg = JsonSerializer.Deserialize<EventMessege>(json);
                if (msg == null) continue;

                switch (msg.Type)
                {
                    case EventType.PlayerConnected:
                        if (msg.Players != null)
                        {
                            MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                PlayersList.ItemsSource = msg.Players;
                            });
                        }
                        break;

                    case EventType.PointPlaced:
                        if (!string.IsNullOrEmpty(msg.Username) && !string.IsNullOrEmpty(msg.Color))
                        {
                            _playerColors[msg.Username] = msg.Color;
                            // Добавляем точку другого игрока
                            _points.Add((msg.X, msg.Y, msg.Color));
                            MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                DrawingView.Invalidate();
                            });
                        }
                        break;

                    case EventType.PlayerDisconected:
                        MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            var list = new List<string>(PlayersList.ItemsSource?.Cast<string>() ?? []);
                            list.Remove(msg.Username);
                            PlayersList.ItemsSource = list;
                            _playerColors.Remove(msg.Username);
                        });
                        break;
                }
            }
            catch
            {
                break;
            }
        }
    }

    private async void SendPointToServer(int x, int y)
    {
        if (string.IsNullOrEmpty(_userName)) return;

        var msg = new EventMessege
        {
            Type = EventType.PointPlaced,
            Username = _userName,
            X = x,
            Y = y,
            Color = _userColor
        };
        string json = JsonSerializer.Serialize(msg);
        await SendMessageAsync(json);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _writer?.Dispose();
        _reader?.Dispose();
        _client?.Close();
    }
}