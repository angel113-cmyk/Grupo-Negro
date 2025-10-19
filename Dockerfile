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

# Copiar la aplicación publicada
COPY --from=publish /app/publish .

# Exponer el puerto que usará la aplicación
EXPOSE 8080

# Variables de entorno para producción
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Comando para iniciar la aplicación
ENTRYPOINT ["dotnet", "Grupo-negro.dll"]
