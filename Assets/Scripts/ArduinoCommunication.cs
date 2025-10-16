using System;
using System.Collections;
using System.IO.Ports;
using UnityEngine;
using System.Collections.Generic;

public class ArduinoCommunication : MonoBehaviour
{
    [Header("Serial Port Settings")]
    [SerializeField] private string portName = "COM3"; // Change this to your Arduino's port
    [SerializeField] private int baudRate = 9600;
    [SerializeField] private int dataBits = 8;
    [SerializeField] private Parity parity = Parity.None;
    [SerializeField] private StopBits stopBits = StopBits.One;
    
    [Header("Connection Settings")]
    [SerializeField] private float connectionTimeout = 5f;
    [SerializeField] private float dataTimeout = 1f;
    
    private SerialPort serialPort;
    private bool isConnected = false;
    private Coroutine connectionCoroutine;
    private Coroutine dataTimeoutCoroutine;
    
    // Events for sensor data
    public static event Action<ForceSensorData[]> OnSensorDataReceived;
    public static event Action<bool> OnConnectionStatusChanged;
    public static event Action<string> OnErrorOccurred;
    
    void Start()
    {
        ConnectToArduino();
    }
    
    void OnDestroy()
    {
        DisconnectFromArduino();
    }
    
    public void ConnectToArduino()
    {
        if (connectionCoroutine != null)
        {
            StopCoroutine(connectionCoroutine);
        }
        connectionCoroutine = StartCoroutine(ConnectCoroutine());
    }
    
    public void DisconnectFromArduino()
    {
        if (connectionCoroutine != null)
        {
            StopCoroutine(connectionCoroutine);
        }
        
        if (dataTimeoutCoroutine != null)
        {
            StopCoroutine(dataTimeoutCoroutine);
        }
        
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
            serialPort.Dispose();
        }
        
        isConnected = false;
        OnConnectionStatusChanged?.Invoke(false);
    }
    
    private IEnumerator ConnectCoroutine()
    {
        bool connectionSuccessful = false;
        string errorMessage = "";
        
        try
        {
            // Get available ports for debugging
            string[] availablePorts = SerialPort.GetPortNames();
            Debug.Log($"Available ports: {string.Join(", ", availablePorts)}");
            
            serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            serialPort.ReadTimeout = 1000;
            serialPort.WriteTimeout = 1000;
            
            serialPort.Open();
            connectionSuccessful = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to connect to Arduino: {e.Message}");
            errorMessage = e.Message;
            connectionSuccessful = false;
        }
        
        // Wait a moment for connection to stabilize
        yield return new WaitForSeconds(0.5f);
        
        if (connectionSuccessful && serialPort != null && serialPort.IsOpen)
        {
            isConnected = true;
            OnConnectionStatusChanged?.Invoke(true);
            Debug.Log($"Successfully connected to Arduino on {portName}");
            
            // Start reading data
            StartCoroutine(ReadDataCoroutine());
        }
        else
        {
            OnErrorOccurred?.Invoke($"Connection failed: {errorMessage}");
            isConnected = false;
            OnConnectionStatusChanged?.Invoke(false);
        }
    }
    
    private IEnumerator ReadDataCoroutine()
    {
        while (isConnected && serialPort != null && serialPort.IsOpen)
        {
            bool hasData = false;
            string data = "";
            bool hasError = false;
            string errorMessage = "";
            float waitTime = 0.01f; // Default wait time
            
            try
            {
                if (serialPort.BytesToRead > 0)
                {
                    data = serialPort.ReadLine();
                    hasData = true;
                }
            }
            catch (TimeoutException)
            {
                // This is normal when no data is available
                hasError = false;
                waitTime = 0.01f;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error reading from Arduino: {e.Message}");
                errorMessage = e.Message;
                hasError = true;
                waitTime = 0.1f;
            }
            
            // Process data if we have it
            if (hasData)
            {
                ProcessSensorData(data);
                
                // Reset data timeout
                if (dataTimeoutCoroutine != null)
                {
                    StopCoroutine(dataTimeoutCoroutine);
                }
                dataTimeoutCoroutine = StartCoroutine(DataTimeoutCoroutine());
            }
            
            // Handle errors
            if (hasError)
            {
                OnErrorOccurred?.Invoke($"Read error: {errorMessage}");
            }
            
            // Wait before next iteration
            if (hasData)
            {
                yield return null; // Continue immediately if we got data
            }
            else
            {
                yield return new WaitForSeconds(waitTime);
            }
        }
    }
    
    private IEnumerator DataTimeoutCoroutine()
    {
        yield return new WaitForSeconds(dataTimeout);
        Debug.LogWarning("No data received from Arduino within timeout period");
        OnErrorOccurred?.Invoke("Data timeout - check Arduino connection");
    }
    
    private void ProcessSensorData(string rawData)
    {
        try
        {
            // Check if this is sensor data (comma-separated numbers) or status/debug message
            if (rawData.Contains("Sensor") || rawData.Contains("Min:") || rawData.Contains("Max:"))
            {
                // This is a status/debug message, not sensor data
                return;
            }
            
            // Expected format: "sensor1,sensor2,sensor3,sensor4,sensor5,sensor6"
            // Example: "1023,512,0,256,768,1023"
            
            string[] values = rawData.Trim().Split(',');
            
            if (values.Length < 6)
            {
                Debug.LogWarning($"Insufficient sensor data received. Expected 6 values, got {values.Length}");
                return;
            }
            
            ForceSensorData[] sensorData = new ForceSensorData[6];
            
            for (int i = 0; i < 6; i++)
            {
                if (float.TryParse(values[i], out float value))
                {
                    sensorData[i] = new ForceSensorData
                    {
                        sensorId = i,
                        rawValue = value,
                        normalizedValue = Mathf.Clamp01(value / 1023f), // Assuming 10-bit ADC (0-1023)
                        timestamp = Time.time
                    };
                }
                else
                {
                    Debug.LogWarning($"Failed to parse sensor {i} value: {values[i]}");
                    return;
                }
            }
            
            // DIRECTLY update FSR components - bypass the event system
            FSR[] allFSRs = FindObjectsOfType<FSR>();
            
            for (int i = 0; i < sensorData.Length; i++)
            {
                // Find the FSR component with matching sensorId
                FSR targetFSR = null;
                foreach (FSR fsr in allFSRs)
                {
                    if (fsr.GetSensorId() == i)
                    {
                        targetFSR = fsr;
                        break;
                    }
                }
                
                if (targetFSR != null)
                {
                    // Only update if sensor has meaningful data (not floating pin values)
                    // Unconnected pins typically read very low values (< 10) or very high values (> 1000)
                    if (sensorData[i].rawValue > 10 && sensorData[i].rawValue < 1000)
                    {
                        targetFSR.UpdateSensorDataDirect(sensorData[i]);
                    }
                }
            }
            
            OnSensorDataReceived?.Invoke(sensorData);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing sensor data: {e.Message}");
            OnErrorOccurred?.Invoke($"Data processing error: {e.Message}");
        }
    }
    
    public bool IsConnected()
    {
        return isConnected && serialPort != null && serialPort.IsOpen;
    }
    
    public void SendCommand(string command)
    {
        if (IsConnected())
        {
            try
            {
                serialPort.WriteLine(command);
                Debug.Log($"Sent command to Arduino: {command}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to send command: {e.Message}");
                OnErrorOccurred?.Invoke($"Command send error: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("Cannot send command - Arduino not connected");
        }
    }
    
    // Public properties for inspector
    public string PortName => portName;
    public int BaudRate => baudRate;
    public bool IsArduinoConnected => IsConnected();
}
