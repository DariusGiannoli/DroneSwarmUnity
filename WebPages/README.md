

# Idea
Use ESP-NOW instead of BLE to communicate with the haptic control unit 


# New Architecture
Unity -> WebSocket -> Python Script -> Serial (USB) -> Gateway ESP32 -> ESP-NOW -> Haptic ESP32s