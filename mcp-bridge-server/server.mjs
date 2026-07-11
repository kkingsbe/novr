import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
} from "@modelcontextprotocol/sdk/types.js";

const BRIDGE_PORT = parseInt(process.env.NOVR_MCP_BRIDGE_PORT || "3334", 10);
const BRIDGE_BASE = `http://localhost:${BRIDGE_PORT}`;

const server = new Server(
  { name: "novr-mcp-bridge", version: "0.1.0" },
  { capabilities: { tools: {} } }
);

let toolCache = [];

async function fetchTools() {
  const res = await fetch(`${BRIDGE_BASE}/tools`);
  if (!res.ok) throw new Error(`Bridge returned ${res.status}`);
  return await res.json();
}

async function invokeTool(name, args) {
  const res = await fetch(`${BRIDGE_BASE}/invoke`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ tool: name, args: args ?? {} }),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({ error: res.statusText }));
    throw new Error(err.error || `Bridge returned ${res.status}`);
  }
  const data = await res.json();
  return data.result;
}

server.setRequestHandler(ListToolsRequestSchema, async () => {
  try {
    toolCache = await fetchTools();
  } catch (e) {
    return { tools: toolCache.length > 0 ? toolCache : [] };
  }

  return {
    tools: toolCache.map((t) => ({
      name: t.name,
      description: t.description || "",
      inputSchema: t.inputSchema || {
        type: "object",
        properties: {},
        required: [],
      },
    })),
  };
});

function normalizeContent(result) {
  if (typeof result === "string") return [{ type: "text", text: result }];
  if (Array.isArray(result)) {
    return result.map((block) => {
      if (block.type === "image") {
        return {
          type: "image",
          data: block.data,
          mimeType: block.mimeType || "image/png",
        };
      }
      return { type: "text", text: String(block.text ?? "") };
    });
  }
  return [{ type: "text", text: String(result ?? "") }];
}

server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;
  try {
    const result = await invokeTool(name, args);
    return { content: normalizeContent(result) };
  } catch (e) {
    return {
      isError: true,
      content: [{ type: "text", text: `Error: ${e.message}` }],
    };
  }
});

async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
}

main().catch((e) => {
  console.error("Fatal:", e);
  process.exit(1);
});
