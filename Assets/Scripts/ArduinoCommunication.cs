using System;
using System.Collections;
using System.IO.Ports;
using UnityEngine;
using System.Collections.Generic;

public class ArduinoCommunication : MonoBehaviour
{
    [Header("Serial Port Settings")]
    [SerializeField] private string portName = ""; // Will auto-detect if empty
    [SerializeField] private int baudRate = 9600;
    [SerializeField] private int dataBits = 8;
    [SerializeField] private Parity parity = Parity.None;
    [SerializeField] private StopBits stopBits = StopBits.One;
    [SerializeField] private bool autoDetectPort = true;
    
    [Header("Connection Settings")]
    [SerializeField] private float connectionTimeout = 5f;
    [SerializeField] private float dataTimeout = 1f;
    [SerializeField] private int maxRetryAttempts = 3;
    [SerializeField] private float retryDelay = 2f;
    [SerializeField] private float portScanInterval = 2f; // How often to scan for new ports
    [SerializeField] private bool enableContinuousMonitoring = true;
    
    private SerialPort serialPort;
    private bool isConnected = false;
    private Coroutine connectionCoroutine;
    private Coroutine dataTimeoutCoroutine;
    private Coroutine portMonitoringCoroutine;
    private string lastKnownPort = "";
    private HashSet<string> knownPorts = new HashSet<string>();
    
    // Events for sensor data
    public static event Action<ForceSensorData[]> OnSensorDataReceived;
    public static event Action<bool> OnConnectionStatusChanged;
    public static event Action<string> OnErrorOccurred;
    
    void Start()
    {
        ConnectToArduino();
        
        // Start continuous port monitoring if enabled
        if (enableContinuousMonitoring)
        {
            StartPortMonitoring();
        }
    }
    
