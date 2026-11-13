import { useCallback, useState } from "react";
import { Box, Circle, Cylinder, Link2, Move3D, Trash2, List, Activity, Clipboard, Play, Sparkles, AlertCircle, CheckCircle2 } from "lucide-react";
import { SceneViewport } from "./SceneViewport";
import type { SceneObjectRecord } from "../../../packages/contracts/src/index";

type Operation = "create_sphere" | "create_box" | "create_cylinder" | "move_object" | "delete_object" | "list_objects";

const endpoint: Record<Operation, string> = {
  create_sphere: "/commands/create-sphere",
  create_box: "/commands/create-box",
  create_cylinder: "/commands/create-cylinder",
  move_object: "/commands/move-object",
  delete_object: "/commands/delete-object",
  list_objects: "/commands/list-objects"
};

const starterCommand = JSON.stringify({
  operation: "create_sphere",
  payload: { diameterMm: 20, center: { x: 0, y: 0, z: 0 } }
}, null, 2);

const chatPrompt = `Eres el intérprete de instrucciones de AURUM AI CONNECT. Convierte mi petición en UNA operación y devuelve únicamente JSON válido, sin Markdown ni explicaciones.
Formato:
{
  "operation": "create_sphere | create_box | create_cylinder | move_object | delete_object | list_objects",
  "payload": {}
}
Reglas:
- Todas las dimensiones y coordenadas están en milímetros.
- create_sphere: diameterMm positivo; center {x,y,z} opcional.
- create_box: widthMm, depthMm y heightMm positivos.
- create_cylinder: radiusMm y heightMm positivos; topRadiusMm opcional.
- move_object: objectId, translation {x,y,z}, mode "relative" o "absolute".
- delete_object: objectId y confirm true.
- list_objects: includeTransforms true.
- Nunca inventes objectId. Si no conoces el ID, devuelve primero list_objects y espera a que te comparta su resultado.`;

