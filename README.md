# AURUM CONECT — Rhino 7 + barra nativa AURUM LAB

Este repositorio contiene el complemento de AURUM CONECT para Rhino 7 x64 y su colección nativa de barras de herramientas. La plataforma AURUM LAB y el motor óptico de AURUM RENDER permanecen fuera de este repositorio.

## Distribución 1.0.12

El paquete ZIP contiene dos archivos que deben permanecer juntos:

- `AurumConect.rhp`: complemento Rhino 7 x64 (.NET Framework 4.8). WebView2.Core y WebView2Loader x64 se integran como recursos del propio ensamblado.
- `AurumConect.rui`: barra nativa Rhino con tres botones e iconos: **AURUM RENDER**, **ENVIAR DISEÑO** y **PANEL AURUM**.

El archivo RHP también se publica por separado para quien solo necesite cargar el complemento. Para instalar los botones con sus iconos, descarga el ZIP y conserva el RHP y el RUI juntos.

## Requisitos

- Windows x64
- Rhino 7 x64
- MatrixGold 3.1 dentro de Rhino 7 para el flujo CAD
- Microsoft Edge WebView2 Evergreen Runtime x64
- Conexión a Internet para acceder a AURUM LAB

WebView2 Runtime es un componente de sistema de Microsoft y no se incrusta dentro del .RHP.

## Instalación manual

1. Descarga y extrae `AurumConect-Rhino7-1.0.12.zip` en una carpeta permanente.
2. No separes ni renombres `AurumConect.rhp` y `AurumConect.rui`.
3. En Rhino 7, abre `_PlugInManager` e instala/carga `AurumConect.rhp` desde esa carpeta.
4. Reinicia Rhino si lo solicita. Si la barra no aparece automáticamente, ejecuta `_Toolbar`, busca `AurumConect.rui` y ábrelo.
5. Comprueba que aparecen los tres botones de la barra AURUM CONECT.

También puedes cargar solo el RHP desde el administrador de complementos, pero para recibir la barra nativa debes conservar el RUI en la misma carpeta que el RHP.

## Comandos Rhino — cómo llamar las instrucciones

Escribe cualquiera de estos comandos en la línea de comandos de Rhino y pulsa Enter. También puedes usar los tres botones de la barra nativa:

| Botón | Comando exacto | Acción |
|---|---|---|
| PANEL AURUM | `AurumConect` | Abre el panel acoplable AURUM CONECT con sus acciones e instrucciones. |
| AURUM RENDER | `AurumRender` | Abre el visor integrado sin enviar un documento nuevo. |
| ENVIAR DISEÑO | `AurumConectActualizar` | Crea una copia temporal completa del documento Rhino activo y la transfiere a AURUM RENDER. |

**Flujo recomendado:** ejecuta `AurumConect` para abrir el panel y consultar las acciones; usa `AurumConectActualizar` para enviar el diseño actual; ejecuta `AurumRender` cuando solo quieras abrir el visor. Para enviar cambios posteriores, vuelve a ejecutar `AurumConectActualizar`.

## Compilación y validación

GitHub Actions compila .NET Framework 4.8 x64, valida la cabecera PE y los recursos WebView2 integrados, comprueba que el RUI sea XML válido, verifica las tres macros y valida los atlas PNG de iconos. Esta comprobación no sustituye una prueba visual y funcional en Rhino 7 + MatrixGold 3.1.

## Seguridad

No se almacenan credenciales ni secretos en este repositorio. El canal local de transferencia escucha únicamente en loopback, usa un token aleatorio de un solo uso y expira a los tres minutos. El archivo temporal tiene un límite de 100 MB.

Este repositorio es público para aprovechar ejecutores estándar de GitHub Actions. No contiene el código de AURUM RENDER ni secretos del repositorio privado.

## Historial relevante

### 1.0.12 — recuperación de superficie WebView2 y barra nativa

Corrige la pérdida de imagen que puede ocurrir al maximizar o restaurar la ventana de AURUM CONECT. La recuperación actúa en la ventana anfitriona: no navega de nuevo, no reinicia Chromium, no recrea el contexto WebGL y no modifica la escena ni el motor óptico.

Incluye una barra Rhino 7 con iconografía propia y accesos directos al visor, transferencia del diseño y panel de conexión. Los metadatos del ensamblado, los mensajes de diagnóstico y la carpeta de runtime WebView2 usan ahora la versión 1.0.12.
