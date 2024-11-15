import asyncio
import json
import re
from bleak import BleakScanner, BleakClient
import argparse
import websockets

CHARACTERISTIC_UUID = 'f22535de-5375-44bd-8ca9-d0ea9ff9e410'
CONTROL_UNIT_NAME = 'QT Py ESP32-S3'

ble_client = None  # Global BLE client

def create_command(addr, mode, duty, freq):
    serial_group = addr // 30
    serial_addr = addr % 30
    byte1 = (serial_group << 2) | (mode & 0x01)
    byte2 = 0x40 | (serial_addr & 0x3F)  # 0x40 represents the leading '01'
    byte3 = 0x80 | ((duty & 0x0F) << 3) | (freq)  # 0x80 represents the leading '1'
    return bytearray([byte1, byte2, byte3])

async def setMotor(client, message):
    try:
        data_segments = re.findall(r'\{.*?\}', message)
        if not data_segments:
            return  # Skip empty or invalid messages

        data_chunks = [data_segments[i:i + 10] for i in range(0, len(data_segments), 10)]
        for data_chunk in data_chunks:
            command = bytearray()
            for data_segment in data_chunk:
                #print('data_segment = ', data_segment)
                data_parsed = json.loads(data_segment)
                command += create_command(data_parsed['addr'], data_parsed['mode'], data_parsed['duty'], data_parsed['freq'])

            padding_needed = 20 - len(data_chunk)
            command += bytearray([0xFF, 0xFF, 0xFF]) * padding_needed
            await client.write_gatt_char(CHARACTERISTIC_UUID, command)
            #print('Motor write = ', command)
    except Exception as e:
        print(f'Error in setMotor: {e}')

async def ble_task():
    global ble_client
    while True:
        devices = await BleakScanner.discover()
        for d in devices:
            if d.name and d.name == CONTROL_UNIT_NAME:
                print('Feather device found!')
                ble_client = BleakClient(d.address)
                await ble_client.connect()
                print(f'BLE connected to {d.address}')
                val = await ble_client.read_gatt_char(CHARACTERISTIC_UUID)
                print('Motor read = ', val)
                return  # Exit after successful connection
        await asyncio.sleep(5)  # Retry every 5 seconds

async def handle_connection(websocket):
    print('WebSocket connection established!')
    try:
        async for message in websocket:
            print(f"Received message: {message}")
            if ble_client and ble_client.is_connected:
                await setMotor(ble_client, message)
            else:
                print("BLE client not connected yet.")
                # Optionally send a message back to client
    except websockets.exceptions.ConnectionClosed as e:
        print(f'WebSocket closed: {e}')
    except Exception as e:
        print(f'Error in handle_connection: {e}')


        
async def main():
    # Initialize the argument parser
    parser = argparse.ArgumentParser(description="Read CHARACTERISTIC_UUID and CONTROL_UNIT_NAME from the command line.")

    # Add arguments with flags
    parser.add_argument(
        "-uuid", "--characteristic_uuid", required=False, type=str,
        default="f22535de-5375-44bd-8ca9-d0ea9ff9e410",
        help="The UUID of the characteristic"
    )
    
    parser.add_argument(
        "-name", "--control_unit_name", required=False, type=str, 
        default="QT Py ESP32-S3",
        help="The Bluetooth name of the control unit"
    )

    # Parse the arguments
    args = parser.parse_args()

    # Access and print the parameters
    print(f"CHARACTERISTIC_UUID: {args.characteristic_uuid}")
    print(f"CONTROL_UNIT_NAME: {args.control_unit_name}")

    global CHARACTERISTIC_UUID, CONTROL_UNIT_NAME
    CHARACTERISTIC_UUID = args.characteristic_uuid
    CONTROL_UNIT_NAME = args.control_unit_name

    # Start BLE scanning task
    ble_scanning_task = asyncio.create_task(ble_task())

    # Start the WebSocket server
    server = await websockets.serve(handle_connection, 'localhost', 9052)
    print("WebSocket server running on ws://localhost:9052")

    # Keep the server running
    await asyncio.Future()  # Run forever

if __name__ == "__main__":
    asyncio.run(main())
