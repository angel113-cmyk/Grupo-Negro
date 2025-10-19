using System.ComponentModel.DataAnnotations;

namespace Grupo_negro.Models
{
    public class ComentarioViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El contenido del comentario es obligatorio")]
        [StringLength(1000, ErrorMessage = "El comentario no puede exceder 1000 caracteres")]
        [Display(Name = "Escribe tu comentario...")]
        public string Contenido { get; set; } = string.Empty;

        public int? ComentarioPadreId { get; set; }
        
        public string? NombreAutor { get; set; }
        
        public DateTime FechaCreacion { get; set; }
        
        public bool EsEdicion { get; set; } = false;
    }

    public class ListaComentariosViewModel
    {
        public List<Comentario> Comentarios { get; set; } = new List<Comentario>();
        public ComentarioViewModel NuevoComentario { get; set; } = new ComentarioViewModel();
        public int TotalComentarios { get; set; }
        public bool PuedeComentarUsuario { get; set; } = true;
    }
}