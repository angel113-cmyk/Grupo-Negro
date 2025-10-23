using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grupo_negro.Migrations
{
    /// <inheritdoc />
    public partial class AgregarUsuariosRealesDePrueba : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Password para todos: Apuesta2024! (hash pre-calculado)
            var passwordHash = "AQAAAAIAAYagAAAAEGVSt9qT3xKLJ6p3zx5xC8kxQp7UvQ+YwBl5z1rZF8xN6pL9mKjHgFdE4cB3aA2sQw==";
            
            // Usuario 1: Carlos Mendoza
            migrationBuilder.Sql($@"
                INSERT INTO AspNetUsers 
                (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, 
                 PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, 
                 TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount,
                 Nombres, Apellidos, DNI, Celular, Negocio, Saldo, FechaRegistro)
                VALUES 
                ('{Guid.NewGuid()}', 'carlos.mendoza@gmail.com', 'CARLOS.MENDOZA@GMAIL.COM', 
                 'carlos.mendoza@gmail.com', 'CARLOS.MENDOZA@GMAIL.COM', 1, 
                 '{passwordHash}', '{Guid.NewGuid()}', '{Guid.NewGuid()}', 
                 '+51 987654321', 0, 0, NULL, 1, 0,
                 'Carlos', 'Mendoza Ríos', '72548963', '+51 987654321', 
                 'Bodega El Triunfo', 2500.00, '{DateTime.Now.AddDays(-25):yyyy-MM-dd HH:mm:ss}');
            ");

            // Usuario 2: María García
            migrationBuilder.Sql($@"
                INSERT INTO AspNetUsers 
                (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, 
                 PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, 
                 TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount,
                 Nombres, Apellidos, DNI, Celular, Negocio, Saldo, FechaRegistro)
                VALUES 
                ('{Guid.NewGuid()}', 'maria.garcia@hotmail.com', 'MARIA.GARCIA@HOTMAIL.COM', 
                 'maria.garcia@hotmail.com', 'MARIA.GARCIA@HOTMAIL.COM', 1, 
                 '{passwordHash}', '{Guid.NewGuid()}', '{Guid.NewGuid()}', 
                 '+51 965874123', 0, 0, NULL, 1, 0,
                 'María', 'García López', '45896321', '+51 965874123', 
                 'Restaurante Don José', 1800.00, '{DateTime.Now.AddDays(-22):yyyy-MM-dd HH:mm:ss}');
            ");

            // Usuario 3: Luis Torres
            migrationBuilder.Sql($@"
                INSERT INTO AspNetUsers 
                (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, 
                 PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, 
                 TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount,
                 Nombres, Apellidos, DNI, Celular, Negocio, Saldo, FechaRegistro)
                VALUES 
                ('{Guid.NewGuid()}', 'luis.torres@yahoo.com', 'LUIS.TORRES@YAHOO.COM', 
                 'luis.torres@yahoo.com', 'LUIS.TORRES@YAHOO.COM', 1, 
                 '{passwordHash}', '{Guid.NewGuid()}', '{Guid.NewGuid()}', 
                 '+51 923456789', 0, 0, NULL, 1, 0,
                 'Luis', 'Torres Sánchez', '63258741', '+51 923456789', 
                 'Ferretería San Martín', 3200.00, '{DateTime.Now.AddDays(-18):yyyy-MM-dd HH:mm:ss}');
            ");

            // Usuario 4: Ana Flores
            migrationBuilder.Sql($@"
                INSERT INTO AspNetUsers 
                (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, 
                 PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, 
                 TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount,
                 Nombres, Apellidos, DNI, Celular, Negocio, Saldo, FechaRegistro)
                VALUES 
                ('{Guid.NewGuid()}', 'ana.flores@outlook.com', 'ANA.FLORES@OUTLOOK.COM', 
                 'ana.flores@outlook.com', 'ANA.FLORES@OUTLOOK.COM', 1, 
                 '{passwordHash}', '{Guid.NewGuid()}', '{Guid.NewGuid()}', 
                 '+51 998765432', 0, 0, NULL, 1, 0,
                 'Ana', 'Flores Vega', '78451236', '+51 998765432', 
                 'Peluquería Estilo', 1500.00, '{DateTime.Now.AddDays(-15):yyyy-MM-dd HH:mm:ss}');
            ");

            // Usuario 5: Jorge Ramírez
            migrationBuilder.Sql($@"
                INSERT INTO AspNetUsers 
                (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, 
                 PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, 
                 TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount,
                 Nombres, Apellidos, DNI, Celular, Negocio, Saldo, FechaRegistro)
                VALUES 
                ('{Guid.NewGuid()}', 'jorge.ramirez@gmail.com', 'JORGE.RAMIREZ@GMAIL.COM', 
                 'jorge.ramirez@gmail.com', 'JORGE.RAMIREZ@GMAIL.COM', 1, 
                 '{passwordHash}', '{Guid.NewGuid()}', '{Guid.NewGuid()}', 
                 '+51 956321478', 0, 0, NULL, 1, 0,
                 'Jorge', 'Ramírez Castro', '85296374', '+51 956321478', 
                 'Taxi Los Amigos', 2100.00, '{DateTime.Now.AddDays(-12):yyyy-MM-dd HH:mm:ss}');
            ");

            // Usuario 6: Patricia Silva
            migrationBuilder.Sql($@"
                INSERT INTO AspNetUsers 
                (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, 
                 PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, 
                 TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount,
                 Nombres, Apellidos, DNI, Celular, Negocio, Saldo, FechaRegistro)
                VALUES 
                ('{Guid.NewGuid()}', 'patricia.silva@hotmail.com', 'PATRICIA.SILVA@HOTMAIL.COM', 
                 'patricia.silva@hotmail.com', 'PATRICIA.SILVA@HOTMAIL.COM', 1, 
                 '{passwordHash}', '{Guid.NewGuid()}', '{Guid.NewGuid()}', 
                 '+51 974185263', 0, 0, NULL, 1, 0,
                 'Patricia', 'Silva Morales', '96325874', '+51 974185263', 
                 'Bazar La Economía', 1950.00, '{DateTime.Now.AddDays(-9):yyyy-MM-dd HH:mm:ss}');
            ");

            // Usuario 7: Roberto Vargas
            migrationBuilder.Sql($@"
                INSERT INTO AspNetUsers 
                (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, 
                 PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, 
                 TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount,
                 Nombres, Apellidos, DNI, Celular, Negocio, Saldo, FechaRegistro)
                VALUES 
                ('{Guid.NewGuid()}', 'roberto.vargas@yahoo.com', 'ROBERTO.VARGAS@YAHOO.COM', 
                 'roberto.vargas@yahoo.com', 'ROBERTO.VARGAS@YAHOO.COM', 1, 
                 '{passwordHash}', '{Guid.NewGuid()}', '{Guid.NewGuid()}', 
                 '+51 985236147', 0, 0, NULL, 1, 0,
                 'Roberto', 'Vargas Paredes', '74185296', '+51 985236147', 
                 'Construcciones RV', 2800.00, '{DateTime.Now.AddDays(-7):yyyy-MM-dd HH:mm:ss}');
            ");

            // Usuario 8: Carmen Díaz
            migrationBuilder.Sql($@"
                INSERT INTO AspNetUsers 
                (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, 
                 PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, 
                 TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount,
                 Nombres, Apellidos, DNI, Celular, Negocio, Saldo, FechaRegistro)
                VALUES 
                ('{Guid.NewGuid()}', 'carmen.diaz@gmail.com', 'CARMEN.DIAZ@GMAIL.COM', 
                 'carmen.diaz@gmail.com', 'CARMEN.DIAZ@GMAIL.COM', 1, 
                 '{passwordHash}', '{Guid.NewGuid()}', '{Guid.NewGuid()}', 
                 '+51 963852741', 0, 0, NULL, 1, 0,
                 'Carmen', 'Díaz Rojas', '85274136', '+51 963852741', 
                 'Panadería Santa Rosa', 1650.00, '{DateTime.Now.AddDays(-5):yyyy-MM-dd HH:mm:ss}');
            ");

            // Usuario 9: Fernando Chávez
            migrationBuilder.Sql($@"
                INSERT INTO AspNetUsers 
                (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, 
                 PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, 
                 TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount,
                 Nombres, Apellidos, DNI, Celular, Negocio, Saldo, FechaRegistro)
                VALUES 
                ('{Guid.NewGuid()}', 'fernando.chavez@outlook.com', 'FERNANDO.CHAVEZ@OUTLOOK.COM', 
                 'fernando.chavez@outlook.com', 'FERNANDO.CHAVEZ@OUTLOOK.COM', 1, 
                 '{passwordHash}', '{Guid.NewGuid()}', '{Guid.NewGuid()}', 
                 '+51 992583614', 0, 0, NULL, 1, 0,
                 'Fernando', 'Chávez Luna', '96374185', '+51 992583614', 
                 'Zapatería El Calzado', 2300.00, '{DateTime.Now.AddDays(-3):yyyy-MM-dd HH:mm:ss}');
            ");

            // Usuario 10: Lucía Herrera
            migrationBuilder.Sql($@"
                INSERT INTO AspNetUsers 
                (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, 
                 PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, 
                 TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount,
                 Nombres, Apellidos, DNI, Celular, Negocio, Saldo, FechaRegistro)
                VALUES 
                ('{Guid.NewGuid()}', 'lucia.herrera@gmail.com', 'LUCIA.HERRERA@GMAIL.COM', 
                 'lucia.herrera@gmail.com', 'LUCIA.HERRERA@GMAIL.COM', 1, 
                 '{passwordHash}', '{Guid.NewGuid()}', '{Guid.NewGuid()}', 
                 '+51 978451236', 0, 0, NULL, 1, 0,
                 'Lucía', 'Herrera Campos', '74125836', '+51 978451236', 
                 'Farmacia San Juan', 1750.00, '{DateTime.Now.AddDays(-1):yyyy-MM-dd HH:mm:ss}');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
