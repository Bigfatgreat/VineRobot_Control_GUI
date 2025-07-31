#include <Arduino.h>

#define btnpin 8

// Pin definitions for the stepper motor control
const int8_t PUL = 5;
const int8_t DIR = 6;
const int8_t ENA = 12;

// Debounce delay and timing
const long debounceDelay = 50;
unsigned long lastDebounceTime = 0;

// Stepper motor control variables
unsigned long currentMicros;
unsigned long stepMicros;
int stepDelay = 2;  // Time in microseconds between steps (adjust for motor speed)
bool nextStep = LOW; // Pulse signal state

long distanceTraveled = 0;  // Track the distance traveled in terms of steps
long currentPosition = 0;   // Current position of the motor

// Task management flags
bool taskRunning = false;    // Is the motor moving or not
long targetSteps = 0;        // Target number of steps to complete
unsigned long pressStartTime = 0;  // Time when button was pressed

// Button state tracking
typedef struct {
    bool currentState;
    bool lastState;
} ButtonState;

ButtonState buttonRead(int readPin) {
    static int lastButtonState = HIGH;
    static int buttonState = HIGH;
    static unsigned long lastDebounceTime = 0;

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
  pinMode(btnpin, INPUT);
  pinMode(PUL, OUTPUT);
  pinMode(DIR, OUTPUT);
  pinMode(ENA, OUTPUT);

  // Start with the motor driver disabled
  digitalWrite(ENA, HIGH);
  digitalWrite(PUL, LOW);  // Make sure pulse is low at the beginning
  digitalWrite(DIR, LOW);

  Serial.begin(9600);

  // Setup LEDC (PWM) for controlling the pulse pin
  ledcSetup(0, 1000, 8);  // Channel 0, 1 kHz frequency, 8-bit resolution (0-255)
  ledcAttachPin(PUL, 0);   // Attach the pulse pin (PUL) to channel 0 for PWM
}

void loop() {
    // IN FUTURE PLAN mapping analog controller to frequency? How do we move robot more TORQUE or SPD?
// Not sure if Controller should map to frequency or PWM? What really effects the movement
  ButtonState btn = buttonRead(btnpin);
  currentMicros = micros();

  if (btn.currentState == LOW) {
    // If  button pressed, start the timer
    if (btn.lastState == HIGH && !taskRunning) {
        pressStartTime = millis();  // Record the start time when the button is pressed
        taskRunning = true;          // Start the task
        targetSteps = 0;             // Reset target steps on new press
    }

    //  BTN press duration mapping to number of steps to move
    unsigned long pressDuration = millis() - pressStartTime;
    targetSteps = map(pressDuration, 100, 3000, 0, 800);  // 100ms to 3000ms maps to 0 to 800 steps
    targetSteps = constrain(targetSteps, 0, 800);  // Limit maximum steps

    // Enable motor
    digitalWrite(ENA, LOW);

    // Start the pulse signal with PWM
    if (currentMicros - stepMicros >= stepDelay) {
        stepMicros = currentMicros; // Update time for next pulse

        // Toggle pulse state to create a pulse on the motor driver
        nextStep = !nextStep;

        // Adjust PWM signal for step pulse
        if (nextStep == LOW) {
            ledcWrite(0, 127);  // Set pulse high (50% duty cycle)
        } else {
            ledcWrite(0, 0);    // Set pulse low (0% duty cycle)
        }

        // Increment the distance moved
        if (nextStep == LOW) {
            distanceTraveled++;  // Count each step when the pulse goes low
            Serial.print("Distance Traveled: ");
            Serial.println(distanceTraveled);
        }
    }
  }
  else if (btn.currentState == HIGH && taskRunning) {
    // When the button is released, stop the pulse and disable the motor
    ledcWrite(0, 0);  // Stop PWM signal (no pulse)

    // Check if we have reached the target steps
    if (distanceTraveled >= targetSteps) {
        digitalWrite(ENA, HIGH);  // Disable the motor driver
        taskRunning = false;      // Mark the task as done
        Serial.print("Task Done. Total Distance: ");
        Serial.println(distanceTraveled);
    }
  }

  // Ensure that the motor keeps running until target is reached even after button release
  if (taskRunning && distanceTraveled < targetSteps) {
    // Continue generating pulses until the target position is reached
    if (currentMicros - stepMicros >= stepDelay) {
        stepMicros = currentMicros;
        nextStep = !nextStep;

        if (nextStep == LOW) {
            ledcWrite(0, 127);  // Set pulse high (50% duty cycle)
        } else {
            ledcWrite(0, 0);    // Set pulse low (0% duty cycle)
        }

        if (nextStep == LOW) {
            distanceTraveled++;
            Serial.print("Distance Traveled: ");
            Serial.println(distanceTraveled);
        }
    }
  }
}