    void OnDestroy()
    {
        DisconnectFromArduino();
        
        // Stop port monitoring
        if (portMonitoringCoroutine != null)
        {
            StopCoroutine(portMonitoringCoroutine);
        }
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
        string targetPort = portName; // Declare outside the loop
        
        for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
        {
            Debug.Log($"Connection attempt {attempt}/{maxRetryAttempts}");
            
            // Get available ports for debugging
            string[] availablePorts = SerialPort.GetPortNames();
            Debug.Log($"Available ports: {string.Join(", ", availablePorts)}");
            
            // Auto-detect Arduino port if enabled and no port specified
            targetPort = portName;
            Debug.Log($"Current portName setting: '{portName}'");
            Debug.Log($"Auto-detect enabled: {autoDetectPort}");
            Debug.Log($"Port name is empty: {string.IsNullOrEmpty(portName)}");
            
            if (autoDetectPort && string.IsNullOrEmpty(portName))
            {
                Debug.Log("Attempting auto-detection...");
                targetPort = DetectArduinoPort(availablePorts);
                if (string.IsNullOrEmpty(targetPort))
                {
                    errorMessage = "No Arduino port detected. Make sure Arduino is connected and not in use by another application.";
                    Debug.LogError(errorMessage);
                    connectionSuccessful = false;
                }
                else
                {
                    Debug.Log($"Auto-detected Arduino port: {targetPort}");
                }
            }
            else if (!string.IsNullOrEmpty(portName))
            {
                Debug.Log($"Using manually specified port: {portName}");
                targetPort = portName;
            }
            else
            {
                Debug.LogError("No port specified and auto-detection is disabled!");
                errorMessage = "No port specified. Either set a port name or enable auto-detection.";
                connectionSuccessful = false;
            }
            
            // Try to connect if we have a valid port
            if (!string.IsNullOrEmpty(targetPort))
            {
                Debug.Log($"Attempting to connect to: {targetPort}");
                
                try
                {
                    serialPort = new SerialPort(targetPort, baudRate, parity, dataBits, stopBits);
                    serialPort.ReadTimeout = 1000;
                    serialPort.WriteTimeout = 1000;
                    
                    serialPort.Open();
                    connectionSuccessful = true;
                    Debug.Log($"Successfully opened port: {targetPort}");
                    break; // Success, exit retry loop
                }
                catch (Exception e)
                {
                    Debug.LogError($"Connection attempt {attempt} failed: {e.Message}");
                    Debug.LogError($"Exception type: {e.GetType().Name}");
                    if (e.InnerException != null)
                    {
                        Debug.LogError($"Inner exception: {e.InnerException.Message}");
                    }
                    errorMessage = e.Message;
                    connectionSuccessful = false;
                    
                    // Clean up failed connection
                    if (serialPort != null)
                    {
                        try
                        {
                            if (serialPort.IsOpen)
                                serialPort.Close();
                            serialPort.Dispose();
                        }
                        catch { }
                        serialPort = null;
                    }
                }
            }
            
            // Wait before retry (except on last attempt)
            if (!connectionSuccessful && attempt < maxRetryAttempts)
            {
                Debug.Log($"Waiting {retryDelay} seconds before retry...");
                yield return new WaitForSeconds(retryDelay);
            }
        }
        
        // Wait a moment for connection to stabilize
        yield return new WaitForSeconds(0.5f);
        
        if (connectionSuccessful && serialPort != null && serialPort.IsOpen)
        {
            isConnected = true;
            lastKnownPort = targetPort; // Store the successful port
            knownPorts.Add(targetPort); // Add to known ports
            OnConnectionStatusChanged?.Invoke(true);
            Debug.Log($"Successfully connected to Arduino on {targetPort}");
            
            // Start reading data
            StartCoroutine(ReadDataCoroutine());
        }
        else
        {
            OnErrorOccurred?.Invoke($"Connection failed after {maxRetryAttempts} attempts: {errorMessage}");
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
    
    
    private bool TestPort(string portName)
    {
        SerialPort testPort = null;
        try
        {
            testPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            testPort.ReadTimeout = 500;
            testPort.WriteTimeout = 500;
            testPort.Open();
            
            // Try to read any available data (this helps identify if it's an Arduino)
            if (testPort.BytesToRead > 0)
            {
                string testData = testPort.ReadLine();
                Debug.Log($"Port {portName} has data: {testData}");
            }
            
            return true;
        }
        catch (Exception e)
        {
            Debug.Log($"Port {portName} test failed: {e.Message}");
            return false;
        }
        finally
        {
            if (testPort != null && testPort.IsOpen)
            {
                testPort.Close();
                testPort.Dispose();
            }
        }
    }
    
    // Public method to manually set port
    public void SetPortName(string newPortName)
    {
        portName = newPortName;
        Debug.Log($"Port name set to: {portName}");
    }
    
    // Public method to refresh available ports
    [ContextMenu("Refresh Available Ports")]
    public void RefreshAvailablePorts()
    {
        string[] availablePorts = SerialPort.GetPortNames();
        Debug.Log($"Available ports: {string.Join(", ", availablePorts)}");
        
        if (autoDetectPort)
        {
            string detectedPort = DetectArduinoPort(availablePorts);
            if (!string.IsNullOrEmpty(detectedPort))
            {
                portName = detectedPort;
                Debug.Log($"Auto-detected and set port to: {portName}");
            }
        }
    }
    
    // Manual method to set port to a specific Arduino port
    [ContextMenu("Set to First Arduino Port")]
    public void SetToFirstArduinoPort()
    {
        string[] availablePorts = SerialPort.GetPortNames();
        string[] arduinoPatterns = { "cu.usbmodem", "cu.usbserial", "cu.SLAB_USBtoUART" };
        
        foreach (string port in availablePorts)
        {
            foreach (string pattern in arduinoPatterns)
            {
                if (port.Contains(pattern))
                {
                    portName = port;
                    Debug.Log($"Manually set port to: {portName}");
                    return;
                }
            }
        }
        
        Debug.LogWarning("No Arduino ports found to set manually");
    }
    
    // Test connection with specific port
    [ContextMenu("Test Connection with First Port")]
    public void TestConnectionWithFirstPort()
    {
        string[] availablePorts = SerialPort.GetPortNames();
        string[] arduinoPatterns = { "cu.usbmodem", "cu.usbserial", "cu.SLAB_USBtoUART" };
        
        foreach (string port in availablePorts)
        {
            foreach (string pattern in arduinoPatterns)
            {
                if (port.Contains(pattern))
                {
                    Debug.Log($"Testing connection to: {port}");
                    TestSpecificPort(port);
                    return;
                }
            }
        }
        
        Debug.LogWarning("No Arduino ports found to test");
    }
    
    private void TestSpecificPort(string testPort)
    {
        SerialPort testSerialPort = null;
        try
        {
            Debug.Log($"Creating SerialPort with: {testPort}, {baudRate}, {parity}, {dataBits}, {stopBits}");
            testSerialPort = new SerialPort(testPort, baudRate, parity, dataBits, stopBits);
            testSerialPort.ReadTimeout = 2000;
            testSerialPort.WriteTimeout = 2000;
            
            Debug.Log("Attempting to open port...");
            testSerialPort.Open();
            Debug.Log($"SUCCESS: Port {testPort} opened successfully!");
            
            // Try to read any data
            if (testSerialPort.BytesToRead > 0)
            {
                string data = testSerialPort.ReadLine();
                Debug.Log($"Data received: {data}");
            }
            else
            {
                Debug.Log("No data available (this is normal if Arduino isn't sending data)");
            }
            
            // Try to send a test command
            testSerialPort.WriteLine("test");
            Debug.Log("Test command sent successfully");
            
        }
        catch (Exception e)
        {
            Debug.LogError($"FAILED to connect to {testPort}: {e.Message}");
            Debug.LogError($"Exception type: {e.GetType().Name}");
            if (e.InnerException != null)
            {
                Debug.LogError($"Inner exception: {e.InnerException.Message}");
            }
        }
        finally
        {
            if (testSerialPort != null && testSerialPort.IsOpen)
            {
                testSerialPort.Close();
                testSerialPort.Dispose();
                Debug.Log("Test port closed");
            }
        }
    }
    
    // Force connection with specific port
    [ContextMenu("Force Connect to /dev/cu.usbmodem142401")]
    public void ForceConnectToFirstPort()
    {
        portName = "/dev/cu.usbmodem142401";
        autoDetectPort = false;
        Debug.Log($"Forced port to: {portName}");
        ConnectToArduino();
    }
    
    [ContextMenu("Force Connect to /dev/cu.usbmodemSN234567892")]
    public void ForceConnectToSecondPort()
    {
        portName = "/dev/cu.usbmodemSN234567892";
        autoDetectPort = false;
        Debug.Log($"Forced port to: {portName}");
        ConnectToArduino();
    }
    
    // Check if Arduino is sending data
    [ContextMenu("Check Arduino Data")]
    public void CheckArduinoData()
    {
        if (IsConnected())
        {
            Debug.Log("Arduino is connected, checking for data...");
            try
            {
                if (serialPort.BytesToRead > 0)
                {
                    string data = serialPort.ReadLine();
                    Debug.Log($"Arduino data: {data}");
                }
                else
                {
                    Debug.Log("No data from Arduino (this might be normal)");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error reading from Arduino: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("Arduino is not connected");
        }
    }
    
    // Print current connection status
    [ContextMenu("Print Connection Status")]
    public void PrintConnectionStatus()
    {
        Debug.Log($"=== Arduino Connection Status ===");
        Debug.Log($"Port Name: {portName}");
        Debug.Log($"Auto Detect: {autoDetectPort}");
        Debug.Log($"Baud Rate: {baudRate}");
        Debug.Log($"Is Connected: {IsConnected()}");
        Debug.Log($"Serial Port: {(serialPort != null ? "Created" : "Null")}");
        if (serialPort != null)
        {
            Debug.Log($"Serial Port Open: {serialPort.IsOpen}");
        }
        Debug.Log($"Available Ports: {string.Join(", ", SerialPort.GetPortNames())}");
        Debug.Log($"================================");
    }
    
    // Continuous port monitoring
    private void StartPortMonitoring()
    {
        if (portMonitoringCoroutine != null)
        {
            StopCoroutine(portMonitoringCoroutine);
        }
        portMonitoringCoroutine = StartCoroutine(PortMonitoringCoroutine());
    }
    
    private IEnumerator PortMonitoringCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(portScanInterval);
            
            // Check if we're currently connected
            if (!IsConnected())
            {
                Debug.Log("Not connected, scanning for Arduino ports...");
                ScanForArduinoPorts();
            }
            else
            {
                // We're connected, but check if the port is still valid
                if (!IsPortStillValid())
                {
                    Debug.LogWarning("Current port is no longer valid, attempting to reconnect...");
                    DisconnectFromArduino();
                    yield return new WaitForSeconds(1f); // Brief pause before reconnecting
                    ConnectToArduino();
                }
            }
        }
    }
    
    private void ScanForArduinoPorts()
    {
        string[] availablePorts = SerialPort.GetPortNames();
        Debug.Log($"Scanning {availablePorts.Length} available ports...");
        
        // Look for new Arduino ports
        foreach (string port in availablePorts)
        {
            if (IsArduinoPort(port) && !knownPorts.Contains(port))
            {
                Debug.Log($"Found new Arduino port: {port}");
                knownPorts.Add(port);
                
                // Try to connect to this new port
                if (!IsConnected())
                {
                    Debug.Log($"Attempting to connect to new Arduino port: {port}");
                    portName = port;
                    ConnectToArduino();
                    break; // Only try one port at a time
                }
            }
        }
    }
    
    private bool IsPortStillValid()
    {
        if (serialPort == null || !serialPort.IsOpen)
        {
            return false;
        }
        
        try
        {
            // Try to read from the port to see if it's still responsive
            if (serialPort.BytesToRead > 0)
            {
                serialPort.ReadExisting(); // Clear any buffered data
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private bool IsArduinoPort(string portName)
    {
        // Enhanced Arduino port detection
        string[] arduinoPatterns = {
            // macOS patterns
            "cu.usbmodem", "cu.usbserial", "cu.SLAB_USBtoUART",
            // Windows patterns  
            "COM", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            // Linux patterns
            "ttyUSB", "ttyACM", "tty.usbmodem", "tty.usbserial"
        };
        
        foreach (string pattern in arduinoPatterns)
        {
            if (portName.Contains(pattern))
            {
                return true;
            }
        }
        
        return false;
    }
    
    // Enhanced port detection with better validation
    private string DetectArduinoPort(string[] availablePorts)
    {
        Debug.Log("Enhanced Arduino port detection...");
        Debug.Log($"Total available ports: {availablePorts.Length}");
        
        // First, try to find ports that match Arduino patterns
        List<string> candidatePorts = new List<string>();
        
        foreach (string port in availablePorts)
        {
            Debug.Log($"Checking port: {port}");
            
            if (IsArduinoPort(port))
            {
                Debug.Log($"Found potential Arduino port: {port}");
                candidatePorts.Add(port);
            }
        }
        
        // Test each candidate port
        foreach (string port in candidatePorts)
        {
            Debug.Log($"Testing Arduino port: {port}");
            
            if (TestPort(port))
            {
                Debug.Log($"Successfully validated Arduino port: {port}");
                return port;
            }
        }
        
        Debug.LogWarning("No valid Arduino ports found");
        return null;
    }
    
    
    // Public method to manually trigger port scan
    [ContextMenu("Scan for Arduino Ports")]
    public void ManualPortScan()
    {
        Debug.Log("Manual port scan triggered");
        ScanForArduinoPorts();
    }
    
    // Public method to reset port detection
    [ContextMenu("Reset Port Detection")]
    public void ResetPortDetection()
    {
        knownPorts.Clear();
        lastKnownPort = "";
        portName = "";
        Debug.Log("Port detection reset - will scan for new ports");
    }

    // Public properties for inspector
    public string PortName => portName;
    public int BaudRate => baudRate;
    public bool IsArduinoConnected => IsConnected();
}
