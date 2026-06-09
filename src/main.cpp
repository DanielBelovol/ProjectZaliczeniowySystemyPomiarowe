#include <Arduino.h>
#include <Wire.h>
#include <Adafruit_BMP085.h>
#include <ArduinoJson.h>

const int TRIG_PIN = 5;
const int ECHO_PIN = 18;

Adafruit_BMP085 bmp;

void setup() {
  Serial.begin(115200);
  pinMode(TRIG_PIN, OUTPUT);
  pinMode(ECHO_PIN, INPUT);

  if (!bmp.begin()) {
    Serial.println("{\"error\": \"BMP180 sensor not found! Check wiring.\"}");
    while (1) {}
  }

  Serial.println("{\"status\": \"Sensor Node Booted Successfully\"}");
  delay(1000);
}

void loop() {
  digitalWrite(TRIG_PIN, LOW);
  delayMicroseconds(2);
  digitalWrite(TRIG_PIN, HIGH);
  delayMicroseconds(10);
  digitalWrite(TRIG_PIN, LOW);

  long duration = pulseIn(ECHO_PIN, HIGH);
  float distance = duration * 0.034 / 2;

  float temperature = bmp.readTemperature();

  StaticJsonDocument<200> doc;
  doc["distance"] = distance;
  doc["temperature"] = temperature;

  serializeJson(doc, Serial);
  Serial.println();

  delay(2000);
}