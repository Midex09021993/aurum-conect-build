import { useEffect, useRef } from "react";
import * as THREE from "three";
import type { SceneCommand, SceneCommandAck, SceneObjectRecord, Vec3 } from "../../../packages/contracts/src/index";

interface Props {
  onObjectsChange: (objects: SceneObjectRecord[]) => void;
  onConnectionChange: (connected: boolean) => void;
  onActivity: (message: string) => void;
}

export function SceneViewport({ onObjectsChange, onConnectionChange, onActivity }: Props) {
  const hostRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const host = hostRef.current;
    if (!host) return;

    const scene = new THREE.Scene();
    scene.background = new THREE.Color("#111a22");
    const camera = new THREE.PerspectiveCamera(45, 1, 0.1, 10000);
    camera.position.set(90, 75, 110);
    camera.lookAt(0, 0, 0);

    const renderer = new THREE.WebGLRenderer({ antialias: true, powerPreference: "high-performance" });
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 1.5));
    renderer.setSize(host.clientWidth, host.clientHeight);
    renderer.outputColorSpace = THREE.SRGBColorSpace;
    host.appendChild(renderer.domElement);

    scene.add(new THREE.HemisphereLight(0xffffff, 0x34404b, 2));
    const key = new THREE.DirectionalLight(0xffffff, 2.2);
    key.position.set(50, 80, 60);
    scene.add(key);

    const grid = new THREE.GridHelper(200, 40, 0x6f7d88, 0x35414a);
    scene.add(grid);
    const axes = new THREE.AxesHelper(30);
    scene.add(axes);

    const objects = new Map<string, { mesh: THREE.Mesh; record: SceneObjectRecord }>();
    let revision = 0;
    let frame = 0;
    let disposed = false;

    const updateObjects = () => onObjectsChange([...objects.values()].map(({ record }) => ({
      ...record,
      positionMm: { ...record.positionMm },
      dimensionsMm: { ...record.dimensionsMm }
    })));

    const materials = () => new THREE.MeshStandardMaterial({
      color: 0xd6b54f, metalness: 0.55, roughness: 0.28
    });

    const handleCommand = (command: SceneCommand): SceneCommandAck => {
      try {
        let result: unknown;
        if (command.operation === "create_sphere") {
          const input = command.payload as { diameterMm: number; center?: Vec3 };
          const radius = input.diameterMm / 2;
          const mesh = new THREE.Mesh(new THREE.SphereGeometry(radius, 32, 24), materials());
          const id = crypto.randomUUID();
          const p = input.center ?? { x: 0, y: 0, z: 0 };
          mesh.position.set(p.x, p.y, p.z);
          scene.add(mesh);
          objects.set(id, { mesh, record: { id, type: "sphere", name: `Sphere ${objects.size + 1}`, dimensionsMm: { diameter: input.diameterMm }, positionMm: { ...p } } });
          result = { object: objects.get(id)!.record };
          onActivity(`Esfera creada · Ø ${input.diameterMm} mm · ${id.slice(0, 8)}`);
        } else if (command.operation === "create_box") {
          const input = command.payload as { widthMm: number; depthMm: number; heightMm: number; center?: Vec3 };
          const mesh = new THREE.Mesh(new THREE.BoxGeometry(input.widthMm, input.heightMm, input.depthMm), materials());
          const id = crypto.randomUUID();
          const p = input.center ?? { x: 0, y: 0, z: 0 };
          mesh.position.set(p.x, p.y, p.z);
          scene.add(mesh);
          objects.set(id, { mesh, record: { id, type: "box", name: `Box ${objects.size + 1}`, dimensionsMm: { width: input.widthMm, depth: input.depthMm, height: input.heightMm }, positionMm: { ...p } } });
          result = { object: objects.get(id)!.record };
          onActivity(`Cubo creado · ${input.widthMm} × ${input.depthMm} × ${input.heightMm} mm`);
        } else if (command.operation === "create_cylinder") {
          const input = command.payload as { radiusMm: number; heightMm: number; topRadiusMm?: number; center?: Vec3 };
          const mesh = new THREE.Mesh(new THREE.CylinderGeometry(input.topRadiusMm ?? input.radiusMm, input.radiusMm, input.heightMm, 32), materials());
          const id = crypto.randomUUID();
          const p = input.center ?? { x: 0, y: 0, z: 0 };
          mesh.position.set(p.x, p.y, p.z);
          scene.add(mesh);
          objects.set(id, { mesh, record: { id, type: "cylinder", name: `Cylinder ${objects.size + 1}`, dimensionsMm: { radius: input.radiusMm, topRadius: input.topRadiusMm ?? input.radiusMm, height: input.heightMm }, positionMm: { ...p } } });
          result = { object: objects.get(id)!.record };
          onActivity(`Cilindro creado · radio ${input.radiusMm} mm · altura ${input.heightMm} mm`);
        } else if (command.operation === "move_object") {
          const input = command.payload as { objectId: string; translation: Vec3; mode: "relative" | "absolute" };
          const entry = objects.get(input.objectId);
          if (!entry) throw new Error(`Objeto no encontrado: ${input.objectId}`);
          const p = input.mode === "absolute"
            ? input.translation
            : { x: entry.mesh.position.x + input.translation.x, y: entry.mesh.position.y + input.translation.y, z: entry.mesh.position.z + input.translation.z };
          entry.mesh.position.set(p.x, p.y, p.z);
          entry.record.positionMm = { ...p };
          result = { object: entry.record };
          onActivity(`Objeto movido · X ${p.x} · Y ${p.y} · Z ${p.z} mm`);
        } else if (command.operation === "delete_object") {
          const input = command.payload as { objectId: string; confirm: true };
          const entry = objects.get(input.objectId);
          if (!entry) throw new Error(`Objeto no encontrado: ${input.objectId}`);
          scene.remove(entry.mesh);
          entry.mesh.geometry.dispose();
          const material = entry.mesh.material;
          if (Array.isArray(material)) material.forEach(m => m.dispose()); else material.dispose();
          objects.delete(input.objectId);
          result = { deleted: true, objectId: input.objectId };
          onActivity(`Objeto eliminado · ${input.objectId.slice(0, 8)}`);
        } else {
          result = { objects: [...objects.values()].map(({ record }) => record) };
          onActivity(`Inventario consultado · ${objects.size} objetos`);
        }
        revision += 1;
        updateObjects();
        return { commandId: command.commandId, success: true, result, sceneRevision: revision };
      } catch (error) {
        return { commandId: command.commandId, success: false, error: error instanceof Error ? error.message : "Error de escena", sceneRevision: revision };
      }
    };

    let socket: WebSocket | null = null;
    let reconnectTimer: number | undefined;
    let reconnectAttempt = 0;
    const connect = () => {
      if (disposed) return;
      const currentSocket = new WebSocket("ws://127.0.0.1:4317/scene");
      socket = currentSocket;
      currentSocket.addEventListener("open", () => {
        reconnectAttempt = 0;
        onConnectionChange(true);
        onActivity("Viewport conectado al backend");
      });
      currentSocket.addEventListener("close", () => {
        onConnectionChange(false);
        if (!disposed) {
          reconnectAttempt += 1;
          const delay = Math.min(5000, 500 * 2 ** Math.min(reconnectAttempt - 1, 3));
          reconnectTimer = window.setTimeout(connect, delay);
        }
      });
      currentSocket.addEventListener("error", () => onConnectionChange(false));
      currentSocket.addEventListener("message", event => {
        let command: SceneCommand;
        try { command = JSON.parse(String(event.data)) as SceneCommand; }
        catch { return; }
        const ack = handleCommand(command);
        if (currentSocket.readyState === WebSocket.OPEN) currentSocket.send(JSON.stringify(ack));
      });
    };
    connect();

    const resize = () => {
      if (!host || disposed) return;
      const width = Math.max(1, host.clientWidth);
      const height = Math.max(1, host.clientHeight);
      camera.aspect = width / height;
      camera.updateProjectionMatrix();
      renderer.setSize(width, height);
    };
    const observer = new ResizeObserver(resize);
    observer.observe(host);

    const animate = () => {
      if (disposed) return;
      frame = requestAnimationFrame(animate);
      renderer.render(scene, camera);
    };
    animate();

    return () => {
      disposed = true;
      cancelAnimationFrame(frame);
      observer.disconnect();
      if (reconnectTimer !== undefined) window.clearTimeout(reconnectTimer);
      socket?.close();
      for (const { mesh } of objects.values()) {
        mesh.geometry.dispose();
        const material = mesh.material;
        if (Array.isArray(material)) material.forEach(m => m.dispose()); else material.dispose();
      }
      renderer.dispose();
      renderer.domElement.remove();
    };
  }, [onObjectsChange, onConnectionChange, onActivity]);

  return <div className="viewport" ref={hostRef} />;
}
