import socket
import struct
import json
import glob
import os

def get_unity_instance():
    appdata = os.environ.get('LOCALAPPDATA', '')
    pattern = os.path.join(appdata, 'unity-editor-mcp', 'instances', '*.json')
    files = glob.glob(pattern)
    # Sort by newest modified time
    files.sort(key=os.path.getmtime, reverse=True)
    for f in files:
        with open(f, 'r', encoding='utf-8') as fp:
            data = json.load(fp)
            if 'D:/Ae/GameAE'.lower() in data.get('projectPath', '').lower():
                return data
    if files:
        with open(files[0], 'r', encoding='utf-8') as fp:
            return json.load(fp)
    return {'port': 7351, 'pid': 30048, 'projectPath': 'D:/Ae/GameAE'}

def send_command(cmd_type, params=None, timeout=30.0):
    inst = get_unity_instance()
    if not inst:
        raise RuntimeError("No Unity instance found in registry")
    port = inst['port']
    
    req = {
        "id": "1",
        "type": cmd_type,
        "params": params or {}
    }
    payload = json.dumps(req).encode('utf-8')
    header = struct.pack('>I', len(payload))
    
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    s.settimeout(timeout)
    s.connect(('127.0.0.1', port))
    try:
        s.sendall(header + payload)
        
        # Read reply header (4 bytes)
        raw_len = s.recv(4)
        if len(raw_len) < 4:
            raise RuntimeError("Truncated response header")
        resp_len = struct.unpack('>I', raw_len)[0]
        
        # Read payload
        chunks = []
        bytes_recd = 0
        while bytes_recd < resp_len:
            chunk = s.recv(min(resp_len - bytes_recd, 4096))
            if not chunk:
                break
            chunks.append(chunk)
            bytes_recd += len(chunk)
            
        resp_body = b''.join(chunks).decode('utf-8')
        return json.loads(resp_body)
    finally:
        s.close()

if __name__ == '__main__':
    res = send_command("get_editor_state")
    print(json.dumps(res, indent=2))
