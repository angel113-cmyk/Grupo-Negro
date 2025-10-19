# 🚀 Despliegue en Render

Esta rama contiene la configuración necesaria para desplegar la aplicación **Grupo Negro - Sistema de Apuestas Deportivas** en Render usando Docker.

## 📋 Archivos de Configuración

### `Dockerfile`
- Imagen base: .NET 9.0 SDK y Runtime
- Compilación optimizada en Release
- Expone puerto 8080
- Configurado para producción

### `render.yaml`
- Configuración automática del servicio en Render
- Disco persistente de 1GB para la base de datos SQLite
- Variables de entorno configuradas
- Health check habilitado

### `.dockerignore`
- Excluye archivos innecesarios del build
- Optimiza el tamaño de la imagen Docker

### `appsettings.Production.json`
- Configuración específica para producción
- Logging optimizado
- Base de datos SQLite en disco persistente

## 🔧 Pasos para Desplegar

### Opción 1: Despliegue Automático con render.yaml

1. **Conectar el repositorio a Render:**
   - Ve a [Render Dashboard](https://dashboard.render.com/)
   - Click en "New" → "Blueprint"
   - Conecta tu repositorio de GitHub
   - Selecciona la rama `deploy/render`
   - Render detectará automáticamente el `render.yaml`

2. **Configurar variables de entorno (opcional):**
   - Las variables ya están en el `render.yaml`
   - Puedes agregar secretos adicionales si es necesario

3. **Desplegar:**
   - Render construirá automáticamente la imagen Docker
   - Aplicará las migraciones de la base de datos
   - Iniciará el servicio

### Opción 2: Despliegue Manual

1. **Crear un nuevo Web Service en Render:**
   - Ve a [Render Dashboard](https://dashboard.render.com/)
   - Click en "New" → "Web Service"
   - Conecta tu repositorio
   - Selecciona la rama `deploy/render`

2. **Configurar el servicio:**
   - **Environment:** Docker
   - **Region:** Oregon (o tu preferencia)
   - **Plan:** Free (o superior)
   - **Dockerfile Path:** ./Dockerfile

3. **Variables de entorno:**
   ```
   ASPNETCORE_ENVIRONMENT=Production
   ASPNETCORE_URLS=http://+:8080
   ConnectionStrings__DefaultConnection=Data Source=/data/GrupoNegro.db
   ```

4. **Configurar disco persistente:**
   - Name: `database`
   - Mount Path: `/data`
   - Size: 1 GB

5. **Deploy:**
   - Click en "Create Web Service"

## 🔒 Consideraciones de Seguridad

1. **Secrets y Claves:**
   - Nunca incluyas claves secretas en el código
   - Usa las variables de entorno de Render para secretos
   - Configura `ASPNETCORE_HTTPS_PORT` si usas HTTPS

2. **Base de Datos:**
   - SQLite funciona bien para aplicaciones pequeñas
   - Para producción con alto tráfico, considera PostgreSQL
   - Los datos están en disco persistente (`/data`)

3. **CORS y Seguridad:**
   - Configura CORS apropiadamente en producción
   - Habilita HTTPS redirects
   - Configura políticas de seguridad

## 📊 Monitoreo

- **Logs:** Disponibles en el dashboard de Render
- **Métricas:** CPU, memoria y red en tiempo real
- **Health Checks:** Configurado en `/`

## 🔄 Actualizaciones

Para actualizar la aplicación:

```bash
# Hacer cambios en tu código local
git add .
git commit -m "Tu mensaje de commit"
git push origin deploy/render
```

Render automáticamente detectará los cambios y redesplegar la aplicación.

## 🆘 Troubleshooting

### La aplicación no inicia
- Verifica los logs en Render Dashboard
- Asegúrate que el puerto 8080 esté configurado correctamente
- Verifica las variables de entorno

### Error de base de datos
- Confirma que el disco persistente esté montado en `/data`
- Verifica que las migraciones se hayan aplicado
- Revisa la cadena de conexión

### Error 502
- La aplicación puede estar tardando en iniciar
- Verifica que el health check esté funcionando
- Aumenta el timeout si es necesario

## 📱 URLs

Después del despliegue, tu aplicación estará disponible en:
```
https://grupo-negro-apuestas.onrender.com
```

## 🎯 Características del Sistema

✅ Sistema de apuestas deportivas
✅ Apuestas combinadas
✅ Sistema de comentarios
✅ Gestión de saldo (depósitos/retiros)
✅ Temas claro/oscuro con cookies
✅ Ligas favoritas
✅ Autenticación con Identity

## 👥 Equipo

Proyecto desarrollado por el **Grupo Negro** - Universidad InfoYachay

---

**Nota:** Este es un proyecto educativo. Para uso en producción real, considera implementar medidas adicionales de seguridad y rendimiento.
