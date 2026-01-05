using System.Collections.Concurrent;
using System.Drawing;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using mirabox;

namespace MiraboxBridge;

public class MiraboxBridgeService
{
    private readonly ConcurrentBag<WebSocket> _connectedClients = new();
    private readonly ConcurrentDictionary<int, ActionMapping> _buttonMappings = new();
    
    private MiraboxLibUsbTransfer? _miraboxTransfer;
    private MiraboxHidTransfer? _hidTransfer;
    private IMiraboxReader? _buttonReader;
    private MiraboxButtonProgrammer? _programmer;
    private CancellationTokenSource? _buttonListenerCts;
    private bool _isConnected = false;
    
    public class ActionMapping
    {
        public string ActionId { get; set; } = "";
        public string ViewId { get; set; } = "";
        public string Caption { get; set; } = "";
    }
    
    public class SetActionsMessage
    {
        public string Type { get; set; } = "";
        public string ViewId { get; set; } = "";
        public string ViewType { get; set; } = "";
        public List<ActionInfo> Actions { get; set; } = new();
    }
    
    public class ActionInfo
    {
        public string Id { get; set; } = "";
        public string Caption { get; set; } = "";
        public string Icon { get; set; } = "";
        public int? ButtonNumber { get; set; }
    }
    
    /// <summary>
    /// Łączy się z urządzeniem MiraBox
    /// </summary>
    public async Task<bool> ConnectToMiraBox()
    {
        try
        {
            Console.WriteLine("\n=== PRÓBA POŁĄCZENIA Z MIRABOX ===");
            
            // Najpierw spróbuj LibUSB
            _miraboxTransfer = new MiraboxLibUsbTransfer();
            if (_miraboxTransfer.Connect(0x5548, 0x6670))
            {
                Console.WriteLine("✓ Połączono przez LibUSB");
                _buttonReader = new MiraboxLibUsbButtonReader(_miraboxTransfer);
                _programmer = new MiraboxButtonProgrammer(_buttonReader);
                
                // Wyślij inicjalizację
                var initCommand = new byte[512];
                initCommand[0] = 0x43; initCommand[1] = 0x52; initCommand[2] = 0x54;
                initCommand[5] = 0x44; initCommand[6] = 0x49; initCommand[7] = 0x53;
                _buttonReader.WriteData(initCommand, false);
                await Task.Delay(100);
                
                _isConnected = true;
                StartButtonListener();
                return true;
            }
            
            // Fallback do HID
            Console.WriteLine("LibUSB nie zadziałał, próba HID...");
            _hidTransfer = new MiraboxHidTransfer();
            if (_hidTransfer.Connect(0x5548, 0x6670))
            {
                Console.WriteLine("✓ Połączono przez HID");
                _buttonReader = new MiraboxHidButtonReader(_hidTransfer);
                _programmer = new MiraboxButtonProgrammer(_buttonReader);
                
                _isConnected = true;
                StartButtonListener();
                return true;
            }
            
            Console.WriteLine("✗ Nie można połączyć się z urządzeniem MiraBox");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Błąd połączenia: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Ustawia akcje na przyciskach MiraBox
    /// </summary>
    public async Task SetActions(string viewId, List<ActionInfo> actions)
    {
        if (!_isConnected || _programmer == null)
        {
            Console.WriteLine("⚠ Nie połączono z MiraBox - nie można ustawić akcji");
            await BroadcastError("DEVICE_NOT_CONNECTED", "Urządzenie MiraBox nie jest połączone");
            return;
        }
        
        Console.WriteLine($"\n=== USTAWIANIE AKCJI DLA WIDOKU: {viewId} ===");
        Console.WriteLine($"Liczba akcji: {actions.Count}");
        
        _buttonMappings.Clear();
        
        for (int i = 0; i < Math.Min(actions.Count, 15); i++)
        {
            var action = actions[i];
            var buttonNumber = action.ButtonNumber ?? (i + 1);
            
            if (buttonNumber < 1 || buttonNumber > 15)
            {
                Console.WriteLine($"⚠ Nieprawidłowy numer przycisku: {buttonNumber}, pomijam");
                continue;
            }
            
            try
            {
                // Załaduj ikonę
                var iconPath = Path.Combine("Images", action.Icon);
                byte[] iconData;
                
                if (!File.Exists(iconPath))
                {
                    Console.WriteLine($"⚠ Ikona nie znaleziona: {iconPath}, używam domyślnej");
                    // Użyj domyślnej ikony (proste kółko)
                    iconData = MiraboxImageGenerator.GenerateSimpleShape(
                        shapeType: 1, // Kółko
                        backgroundColor: Color.FromArgb(40, 40, 40),
                        shapeColor: Color.White
                    );
                }
                else if (iconPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                {
                    // SVG
                    iconData = MiraboxImageGenerator.LoadSvgIcon(
                        iconPath,
                        Color.FromArgb(40, 40, 40)
                    );
                }
                else
                {
                    // JPG/PNG
                    iconData = MiraboxImageGenerator.LoadImageIcon(
                        iconPath,
                        Color.FromArgb(40, 40, 40),
                        width: 100,
                        height: 100,
                        cropToCircle: false
                    );
                }
                
                // Zaprogramuj przycisk
                Console.WriteLine($"  Przycisk {buttonNumber}: {action.Caption} ({action.Icon})");
                if (_programmer.ProgramButton(buttonNumber, iconData, 512))
                {
                    // Zapisz mapowanie
                    _buttonMappings[buttonNumber] = new ActionMapping
                    {
                        ActionId = action.Id,
                        ViewId = viewId,
                        Caption = action.Caption
                    };
                    
                    await Task.Delay(50); // Przerwa między przyciskami
                }
                else
                {
                    Console.WriteLine($"  ✗ Błąd programowania przycisku {buttonNumber}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Błąd dla przycisku {buttonNumber}: {ex.Message}");
            }
        }
        
        Console.WriteLine($"✓ Ustawiono {_buttonMappings.Count} przycisków");
        
        // Wyślij potwierdzenie
        await BroadcastToClients(new
        {
            type = "actionsSet",
            viewId = viewId,
            status = "success",
            message = $"{_buttonMappings.Count} przycisków zaprogramowanych"
        });
    }
    
    /// <summary>
    /// Nasłuchuje naciśnięć przycisków
    /// </summary>
    private void StartButtonListener()
    {
        if (_buttonReader == null) return;
        
        _buttonListenerCts = new CancellationTokenSource();
        Task.Run(async () =>
        {
            Console.WriteLine("\n=== NASŁUCHIWANIE NACIŚNIĘĆ PRZYCISKÓW ===");
            
            while (!_buttonListenerCts.Token.IsCancellationRequested)
            {
                try
                {
                    ButtonPress? buttonPress = null;
                    
                    if (_buttonReader is MiraboxLibUsbButtonReader libUsbReader)
                    {
                        buttonPress = libUsbReader.ReadButtonPress();
                    }
                    else if (_buttonReader is MiraboxHidButtonReader hidReader)
                    {
                        buttonPress = hidReader.ReadButtonPress();
                    }
                    
                    if (buttonPress != null && buttonPress.State == "pressed")
                    {
                        if (_buttonMappings.TryGetValue(buttonPress.ButtonNumber, out var mapping))
                        {
                            Console.WriteLine($"\n🎯 PRZYCISK {buttonPress.ButtonNumber} NACIŚNIĘTY - akcja: {mapping.ActionId}");
                            
                            // Wyślij do wszystkich połączonych klientów
                            await BroadcastToClients(new
                            {
                                type = "buttonPress",
                                buttonNumber = buttonPress.ButtonNumber,
                                state = buttonPress.State,
                                actionId = mapping.ActionId,
                                viewId = mapping.ViewId
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Błąd odczytu przycisku: {ex.Message}");
                }
                
                await Task.Delay(10, _buttonListenerCts.Token);
            }
        }, _buttonListenerCts.Token);
    }
    
    /// <summary>
    /// Obsługuje połączenie WebSocket
    /// </summary>
    public async Task HandleWebSocketConnection(WebSocket webSocket)
    {
        _connectedClients.Add(webSocket);
        Console.WriteLine($"\n✓ Nowy klient WebSocket połączony (łącznie: {_connectedClients.Count})");
        
        try
        {
            var buffer = new byte[1024 * 4];
            
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None
                );
                
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await ProcessMessage(message);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Zamknięto połączenie",
                        CancellationToken.None
                    );
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Błąd WebSocket: {ex.Message}");
        }
        finally
        {
            _connectedClients.TryTake(out _);
            Console.WriteLine($"✗ Klient WebSocket rozłączony (pozostało: {_connectedClients.Count})");
        }
    }
    
    /// <summary>
    /// Przetwarza wiadomość JSON od klienta
    /// </summary>
    private async Task ProcessMessage(string jsonMessage)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonMessage);
            var root = doc.RootElement;
            
            if (!root.TryGetProperty("type", out var typeProperty))
            {
                Console.WriteLine("⚠ Wiadomość bez pola 'type'");
                return;
            }
            
            var messageType = typeProperty.GetString();
            Console.WriteLine($"\n📨 Otrzymano wiadomość: {messageType}");
            
            switch (messageType)
            {
                case "setActions":
                    var setActionsMsg = JsonSerializer.Deserialize<SetActionsMessage>(jsonMessage);
                    if (setActionsMsg != null)
                    {
                        await SetActions(setActionsMsg.ViewId, setActionsMsg.Actions);
                    }
                    break;
                    
                case "viewChanged":
                    // Można dodać obsługę zmiany widoku
                    Console.WriteLine($"  Zmiana widoku: {root.GetProperty("viewId").GetString()}");
                    break;
                    
                default:
                    Console.WriteLine($"  ⚠ Nieznany typ wiadomości: {messageType}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Błąd przetwarzania wiadomości: {ex.Message}");
            await BroadcastError("MESSAGE_PARSE_ERROR", ex.Message);
        }
    }
    
    /// <summary>
    /// Wysyła wiadomość do wszystkich połączonych klientów
    /// </summary>
    private async Task BroadcastToClients(object message)
    {
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        
        var clientsToRemove = new List<WebSocket>();
        
        foreach (var client in _connectedClients)
        {
            if (client.State == WebSocketState.Open)
            {
                try
                {
                    await client.SendAsync(
                        new ArraySegment<byte>(bytes),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    );
                }
                catch
                {
                    clientsToRemove.Add(client);
                }
            }
            else
            {
                clientsToRemove.Add(client);
            }
        }
        
        // Usuń nieaktywne połączenia
        foreach (var client in clientsToRemove)
        {
            _connectedClients.TryTake(out _);
        }
    }
    
    /// <summary>
    /// Wysyła błąd do klientów
    /// </summary>
    private async Task BroadcastError(string code, string message)
    {
        await BroadcastToClients(new
        {
            type = "error",
            code = code,
            message = message
        });
    }
    
    /// <summary>
    /// Zatrzymuje nasłuchiwanie i zamyka połączenia
    /// </summary>
    public void Dispose()
    {
        _buttonListenerCts?.Cancel();
        _miraboxTransfer?.Dispose();
        _hidTransfer?.Dispose();
        
        foreach (var client in _connectedClients)
        {
            if (client.State == WebSocketState.Open)
            {
                client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Serwer zamyka połączenie", CancellationToken.None).Wait();
            }
        }
        
        _connectedClients.Clear();
    }
}

