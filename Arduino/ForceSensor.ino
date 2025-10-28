/*
 * Force Sensor Array Example for Unity
 * 
 * This Arduino sketch reads from 6 force sensors (FSRs) and sends the data
 * to Unity via serial communication.
 * 
 * Hardware Setup:
 * - Connect 6 FSRs to analog pins A0-A5
 * - Connect pull-down resistors (10kΩ) from each analog pin to GND
 * - Connect the other end of each FSR to 5V
 * 
 * Data Format:
 * Sends comma-separated values: "sensor1,sensor2,sensor3,sensor4,sensor5,sensor6"
 * Example: "1023,512,0,256,768,1023"
 * 
 * Unity expects 10-bit ADC values (0-1023)
 */

// Pin definitions for the 6 force sensors
const int SENSOR_PINS[6] = {A0, A1, A2, A3, A4, A5};

// Sensor data array
int sensorValues[6] = {0, 0, 0, 0, 0, 0};

// Timing variables
unsigned long lastReadTime = 0;
const unsigned long READ_INTERVAL = 50; // Read sensors every 50ms (20Hz)

// Smoothing variables
float smoothedValues[6] = {0, 0, 0, 0, 0, 0};
const float SMOOTHING_FACTOR = 0.7; // Higher = more smoothing

// Calibration variables
int sensorMin[6] = {1023, 1023, 1023, 1023, 1023, 1023};
int sensorMax[6] = {0, 0, 0, 0, 0, 0};
bool isCalibrating = false;
unsigned long calibrationStartTime = 0;
const unsigned long CALIBRATION_DURATION = 5000; // 5 seconds

void setup() {
  // Initialize serial communication
  Serial.begin(9600);
  
  // Wait for serial connection
  while (!Serial) {
    delay(10);
  }
  
  // Initialize sensor pins
  for (int i = 0; i < 6; i++) {
    pinMode(SENSOR_PINS[i], INPUT);
  }
  
  // Send startup message
  Serial.println("Force Sensor Array Ready");
  Serial.println("Commands: 'calibrate' - start calibration, 'status' - show status");
  
  // Start calibration
  startCalibration();
}

void loop() {
  // Check for serial commands
  if (Serial.available() > 0) {
    String command = Serial.readStringUntil('\n');
    command.trim();
    handleCommand(command);
  }
  
  // Read sensors at specified interval
  if (millis() - lastReadTime >= READ_INTERVAL) {
    readSensors();
    sendSensorData();
    lastReadTime = millis();
  }
  
  // Handle calibration
  if (isCalibrating) {
    handleCalibration();
  }
}

void readSensors() {
  for (int i = 0; i < 6; i++) {
    // Read raw sensor value
    int rawValue = analogRead(SENSOR_PINS[i]);
    
    // Apply smoothing
    smoothedValues[i] = (SMOOTHING_FACTOR * smoothedValues[i]) + 
                       ((1.0 - SMOOTHING_FACTOR) * rawValue);
    
    // Store the smoothed value
    sensorValues[i] = (int)smoothedValues[i];
    
    // Update calibration values if not calibrating
    if (!isCalibrating) {
      if (sensorValues[i] < sensorMin[i]) {
        sensorMin[i] = sensorValues[i];
      }
      if (sensorValues[i] > sensorMax[i]) {
        sensorMax[i] = sensorValues[i];
      }
    }
  }
}

void sendSensorData() {
  // Send comma-separated values
  for (int i = 0; i < 6; i++) {
    Serial.print(sensorValues[i]);
    if (i < 5) {
      Serial.print(",");
    }
  }
  Serial.println(); // End of line
}

void handleCommand(String command) {
  if (command.equals("calibrate")) {
    startCalibration();
  }
  else if (command.equals("status")) {
    sendStatus();
  }
  else if (command.equals("reset")) {
    resetCalibration();
  }
  else if (command.equals("help")) {
    sendHelp();
  }
  else {
    Serial.println("Unknown command: " + command);
  }
}

void startCalibration() {
  isCalibrating = true;
  calibrationStartTime = millis();
  
  // Reset calibration values
  for (int i = 0; i < 6; i++) {
    sensorMin[i] = 1023;
    sensorMax[i] = 0;
  }
  
  Serial.println("Calibration started. Apply pressure to all sensors for 5 seconds...");
}

void handleCalibration() {
  if (millis() - calibrationStartTime >= CALIBRATION_DURATION) {
    isCalibrating = false;
    Serial.println("Calibration complete!");
    sendStatus();
  }
  else {
    // During calibration, update min/max values
    for (int i = 0; i < 6; i++) {
      if (sensorValues[i] < sensorMin[i]) {
        sensorMin[i] = sensorValues[i];
      }
      if (sensorValues[i] > sensorMax[i]) {
        sensorMax[i] = sensorValues[i];
      }
    }
  }
}

void sendStatus() {
  Serial.println("=== Sensor Status ===");
  for (int i = 0; i < 6; i++) {
    Serial.print("Sensor ");
    Serial.print(i);
    Serial.print(": ");
    Serial.print(sensorValues[i]);
    Serial.print(" (Min: ");
    Serial.print(sensorMin[i]);
    Serial.print(", Max: ");
    Serial.print(sensorMax[i]);
    Serial.println(")");
  }
  Serial.println("===================");
}

void resetCalibration() {
  for (int i = 0; i < 6; i++) {
    sensorMin[i] = 1023;
    sensorMax[i] = 0;
  }
  Serial.println("Calibration reset");
}

void sendHelp() {
  Serial.println("=== Available Commands ===");
  Serial.println("calibrate - Start sensor calibration");
  Serial.println("status - Show current sensor status");
  Serial.println("reset - Reset calibration values");
  Serial.println("help - Show this help message");
  Serial.println("==========================");
}

// Optional: Add LED feedback for calibration
void updateCalibrationLED() {
  if (isCalibrating) {
    // Blink LED during calibration
    digitalWrite(LED_BUILTIN, (millis() / 500) % 2);
  } else {
    // Solid LED when ready
    digitalWrite(LED_BUILTIN, HIGH);
  }
}
