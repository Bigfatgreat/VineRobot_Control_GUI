// Low level Programming ESP32 to control stepper motor, before we dig deep lets begin with a basics. We control the amount of pulse signal being send (distance travel) of stepper motor while keeping track of them BY pressing button -> IF button pressed timer start -> when release timer stop record them map to the amount of signal pulse (count) being sent -> PERFORM ACTION -> Keep track. We try to make the close loop control if possible. This is our current code try implement what we have ex. Debounce to take advantage, #include <Arduino.h>
/*
/#include <Arduino.h>
#define btnpin 36
unsigned long lastDebounceTime = 0;
typedef struct {
    bool currentState;
    bool lastState;
} ButtonState;


const int8_t PUL = 32;
const int8_t DIR = 33;
const int8_t ENA = 12;

unsigned long currentMicros;
unsigned long stepMicros;
int stepDelay = 2;
bool nextStep = LOW;
const long debounceDelay = 100;


ButtonState buttonRead(int readPin) {
    static int lastButtonState = HIGH;
    static int buttonState = HIGH;
    static unsigned long lastDebounceTime = 0;
    const unsigned long debounceDelay = 50;

    int reading = digitalRead(readPin);

    // Save last stable state before potentially updating
    ButtonState result;
    result.lastState = buttonState;

    // Debounce logic
    if (reading != lastButtonState) {
        lastDebounceTime = millis();
    }

    if ((millis() - lastDebounceTime) > debounceDelay) {
        if (reading != buttonState) {
            buttonState = reading;
        }
    }

    lastButtonState = reading;
    result.currentState = buttonState;

    return result;
}


void setup() {
  // put your setup code here, to run once:
  pinMode(btnpin,INPUT);

  pinMode(PUL, OUTPUT);
  pinMode(DIR, OUTPUT);
  pinMode(ENA, OUTPUT);

  // Start with the motor driver disabled
  digitalWrite(ENA, HIGH);
  digitalWrite(PUL, LOW);  // Make sure pulse is low at the beginning
  digitalWrite(DIR, LOW);


  
  // Setup LEDC (PWM) for controlling the pulse pin
  ledcSetup(0, 500, 8);  // Channel 0, 1 kHz frequency, 8-bit resolution (0-255)
  ledcAttachPin(PUL, 0);   // Attach the pulse pin (PUL) to channel 0 for PWM

  Serial.begin(115200);
}

void loop() {
  ButtonState btn = buttonRead(btnpin);
  currentMicros = micros();
  if (btn.currentState == LOW) {
      ledcWrite(0, 127);
      Serial.println("kuy");
  }
  else {
    ledcWrite(0, 0);
   Serial.println("kuyno");
  }
  /*else if(btn.lastState == LOW && btn.currentState == LOW) {
      Serial.println("wer");
  }
}*/
