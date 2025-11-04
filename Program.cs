using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Grupo_negro.Data;
using Grupo_negro.Models;
using Grupo_negro.Services;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient();
builder.Services.AddSingleton<SemanticKernelService>();
builder.Services.AddScoped<ChatService>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => 
{
    // Configuración de contraseñas
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;
    
    // Configuración de lockout
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    
    // Configuración de usuarios
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Registrar servicio para datos simulados
builder.Services.AddScoped<Grupo_negro.Services.DatosSimuladosService>();

// Registrar servicio para usuario
builder.Services.AddScoped<Grupo_negro.Services.UsuarioService>();

// Registrar servicio de cookies
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Grupo_negro.Services.CookieService>();

// Registrar servicio de apuestas combinadas
builder.Services.AddScoped<Grupo_negro.Services.ApuestaCombinadadService>();

// Registrar servicio de Football API
builder.Services.AddHttpClient<Grupo_negro.Services.IFootballApiService, Grupo_negro.Services.FootballApiService>();
builder.Services.AddScoped<Grupo_negro.Services.IFootballApiService, Grupo_negro.Services.FootballApiService>();

// Registrar servicios de Pagos
builder.Services.Configure<MercadoPagoSettings>(builder.Configuration.GetSection("MercadoPago"));
builder.Services.AddHttpClient<Grupo_negro.Services.MercadoPagoService>();
builder.Services.AddScoped<Grupo_negro.Services.MercadoPagoService>();
builder.Services.AddScoped<Grupo_negro.Services.IPagosService, Grupo_negro.Services.PagosService>();

// Registrar servicios de Machine Learning
builder.Services.AddScoped<Grupo_negro.Services.MLTrainingService>();
builder.Services.AddScoped<Grupo_negro.Services.MLPredictionService>();

// Configurar sesiones para el carrito de apuestas
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession(); // Habilitar sesiones para el carrito de apuestas

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Aplicar migraciones automáticamente en producción
if (app.Environment.IsProduction())
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            
            // Asegurar que el directorio de la base de datos existe
            var connectionString = app.Configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(connectionString) && connectionString.Contains("Data Source="))
            {
                var dbPath = connectionString.Split("Data Source=")[1].Split(';')[0];
                var dbDirectory = Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
                {
                    Directory.CreateDirectory(dbDirectory);
                    Console.WriteLine($"Directorio de base de datos creado: {dbDirectory}");
                }
            }
            
            // Aplicar migraciones pendientes
            Console.WriteLine("Aplicando migraciones de base de datos...");
            context.Database.Migrate();
            Console.WriteLine("Migraciones aplicadas exitosamente.");
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Un error ocurrió al aplicar las migraciones.");
        }
    }
}

// Inicializar datos (roles y usuario admin)
await Grupo_negro.Services.InicializacionService.InicializarDatos(app.Services);

app.Run();
