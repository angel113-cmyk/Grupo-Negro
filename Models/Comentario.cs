using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Grupo_negro.Models
{
    public class Comentario
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El contenido del comentario es obligatorio")]
        [StringLength(1000, ErrorMessage = "El comentario no puede exceder 1000 caracteres")]
        [Display(Name = "Comentario")]
        public string Contenido { get; set; } = string.Empty;

        [Display(Name = "Fecha de creación")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Display(Name = "Fecha de modificación")]
        public DateTime? FechaModificacion { get; set; }

        // Relación con el usuario
        [Required]
        public string UsuarioId { get; set; } = string.Empty;
        public ApplicationUser Usuario { get; set; } = null!;

        // Para respuestas anidadas
        [Display(Name = "Comentario padre")]
        public int? ComentarioPadreId { get; set; }
        public Comentario? ComentarioPadre { get; set; }

        // Respuestas a este comentario
        public ICollection<Comentario> Respuestas { get; set; } = new List<Comentario>();

        // Propiedades adicionales
        [Display(Name = "Está editado")]
        public bool EstaEditado => FechaModificacion.HasValue;

        [Display(Name = "Es respuesta")]
        public bool EsRespuesta => ComentarioPadreId.HasValue;

        // Método para obtener el nombre del autor
        public string NombreAutor => $"{Usuario?.Nombres} {Usuario?.Apellidos}".Trim();
    }
}