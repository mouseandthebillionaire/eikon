# Force Sensor Unity Project

This Unity project provides a complete system for reading data from 6 force sensors connected to an Arduino, with an extensible design that allows easy addition of more sensors.

## Features

- **Real-time sensor data streaming** from Arduino to Unity
- **Extensible architecture** - easily add more sensors beyond the initial 6
- **Event-driven system** for responsive sensor data handling
- **Visual and audio feedback** for individual sensors
- **Data smoothing and filtering** to reduce noise
- **Calibration system** for accurate sensor readings
- **Debug tools** and comprehensive logging

## System Architecture

### Core Components

1. **ArduinoCommunication.cs** - Handles serial communication with Arduino
2. **ForceSensorData.cs** - Data structures and sensor management
3. **ForceSensorManager.cs** - Central sensor data processing and event system
4. **Controller.cs** - Main controller with extensible event handling
5. **FSR.cs** - Individual sensor component with visual/audio feedback

### Data Flow

```
Arduino → Serial Port → ArduinoCommunication → ForceSensorManager → Events → Controller/FSR Components
```

## Setup Instructions

### 1. Hardware Setup

#### Arduino Wiring
Connect 6 Force Sensitive Resistors (FSRs) to your Arduino:

```
FSR 1: Pin A0 (with 10kΩ pull-down resistor to GND)
FSR 2: Pin A1 (with 10kΩ pull-down resistor to GND)
FSR 3: Pin A2 (with 10kΩ pull-down resistor to GND)
FSR 4: Pin A3 (with 10kΩ pull-down resistor to GND)
FSR 5: Pin A4 (with 10kΩ pull-down resistor to GND)
FSR 6: Pin A5 (with 10kΩ pull-down resistor to GND)
```

Each FSR should be connected:
- One terminal to 5V
- Other terminal to analog pin
- 10kΩ resistor from analog pin to GND

#### Arduino Code
1. Upload the provided `ForceSensorExample.ino` to your Arduino
2. Open Serial Monitor to verify communication (9600 baud)
3. Run calibration command: `calibrate`

### 2. Unity Setup

#### Scene Setup
1. Create an empty GameObject and name it "ForceSensorSystem"
2. Add the following components to this GameObject:
   - `ArduinoCommunication`
   - `ForceSensorManager`
   - `Controller`

#### Component Configuration

**ArduinoCommunication:**
- Set `Port Name` to your Arduino's COM port (e.g., "COM3" on Windows, "/dev/ttyUSB0" on Linux, "/dev/cu.usbmodem..." on Mac)
- Set `Baud Rate` to 9600
- Adjust other serial settings if needed

**ForceSensorManager:**
- Enable `Auto Create Configs` for automatic sensor configuration
- Adjust `Max Data History` for data logging
- Configure individual sensor settings in the `Sensor Configs` array

**Controller:**
- Enable `Enable Debug Logging` for development
- Set `Show Sensor Values In Console` to see real-time values

#### Individual Sensor Setup
For each sensor you want to visualize:

1. Create a GameObject (e.g., Cube, Sphere)
2. Add the `FSR` component
3. Set the `Sensor ID` (0-5 for the first 6 sensors)
4. Configure visual feedback (colors, scaling)
5. Optionally add AudioSource for audio feedback

## Usage

### Basic Usage

```csharp
// Get sensor data
ForceSensorData sensorData = controller.GetSensorData(0); // Get sensor 0 data
float value = sensorData.normalizedValue; // 0-1 range
float rawValue = sensorData.rawValue; // 0-1023 range

// Check if sensor is active
bool isActive = controller.IsSensorActive(0);

// Get all sensor data
ForceSensorData[] allData = controller.GetAllSensorData();
```

### Event Handling

The system provides several events for responsive programming:

```csharp
// Subscribe to sensor value changes
ForceSensorManager.OnSensorValueChanged += (sensorData) => {
    Debug.Log($"Sensor {sensorData.sensorId} value: {sensorData.normalizedValue}");
};

// Subscribe to sensor activation changes
ForceSensorManager.OnSensorActivationChanged += (sensorId, isActive) => {
    Debug.Log($"Sensor {sensorId} {(isActive ? "activated" : "deactivated")}");
};

// Subscribe to all sensors updated
ForceSensorManager.OnAllSensorsUpdated += (allData) => {
    // Process all sensor data at once
};
```

### Extending for More Sensors

To add more than 6 sensors:

1. **Arduino Side:**
   - Add more analog pins to `SENSOR_PINS` array
   - Update the loop to read additional sensors
   - Modify data format to include more values

2. **Unity Side:**
   - Update array sizes in `ForceSensorData.cs` and `ForceSensorManager.cs`
   - Modify the data parsing in `ArduinoCommunication.cs`
   - Update sensor ID validation in `FSR.cs`

Example for 8 sensors:
```csharp
// In ForceSensorManager.cs, change:
private ForceSensorData[] currentSensorData = new ForceSensorData[8];
private ForceSensorConfig[] sensorConfigs = new ForceSensorConfig[8];

// In ArduinoCommunication.cs, change:
ForceSensorData[] sensorData = new ForceSensorData[8];
```

## Arduino Commands

Send these commands via Serial Monitor or Unity:

- `calibrate` - Start 5-second calibration process
- `status` - Show current sensor values and calibration data
- `reset` - Reset calibration values
- `help` - Show available commands

## Troubleshooting

### Common Issues

1. **No data received:**
   - Check COM port name in Unity
   - Verify Arduino is connected and powered
   - Check baud rate matches (9600)
   - Ensure Arduino code is uploaded correctly

2. **Inconsistent readings:**
   - Run calibration: send `calibrate` command
   - Check wiring connections
   - Verify pull-down resistors are properly connected

3. **Unity connection fails:**
   - Close Serial Monitor in Arduino IDE
   - Check if another application is using the COM port
   - Try different COM port numbers

### Debug Tools

- Use `Print System Status` context menu on Controller
- Use `Print All Sensor Data` context menu on Controller
- Use `Print FSR Status` context menu on individual FSR components
- Enable debug logging in Controller component

## Performance Notes

- Default update rate: 20Hz (50ms intervals)
- Data smoothing reduces noise but adds slight delay
- Event system is optimized for real-time performance
- Consider reducing update rate for battery-powered applications

## Future Enhancements

- WebSocket communication for remote monitoring
- Machine learning integration for gesture recognition
- Data recording and playback system
- Advanced filtering algorithms
- Multi-Arduino support for larger sensor arrays
