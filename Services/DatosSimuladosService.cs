using Grupo_negro.Data;
using Grupo_negro.Models;
using Microsoft.EntityFrameworkCore;

namespace Grupo_negro.Services
{
    public class DatosSimuladosService
    {
        private readonly ApplicationDbContext _context;

        public DatosSimuladosService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task InicializarDatosAsync()
        {
            // Verificar si ya existen datos
            if (await _context.Ligas.AnyAsync())
                return;

            await CrearLigasYEquiposAsync();
            await CrearPartidosAsync();
            await _context.SaveChangesAsync();
        }

        public async Task RegenerarPartidosAsync()
        {
            // Eliminar partidos existentes
            var partidosExistentes = await _context.Partidos.ToListAsync();
            _context.Partidos.RemoveRange(partidosExistentes);
            
            // Crear nuevos partidos con fechas futuras
            await CrearPartidosAsync();
            await _context.SaveChangesAsync();
            
            Console.WriteLine($"Regenerados {partidosExistentes.Count} partidos con fechas futuras");
        }

        private async Task CrearLigasYEquiposAsync()
        {
            // Premier League (Inglaterra)
            var premierLeague = new Liga
            {
                Nombre = "Premier League",
                Pais = "Inglaterra",
                Descripcion = "La liga de fútbol más competitiva del mundo"
            };
            _context.Ligas.Add(premierLeague);
            await _context.SaveChangesAsync();

            var equiposPremier = new List<Equipo>
            {
                new Equipo { Nombre = "Manchester City", Ciudad = "Manchester", LigaId = premierLeague.Id },
                new Equipo { Nombre = "Arsenal", Ciudad = "Londres", LigaId = premierLeague.Id },
                new Equipo { Nombre = "Liverpool", Ciudad = "Liverpool", LigaId = premierLeague.Id },
                new Equipo { Nombre = "Chelsea", Ciudad = "Londres", LigaId = premierLeague.Id },
                new Equipo { Nombre = "Manchester United", Ciudad = "Manchester", LigaId = premierLeague.Id },
                new Equipo { Nombre = "Tottenham", Ciudad = "Londres", LigaId = premierLeague.Id }
            };
            _context.Equipos.AddRange(equiposPremier);

            // La Liga (España)
            var laLiga = new Liga
            {
                Nombre = "La Liga",
                Pais = "España",
                Descripcion = "Primera División del fútbol español"
            };
            _context.Ligas.Add(laLiga);
            await _context.SaveChangesAsync();

            var equiposLaLiga = new List<Equipo>
            {
                new Equipo { Nombre = "Real Madrid", Ciudad = "Madrid", LigaId = laLiga.Id },
                new Equipo { Nombre = "FC Barcelona", Ciudad = "Barcelona", LigaId = laLiga.Id },
                new Equipo { Nombre = "Atlético Madrid", Ciudad = "Madrid", LigaId = laLiga.Id },
                new Equipo { Nombre = "Sevilla", Ciudad = "Sevilla", LigaId = laLiga.Id },
                new Equipo { Nombre = "Valencia", Ciudad = "Valencia", LigaId = laLiga.Id },
                new Equipo { Nombre = "Real Sociedad", Ciudad = "San Sebastián", LigaId = laLiga.Id }
            };
            _context.Equipos.AddRange(equiposLaLiga);

            // Serie A (Italia)
            var serieA = new Liga
            {
                Nombre = "Serie A",
                Pais = "Italia",
                Descripcion = "Primera División del fútbol italiano"
            };
            _context.Ligas.Add(serieA);
            await _context.SaveChangesAsync();

            var equiposSerieA = new List<Equipo>
            {
                new Equipo { Nombre = "Juventus", Ciudad = "Turín", LigaId = serieA.Id },
                new Equipo { Nombre = "Inter Milan", Ciudad = "Milán", LigaId = serieA.Id },
                new Equipo { Nombre = "AC Milan", Ciudad = "Milán", LigaId = serieA.Id },
                new Equipo { Nombre = "Napoli", Ciudad = "Nápoles", LigaId = serieA.Id },
                new Equipo { Nombre = "Roma", Ciudad = "Roma", LigaId = serieA.Id },
                new Equipo { Nombre = "Lazio", Ciudad = "Roma", LigaId = serieA.Id }
            };
            _context.Equipos.AddRange(equiposSerieA);

            // Bundesliga (Alemania)
            var bundesliga = new Liga
            {
                Nombre = "Bundesliga",
                Pais = "Alemania",
                Descripcion = "Primera División del fútbol alemán"
            };
            _context.Ligas.Add(bundesliga);
            await _context.SaveChangesAsync();

            var equiposBundesliga = new List<Equipo>
            {
                new Equipo { Nombre = "Bayern Munich", Ciudad = "Múnich", LigaId = bundesliga.Id },
                new Equipo { Nombre = "Borussia Dortmund", Ciudad = "Dortmund", LigaId = bundesliga.Id },
                new Equipo { Nombre = "RB Leipzig", Ciudad = "Leipzig", LigaId = bundesliga.Id },
                new Equipo { Nombre = "Bayer Leverkusen", Ciudad = "Leverkusen", LigaId = bundesliga.Id },
                new Equipo { Nombre = "Eintracht Frankfurt", Ciudad = "Frankfurt", LigaId = bundesliga.Id },
                new Equipo { Nombre = "Borussia M'gladbach", Ciudad = "Mönchengladbach", LigaId = bundesliga.Id }
            };
            _context.Equipos.AddRange(equiposBundesliga);

            await _context.SaveChangesAsync();
        }

        private async Task CrearPartidosAsync()
        {
            var ligas = await _context.Ligas.Include(l => l.Equipos).ToListAsync();
            var random = new Random();
            var fechaBase = DateTime.Now.AddDays(1);

            foreach (var liga in ligas)
            {
                var equipos = liga.Equipos.ToList();
                
                // Crear 5 partidos por liga con diferentes combinaciones
                for (int i = 0; i < 5; i++)
                {
                    var equipoLocal = equipos[random.Next(equipos.Count)];
                    var equipoVisitante = equipos[random.Next(equipos.Count)];
                    
                    // Asegurar que no sean el mismo equipo
                    while (equipoVisitante.Id == equipoLocal.Id)
                    {
                        equipoVisitante = equipos[random.Next(equipos.Count)];
                    }

                    var partido = new Partido
                    {
                        EquipoLocalId = equipoLocal.Id,
                        EquipoVisitanteId = equipoVisitante.Id,
                        LigaId = liga.Id,
                        FechaHora = fechaBase.AddDays(i).AddHours(random.Next(14, 22)),
                        Jornada = $"Jornada {i + 1}",
                        CuotaLocal = (decimal)(1.2 + random.NextDouble() * 2.0), // 1.2 - 3.2
                        CuotaEmpate = (decimal)(2.5 + random.NextDouble() * 1.5), // 2.5 - 4.0
                        CuotaVisitante = (decimal)(1.2 + random.NextDouble() * 2.0) // 1.2 - 3.2
                    };

                    _context.Partidos.Add(partido);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}