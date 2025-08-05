# ตัวอย่างการรวมโค้ด
import cv2
import serial
import time

# กำหนดค่าสำหรับ Serial
port_name = 'COM4'
baud_rate = 115200
ser = serial.Serial(port_name, baud_rate)
time.sleep(2)

# โค้ดสำหรับสตรีมมิ่งกล้อง
esp32_cam_url = "http://192.168.10.156:81/stream"
cap = cv2.VideoCapture(esp32_cam_url)

while True:
    ret, frame = cap.read()
    if not ret:
        break
    
    # ในส่วนนี้คือโค้ด Object Detection ของคุณ
    # เช่น ถ้าตรวจจับ "คน" ได้
    # ให้ส่งคำสั่ง Serial
    if some_object_is_detected:
        command = "LED_ON\n"
        ser.write(command.encode('utf-8'))
        print("Sent command to ESP32: LED_ON")

    cv2.imshow("ESP32-CAM Stream", frame)
    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

cap.release()
ser.close()
cv2.destroyAllWindows()