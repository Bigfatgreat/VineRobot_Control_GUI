#include <Arduino.h>
#include <Bluepad32.h>

ControllerPtr myControllers[BP32_MAX_GAMEPADS];

// ---------------- Pin Setup ----------------
const uint8_t RELAY_MAIN_FILL = 16;
const uint8_t RELAY_MAIN_EXH  = 17;

// Section 1 Pouches
const uint8_t RELAY_S1_LEFT   = 18;
const uint8_t RELAY_S1_RIGHT  = 19;
const uint8_t RELAY_S1_TOP    = 21;

// Section 2 Pouches
const uint8_t RELAY_S2_LEFT   = 22;
const uint8_t RELAY_S2_RIGHT  = 23;
const uint8_t RELAY_S2_TOP    = 25;

const int RELAY_ON  = LOW;
const int RELAY_OFF = HIGH;
unsigned long lastRelayActionTime = 0;
const unsigned long DEADTIME_MS = 80;

enum ValveState { HOLD = 0, FILL = 1, EXHAUST = 2 };
ValveState mainLastState = HOLD;

// ---------------- Mode Flags ----------------
bool modeInput = true;       // true = Input (FILL), false = Exhaust
bool sectionTwo = false;     // true = Section 2, false = Section 1

uint16_t prevButtons[BP32_MAX_GAMEPADS] = {0};

#define BUTTON_SQUARE    0x0004
#define BUTTON_CIRCLE    0x0002
#define BUTTON_TRIANGLE  0x0008

const uint8_t STEPPER_DIR_PIN  = 26;  // Direction control pin
const uint8_t STEPPER_PWM_PIN  = 27;  // PWM pin for motor

// ---------------- Utility ----------------
inline void relayOff(uint8_t pin) {
  digitalWrite(pin, RELAY_OFF);
}
inline void relayOn(uint8_t pin) {
  digitalWrite(pin, RELAY_ON);
}

void allPouchOff() {
  uint8_t pins[] = {
    RELAY_S1_LEFT, RELAY_S1_RIGHT, RELAY_S1_TOP,
    RELAY_S2_LEFT, RELAY_S2_RIGHT, RELAY_S2_TOP
  };
  for (auto p : pins) relayOff(p);
}

void bothMainOff() {
  relayOff(RELAY_MAIN_FILL);
  relayOff(RELAY_MAIN_EXH);
}

void setMainHold() {
  if (mainLastState != HOLD) {
    bothMainOff();
    mainLastState = HOLD;
    Serial.println("[MAIN] HOLD");
  }
}
void setMainFill() {
  if (mainLastState != FILL && millis() - lastRelayActionTime >= DEADTIME_MS) {
    bothMainOff();
    relayOn(RELAY_MAIN_FILL);
    mainLastState = FILL;
    lastRelayActionTime = millis();
    Serial.println("[MAIN] FILL");
  }
}

void setMainExhaust() {
  if (mainLastState != EXHAUST && millis() - lastRelayActionTime >= DEADTIME_MS) {
    bothMainOff();
    relayOn(RELAY_MAIN_EXH);
    mainLastState = EXHAUST;
    lastRelayActionTime = millis();
    Serial.println("[MAIN] EXHAUST");
  }
}


// Set LED color based on current mode and section
void updateLED(ControllerPtr ctl) {
  if (!ctl) return;

  if (!sectionTwo) {
    if (modeInput) ctl->setColorLED(0, 0, 255);       // Light Blue
    else           ctl->setColorLED(255, 0, 0);       // Red
  } else {
    if (modeInput) ctl->setColorLED(255, 255, 255);   // White
    else           ctl->setColorLED(128, 0, 128);     // Purple
  }
}

// ---------------- Gamepad Handler ----------------
void handlePouch(ControllerPtr ctl, uint16_t buttons, uint16_t buttonMask, const char* name, uint8_t pin) {
  if (buttons & buttonMask) {
    relayOn(pin);
    Serial.printf("[%s] %s ON\n", sectionTwo ? "S2" : "S1", name);
  } else {
    relayOff(pin);
    Serial.printf("[%s] %s OFF\n", sectionTwo ? "S2" : "S1", name);
  }
}


