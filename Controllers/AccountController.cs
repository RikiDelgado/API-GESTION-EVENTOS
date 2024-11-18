using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using api_gestion.DTOs;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;

    public AccountController(
        UserManager<ApplicationUser> userManager, 
        RoleManager<IdentityRole> roleManager, 
        SignInManager<ApplicationUser> signInManager, 
        IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    // Registro de usuarios
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDTO model)
    {
        var userExists = await _userManager.FindByNameAsync(model.Username);
        if (userExists != null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "El usuario ya existe" });
        }

        var user = new ApplicationUser
        {
            UserName = model.Username,
            Email = model.Email,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Error al crear usuario" });
        }

        return Ok(new { Message = "Usuario creado satisfactoriamente" });
    }

    // Login de usuarios y generación de JWT
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO model)
    {
        var user = await _userManager.FindByNameAsync(model.Username);
        if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
        {
            var userRoles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Identificador único para el token
            };

            // Añadir los roles del usuario como claims
            foreach (var role in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo
            });
        }
        return Unauthorized();
    }

    // Asignar un rol a un usuario
    [HttpPost("assign-role")]
    [Authorize(Roles = "Admin")] // Solo los administradores pueden asignar roles
    public async Task<IActionResult> AssignRole([FromBody] RoleAssignmentDTO model)
    {
        var user = await _userManager.FindByNameAsync(model.Username);
        if (user == null)
        {
            return NotFound(new { Message = "Usuario no encontrado" });
        }

        var roleExists = await _userManager.IsInRoleAsync(user, model.Role);
        if (roleExists)
        {
            return BadRequest(new { Message = "El usuario ya tiene este rol" });
        }

        var result = await _userManager.AddToRoleAsync(user, model.Role);
        if (result.Succeeded)
        {
            return Ok(new { Message = "Rol asignado correctamente" });
        }

        return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Error al asignar el rol" });
    }

    // Obtener todos los roles
    [HttpGet("roles")]
    [Authorize(Roles = "Admin")] // Solo los administradores pueden ver los roles
    public async Task<IActionResult> GetRoles()
    {
        List<IdentityRole> roles = await _roleManager.Roles.ToListAsync();
        return Ok(roles);
    }

    // Obtener todos los usuarios
    [HttpGet("users")]
    [Authorize(Roles = "Admin")] // Solo los administradores pueden ver los usuarios
    public async Task<IActionResult> GetUsers()
    {
        List<ApplicationUser> users = await _userManager.Users.ToListAsync();
        return Ok(users);
    }

    // Obtener los roles de un usuario específico
    [HttpGet("users/{id}/roles")]
    [Authorize(Roles = "Admin")] // Solo los administradores pueden ver los roles de un usuario
    public async Task<IActionResult> GetUserRoles(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user != null)
        {
            IList<string> roles = await _userManager.GetRolesAsync(user);
            return Ok(roles);
        }
        return NotFound(new { Message = "Usuario no encontrado" });
    }

    // Crear un rol nuevo
    [HttpPost("role")]
    [Authorize(Roles = "Admin")] // Solo los administradores pueden crear roles
    public async Task<IActionResult> CreateRole([FromBody] string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return BadRequest("El nombre del rol no puede estar vacío.");
        }

        // Verifica si el rol ya existe
        var roleExists = await _roleManager.RoleExistsAsync(roleName);
        if (roleExists)
        {
            return Conflict($"El rol '{roleName}' ya existe.");
        }

        // Crea un nuevo rol
        var result = await _roleManager.CreateAsync(new IdentityRole(roleName));

        if (result.Succeeded)
        {
            return Ok($"El rol '{roleName}' ha sido creado exitosamente.");
        }

        // Si algo falla, devuelve los errores
        return BadRequest(result.Errors);
    }
}
