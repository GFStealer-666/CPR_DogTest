using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using TMPro;              // Remove if you don't use TextMeshPro
using UnityEngine.UI;
using UnityEditor.Overlays;    // For normal UI Text (optional)

public class UdpForceReceiver : MonoBehaviour
{
    [Header("Network Settings")]
    [Tooltip("UDP port to listen on. Must match ESP32 UDP_PORT.")]
    public int listenPort = 4210;

    [Header("Formatting")]
    [Tooltip("Label to show before the number.")]
    public string label = "Force: ";
    [Tooltip("Number of decimals to show.")]
    public int decimals = 2;
    [Tooltip("Unit string to append after the value.")]
    public string unit = " kg";

    private Thread _listenThread;
    private UdpClient _udpClient;
    private bool _running = false;

    private readonly object _lock = new object();
    private float _latestForceKg = 0f;
    private bool _hasValue = false;
    private string _lastRawMessage = "";

    void Start()
    {
        StartUdpListener();
    }

    void Update()
    {
        // Copy latest value under lock and update UI on main thread
        float displayValue;
        string lastMsg;
        bool hasValue;

        lock (_lock)
        {
            displayValue = _latestForceKg;
            hasValue = _hasValue;
            lastMsg = _lastRawMessage;
        }

    }

    private void StartUdpListener()
    {
        try
        {
            _udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, listenPort));

            _udpClient.Client.ReceiveTimeout = 0; // blocking

            _running = true;
            _listenThread = new Thread(ListenLoop);
            _listenThread.IsBackground = true;
            _listenThread.Start();

            Debug.Log($"[UdpForceReceiver] Listening on UDP port {listenPort}");
        }
        catch (Exception e)
        {
            Debug.LogError("[UdpForceReceiver] Failed to start UDP listener: " + e.Message);
        }
    }

    private void ListenLoop()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, listenPort);

        while (_running)
        {
            try
            {
                byte[] data = _udpClient.Receive(ref remoteEndPoint);
                string message = Encoding.UTF8.GetString(data).Trim();
                Debug.Log($"Force : {message} kg");
                float parsedForce;
                if (TryParseForce(message, out parsedForce))
                {
                    lock (_lock)
                    {
                        _latestForceKg = parsedForce;
                        _hasValue = true;
                        _lastRawMessage = message;
                        
                        ForceDataBuffer.Instance?.AddForceBuffer(_latestForceKg);
                    }
                }
                else
                {
                    lock (_lock)
                    {
                        _lastRawMessage = message;
                    }
                    Debug.LogWarning("[UdpForceReceiver] Could not parse message: " + message);
                }
            }
            catch (SocketException se)
            {
                if (_running) // ignore if we're shutting down
                    Debug.LogWarning("[UdpForceReceiver] SocketException: " + se.Message);
            }
            catch (Exception e)
            {
                if (_running)
                    Debug.LogError("[UdpForceReceiver] Exception in listen loop: " + e.Message);
            }
        }
    }

    // Summary : Parsing from string to numeric for storing
    private bool TryParseForce(string message, out float forceKg)
    {
        forceKg = 0f;

        if (string.IsNullOrWhiteSpace(message))
            return false;

        message = message.Trim();

        // If message has '=', assume "key=value"
        int eqIndex = message.IndexOf('=');
        if (eqIndex >= 0 && eqIndex < message.Length - 1)
        {
            string valuePart = message.Substring(eqIndex + 1).Trim();
            return float.TryParse(valuePart, System.Globalization.NumberStyles.Float,
                                  System.Globalization.CultureInfo.InvariantCulture,
                                  out forceKg);
        }

        // Otherwise assume it's just a number
        return float.TryParse(message, System.Globalization.NumberStyles.Float,
                              System.Globalization.CultureInfo.InvariantCulture,
                              out forceKg);
    }

    private void OnApplicationQuit()
    {
        StopUdpListener();
    }

    private void OnDestroy()
    {
        StopUdpListener();
    }

    private void StopUdpListener()
    {
        _running = false;

        try
        {
            _udpClient?.Close();
        }
        catch { }

        if (_listenThread != null && _listenThread.IsAlive)
        {
            try
            {
                _listenThread.Join(100);
            }
            catch { }
        }
    }
}
