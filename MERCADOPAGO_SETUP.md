# 🚀 Configuración de MercadoPago - Credenciales Reales

## 📋 Para obtener credenciales reales de MercadoPago:

### 1. 🌐 Crear cuenta de desarrollador
1. Ve a [MercadoPago Developers](https://www.mercadopago.com/developers/)
2. Registra una cuenta o inicia sesión
3. Crea una nueva aplicación

### 2. 🔑 Obtener credenciales de prueba (sandbox)
En tu dashboard de MercadoPago:
1. Ve a "Tus integraciones" → Tu aplicación
2. En la sección "Credenciales de prueba":
   - **Access Token**: `TEST-1234567890-123456-abcd1234abcd1234abcd1234abcd1234-123456789`
   - **Public Key**: `TEST-abcd1234-abcd-1234-abcd-123456789012`

### 3. 📝 Configurar en appsettings.json
```json
{
  "MercadoPago": {
    "AccessToken": "TEST-TU-ACCESS-TOKEN-REAL-AQUI",
    "PublicKey": "TEST-TU-PUBLIC-KEY-REAL-AQUI", 
    "Environment": "sandbox",
    "WebhookUrl": "http://localhost:5244/api/webhooks/mercadopago"
  }
}
```

### 4. 🚀 Para producción
Cambia a credenciales de producción:
```json
{
  "MercadoPago": {
    "AccessToken": "APP_USR-TU-ACCESS-TOKEN-PRODUCCION",
    "PublicKey": "APP_USR-TU-PUBLIC-KEY-PRODUCCION",
    "Environment": "production",
    "WebhookUrl": "https://tu-dominio.com/api/webhooks/mercadopago"
  }
}
```

## 🔧 Estado Actual
- ✅ API de MercadoPago integrada correctamente
- ✅ Manejo de errores implementado  
- ⚠️ Credenciales de prueba necesarias para funcionamiento completo
- ✅ Simulación disponible como fallback

## 🧪 Modos de funcionamiento
1. **Con credenciales reales**: API real de MercadoPago
2. **Sin credenciales**: Simulación local (funcionando actualmente)

## 📞 Soporte
Para obtener credenciales reales:
- Documentación: https://www.mercadopago.com/developers/
- Soporte: https://developers.mercadopago.com/support