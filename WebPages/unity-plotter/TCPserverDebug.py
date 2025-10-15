import asyncio
import json
import re
import time
import argparse

import websockets
import aiohttp
import serial 
import sys

# Holds serial connection
ser = None

# --- Message batching for each controller ---
message_batch = {}          # A single dictionary for all messages
batch_first_time = None     # A single timestamp
batch_lock = asyncio.Lock() # A single lock

IMMEDIATE_THRESHOLD = 20  # New constant for immediate flush
DEBUG = False

def create_command(addr, mode, duty, freq):
    """
    Create a 3-byte command for the motor controller.

    The command is built from:
    - serial_group: addr // 30
    - serial_addr:  addr % 30
    - mode, duty, freq are encoded into the bits as required.
    """
    serial_group = addr // 30
    serial_addr = addr % 30
    byte1 = (serial_group << 2) | (mode & 0x01)
    byte2 = 0x40 | (serial_addr & 0x3F)  # 0x40 represents the leading '01'
    byte3 = 0x80 | ((duty & 0x0F) << 3) | (freq)  # 0x80 represents the leading '1'
    return bytearray([byte1, byte2, byte3])

def send_commands_via_serial(serial_conn, combined_message):
    """
    Parses JSON messages, creates byte commands, and sends them over the serial port.
    """
    # Check if the serial port is connected and open
    if not serial_conn or not serial_conn.is_open:
        print("Serial port not available. Cannot send commands.")
        return

    try:
        # Find all individual JSON command strings (e.g., "{'addr':...}")
        data_segments = re.findall(r'\{.*?\}', combined_message)
        if not data_segments:
            return

        # Prepare a byte array to hold all commands
        all_commands = bytearray()
        for segment in data_segments:
            data_parsed = json.loads(segment)
            # Use the existing translator function to create the 3-byte command
            command = create_command(
                data_parsed['addr'],
                data_parsed['mode'],
                data_parsed['duty'],
                data_parsed['freq']
            )
            all_commands += command

        # If we have commands, send them all at once
        if all_commands:
            serial_conn.write(all_commands)

    except Exception as e:
        print(f"❌ Error in send_commands_via_serial: {e}")


async def handle_connection(websocket):
    """
    Starts the background tasks for processing the unified message batch.
    """
    print('✅ WebSocket connection established!')
    # Start background tasks:
    asyncio.create_task(collect_messages(websocket))
    # Note: You will create this new unified timer function in the next step.
    asyncio.create_task(process_batch_timer())
    await websocket.wait_closed()

async def collect_messages(websocket):
    """
    Collects all incoming messages from the WebSocket into a single, unified batch.
    """
    global message_batch, batch_first_time
    try:
        async for message in websocket:
            try:
                msg_obj = json.loads(message)
            except Exception as e:
                print(f"Error parsing JSON: {e}")
                continue

            addr = msg_obj.get('addr')
            if addr is None:
                continue

            # --- Simplified Logic ---
            # No if/else, no address normalization. All messages go into the same batch.
            async with batch_lock:
                if not message_batch:
                    batch_first_time = time.time()
                message_batch[addr] = message  # Store the original message string

                # Check if the batch has reached the immediate flush threshold
                if len(message_batch) >= IMMEDIATE_THRESHOLD:
                    combined_message = ''.join(message_batch.values())
                    message_batch.clear()
                    batch_first_time = None
                    # Note: You will need to create this new unified flush function
                    asyncio.create_task(process_batch_immediate_flush(combined_message))

    except websockets.exceptions.ConnectionClosed as e:
        print(f'WebSocket closed: {e}')
    except Exception as e:
        print(f'Error in collect_messages: {e}')


async def process_batch_immediate_flush(combined_message):
    """
    Immediately processes and sends the unified message batch.
    """
    print(f"Flushing immediate batch ({IMMEDIATE_THRESHOLD} messages)...")
    send_commands_via_serial(ser, combined_message)
    # Also send the data to the logging server
    asyncio.create_task(send_to_server(combined_message))

async def process_batch_timer():
    """
    Periodically checks the unified message batch and flushes it if the
    size or time threshold is met.
    """
    global message_batch, batch_first_time
    THRESHOLD = 10  # Flush if we have this many messages
    TIMEOUT = 0.2   # Or flush if the oldest message is this old (in seconds)

    while True:
        await asyncio.sleep(0.05)  # Check every 50ms
        combined_message = None

        async with batch_lock:
            if message_batch:
                now = time.time()
                if len(message_batch) >= THRESHOLD or (now - batch_first_time >= TIMEOUT):
                    combined_message = ''.join(message_batch.values())
                    message_batch.clear()
                    batch_first_time = None

        if combined_message:
            print(f"Flushing timed batch ({len(combined_message)//len(next(iter(combined_message.values())))} messages)...")
            send_commands_via_serial(ser, combined_message)
            # Also send the data to the logging server
            asyncio.create_task(send_to_server(combined_message))


async def send_to_server(message):
    """
    POST the command message to a local server for logging/debugging.
    """
    try:
        async with aiohttp.ClientSession() as session:
            await session.post('http://localhost:5000/commands', json={'command': message, 'timestamp': time.time()})
    except Exception as e:
        # This prevents the script from crashing if the logging server is down
        print(f"⚠️  Could not connect to logging server: {e}")


async def main():
    """
    Initializes the serial connection to the gateway and starts the WebSocket server.
    """
    global ser  # Make the serial object accessible to other functions

    # --- Initialize Serial Connection ---
    try:
        # !!! IMPORTANT: REPLACE 'COM3' WITH YOUR ESP32's PORT NAME !!!
        # On Windows: 'COM3', 'COM4', etc.
        # On macOS: '/dev/tty.usbmodemXXXX'
        # On Linux: '/dev/ttyUSB0' or '/dev/ttyACM0'
        GATEWAY_PORT = 'COM3'
        BAUD_RATE = 115200  # Must match your ESP32 gateway firmware

        ser = serial.Serial(GATEWAY_PORT, BAUD_RATE, timeout=1)
        print(f"✅ Successfully connected to ESP-NOW gateway on {GATEWAY_PORT}")

    except serial.SerialException as e:
        print(f"❌ FATAL ERROR: Could not open serial port {GATEWAY_PORT}. {e}", file=sys.stderr)
        print("    Please check that the gateway is connected and you chose the correct port name.")
        sys.exit(1)

    # Start the WebSocket server
    server = await websockets.serve(handle_connection, 'localhost', 9052)
    print("✅ WebSocket server running on ws://localhost:9052")
    print("🚀 System is ready. Waiting for connection from Unity...")

    # Run forever
    await asyncio.Future()

if __name__ == "__main__":
    asyncio.run(main())