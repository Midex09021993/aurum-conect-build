import { McpServer } from "@modelcontextprotocol/server";
import { serveStdio } from "@modelcontextprotocol/server/stdio";
import { z } from "zod";
import {
  createSphereSchema, createBoxSchema, createCylinderSchema,
  moveObjectSchema, deleteObjectSchema, listObjectsSchema
} from "../../contracts/src/index.ts";

const API = process.env.AURUM_API_URL ?? "http://127.0.0.1:4317";

async function callApi<T>(path: string, payload: unknown): Promise<T> {
  const response = await fetch(`${API}${path}`, {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify(payload),
    signal: AbortSignal.timeout(10_000)
  });
  const data = await response.json() as T & { error?: string };
  if (!response.ok) throw new Error(data.error ?? `AURUM API HTTP ${response.status}`);
  return data;
}

function createServer() {
  const server = new McpServer({ name: "aurum-ai-connect", version: "0.1.0" });

  server.registerTool("create_sphere", {
    description: "Crea una esfera en el viewport activo. Todas las medidas están en milímetros.",
    inputSchema: createSphereSchema
  }, async input => ({
    content: [{ type: "text", text: JSON.stringify(await callApi("/commands/create-sphere", input)) }]
  }));

  server.registerTool("create_box", {
    description: "Crea una caja o cubo en el viewport activo. Medidas en milímetros.",
    inputSchema: createBoxSchema
  }, async input => ({
    content: [{ type: "text", text: JSON.stringify(await callApi("/commands/create-box", input)) }]
  }));

  server.registerTool("create_cylinder", {
    description: "Crea un cilindro recto o cónico. Radio y altura en milímetros.",
    inputSchema: createCylinderSchema
  }, async input => ({
    content: [{ type: "text", text: JSON.stringify(await callApi("/commands/create-cylinder", input)) }]
  }));

  server.registerTool("move_object", {
    description: "Mueve un objeto existente en milímetros. Usa relative para desplazar o absolute para fijar posición.",
    inputSchema: moveObjectSchema
  }, async input => ({
    content: [{ type: "text", text: JSON.stringify(await callApi("/commands/move-object", input)) }]
  }));

  server.registerTool("delete_object", {
    description: "Elimina un objeto existente. Requiere confirm=true y el ID exacto.",
    inputSchema: deleteObjectSchema
  }, async input => ({
    content: [{ type: "text", text: JSON.stringify(await callApi("/commands/delete-object", input)) }]
  }));

  server.registerTool("list_objects", {
    description: "Lista los objetos existentes en la escena 3D activa, con sus IDs, dimensiones y posiciones.",
    inputSchema: listObjectsSchema
  }, async input => ({
    content: [{ type: "text", text: JSON.stringify(await callApi("/commands/list-objects", input)) }]
  }));

  return server;
}

void serveStdio(createServer);
console.error("AURUM AI CONNECT MCP server running over stdio");
