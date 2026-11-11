# AURUM CONECT — Build público de Rhino 7

Este repositorio contiene únicamente el proyecto necesario para compilar el complemento AURUM CONECT para Rhino 7 de 64 bits. La plataforma AURUM LAB y su código de producto permanecen en el repositorio privado.

## Qué genera

Un instalador Windows x64:

\`AURUM-CONECT-Setup-Rhino7-1.0.0.exe\`

Incluye el complemento .RHP, bibliotecas administradas Microsoft WebView2 y WebView2Loader.dll x64. No distribuye RhinoCommon ni componentes de McNeel/MatrixGold.

## Compilación automática

GitHub Actions se ejecuta al cambiar main o manualmente desde Actions. Restaura NuGet, compila .NET Framework 4.8 x64, valida la cabecera PE, reúne las dependencias y genera el instalador con Inno Setup. El EXE se publica como artefacto y como asset de la release estable v1.0.0.

## Requisitos de uso

- Windows 64 bits
- Rhino 7
- MatrixGold 3.1 dentro de Rhino 7
- Microsoft Edge WebView2 Runtime

## Estado

El código de integración y el empaquetado requieren validación de compilación y pruebas reales de extremo a extremo en Rhino 7 + MatrixGold 3.1. No se debe considerar certificado hasta completar esas pruebas.

## Seguridad

No se almacenan credenciales ni secretos en este repositorio. El canal local de transferencia escucha únicamente en loopback, usa un token aleatorio de un solo uso y expira a los tres minutos. El archivo temporal tiene un límite de 100 MB.

Este repositorio es público para aprovechar ejecutores estándar de GitHub Actions en proyectos públicos. No contiene el código de AURUM RENDER ni secretos del repositorio privado.
