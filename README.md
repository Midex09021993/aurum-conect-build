# AURUM CONECT — Complemento de archivo único para Rhino 7

Este repositorio contiene el proyecto necesario para compilar AURUM CONECT para Rhino 7 de 64 bits. La plataforma AURUM LAB y el motor AURUM RENDER permanecen fuera de este repositorio.

## Distribución

El resultado oficial es un único archivo:

`AurumConect-Rhino7-1.0.0.rhp`

Las bibliotecas administradas Microsoft WebView2 Core/WinForms y el cargador nativo WebView2Loader x64 se incrustan como recursos dentro del propio ensamblado. Al cargar el plugin, el cargador nativo se extrae a una carpeta de datos local de AURUM LAB para que Windows pueda cargarlo. No se distribuyen DLL adicionales junto al RHP.

## Requisitos

- Windows x64
- Rhino 7 x64
- MatrixGold 3.1 dentro de Rhino 7 para el flujo CAD
- Microsoft Edge WebView2 Evergreen Runtime para la ventana Chromium integrada
- Conexión a Internet para acceder a AURUM LAB

WebView2 Runtime es un componente de sistema de Microsoft y no se puede incrustar de forma soportada dentro del .RHP. Si falta, el plugin informa del requisito y abre la página en el navegador predeterminado como alternativa; Rhino no debe cerrarse por ese motivo.

## Instalación

1. Descarga el archivo `.rhp`.
2. En Rhino 7, abre `_PlugInManager`.
3. Usa la opción para instalar/cargar un complemento y selecciona `AurumConect-Rhino7-1.0.0.rhp`.
4. Ejecuta `AurumConect` en la línea de comandos de Rhino.
5. Si la ventana integrada no inicia, instala o actualiza Microsoft Edge WebView2 Evergreen Runtime y reinicia Rhino.

## Compilación y validación

GitHub Actions compila .NET Framework 4.8 x64, valida la cabecera PE y la presencia de los tres recursos WebView2 dentro del ensamblado, y publica únicamente el archivo RHP. La validación automática de recursos no sustituye una prueba real en Rhino 7 + MatrixGold 3.1.

## Seguridad

No se almacenan credenciales ni secretos en este repositorio. El canal local de transferencia escucha únicamente en loopback, usa un token aleatorio de un solo uso y expira a los tres minutos. El archivo temporal tiene un límite de 100 MB.

Este repositorio es público para aprovechar ejecutores estándar de GitHub Actions. No contiene el código de AURUM RENDER ni secretos del repositorio privado.


## Versión 1.0.2
Incluye diagnóstico de inicialización y navegación de WebView2, pantalla de carga y recuperación mediante reintento o navegador externo.

<!-- AURUM CONECT BUILD RETRY 2026-11-11 -->

<!-- AURUM CONECT BUILD RETRY 2 · 2026-11-11 -->


## Versión 1.0.11

Refuerza la recuperación del viewport WebView2 con sincronización escalonada tras cambios de tamaño, maximización/restauración, reactivación y cambios de DPI. Añade hasta dos intentos automáticos de recuperación mediante recarga del proceso Chromium cuando WebView2 notifica un fallo. No modifica el motor óptico de AURUM RENDER.


## Versión 1.0.11 — recuperación reforzada del visor

La versión 1.0.11 incorpora sincronización escalonada del viewport WebView2 tras maximizar, restaurar, redimensionar, reactivar la ventana o cambiar el DPI de Windows. También añade hasta dos intentos automáticos de recuperación mediante recarga del contenido cuando WebView2 notifica un fallo del proceso Chromium.

Esta mejora actúa en la ventana anfitriona de AURUM CONECT. No modifica los shaders, materiales, HDRI, BVH ni el motor óptico de AURUM RENDER.

**Instalación:** cierra Rhino 7, sustituye el complemento anterior por `AurumConect-Rhino7-1.0.11.rhp`, vuelve a registrarlo en `_PlugInManager` y ejecuta `AurumConect`. Requiere Windows x64, Rhino 7 x64 y Microsoft Edge WebView2 Evergreen Runtime x64.

Registro de diagnóstico: `%LOCALAPPDATA%\\AurumLab\\AurumConect\\Logs\\AurumConect-1.0.11.log`.


## Versión 1.0.12 — recuperación de superficie WebView2

La versión 1.0.12 corrige la pérdida de imagen que puede ocurrir al maximizar o restaurar la ventana de AURUM CONECT. Detecta los cambios reales de estado de la ventana WinForms y fuerza una reconexión controlada de la superficie de composición WebView2: oculta temporalmente el controlador, actualiza sus límites, notifica el cambio de posición y vuelve a mostrarlo después de que Windows complete el nuevo ciclo de composición.

Después de reconectar la superficie, notifica al visor para que mida el viewport y solicite un nuevo fotograma. No navega de nuevo, no reinicia Chromium, no recrea el contexto WebGL y no modifica la escena ni el motor óptico de AURUM RENDER.

**Instalación:** cierra Rhino 7, sustituye el complemento anterior por `AurumConect-Rhino7-1.0.12.rhp`, vuelve a registrarlo en `_PlugInManager` y ejecuta `AurumConect`. Requiere Windows x64, Rhino 7 x64 y Microsoft Edge WebView2 Evergreen Runtime x64.

Registro de diagnóstico: `%LOCALAPPDATA%\\AurumLab\\AurumConect\\Logs\\AurumConect-1.0.12.log`.

La validación automatizada de compilación debe completarse y la recuperación visual debe probarse en Rhino 7 + MatrixGold 3.1 antes de considerar esta versión lista para producción.
