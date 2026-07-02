import json
import subprocess
import sys

proc = subprocess.Popen(['./publish/VenEl.MCPAssistant.Server'], stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)

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

send_request({"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {"protocolVersion": "2024-11-05", "capabilities": {}, "clientInfo": {"name": "test", "version": "1.0"}}})
read_response(1)
send_request({"jsonrpc": "2.0", "method": "notifications/initialized"})

send_request({"jsonrpc": "2.0", "id": 2, "method": "tools/call", "params": {"name": "mcp_venel_atlassian_commands", "arguments": {"Action": "confluence_search_pages", "Cql": "text~\"AssistantMCP\""}}})
res = read_response(2)

proc.stdin.close()
proc.wait()
print(proc.stderr.read())
