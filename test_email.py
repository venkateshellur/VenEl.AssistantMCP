import json
import subprocess
import sys

print("Starting MCP Server...")

# Launch the newly compiled server DLL directly so stdout remains clean for JSON-RPC
proc = subprocess.Popen(
    ['dotnet', 'src/VenEl.AssistantMCP.Server/bin/Debug/net10.0/VenEl.AssistantMCP.Server.dll'], 
    stdin=subprocess.PIPE, 
    stdout=subprocess.PIPE, 
    stderr=subprocess.PIPE, 
    text=True
)

def send_request(req):
    proc.stdin.write(json.dumps(req) + "\n")
    proc.stdin.flush()

def read_response(expected_id=None):
    while True:
        line = proc.stdout.readline()
        if not line:
            return None
        line = line.strip()
        if not line.startswith('{'):
            continue
        try:
            msg = json.loads(line)
            if expected_id and msg.get("id") == expected_id:
                return msg
            if not expected_id:
                return msg
        except:
            pass

print("Initializing...")
send_request({
    "jsonrpc": "2.0", 
    "id": 1, 
    "method": "initialize", 
    "params": {
        "protocolVersion": "2024-11-05", 
        "capabilities": {}, 
        "clientInfo": {"name": "test", "version": "1.0"}
    }
})
read_response(1)
send_request({"jsonrpc": "2.0", "method": "notifications/initialized"})

print("Executing send_email tool...")
email_request = {
    "jsonrpc": "2.0", 
    "id": 2, 
    "method": "tools/call", 
    "params": {
        "name": "email_commands", 
        "arguments": {
            "args": {
                "action": "send_email", 
                "to": "venkatesh.ellur@gmail.com", 
                "subject": "Test from VenEl MCP", 
                "body": "Hello! The MCP email feature is working perfectly.",
                "isHtml": False
            }
        }
    }
}
send_request(email_request)

res = read_response(2)
print("\nResponse from Server:")
print(json.dumps(res, indent=2))

proc.stdin.close()
proc.wait()
print("\nSTDERR Output:")
print(proc.stderr.read())