export default function App() {
  const [objects, setObjects] = useState<SceneObjectRecord[]>([]);
  const [connected, setConnected] = useState(false);
  const [activity, setActivity] = useState<string[]>(["Esperando conexión con el viewport…"]);
  const [commandText, setCommandText] = useState(starterCommand);
  const [executing, setExecuting] = useState(false);
  const [commandResult, setCommandResult] = useState<{ ok: boolean; message: string } | null>(null);

  const onObjectsChange = useCallback((items: SceneObjectRecord[]) => setObjects(items), []);
  const onConnectionChange = useCallback((value: boolean) => setConnected(value), []);
  const onActivity = useCallback((message: string) => setActivity(old => [message, ...old].slice(0, 8)), []);

  const copyPrompt = async () => {
    await navigator.clipboard.writeText(chatPrompt);
    setCommandResult({ ok: true, message: "Instrucción para ChatGPT copiada." });
  };

  const executeCommand = async () => {
    if (!connected) {
      setCommandResult({ ok: false, message: "Primero inicia el backend y conecta el viewport." });
      return;
    }
    setExecuting(true);
    setCommandResult(null);
    try {
      const parsed: unknown = JSON.parse(commandText);
      if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) throw new Error("El JSON debe ser un objeto.");
      const command = parsed as { operation?: Operation; payload?: unknown };
      if (!command.operation || !Object.prototype.hasOwnProperty.call(endpoint, command.operation)) throw new Error("Operación no reconocida. Usa una de las seis herramientas disponibles.");
      if (!command.payload || typeof command.payload !== "object" || Array.isArray(command.payload)) throw new Error("Falta un objeto payload válido.");
      const response = await fetch(`http://127.0.0.1:4317${endpoint[command.operation]}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(command.payload),
        signal: AbortSignal.timeout(10000)
      });
      const data = await response.json() as { error?: string; [key: string]: unknown };
      if (!response.ok) throw new Error(data.error ?? `Error HTTP ${response.status}`);
      setCommandResult({ ok: true, message: `Operación ${command.operation} ejecutada y confirmada por la escena.` });
      onActivity(`ChatGPT Web → ${command.operation} · confirmación recibida`);
    } catch (error) {
      setCommandResult({ ok: false, message: error instanceof Error ? error.message : "No se pudo ejecutar la operación." });
    } finally {
      setExecuting(false);
    }
  };

  return (
    <main className="shell">
      <header className="topbar">
        <div className="brand"><span className="brand-mark">A</span><div><strong>AURUM <em>AI CONNECT</em></strong><small>GEOMETRY CONTROL · MVP 0.2</small></div></div>
        <div className={connected ? "status online" : "status"}><span />{connected ? "VIEWPORT CONECTADO" : "ESPERANDO VIEWPORT"}</div>
      </header>

      <section className="intro">
        <div><p className="eyebrow">CHATGPT WEB · JSON BRIDGE · THREE.JS</p><h1>Tu IA. Tu escena 3D.</h1><p>Usa ChatGPT desde el navegador, pega la operación y observa cómo cambia la escena.</p></div>
        <div className="unit-pill">UNIDADES <b>mm</b></div>
      </section>

      <section className="workspace">
        <div className="scene-panel">
          <div className="panel-heading"><span>SCENE VIEWPORT</span><span>THREE.JS · LIVE</span></div>
          <SceneViewport onObjectsChange={onObjectsChange} onConnectionChange={onConnectionChange} onActivity={onActivity} />
          <div className="scene-footer"><span><Activity size={14}/> {connected ? "Canal de comandos activo" : "Inicia el backend para conectar"}</span><span>{objects.length} objetos</span></div>
        </div>

        <aside className="side-panel">
          <section className="card bridge-card">
            <div className="card-title"><span><Sparkles size={14}/> PUENTE CHATGPT WEB</span><span className="free-label">SIN API</span></div>
            <p className="bridge-copy">Pide a ChatGPT que genere el JSON, cópialo aquí y ejecútalo en la escena local.</p>
            <button className="copy-prompt" onClick={copyPrompt}><Clipboard size={14}/> Copiar instrucción para ChatGPT</button>
            <label className="command-label" htmlFor="command-json">OPERACIÓN ESTRUCTURADA</label>
            <textarea id="command-json" className="command-editor" value={commandText} onChange={event => setCommandText(event.target.value)} spellCheck={false} />
            <button className="execute-command" onClick={executeCommand} disabled={executing || !connected}>
              <Play size={14} fill="currentColor"/> {executing ? "Ejecutando…" : "Ejecutar en la escena"}
            </button>
            {commandResult && <div className={commandResult.ok ? "command-result success" : "command-result error"}>{commandResult.ok ? <CheckCircle2 size={15}/> : <AlertCircle size={15}/>}<span>{commandResult.message}</span></div>}
            <p className="bridge-note">La operación se valida en el backend y el viewport confirma su ejecución. No se envía información a una API de IA.</p>
          </section>

          <section className="card">
            <div className="card-title"><span>OBJETOS DE ESCENA</span><span>{objects.length.toString().padStart(2,"0")}</span></div>
            {objects.length === 0 ? <div className="empty"><Box size={25}/><p>La escena está vacía</p><small>Conecta el viewport y ejecuta tu primera operación.</small></div> : <div className="object-list">{objects.map(item => <div className="object-row" key={item.id}><span className="object-icon">{item.type === "sphere" ? <Circle size={17}/> : item.type === "box" ? <Box size={17}/> : <Cylinder size={17}/>}</span><div><b>{item.name}</b><small>{item.type} · {Object.values(item.dimensionsMm).join(" × ")} mm</small><small className="muted-id">{item.id}</small></div></div>)}</div>}
            <div className="hint"><List size={14}/> Usa <code>list_objects</code> para consultar IDs.</div>
          </section>

          <section className="card">
            <div className="card-title"><span>HERRAMIENTAS MCP</span><Link2 size={15}/></div>
            <div className="tool-row"><Circle size={15}/> create_sphere</div>
            <div className="tool-row"><Box size={15}/> create_box</div>
            <div className="tool-row"><Cylinder size={15}/> create_cylinder</div>
            <div className="tool-row"><Move3D size={15}/> move_object</div>
            <div className="tool-row"><Trash2 size={15}/> delete_object</div>
            <div className="tool-row"><List size={15}/> list_objects</div>
          </section>

          <section className="card">
            <div className="card-title"><span>ACTIVIDAD</span><Activity size={15}/></div>
            <div className="activity-list">{activity.map((line, index) => <p key={index}>{line}</p>)}</div>
          </section>
        </aside>
      </section>
      <footer>FASE 1 · PUENTE MANUAL CHATGPT WEB · SIN COSTE DE API · MOTOR ÓPTICO AURUM RENDER AISLADO</footer>
    </main>
  );
}
