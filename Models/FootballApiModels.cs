using Newtonsoft.Json;

namespace Grupo_negro.Models
{
    // Modelos para Football-Data.org API
    public class FootballApiCompetition
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("code")]
        public string Code { get; set; }
        
        [JsonProperty("type")]
        public string Type { get; set; }
        
        [JsonProperty("emblem")]
        public string Emblem { get; set; }
        
        [JsonProperty("currentSeason")]
        public FootballApiSeason CurrentSeason { get; set; }
    }

    public class FootballApiSeason
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        
        [JsonProperty("startDate")]
        public DateTime StartDate { get; set; }
        
        [JsonProperty("endDate")]
        public DateTime EndDate { get; set; }
        
        [JsonProperty("currentMatchday")]
        public int CurrentMatchday { get; set; }
    }

    public class FootballApiTeam
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("shortName")]
        public string ShortName { get; set; }
        
        [JsonProperty("tla")]
        public string Tla { get; set; }
        
        [JsonProperty("crest")]
        public string Crest { get; set; }
        
        [JsonProperty("founded")]
        public int? Founded { get; set; }
        
        [JsonProperty("venue")]
        public string Venue { get; set; }
    }

    public class FootballApiMatch
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        
        [JsonProperty("utcDate")]
        public DateTime UtcDate { get; set; }
        
        [JsonProperty("status")]
        public string Status { get; set; }
        
        [JsonProperty("matchday")]
        public int Matchday { get; set; }
        
        [JsonProperty("stage")]
        public string Stage { get; set; }
        
        [JsonProperty("homeTeam")]
        public FootballApiTeam HomeTeam { get; set; }
        
        [JsonProperty("awayTeam")]
        public FootballApiTeam AwayTeam { get; set; }
        
        [JsonProperty("score")]
        public FootballApiScore Score { get; set; }
        
        [JsonProperty("odds")]
        public FootballApiOdds Odds { get; set; }
        
        [JsonProperty("competition")]
        public FootballApiCompetition Competition { get; set; }
    }

    public class FootballApiScore
    {
        [JsonProperty("winner")]
        public string Winner { get; set; }
        
        [JsonProperty("duration")]
        public string Duration { get; set; }
        
        [JsonProperty("fullTime")]
        public FootballApiResult FullTime { get; set; }
        
        [JsonProperty("halfTime")]
        public FootballApiResult HalfTime { get; set; }
    }

    public class FootballApiResult
    {
        [JsonProperty("home")]
        public int? Home { get; set; }
        
        [JsonProperty("away")]
        public int? Away { get; set; }
    }

    public class FootballApiOdds
    {
        [JsonProperty("msg")]
        public string Message { get; set; }
        
        // Simulamos odds ya que la API gratuita no las incluye
        public decimal HomeWin => GenerateOdds(1.5m, 3.5m);
        public decimal Draw => GenerateOdds(2.8m, 4.2m);
        public decimal AwayWin => GenerateOdds(1.8m, 4.0m);
        
        private decimal GenerateOdds(decimal min, decimal max)
        {
            var random = new Random();
            return Math.Round((decimal)(random.NextDouble() * (double)(max - min) + (double)min), 2);
        }
    }

    // Respuestas de la API
    public class FootballApiCompetitionsResponse
    {
        [JsonProperty("competitions")]
        public List<FootballApiCompetition> Competitions { get; set; }
    }

    public class FootballApiTeamsResponse
    {
        [JsonProperty("teams")]
        public List<FootballApiTeam> Teams { get; set; }
    }

    public class FootballApiMatchesResponse
    {
        [JsonProperty("matches")]
        public List<FootballApiMatch> Matches { get; set; }
    }

    // Modelos para configuración
    public class FootballApiSettings
    {
        public string BaseUrl { get; set; } = "https://api.football-data.org/v4";
        public string ApiKey { get; set; }
        public int RequestDelayMs { get; set; } = 6000; // 10 requests per minute limit
    }
}