void processGamepad(ControllerPtr ctl) {
  int idx = -1;
  for (int i = 0; i < BP32_MAX_GAMEPADS; ++i) {
    if (myControllers[i] == ctl) {
      idx = i;
      break;
    }
  }
  if (idx == -1) return;

  // ---------------- L1 / R1 toggle ----------------
  static bool prevL1[BP32_MAX_GAMEPADS] = {false}, prevR1[BP32_MAX_GAMEPADS] = {false};
  bool currL1 = ctl->l1(), currR1 = ctl->r1();

  if (!prevL1[idx] && currL1) {
    modeInput ^= 1;
    updateLED(ctl);
  }
  if (!prevR1[idx] && currR1) {
    sectionTwo ^= 1;
    updateLED(ctl);
  }

  prevL1[idx] = currL1;
  prevR1[idx] = currR1;

  // ---------------- Idle Detection ----------------
  static bool wasIdle[BP32_MAX_GAMEPADS] = {false};
  uint16_t btns = ctl->buttons();
  uint8_t dpad = ctl->dpad();
  bool anyInput = (btns != 0 || dpad != 0);

  if (!anyInput && !wasIdle[idx]) {
    ctl->setColorLED(128, 128, 128); // Grey for idle
    wasIdle[idx] = true;
  } else if (anyInput && wasIdle[idx]) {
    updateLED(ctl); // Restore based on mode/section
    wasIdle[idx] = false;
  }

  // ---------------- Main D-pad Control ----------------
  if ((dpad & DPAD_UP) && !(dpad & DPAD_DOWN)) {
    setMainFill();
  } else if ((dpad & DPAD_DOWN) && !(dpad & DPAD_UP)) {
    setMainExhaust();
  } else {
    setMainHold();
  }

  // ---------------- Pouch Control ----------------
  uint16_t prev = prevButtons[idx];
  bool l1Held = ctl->l1();  // Check L1 for exhaust override

  auto handleButton = [&](uint16_t mask, uint8_t pin, const char* name) {
    bool isPressed = (btns & mask);
    bool wasPressed = (prev & mask);

    if (!wasPressed && isPressed) {
      if (millis() - lastRelayActionTime >= DEADTIME_MS) {
        if (l1Held) {
          relayOn(RELAY_MAIN_EXH);
          relayOff(RELAY_MAIN_FILL);
          Serial.printf("[%s] %s EXHAUST\n", sectionTwo ? "S2" : "S1", name);
        } else {
          relayOn(pin);
          Serial.printf("[%s] %s FILL\n", sectionTwo ? "S2" : "S1", name);
        }
        lastRelayActionTime = millis();
      }
    } else if (wasPressed && !isPressed) {
      relayOff(pin);
      relayOff(RELAY_MAIN_EXH);
      Serial.printf("[%s] %s OFF\n", sectionTwo ? "S2" : "S1", name);
    }
  };

    // -------- Stepper Motor via L2 / R2 (Analog Triggers) --------

  static uint8_t lastTrigger[BP32_MAX_GAMEPADS] = {0}; // 0: none, 1: L2, 2: R2

  int brakeVal = ctl->brake();     // L2 analog (0–1023)
  int throttleVal = ctl->throttle(); // R2 analog (0–1023)

  // Convert 0–1023 to 0–255 range for PWM
  uint8_t pwmBrake = map(brakeVal, 0, 1023, 0, 255);
  uint8_t pwmThrottle = map(throttleVal, 0, 1023, 0, 255);

  bool l2Pressed = brakeVal > 10;
  bool r2Pressed = throttleVal > 10;

  if (l2Pressed && (!r2Pressed || lastTrigger[idx] != 2)) {
    digitalWrite(STEPPER_DIR_PIN, LOW); // Reverse
    ledcWrite(0, pwmBrake);
    lastTrigger[idx] = 1;
    Serial.printf("[MOTOR] L2 Reverse | PWM: %u\n", pwmBrake);
  } else if (r2Pressed && (!l2Pressed || lastTrigger[idx] != 1)) {
    digitalWrite(STEPPER_DIR_PIN, HIGH); // Forward
    ledcWrite(0, pwmThrottle);
    lastTrigger[idx] = 2;
    Serial.printf("[MOTOR] R2 Forward | PWM: %u\n", pwmThrottle);
  } else if (!l2Pressed && !r2Pressed) {
    ledcWrite(0, 0); // Stop
    lastTrigger[idx] = 0;
    //Serial.println("[MOTOR] Idle");
  }


  // ---------------- Apply to each pouch ----------------
  if (!sectionTwo) {
    handleButton(BUTTON_SQUARE,   RELAY_S1_LEFT,  "LEFT");
    handleButton(BUTTON_CIRCLE,   RELAY_S1_RIGHT, "RIGHT");
    handleButton(BUTTON_TRIANGLE, RELAY_S1_TOP,   "TOP");
  } else {
    handleButton(BUTTON_SQUARE,   RELAY_S2_LEFT,  "LEFT");
    handleButton(BUTTON_CIRCLE,   RELAY_S2_RIGHT, "RIGHT");
    handleButton(BUTTON_TRIANGLE, RELAY_S2_TOP,   "TOP");
  }

  prevButtons[idx] = btns;

  
}






void processControllers() {
  for (auto ctl : myControllers) {
    if (ctl && ctl->isConnected() && ctl->hasData() && ctl->isGamepad())
      processGamepad(ctl);
  }
}

// ---------------- Bluepad32 Callbacks ----------------
void onConnectedController(ControllerPtr ctl) {
  for (int i = 0; i < BP32_MAX_GAMEPADS; ++i) {
    if (!myControllers[i]) {
      myControllers[i] = ctl;
      auto p = ctl->getProperties();
      Serial.printf("Controller %d connected: %s VID=%04x PID=%04x\n",
                    i, ctl->getModelName().c_str(), p.vendor_id, p.product_id);
      updateLED(ctl);
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

// ---------------- Setup & Loop ----------------
void setup() {
  Serial.begin(115200);
  delay(50);

  // Relay pins
  uint8_t allPins[] = {
    RELAY_MAIN_FILL, RELAY_MAIN_EXH,
    RELAY_S1_LEFT, RELAY_S1_RIGHT, RELAY_S1_TOP,
    RELAY_S2_LEFT, RELAY_S2_RIGHT, RELAY_S2_TOP
  };
  for (auto p : allPins) pinMode(p, OUTPUT);
  bothMainOff(); allPouchOff();

    // Stepper motor setup
  pinMode(STEPPER_DIR_PIN, OUTPUT);
  ledcSetup(0, 500, 8); // Channel 0, 500Hz, 8-bit
  ledcAttachPin(STEPPER_PWM_PIN, 0);


  BP32.setup(&onConnectedController, &onDisconnectedController);
  BP32.enableVirtualDevice(false);

  Serial.printf("FW: %s  BDAddr: ", BP32.firmwareVersion());
  const uint8_t* a = BP32.localBdAddress();
  Serial.printf("%02X:%02X:%02X:%02X:%02X:%02X\n", a[0],a[1],a[2],a[3],a[4],a[5]);

  Serial.println("Controls ready: D-pad for Main Body, L1 to toggle mode, R1 to toggle section.");
  
}

void loop() {
  if (BP32.update())
    processControllers();
  vTaskDelay(1);
}
