public class EventoUsuario
{    
    public int EventoId { get; set; }
    public string ApplicationUserId { get; set; }  // Mantén esto como clave foránea hacia ApplicationUser.

    // Relaciones de navegación
    public Evento Evento { get; set; }
    public ApplicationUser ApplicationUser { get; set; }
}
