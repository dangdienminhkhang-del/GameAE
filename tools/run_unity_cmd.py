from tools.unity_bridge import send_command
import json

with open(r'Library/PackageCache/com.burakk.unity-editor-mcp@80e2fb90f1d8/Core/CommandCatalog.g.cs', 'r', encoding='utf-8') as f:
    text = f.read()

import re
cmds = re.findall(r'"([a-z0-9_]+)"', text)
relevant = [c for c in cmds if any(k in c for k in ['asset', 'refresh', 'compile', 'script', 'menu', 'execute'])]
print("Matching commands in catalog:")
print(relevant)
