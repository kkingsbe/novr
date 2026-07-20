"""Tiny CLI helper for the NOVR in-game MCP bridge.

The in-game HttpListener registers prefix "http://localhost:3334/" which Windows
binds to the IPv6 loopback only, and HttpListener rejects requests whose Host
header is not "localhost". So we connect to ::1 and send Host: localhost.

Usage:
    python mcp_call.py health
    python mcp_call.py tools
    python mcp_call.py call <tool_name> [json_args]
"""

import http.client
import json
import sys

PORT = 3334
HOST_HEADER = f"localhost:{PORT}"


def _request(method: str, path: str, payload: dict | None = None) -> str:
    conn = http.client.HTTPConnection("::1", PORT, timeout=30)
    body = json.dumps(payload) if payload is not None else None
    headers = {"Host": HOST_HEADER}
    if body is not None:
        headers["Content-Type"] = "application/json"
    conn.request(method, path, body=body, headers=headers)
    resp = conn.getresponse()
    data = resp.read().decode("utf-8", errors="replace")
    if resp.status != 200:
        raise SystemExit(f"HTTP {resp.status}: {data[:500]}")
    conn.close()
    return data


def main() -> None:
    mode = sys.argv[1] if len(sys.argv) > 1 else "health"
    if mode == "health":
        print(_request("GET", "/health"))
    elif mode == "tools":
        raw = _request("GET", "/tools")
        try:
            tools = json.loads(raw)
            items = tools if isinstance(tools, list) else tools.get("tools", tools)
            for t in items:
                if isinstance(t, dict):
                    print(f"{t.get('name')} - {str(t.get('description'))[:100]}")
                else:
                    print(t)
        except json.JSONDecodeError:
            print(raw)
    elif mode == "call":
        tool = sys.argv[2]
        args = json.loads(sys.argv[3]) if len(sys.argv) > 3 else {}
        out = _request("POST", "/invoke", {"tool": tool, "args": args})
        try:
            print(json.dumps(json.loads(out), indent=2)[:12000])
        except json.JSONDecodeError:
            print(out[:12000])
    else:
        print(__doc__)
        sys.exit(1)


if __name__ == "__main__":
    main()
