using Grupo_negro.Models;
using Grupo_negro.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;

namespace Grupo_negro.Services
{
    public interface IFootballApiService
    {
        Task<List<FootballApiCompetition>> GetCompetitionsAsync();
        Task<List<FootballApiTeam>> GetTeamsByCompetitionAsync(int competitionId);
        Task<List<FootballApiMatch>> GetMatchesByCompetitionAsync(int competitionId, int days = 7);
        Task<FootballApiMatch?> GetMatchByIdAsync(int matchId);
        Task<List<FootballApiMatch>> GetTodayMatchesAsync();
        Task SyncDataToLocalDatabaseAsync();
    }

    public class FootballApiService : IFootballApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FootballApiService> _logger;
        private readonly FootballApiSettings _settings;
        private readonly ApplicationDbContext _context;
        private readonly IServiceScopeFactory _scopeFactory;

        public FootballApiService(
            HttpClient httpClient,
            ILogger<FootballApiService> logger,
            IConfiguration configuration,
            ApplicationDbContext context,
            IServiceScopeFactory scopeFactory)
        {
            _httpClient = httpClient;
            _logger = logger;
            _context = context;
            _scopeFactory = scopeFactory;
            
            _settings = new FootballApiSettings
            {
                BaseUrl = configuration["FootballApi:BaseUrl"] ?? "https://api.football-data.org/v4",
                ApiKey = configuration["FootballApi:ApiKey"] ?? "demo", // Token gratuito
                RequestDelayMs = int.Parse(configuration["FootballApi:RequestDelayMs"] ?? "1000")
            };

            // Configurar headers
            _httpClient.DefaultRequestHeaders.Add("X-Auth-Token", _settings.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Grupo-Negro-Betting/1.0");
        }

        public async Task<List<FootballApiCompetition>> GetCompetitionsAsync()
        {
            try
            {
                await DelayRequest();
                var response = await _httpClient.GetStringAsync($"{_settings.BaseUrl}/competitions");
                var competitionsResponse = JsonConvert.DeserializeObject<FootballApiCompetitionsResponse>(response);
                
                _logger.LogInformation($"Obtenidas {competitionsResponse?.Competitions?.Count ?? 0} competiciones");
                return competitionsResponse?.Competitions ?? new List<FootballApiCompetition>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al obtener competiciones de Football API, usando datos simulados");
                return GetSimulatedCompetitions();
            }
        }

        public async Task<List<FootballApiTeam>> GetTeamsByCompetitionAsync(int competitionId)
        {
            try
            {
                await DelayRequest();
                var response = await _httpClient.GetStringAsync($"{_settings.BaseUrl}/competitions/{competitionId}/teams");
                var teamsResponse = JsonConvert.DeserializeObject<FootballApiTeamsResponse>(response);
                
                _logger.LogInformation($"Obtenidos {teamsResponse?.Teams?.Count ?? 0} equipos para competición {competitionId}");
                return teamsResponse?.Teams ?? new List<FootballApiTeam>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error al obtener equipos de competición {competitionId}, usando datos simulados");
                return GetSimulatedTeams(competitionId);
            }
        }

        public async Task<List<FootballApiMatch>> GetMatchesByCompetitionAsync(int competitionId, int days = 7)
        {
            try
            {
                await DelayRequest();
                var dateFrom = DateTime.Today.ToString("yyyy-MM-dd");
                var dateTo = DateTime.Today.AddDays(days).ToString("yyyy-MM-dd");
                
                var response = await _httpClient.GetStringAsync(
                    $"{_settings.BaseUrl}/competitions/{competitionId}/matches?dateFrom={dateFrom}&dateTo={dateTo}");
                var matchesResponse = JsonConvert.DeserializeObject<FootballApiMatchesResponse>(response);
                
                _logger.LogInformation($"Obtenidos {matchesResponse?.Matches?.Count ?? 0} partidos para competición {competitionId}");
                return matchesResponse?.Matches ?? new List<FootballApiMatch>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error al obtener partidos de competición {competitionId}, usando datos simulados");
                return GetSimulatedMatches(competitionId);
            }
        }

        public async Task<FootballApiMatch?> GetMatchByIdAsync(int matchId)
        {
            try
            {
                await DelayRequest();
                var response = await _httpClient.GetStringAsync($"{_settings.BaseUrl}/matches/{matchId}");
                var match = JsonConvert.DeserializeObject<FootballApiMatch>(response);
                
                _logger.LogInformation($"Obtenido partido {matchId}");
                return match;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener partido {matchId}");
                return null;
            }
        }

        public async Task<List<FootballApiMatch>> GetTodayMatchesAsync()
        {
            try
            {
                await DelayRequest();
                var today = DateTime.Today.ToString("yyyy-MM-dd");
                
                var response = await _httpClient.GetStringAsync($"{_settings.BaseUrl}/matches?date={today}");
                var matchesResponse = JsonConvert.DeserializeObject<FootballApiMatchesResponse>(response);
                
                _logger.LogInformation($"Obtenidos {matchesResponse?.Matches?.Count ?? 0} partidos para hoy");
                return matchesResponse?.Matches ?? new List<FootballApiMatch>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al obtener partidos de hoy, usando datos simulados");
                return GetSimulatedTodayMatches();
            }
        }

        public async Task SyncDataToLocalDatabaseAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                _logger.LogInformation("Iniciando sincronización de datos deportivos...");

                // 1. Sincronizar competiciones principales
                var competitions = await GetCompetitionsAsync();
                var mainCompetitions = competitions.Where(c => 
                    c.Type == "LEAGUE" && 
                    (c.Code == "PL" || c.Code == "PD" || c.Code == "SA" || c.Code == "BL1" || c.Code == "FL1"))
                    .Take(5).ToList();

                foreach (var apiCompetition in mainCompetitions)
                {
                    await SyncCompetitionAsync(context, apiCompetition);
                    await Task.Delay(_settings.RequestDelayMs); // Respetar límites de API
                }

                await context.SaveChangesAsync();
                _logger.LogInformation("Sincronización completada exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la sincronización de datos");
            }
        }

        private async Task SyncCompetitionAsync(ApplicationDbContext context, FootballApiCompetition apiCompetition)
        {
            // Crear o actualizar liga
            var liga = await context.Ligas.FirstOrDefaultAsync(l => l.Nombre == apiCompetition.Name);
            if (liga == null)
            {
                liga = new Liga
                {
                    Nombre = apiCompetition.Name,
                    Pais = GetCountryFromCompetition(apiCompetition.Code)
                };
                context.Ligas.Add(liga);
                await context.SaveChangesAsync();
            }

            // Obtener equipos de la competición
            var apiTeams = await GetTeamsByCompetitionAsync(apiCompetition.Id);
            foreach (var apiTeam in apiTeams.Take(20)) // Límite para evitar exceso de datos
            {
                var equipo = await context.Equipos.FirstOrDefaultAsync(e => e.Nombre == apiTeam.Name);
                if (equipo == null)
                {
                    equipo = new Equipo
                    {
                        Nombre = apiTeam.Name,
                        LigaId = liga.Id,
                        EscudoUrl = apiTeam.Crest ?? "/img/default-team.png"
                    };
                    context.Equipos.Add(equipo);
                }
            }

            await context.SaveChangesAsync();

            // Obtener partidos próximos
            var apiMatches = await GetMatchesByCompetitionAsync(apiCompetition.Id, 14);
            foreach (var apiMatch in apiMatches.Take(10))
            {
                await SyncMatchAsync(context, apiMatch, liga.Id);
            }
        }

        private async Task SyncMatchAsync(ApplicationDbContext context, FootballApiMatch apiMatch, int ligaId)
        {
            var equipoLocal = await context.Equipos.FirstOrDefaultAsync(e => e.Nombre == apiMatch.HomeTeam.Name);
            var equipoVisitante = await context.Equipos.FirstOrDefaultAsync(e => e.Nombre == apiMatch.AwayTeam.Name);

            if (equipoLocal == null || equipoVisitante == null) return;

            var partidoExistente = await context.Partidos.FirstOrDefaultAsync(p => 
                p.EquipoLocalId == equipoLocal.Id && 
                p.EquipoVisitanteId == equipoVisitante.Id &&
                p.FechaHora.Date == apiMatch.UtcDate.Date);

            if (partidoExistente == null)
            {
                var partido = new Partido
                {
                    EquipoLocalId = equipoLocal.Id,
                    EquipoVisitanteId = equipoVisitante.Id,
                    LigaId = ligaId,
                    FechaHora = apiMatch.UtcDate.AddHours(-5), // Convertir a hora local Peru
                    CuotaLocal = apiMatch.Odds?.HomeWin ?? GenerateRandomOdds(1.5m, 3.5m),
                    CuotaEmpate = apiMatch.Odds?.Draw ?? GenerateRandomOdds(2.8m, 4.2m),
                    CuotaVisitante = apiMatch.Odds?.AwayWin ?? GenerateRandomOdds(1.8m, 4.0m),
                    Estado = GetEstadoFromApiStatus(apiMatch.Status)
                };

                if (apiMatch.Score?.FullTime?.Home.HasValue == true && apiMatch.Score?.FullTime?.Away.HasValue == true)
                {
                    partido.GolesLocal = apiMatch.Score.FullTime.Home.Value;
                    partido.GolesVisitante = apiMatch.Score.FullTime.Away.Value;
                }

                context.Partidos.Add(partido);
            }
        }

        private EstadoPartido GetEstadoFromApiStatus(string apiStatus)
        {
            return apiStatus switch
            {
                "SCHEDULED" => EstadoPartido.Programado,
                "LIVE" => EstadoPartido.EnJuego,
                "IN_PLAY" => EstadoPartido.EnJuego,
                "PAUSED" => EstadoPartido.EnJuego,
                "FINISHED" => EstadoPartido.Finalizado,
                _ => EstadoPartido.Programado
            };
        }

        private string GetCountryFromCompetition(string code)
        {
            return code switch
            {
                "PL" => "Inglaterra",
                "PD" => "España",
                "SA" => "Italia",
                "BL1" => "Alemania",
                "FL1" => "Francia",
                _ => "Internacional"
            };
        }

        private decimal GenerateRandomOdds(decimal min, decimal max)
        {
            var random = new Random();
            return Math.Round((decimal)(random.NextDouble() * (double)(max - min) + (double)min), 2);
        }

        private async Task DelayRequest()
        {
            await Task.Delay(_settings.RequestDelayMs);
        }

        // Métodos para datos simulados como fallback
        private List<FootballApiCompetition> GetSimulatedCompetitions()
        {
            return new List<FootballApiCompetition>
            {
                new FootballApiCompetition
                {
                    Id = 2021,
                    Name = "Premier League",
                    Code = "PL",
                    Type = "LEAGUE",
                    Emblem = "https://crests.football-data.org/PL.png",
                    CurrentSeason = new FootballApiSeason
                    {
                        Id = 1564,
                        StartDate = DateTime.Now.AddMonths(-2),
                        EndDate = DateTime.Now.AddMonths(4),
                        CurrentMatchday = 8
                    }
                },
                new FootballApiCompetition
                {
                    Id = 2014,
                    Name = "Primera División",
                    Code = "PD",
                    Type = "LEAGUE",
                    Emblem = "https://crests.football-data.org/PD.png",
                    CurrentSeason = new FootballApiSeason
                    {
                        Id = 1565,
                        StartDate = DateTime.Now.AddMonths(-2),
                        EndDate = DateTime.Now.AddMonths(4),
                        CurrentMatchday = 9
                    }
                },
                new FootballApiCompetition
                {
                    Id = 2019,
                    Name = "Serie A",
                    Code = "SA",
                    Type = "LEAGUE",
                    Emblem = "https://crests.football-data.org/SA.png",
                    CurrentSeason = new FootballApiSeason
                    {
                        Id = 1566,
                        StartDate = DateTime.Now.AddMonths(-2),
                        EndDate = DateTime.Now.AddMonths(4),
                        CurrentMatchday = 8
                    }
                },
                new FootballApiCompetition
                {
                    Id = 2002,
                    Name = "Bundesliga",
                    Code = "BL1",
                    Type = "LEAGUE",
                    Emblem = "https://crests.football-data.org/BL1.png",
                    CurrentSeason = new FootballApiSeason
                    {
                        Id = 1567,
                        StartDate = DateTime.Now.AddMonths(-2),
                        EndDate = DateTime.Now.AddMonths(4),
                        CurrentMatchday = 7
                    }
                },
                new FootballApiCompetition
                {
                    Id = 2015,
                    Name = "Ligue 1",
                    Code = "FL1",
                    Type = "LEAGUE",
                    Emblem = "https://crests.football-data.org/FL1.png",
                    CurrentSeason = new FootballApiSeason
                    {
                        Id = 1568,
                        StartDate = DateTime.Now.AddMonths(-2),
                        EndDate = DateTime.Now.AddMonths(4),
                        CurrentMatchday = 9
                    }
                },
                new FootballApiCompetition
                {
                    Id = 2013,
                    Name = "Campeonato Brasileiro",
                    Code = "BSA",
                    Type = "LEAGUE",
                    Emblem = "https://crests.football-data.org/BSA.png",
                    CurrentSeason = new FootballApiSeason
                    {
                        Id = 1569,
                        StartDate = DateTime.Now.AddMonths(-8),
                        EndDate = DateTime.Now.AddMonths(-1),
                        CurrentMatchday = 32
                    }
                }
            };
        }

        private List<FootballApiTeam> GetSimulatedTeams(int competitionId)
        {
            return competitionId switch
            {
                2021 => new List<FootballApiTeam> // Premier League
                {
                    new FootballApiTeam { Id = 57, Name = "Arsenal FC", ShortName = "Arsenal", Tla = "ARS", Crest = "https://crests.football-data.org/57.png" },
                    new FootballApiTeam { Id = 61, Name = "Chelsea FC", ShortName = "Chelsea", Tla = "CHE", Crest = "https://crests.football-data.org/61.png" },
                    new FootballApiTeam { Id = 64, Name = "Liverpool FC", ShortName = "Liverpool", Tla = "LIV", Crest = "https://crests.football-data.org/64.png" },
                    new FootballApiTeam { Id = 65, Name = "Manchester City FC", ShortName = "Man City", Tla = "MCI", Crest = "https://crests.football-data.org/65.png" },
                    new FootballApiTeam { Id = 66, Name = "Manchester United FC", ShortName = "Man United", Tla = "MUN", Crest = "https://crests.football-data.org/66.png" },
                    new FootballApiTeam { Id = 73, Name = "Tottenham Hotspur FC", ShortName = "Tottenham", Tla = "TOT", Crest = "https://crests.football-data.org/73.png" }
                },
                2014 => new List<FootballApiTeam> // La Liga
                {
                    new FootballApiTeam { Id = 81, Name = "FC Barcelona", ShortName = "Barcelona", Tla = "FCB", Crest = "https://crests.football-data.org/81.png" },
                    new FootballApiTeam { Id = 86, Name = "Real Madrid CF", ShortName = "Real Madrid", Tla = "RMA", Crest = "https://crests.football-data.org/86.png" },
                    new FootballApiTeam { Id = 78, Name = "Atlético de Madrid", ShortName = "Atlético", Tla = "ATM", Crest = "https://crests.football-data.org/78.png" },
                    new FootballApiTeam { Id = 90, Name = "Real Sociedad de Fútbol", ShortName = "Real Sociedad", Tla = "RSS", Crest = "https://crests.football-data.org/90.png" }
                },
                2019 => new List<FootballApiTeam> // Serie A
                {
                    new FootballApiTeam { Id = 109, Name = "Juventus FC", ShortName = "Juventus", Tla = "JUV", Crest = "https://crests.football-data.org/109.png" },
                    new FootballApiTeam { Id = 113, Name = "SSC Napoli", ShortName = "Napoli", Tla = "NAP", Crest = "https://crests.football-data.org/113.png" },
                    new FootballApiTeam { Id = 98, Name = "AC Milan", ShortName = "Milan", Tla = "MIL", Crest = "https://crests.football-data.org/98.png" },
                    new FootballApiTeam { Id = 108, Name = "Inter", ShortName = "Inter", Tla = "INT", Crest = "https://crests.football-data.org/108.png" }
                },
                _ => new List<FootballApiTeam>()
            };
        }

        private List<FootballApiMatch> GetSimulatedMatches(int competitionId)
        {
            var teams = GetSimulatedTeams(competitionId);
            if (teams.Count < 2) return new List<FootballApiMatch>();

            var matches = new List<FootballApiMatch>();
            var random = new Random();

            // Generar algunos partidos próximos
            for (int i = 0; i < 6; i++)
            {
                var homeTeam = teams[random.Next(teams.Count)];
                var awayTeam = teams[random.Next(teams.Count)];
                while (awayTeam.Id == homeTeam.Id)
                {
                    awayTeam = teams[random.Next(teams.Count)];
                }

                matches.Add(new FootballApiMatch
                {
                    Id = 400000 + competitionId * 100 + i,
                    UtcDate = DateTime.Now.AddDays(random.Next(0, 7)).AddHours(random.Next(10, 20)),
                    Status = "SCHEDULED",
                    Matchday = random.Next(8, 12),
                    Stage = "REGULAR_SEASON",
                    HomeTeam = homeTeam,
                    AwayTeam = awayTeam,
                    Score = new FootballApiScore { Winner = "", Duration = "REGULAR" },
                    Odds = new FootballApiOdds(),
                    Competition = GetSimulatedCompetitions().FirstOrDefault(c => c.Id == competitionId) ?? GetSimulatedCompetitions().First()
                });
            }

            return matches;
        }

        private List<FootballApiMatch> GetSimulatedTodayMatches()
        {
            var matches = new List<FootballApiMatch>();
            var competitions = GetSimulatedCompetitions();
            var random = new Random();

            foreach (var comp in competitions.Take(3))
            {
                var teams = GetSimulatedTeams(comp.Id);
                if (teams.Count >= 2)
                {
                    var homeTeam = teams[random.Next(teams.Count)];
                    var awayTeam = teams[random.Next(teams.Count)];
                    while (awayTeam.Id == homeTeam.Id)
                    {
                        awayTeam = teams[random.Next(teams.Count)];
                    }

                    matches.Add(new FootballApiMatch
                    {
                        Id = 500000 + comp.Id,
                        UtcDate = DateTime.Today.AddHours(random.Next(14, 22)),
                        Status = random.Next(0, 3) switch
                        {
                            0 => "SCHEDULED",
                            1 => "LIVE",
                            _ => "FINISHED"
                        },
                        Matchday = random.Next(8, 12),
                        Stage = "REGULAR_SEASON",
                        HomeTeam = homeTeam,
                        AwayTeam = awayTeam,
                        Score = new FootballApiScore 
                        { 
                            Winner = random.Next(0, 3) switch { 0 => "HOME_TEAM", 1 => "AWAY_TEAM", _ => "DRAW" },
                            Duration = "REGULAR",
                            FullTime = new FootballApiResult 
                            { 
                                Home = random.Next(0, 4), 
                                Away = random.Next(0, 4) 
                            }
                        },
                        Odds = new FootballApiOdds(),
                        Competition = comp
                    });
                }
            }

            return matches;
        }
    }
}