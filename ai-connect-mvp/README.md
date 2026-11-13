# AURUM AI CONNECT — MVP Fase 1

Prototipo aislado de AURUM AI CONNECT. No modifica el plugin Rhino RHP/RUI existente ni AURUM RENDER.

## Alcance

- Crear esfera, caja, cilindro.
- Mover y eliminar objetos.
- Listar objetos.
- Un viewport Three.js conectado por WebSocket al backend.
- Servidor MCP local por stdio para agentes compatibles.

Todas las dimensiones y coordenadas usan milímetros (1 unidad Three.js = 1 mm).

## Arquitectura ejecutable

`MCP stdio -> Fastify HTTP (127.0.0.1:4317) -> WebSocket /scene -> React + Three.js`

El navegador confirma cada comando. El backend no declara éxito hasta recibir el ACK del viewport.

## Requisitos

- Node.js 22 o superior.
- npm 10 o superior.

## Inicio

Desde esta carpeta:

```bash
npm install
npm run dev
```

En terminales separadas, también se puede iniciar cada servicio:

```bash
npm run dev:server
npm run dev:web
npm run dev:mcp
```

- Web: http://127.0.0.1:5173
- API: http://127.0.0.1:4317/health
- WebSocket: ws://127.0.0.1:4317/scene

## Conectar un agente MCP

Ejemplo de configuración de cliente MCP local (ajusta la ruta absoluta):

```json
{
  "mcpServers": {
    "aurum-ai-connect": {
      "command": "npm",
      "args": ["--prefix", "/RUTA/ABSOLUTA/ai-connect-mvp", "run", "dev:mcp"]
    }
  }
}
```

El proceso MCP usa stdio; no escribas logs en stdout. El servidor API debe estar iniciado y el viewport web abierto.

## Prueba manual

1. Inicia los tres servicios.
2. Abre el viewport.
3. Conecta el agente MCP.
4. Solicita: "Crea una esfera de 20 mm".
5. Solicita: "Crea un cubo de 10 por 10 por 10 mm".
6. Usa el ID devuelto por `list_objects` para mover o eliminar objetos.

## Seguridad y límites actuales

- API y WebSocket escuchan solo en loopback.
- No se expone un endpoint remoto ni se admiten comandos arbitrarios.
- El backend valida los seis comandos y limita dimensiones/coordenadas.
- Un solo viewport activo por proceso; una cola de confirmaciones serializa operaciones.
- El estado es en memoria y se pierde al reiniciar.
- El prototipo no debe exponerse a Internet ni usarse con documentos CAD de producción.

## Scripts

- `npm run dev`: inicia API y frontend. El host MCP inicia el proceso `dev:mcp` cuando se conecta.
- `npm run build`: compila frontend y comprueba TypeScript del backend/MCP.
- `npm test`: pruebas unitarias del contrato geométrico.

## Próximos hitos

1. Pruebas de integración con MCP Inspector y al menos dos hosts.
2. Persistencia e historial/reversión.
3. Autenticación local por token.
4. Adaptador RhinoCommon separado para Rhino 7/8.
5. Motor paramétrico de joyería.
