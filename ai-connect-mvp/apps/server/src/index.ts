import Fastify from "fastify";
import cors from "@fastify/cors";
import { WebSocketServer, WebSocket } from "ws";
import { randomUUID } from "node:crypto";
import {
  createSphereSchema, createBoxSchema, createCylinderSchema,
  moveObjectSchema, deleteObjectSchema, listObjectsSchema,
  type SceneCommand, type SceneCommandAck, type SceneCommandName
} from "../../../packages/contracts/src/index.ts";

const HOST = "127.0.0.1";
const PORT = Number(process.env.AURUM_API_PORT ?? 4317);
const app = Fastify({ logger: true, bodyLimit: 64 * 1024 });
await app.register(cors, {
  origin: ["http://127.0.0.1:5173", "http://localhost:5173"]
});

let viewport: WebSocket | null = null;
let sceneRevision = 0;
const pending = new Map<string, { resolve: (ack: SceneCommandAck) => void; timer: NodeJS.Timeout }>();
let commandQueue = Promise.resolve();

const allowedOrigins = new Set(["http://127.0.0.1:5173", "http://localhost:5173"]);
const wss = new WebSocketServer({
  server: app.server,
  path: "/scene",
  verifyClient: (info, done) => {
    done(allowedOrigins.has(info.origin), 403, "Origin not allowed");
  }
});
wss.on("connection", (socket) => {
  if (viewport && viewport.readyState === WebSocket.OPEN) viewport.close(4001, "Viewport replaced");
  viewport = socket;
  app.log.info("3D viewport connected");

  socket.on("message", (raw) => {
    let ack: SceneCommandAck;
    try {
      ack = JSON.parse(raw.toString()) as SceneCommandAck;
    } catch {
      return;
    }
    const item = pending.get(ack.commandId);
    if (!item) return;
    clearTimeout(item.timer);
    pending.delete(ack.commandId);
    if (ack.success) sceneRevision = Math.max(sceneRevision + 1, ack.sceneRevision);
    item.resolve(ack);
  });

  socket.on("close", () => {
    // A replaced viewport must not cancel commands already sent to its successor.
    if (viewport !== socket) return;
    viewport = null;
    for (const [id, item] of pending) {
      clearTimeout(item.timer);
      item.resolve({ commandId: id, success: false, error: "Viewport disconnected", sceneRevision });
      pending.delete(id);
    }
  });
});

async function dispatch(operation: SceneCommandName, payload: unknown) {
  const run = async () => {
    if (!viewport || viewport.readyState !== WebSocket.OPEN) {
      throw new Error("No hay un viewport conectado. Abre la aplicación web primero.");
    }
    const command: SceneCommand = { commandId: randomUUID(), operation, payload };
    const result = await new Promise<SceneCommandAck>((resolve, reject) => {
      const timer = setTimeout(() => {
        pending.delete(command.commandId);
        reject(new Error("Timeout esperando confirmación del viewport"));
      }, 8_000);
      pending.set(command.commandId, { resolve, timer });
      viewport!.send(JSON.stringify(command), (error) => {
        if (error) {
          clearTimeout(timer);
          pending.delete(command.commandId);
          reject(error);
        }
      });
    });
    if (!result.success) throw new Error(result.error ?? "La escena rechazó el comando");
    return { ...((result.result ?? {}) as object), sceneRevision };
  };
  const current = commandQueue.then(run, run);
  commandQueue = current.then(() => undefined, () => undefined);
  return current;
}

app.get("/health", async () => ({
  status: "ok",
  viewportConnected: Boolean(viewport && viewport.readyState === WebSocket.OPEN),
  sceneRevision
}));

app.post("/commands/create-sphere", async (request) => dispatch("create_sphere", createSphereSchema.parse(request.body)));
app.post("/commands/create-box", async (request) => dispatch("create_box", createBoxSchema.parse(request.body)));
app.post("/commands/create-cylinder", async (request) => dispatch("create_cylinder", createCylinderSchema.parse(request.body)));
app.post("/commands/move-object", async (request) => dispatch("move_object", moveObjectSchema.parse(request.body)));
app.post("/commands/delete-object", async (request) => dispatch("delete_object", deleteObjectSchema.parse(request.body)));
app.post("/commands/list-objects", async (request) => dispatch("list_objects", listObjectsSchema.parse(request.body)));

app.setErrorHandler((error, _request, reply) => {
  app.log.error(error);
  const status = error.name === "ZodError" ? 400 : 500;
  void reply.code(status).send({ error: error.message });
});

await app.listen({ host: HOST, port: PORT });
app.log.info(`AURUM API listening at http://${HOST}:${PORT}`);
