# Usar la imagen oficial de .NET SDK para compilar la aplicación
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar el archivo del proyecto y restaurar dependencias
COPY ["Grupo-negro.csproj", "./"]
RUN dotnet restore "Grupo-negro.csproj"

# Copiar todo el código fuente
COPY . .

# Compilar la aplicación en modo Release
RUN dotnet build "Grupo-negro.csproj" -c Release -o /app/build

# Publicar la aplicación
FROM build AS publish
RUN dotnet publish "Grupo-negro.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Usar la imagen runtime para ejecutar la aplicación
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Crear el directorio para la base de datos con permisos correctos
RUN mkdir -p /data && chmod 777 /data

# Copiar la aplicación publicada
COPY --from=publish /app/publish .

# Copiar el script de inicio
COPY start.sh /app/start.sh
RUN chmod +x /app/start.sh

# Exponer el puerto que usará la aplicación
EXPOSE 8080

# Variables de entorno para producción
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Comando para iniciar la aplicación usando el script
ENTRYPOINT ["/app/start.sh"]
