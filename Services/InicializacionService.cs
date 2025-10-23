using Microsoft.AspNetCore.Identity;
using Grupo_negro.Models;

namespace Grupo_negro.Services
{
    public class InicializacionService
    {
        public static async Task InicializarDatos(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Crear roles si no existen
            string[] roles = { "Admin", "Usuario" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Crear usuario administrador si no existe
            var adminEmail = "admin@admin.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    Nombres = "Administrador",
                    Apellidos = "Sistema",
                    DNI = "00000000",
                    Celular = "+51 999 999 999",
                    Negocio = "APUESTA KONGNOSTROS ADMIN",
                    Saldo = 10000.00m, // Saldo inicial para admin
                    FechaRegistro = DateTime.Now,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine("Usuario administrador creado exitosamente.");
                }
                else
                {
                    // Log errors for debugging
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"Error creating admin user: {error.Description}");
                    }
                }
            }
            else
            {
                // Asegurar que el admin tenga el rol correcto
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
                
                // Actualizar contraseña si es necesaria
                var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
                await userManager.ResetPasswordAsync(adminUser, token, "Admin123");
                
                Console.WriteLine("Usuario administrador verificado y actualizado.");
            }
        }
    }
}