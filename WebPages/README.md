# Idea
Use ESP-NOW instead of BLE to communicate with the haptic control unit 

# New Architecture
Unity -> WebSocket -> Python Script -> Serial (USB) -> Gateway ESP32 -> ESP-NOW -> Haptic ESP32s

# Organisation 
- ESP_Setup contains the files needed to configure ESP-NOW
- unity-plotter is the main folder (running esp-now)
- unity-plotter_BLE is the old version running BLE 

To find your COM port on mac: ls /dev/tty.*