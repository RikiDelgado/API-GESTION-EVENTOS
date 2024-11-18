using System.ComponentModel.DataAnnotations;

public class Evento
{
    public int Id { get; set; } // Este no es requerido, la base de datos lo genera.
    
    [Required] // Obligatorio
    public string Titulo { get; set; }
    
    [Required] // Obligatorio
    public string Descripcion { get; set; }
    
    [Required] // Obligatorio
    public DateTime Fecha { get; set; }
    
    // Opcional al crear el evento
    public List<EventoUsuario> Participantes { get; set; } = new List<EventoUsuario>();
}
