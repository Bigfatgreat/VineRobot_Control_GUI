#include <Arduino.h>
#include <Bluepad32.h>

ControllerPtr myControllers[BP32_MAX_GAMEPADS];

// ------- Relay pins (safe) -------
const uint8_t RELAY_FILL = 16;   // SA: FILL
const uint8_t RELAY_EXH  = 17;   // SB: EXHAUST
const int RELAY_ON  = LOW;       // change to HIGH if your board is active-HIGH
const int RELAY_OFF = HIGH;
const unsigned long DEADTIME_MS = 80;

enum ValveState { HOLD = 0, FILL = 1, EXHAUST = 2 };
ValveState lastState = HOLD;

inline void bothOff() {
  digitalWrite(RELAY_FILL, RELAY_OFF);
  digitalWrite(RELAY_EXH,  RELAY_OFF);
}
void setHold() {
  if (lastState != HOLD) {
    bothOff();
    lastState = HOLD;
    Serial.println("[CMD] HOLD");
  }
}
void setFill() {
  if (lastState != FILL) {
    bothOff(); delay(DEADTIME_MS);
    digitalWrite(RELAY_FILL, RELAY_ON);
    lastState = FILL;
    Serial.println("[CMD] FILL");
  }
}
void setExhaust() {
  if (lastState != EXHAUST) {
    bothOff(); delay(DEADTIME_MS);
    digitalWrite(RELAY_EXH, RELAY_ON);
    lastState = EXHAUST;
    Serial.println("[CMD] EXHAUST");
  }
}

// -------- Bluepad32 callbacks --------
void onConnectedController(ControllerPtr ctl) {
  for (int i = 0; i < BP32_MAX_GAMEPADS; ++i) {
    if (!myControllers[i]) {
      myControllers[i] = ctl;
      auto p = ctl->getProperties();
      Serial.printf("Controller %d connected: %s VID=%04x PID=%04x\n",
                    i, ctl->getModelName().c_str(), p.vendor_id, p.product_id);
      return;
    }
  }
  Serial.println("Controller connected but no empty slot");
}
void onDisconnectedController(ControllerPtr ctl) {
  for (int i = 0; i < BP32_MAX_GAMEPADS; ++i) {
    if (myControllers[i] == ctl) {
      myControllers[i] = nullptr;
      Serial.printf("Controller %d disconnected\n", i);
      return;
    }
  }
}

// -------- Map D-pad -> valve --------
void processGamepad(ControllerPtr ctl) {
  // D-pad is a bitmask defined by Bluepad32: DPAD_UP/DOWN/LEFT/RIGHT
  uint8_t d = ctl->dpad();

  if (d) {
    if (d & DPAD_UP)    Serial.println("[BTN] DPAD UP");
    if (d & DPAD_DOWN)  Serial.println("[BTN] DPAD DOWN");
    if (d & DPAD_LEFT)  Serial.println("[BTN] DPAD LEFT");
    if (d & DPAD_RIGHT) Serial.println("[BTN] DPAD RIGHT");
  }

  // UP = FILL, DOWN = EXHAUST, else HOLD
  if ((d & DPAD_UP) && !(d & DPAD_DOWN))      setFill();
  else if ((d & DPAD_DOWN) && !(d & DPAD_UP)) setExhaust();
  else                                        setHold();
}

void processControllers() {
  for (auto ctl : myControllers) {
    if (ctl && ctl->isConnected() && ctl->hasData() && ctl->isGamepad())
      processGamepad(ctl);
  }
}

void setup() {
  Serial.begin(115200);
  delay(50);

  pinMode(RELAY_FILL, OUTPUT);
  pinMode(RELAY_EXH,  OUTPUT);
  bothOff();

  BP32.setup(&onConnectedController, &onDisconnectedController);
  // BP32.forgetBluetoothKeys(); // <-- call only when you want to re-pair
  BP32.enableVirtualDevice(false);

  Serial.printf("FW: %s  BDAddr: ", BP32.firmwareVersion());
  const uint8_t* a = BP32.localBdAddress();
  Serial.printf("%02X:%02X:%02X:%02X:%02X:%02X\n", a[0],a[1],a[2],a[3],a[4],a[5]);

  Serial.println("D-pad: UP=FILL, DOWN=EXHAUST, else HOLD");
}

void loop() {
  if (BP32.update())
    processControllers();
  vTaskDelay(1);
}
