#!/bin/bash

# Script de inicio para Render
echo "==> Iniciando aplicación Grupo Negro..."

# Crear directorio de datos si no existe
echo "==> Verificando directorio de base de datos..."
mkdir -p /data
chmod 777 /data
ls -la /data

# Iniciar la aplicación
echo "==> Iniciando aplicación .NET..."
exec dotnet Grupo-negro.dll
