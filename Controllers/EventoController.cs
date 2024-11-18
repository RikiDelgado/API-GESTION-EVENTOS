using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Linq;
using System;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/eventos")]
public class EventoUsuarioController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public EventoUsuarioController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Crear un nuevo evento
    [HttpPost]
    public async Task<IActionResult> CrearEvento([FromBody] EventoCreacionDTO model)
    {
        if (model.Fecha.Date < DateTime.Today)
        {
            return BadRequest(new { Message = "No se puede crear un evento en el pasado" });
        }

        var nuevoEvento = new Evento
        {
            Titulo = model.Titulo,
            Descripcion = model.Descripcion,
            Fecha = model.Fecha.Date
        };

        _context.Eventos.Add(nuevoEvento);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(ObtenerEventoPorId), new { eventoId = nuevoEvento.Id }, new { Message = "Evento creado satisfactoriamente", EventoId = nuevoEvento.Id });
    }

    // Inscribir un usuario a un evento
    [HttpPost("{eventoId}/inscribir")]
    public async Task<IActionResult> InscribirUsuario(int eventoId, [FromBody] string username)
    {
        var usuario = await _userManager.FindByNameAsync(username);
        if (usuario == null) return NotFound(new { Message = "Usuario no encontrado" });

        var evento = await _context.Eventos.FindAsync(eventoId);
        if (evento == null) return NotFound(new { Message = "Evento no encontrado" });

        if (evento.Fecha < DateTime.Now) return BadRequest(new { Message = "No se puede inscribir a un evento que ya ha pasado" });

        var existeInscripcion = await _context.EventoUsuarios.AnyAsync(eu => eu.EventoId == eventoId && eu.ApplicationUserId == usuario.Id);
        if (existeInscripcion) return BadRequest(new { Message = "El usuario ya está inscrito en este evento" });

        var eventoUsuario = new EventoUsuario
        {
            EventoId = eventoId,
            ApplicationUserId = usuario.Id
        };

        _context.EventoUsuarios.Add(eventoUsuario);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Usuario inscrito en el evento exitosamente" });
    }

    // Obtener todos los eventos
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> ObtenerEventos()
    {
        var eventos = await _context.Eventos
            .Include(e => e.Participantes)
            .Select(e => new
            {
                e.Id,
                e.Titulo,
                e.Descripcion,
                e.Fecha,
                Participantes = e.Participantes.Select(p => p.ApplicationUser.UserName)
            })
            .ToListAsync();

        return Ok(eventos);
    }

    // Obtener evento por ID
    [HttpGet("{eventoId}")]
    public async Task<IActionResult> ObtenerEventoPorId(int eventoId)
    {
        var evento = await _context.Eventos
            .Include(e => e.Participantes)
            .ThenInclude(eu => eu.ApplicationUser)
            .FirstOrDefaultAsync(e => e.Id == eventoId);

        if (evento == null) return NotFound(new { Message = "Evento no encontrado" });

        return Ok(new
        {
            evento.Id,
            evento.Titulo,
            evento.Descripcion,
            evento.Fecha,
            Participantes = evento.Participantes.Select(p => p.ApplicationUser.UserName)
        });
    }

    // Obtener los participantes de un evento
    [HttpGet("{eventoId}/participantes")]
    public async Task<IActionResult> ObtenerParticipantes(int eventoId)
    {
        var participantes = await _context.EventoUsuarios
            .Where(eu => eu.EventoId == eventoId)
            .Include(eu => eu.ApplicationUser)
            .Select(eu => eu.ApplicationUser.UserName)
            .ToListAsync();

        if (!participantes.Any()) return NotFound(new { Message = "No hay participantes para este evento o evento no encontrado" });

        return Ok(participantes);
    }

    // Desinscribir un usuario de un evento
    [HttpPost("{eventoId}/desinscribir")]
    public async Task<IActionResult> DesinscribirUsuario(int eventoId, [FromBody] string username)
    {
        var usuario = await _userManager.FindByNameAsync(username);
        if (usuario == null) return NotFound(new { Message = "Usuario no encontrado" });

        var eventoUsuario = await _context.EventoUsuarios
            .FirstOrDefaultAsync(eu => eu.EventoId == eventoId && eu.ApplicationUserId == usuario.Id);

        if (eventoUsuario == null) return BadRequest(new { Message = "El usuario no está inscrito en este evento" });

        _context.EventoUsuarios.Remove(eventoUsuario);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Usuario desinscrito del evento exitosamente" });
    }
    [HttpDelete("eventos/{id}")]
[Authorize(Roles = "Admin")] // Solo administradores pueden eliminar eventos
public async Task<IActionResult> DeleteEvento(int id)
{
    // Buscar el evento por su ID
    var evento = await _context.Eventos.FindAsync(id);
    if (evento == null)
    {
        return NotFound(new { Message = "Evento no encontrado" });
    }

    // Eliminar el evento de la base de datos
    _context.Eventos.Remove(evento);
    await _context.SaveChangesAsync();

    // Devolver una respuesta exitosa
    return Ok(new { Message = "Evento eliminado correctamente" });
}

}
