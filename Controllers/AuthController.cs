using EcommerceAPI.Data;
using EcommerceAPI.DTOs;
using EcommerceAPI.Helpers;
using EcommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace EcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtHelper _jwtHelper;

        public AuthController(ApplicationDbContext context, JwtHelper jwtHelper)
        {
            _context = context;
            _jwtHelper = jwtHelper;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            try
            {
                // Verifica si el email ya existe
                if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
                {
                    return BadRequest(new { message = "El email ya está registrado" });
                }

                // Crea nuevo usuario (siempre como Customer)
                var user = new User
                {
                    Email = registerDto.Email,
                    PasswordHash = PasswordHelper.HashPassword(registerDto.Password),
                    FirstName = registerDto.FirstName,
                    LastName = registerDto.LastName,
                    Role = "Customer" // Los registros públicos siempre son Customer
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Genera token
                var token = _jwtHelper.GenerateToken(user.Id, user.Email, user.Role);

                var response = new AuthResponseDto
                {
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Role = user.Role
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            try
            {
                // Busca usuario por email
                var user = await _context.Users
                    .Where(u => u.Email == loginDto.Email && u.IsActive)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return Unauthorized(new { message = "Credenciales inválidas" });
                }

                // Verifica contraseña
                if (!PasswordHelper.VerifyPassword(loginDto.Password, user.PasswordHash))
                {
                    return Unauthorized(new { message = "Credenciales inválidas" });
                }

                // Genera token
                var token = _jwtHelper.GenerateToken(user.Id, user.Email, user.Role);

                var response = new AuthResponseDto
                {
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Role = user.Role
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        [HttpGet("me")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

                var user = await _context.Users
                    .Where(u => u.Id == userId && u.IsActive)
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        Email = u.Email,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Role = u.Role
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        [HttpPut("profile")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> UpdateProfile(UpdateUserDto updateUserDto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

                var user = await _context.Users
                    .Where(u => u.Id == userId && u.IsActive)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                // Actualizar campos permitidos
                user.FirstName = updateUserDto.FirstName ?? user.FirstName;
                user.LastName = updateUserDto.LastName ?? user.LastName;

                await _context.SaveChangesAsync();

                // Devolver usuario actualizado
                var userDto = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role
                };

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }
    }
}
