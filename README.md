# AURUM CONECT — Rhino 7 + barra nativa AURUM LAB

Este repositorio contiene el complemento de AURUM CONECT para Rhino 7 x64 y su colección nativa de barras de herramientas. La plataforma AURUM LAB y el motor óptico de AURUM RENDER permanecen fuera de este repositorio.

## Distribución

El paquete de distribución contiene dos archivos que deben permanecer juntos:

- `AurumConect.rhp`: complemento Rhino 7 x64 (.NET Framework 4.8). WebView2.Core y WebView2Loader x64 se integran como recursos del propio ensamblado.
- `AurumConect.rui`: barra nativa Rhino con tres botones e iconos: **AURUM RENDER**, **ENVIAR DISEÑO** y **PANEL AURUM**.

El nombre base idéntico y la ubicación conjunta permiten a Rhino 7 detectar y abrir automáticamente el RUI al cargar por primera vez el complemento, según el mecanismo documentado por McNeel. El RUI puede copiarse a la ubicación de usuario que Rhino administra.

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
4. Reinicia Rhino si lo solicita. La primera carga del complemento debe detectar la barra `AurumConect.rui`.
5. Si no aparece, ejecuta el comando `_Toolbar`, busca el archivo `AurumConect.rui` y ábrelo manualmente.
6. Prueba los tres botones: visor, transferencia del documento activo y panel.

También puedes cargar solo el RHP desde el administrador de complementos, pero para recibir la barra nativa debes conservar el RUI en la misma carpeta que el RHP.

## Botones y comandos

| Botón | Comando Rhino | Acción |
|---|---|---|
| AURUM RENDER | `AurumRender` | Abre el visor integrado sin transferir un nuevo documento |
| ENVIAR DISEÑO | `AurumConectActualizar` | Genera una copia completa temporal del documento Rhino y abre AURUM RENDER con la transferencia |
| PANEL AURUM | `AurumConect` | Abre el panel acoplable AURUM CONECT |

## Compilación y validación

GitHub Actions compila .NET Framework 4.8 x64, valida la cabecera PE y los recursos WebView2 integrados, comprueba que el RUI sea XML válido, verifica las tres macros y valida los atlas PNG de iconos. Esta comprobación no sustituye una prueba visual y funcional en Rhino 7 + MatrixGold 3.1.

## Seguridad

No se almacenan credenciales ni secretos en este repositorio. El canal local de transferencia escucha únicamente en loopback, usa un token aleatorio de un solo uso y expira a los tres minutos. El archivo temporal tiene un límite de 100 MB.

Este repositorio es público para aprovechar ejecutores estándar de GitHub Actions. No contiene el código de AURUM RENDER ni secretos del repositorio privado.

## Historial relevante

### 1.0.12 — recuperación de superficie WebView2

Corrige la pérdida de imagen que puede ocurrir al maximizar o restaurar la ventana de AURUM CONECT. La recuperación actúa en la ventana anfitriona: no navega de nuevo, no reinicia Chromium, no recrea el contexto WebGL y no modifica la escena ni el motor óptico.

### Barra nativa AURUM CONECT

Se incorpora una barra Rhino 7 con iconografía propia y accesos directos al visor, transferencia del diseño y panel de conexión.
