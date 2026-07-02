import json
import subprocess
import sys

proc = subprocess.Popen(['./publish/VenEl.MCPAssistant.Server'], stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)

def send_msg(msg):
    proc.stdin.write(json.dumps(msg) + "\n")
    proc.stdin.flush()
    line = proc.stdout.readline()
    return json.loads(line)

print(send_msg({"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {"protocolVersion": "2024-11-05", "capabilities": {}, "clientInfo": {"name": "test", "version": "1.0"}}}))
send_msg({"jsonrpc": "2.0", "method": "notifications/initialized"})

res = send_msg({"jsonrpc": "2.0", "id": 2, "method": "tools/call", "params": {"name": "mcp_venel_atlassian_commands", "arguments": {"Action": "confluence_search_pages", "Cql": "text~\"Teams\""}}})
print(json.dumps(res, indent=2))
