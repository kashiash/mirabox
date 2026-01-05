using MiraboxBridge;

var builder = WebApplication.CreateBuilder(args);

// Dodaj CORS dla lokalnych połączeń
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost", "https://localhost", "http://localhost:5000", "https://localhost:5001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Włącz CORS
app.UseCors("AllowLocalhost");

// Włącz WebSockets
app.UseWebSockets();

// Utwórz singleton serwisu
var bridgeService = new MiraboxBridgeService();

// Połącz z MiraBox przy starcie
_ = Task.Run(async () =>
{
    await Task.Delay(1000); // Daj czas na uruchomienie aplikacji
    await bridgeService.ConnectToMiraBox();
});

// Endpoint WebSocket
app.Map("/mirabox", async (HttpContext context) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await bridgeService.HandleWebSocketConnection(webSocket);
    }
    else
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("Wymagane połączenie WebSocket");
    }
});

// Endpoint statusu
app.MapGet("/status", () =>
{
    return Results.Json(new
    {
        status = "running",
        timestamp = DateTime.UtcNow
    });
});

Console.WriteLine("=== MIRABOX BRIDGE SERVICE ===");
Console.WriteLine("WebSocket endpoint: ws://localhost:8081/mirabox");
Console.WriteLine("Status endpoint: http://localhost:8081/status");
Console.WriteLine("Naciśnij Ctrl+C aby zakończyć\n");

app.Run("http://localhost:8081");
