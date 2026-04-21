using UnityEngine;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks; // Añadido para poder usar Task
using System.Collections.Concurrent;

/// <summary>
/// Client WebSocket natiu compatible amb Unity Editor + Standalone.
/// Usa Thread dedicat (no async/await) per evitar problemes de plataforma.
/// Connecta a Node.js /gs i envia events de partida (playerDied, gameOver, gameStart).
/// </summary>
public class GameWebSocketClient : MonoBehaviour
{
    [Header("Servidor WebSocket")]
    [SerializeField] private string wsUrl = "ws://localhost:3000/gs";

    private ClientWebSocket    _ws;
    private Thread             _thread;
    private CancellationTokenSource _cts;
    private ConcurrentQueue<string> _sendQueue = new ConcurrentQueue<string>();
    private bool _connected = false;

    public static GameWebSocketClient Instance { get; private set; }

    // ── Cicle de vida Unity ───────────────────────────────────────────────
    
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _cts    = new CancellationTokenSource();
        _thread = new Thread(WorkerThread) { IsBackground = true, Name = "WS-GameClient" };
        _thread.Start();
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _connected = false;
        try { _ws?.Abort(); } catch { }
        _ws?.Dispose();
    }

    // ── Thread de xarxa (fora del main thread) ───────────────────────────

    private void WorkerThread()
    {
        try
        {
            _ws = new ClientWebSocket();
            _ws.ConnectAsync(new Uri(wsUrl), _cts.Token).GetAwaiter().GetResult();
            _connected = true;
            Debug.Log($"[WS] Connectat a {wsUrl}");

            // Enviar missatge de connexio
            string nom = PlayerPrefs.GetString("PlayerName", "Jugador");
            Envia($"{{\"type\":\"connected\",\"player\":\"{E(nom)}\"}}");

            // Bucle d'enviament i recepció
            var buffer = new byte[2048];
            Task<WebSocketReceiveResult> receiveTask = null; // Guardamos la tarea de recepción aquí

            while (!_cts.IsCancellationRequested && _ws.State == WebSocketState.Open)
            {
                // 1. Enviar missatges pendents
                while (_sendQueue.TryDequeue(out string msg))
                {
                    var bytes = Encoding.UTF8.GetBytes(msg);
                    _ws.SendAsync(new ArraySegment<byte>(bytes),
                                  WebSocketMessageType.Text, true, _cts.Token)
                       .GetAwaiter().GetResult();
                }

                // 2. Iniciar la recepció NOMÉS si no n'hi ha cap ja en curs
                if (receiveTask == null)
                {
                    receiveTask = _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                }

                // 3. Comprovar si la tasca ha acabat (sense bloquejar el thread ni forzar esperes)
                if (receiveTask.IsCompleted)
                {
                    // Si ha anat bé i és text, el processem
                    if (receiveTask.Status == TaskStatus.RanToCompletion && receiveTask.Result.MessageType == WebSocketMessageType.Text)
                    {
                        string msg = Encoding.UTF8.GetString(buffer, 0, receiveTask.Result.Count);
                        Debug.Log($"[WS] Rebut: {msg}");
                    }
                    
                    // Netejar la tasca per demanar-ne una de nova a la següent volta del bucle
                    receiveTask = null;
                }
                else
                {
                    // Descans minúscul per no fregir la CPU al 100%
                    Thread.Sleep(10);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            // Connexió fallida: silenciós en producció, avís en editor
            Debug.LogWarning($"[WS] No connectat o error de xarxa (continua sense WS): {ex.Message}");
        }
        finally
        {
            _connected = false;
        }
    }

    // ── API pública (crida des del main thread) ───────────────────────────

    private void Envia(string json)
    {
        if (_connected)
            _sendQueue.Enqueue(json);
    }

    public void NotificaGameStart(string roomName)
        => Envia("{\"type\":\"gameStart\",\"room\":\"" + E(roomName) + "\"}");

    public void NotificaJugadorMort(string nom, float temps)
        => Envia("{\"type\":\"playerDied\",\"player\":\"" + E(nom) + "\",\"time\":" + F(temps) + "}");

    public void NotificaGameOver(string guanyador, float temps)
        => Envia("{\"type\":\"gameOver\",\"winner\":\"" + E(guanyador) + "\",\"time\":" + F(temps) + "}");

    // Helpers
    private static string E(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
    private static string F(float v)  => v.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
}