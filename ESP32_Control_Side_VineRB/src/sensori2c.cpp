#include <Wire.h>
#include <HX711.h>

#define DOUT 32
#define CLK  33

HX711 scale;

void setup() {
  Serial.begin(115200);
  Wire.begin(21, 22, 400000); // Initialize I2C on SDA21/SCL22
  scale.begin(DOUT, CLK);
  scale.set_gain(128);
  scale.tare();
}

void loop() {
  static uint32_t prev = 0;
  uint32_t now = millis();
  if (now - prev >= 50) {
    
    if (scale.wait_ready_timeout(1000)) {
      long raw = scale.read();
      Serial.print("Gain128: ");
      Serial.println(raw);
    } else {
      Serial.println("HX711 not ready");
    }
    prev = now;
  }
}
