import { useCallback, useState } from "react";
import { Box, Circle, Cylinder, Link2, Move3D, Trash2, List, Activity } from "lucide-react";
import { SceneViewport } from "./SceneViewport";
import type { SceneObjectRecord } from "../../../packages/contracts/src/index";

export default function App() {
  const [objects, setObjects] = useState<SceneObjectRecord[]>([]);
  const [connected, setConnected] = useState(false);
  const [activity, setActivity] = useState<string[]>(["Esperando conexión con el viewport…"]);

  const onObjectsChange = useCallback((items: SceneObjectRecord[]) => setObjects(items), []);
  const onConnectionChange = useCallback((value: boolean) => setConnected(value), []);
  const onActivity = useCallback((message: string) => setActivity(old => [message, ...old].slice(0, 8)), []);

  return (
    <main className="shell">
      <header className="topbar">
        <div className="brand"><span className="brand-mark">A</span><div><strong>AURUM <em>AI CONNECT</em></strong><small>GEOMETRY CONTROL · MVP 0.1</small></div></div>
        <div className={connected ? "status online" : "status"}><span />{connected ? "VIEWPORT CONECTADO" : "ESPERANDO VIEWPORT"}</div>
      </header>

      <section className="intro">
        <div><p className="eyebrow">NATURAL LANGUAGE · MCP · THREE.JS</p><h1>Tu IA. Tu escena 3D.</h1><p>Da instrucciones a tu agente y observa cómo crea y modifica geometría en tiempo real.</p></div>
        <div className="unit-pill">UNIDADES <b>mm</b></div>
      </section>

      <section className="workspace">
        <div className="scene-panel">
          <div className="panel-heading"><span>SCENE VIEWPORT</span><span>THREE.JS · LIVE</span></div>
          <SceneViewport onObjectsChange={onObjectsChange} onConnectionChange={onConnectionChange} onActivity={onActivity} />
          <div className="scene-footer"><span><Activity size={14}/> {connected ? "Canal de comandos activo" : "Inicia el backend para conectar"}</span><span>{objects.length} objetos</span></div>
        </div>

        <aside className="side-panel">
          <section className="card">
            <div className="card-title"><span>OBJETOS DE ESCENA</span><span>{objects.length.toString().padStart(2,"0")}</span></div>
            {objects.length === 0 ? <div className="empty"><Box size={25}/><p>La escena está vacía</p><small>Conecta un agente MCP y crea tu primer sólido.</small></div> : <div className="object-list">{objects.map(item => <div className="object-row" key={item.id}><span className="object-icon">{item.type === "sphere" ? <Circle size={17}/> : item.type === "box" ? <Box size={17}/> : <Cylinder size={17}/>}</span><div><b>{item.name}</b><small>{item.type} · {Object.values(item.dimensionsMm).join(" × ")} mm</small><small className="muted-id">{item.id.slice(0, 13)}</small></div></div>)}</div>}
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
      <footer>FASE 1 · MOTOR GEOMÉTRICO AISLADO · SIN DEPENDENCIA DEL MOTOR ÓPTICO AURUM RENDER</footer>
    </main>
  );
}
