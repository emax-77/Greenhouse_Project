#include <WiFi.h>
#include <HTTPClient.h>
#include <Wire.h>
#include <Adafruit_Sensor.h>
#include <Adafruit_BME280.h>

#define SEALEVELPRESSURE_HPA (1013.25)

const char* ssid = "Nase_WiFi_24";
const char* password = "7777020411";
const char* serverUrl = "http://87.197.172.152:5000/api/greenhouse/readings";
const char* apiKey = "greenhouse_local_key_2026";
const unsigned long intervalMs = 300000; // 5 minutes

Adafruit_BME280 bme;
unsigned long previousMillis = 0;

void connectWiFi() {
  if (WiFi.status() == WL_CONNECTED) {
    return;
  }

  Serial.print("Connecting to WiFi");
  WiFi.begin(ssid, password);

  unsigned long start = millis();
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
    if (millis() - start > 30000) {
      Serial.println("\nWiFi connect timeout, retrying...");
      WiFi.disconnect();
      delay(2000);
      WiFi.begin(ssid, password);
      start = millis();
    }
  }

  Serial.println();
  Serial.print("WiFi connected, IP: ");
  Serial.println(WiFi.localIP());
}

void scanI2CDevices() {
  Serial.println("Scanning I2C bus...");

  for (uint8_t addr = 1; addr < 127; addr++) {
    Wire.beginTransmission(addr);
    uint8_t error = Wire.endTransmission();

    if (error == 0) {
      Serial.print("I2C device found at address 0x");
      Serial.println(addr, HEX);
    }
  }

  Serial.println("I2C scan complete.");
}

bool initBme280() {
  Wire.begin(21, 22);
  Wire.setClock(100000);
  scanI2CDevices();

  const uint8_t addresses[] = {0x76, 0x77};

  for (uint8_t addr : addresses) {
    Serial.print("Trying BME280 address 0x");
    Serial.println(addr, HEX);

    if (bme.begin(addr)) {
      Serial.print("BME280 initialized at 0x");
      Serial.println(addr, HEX);
      return true;
    }
  }

  Serial.println("BME280 not found on 0x76 or 0x77.");
  return false;
}

bool sendReading(float temperature, float humidity, float pressure) {
  if (WiFi.status() != WL_CONNECTED) {
    connectWiFi();
  }

  HTTPClient http;
  http.begin(serverUrl);
  http.addHeader("Content-Type", "application/json");
  http.addHeader("x-api-key", apiKey);

  String payload = "{";
  payload += "\"deviceId\": \"ESP32_Greenhouse\",";
  payload += "\"temperature\": " + String(temperature, 2) + ",";
  payload += "\"humidity\": " + String(humidity, 2) + ",";
  payload += "\"pressure\": " + String(pressure, 2);
  payload += "}";

  int httpResponseCode = http.POST(payload);
  bool success = false;

  if (httpResponseCode > 0) {
    String response = http.getString();
    Serial.print("HTTP response code: ");
    Serial.println(httpResponseCode);
    Serial.print("Response: ");
    Serial.println(response);
    success = (httpResponseCode == HTTP_CODE_OK || httpResponseCode == HTTP_CODE_CREATED);
  } else {
    Serial.print("Error sending request: ");
    Serial.println(http.errorToString(httpResponseCode));
  }

  http.end();
  return success;
}

void setup() {
  Serial.begin(115200);
  delay(1000);

  Serial.println("Initializing BME280...");
  if (!initBme280()) {
    Serial.println("Could not find a valid BME280 sensor, check wiring!");
    while (1) {
      delay(1000);
    }
  }

  connectWiFi();
  previousMillis = millis() - intervalMs;
}

void loop() {
  unsigned long currentMillis = millis();

  if (currentMillis - previousMillis >= intervalMs) {
    previousMillis = currentMillis;

    float temperature = bme.readTemperature();
    float humidity = bme.readHumidity();
    float pressure = bme.readPressure() / 100.0F;

    Serial.print("Temperature: ");
    Serial.print(temperature);
    Serial.print(" °C, Humidity: ");
    Serial.print(humidity);
    Serial.print(" %, Pressure: ");
    Serial.print(pressure);
    Serial.println(" hPa");

    if (!sendReading(temperature, humidity, pressure)) {
      Serial.println("Failed to send reading, will retry in next interval.");
    }
  }
}
