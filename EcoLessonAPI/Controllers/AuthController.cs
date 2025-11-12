using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
// using Oracle.ManagedDataAccess.Client; // <-- MUDANÇA 1: REMOVIDO
using Microsoft.Data.SqlClient; // <-- MUDANÇA 2: ADICIONADO (Para capturar erros do SQL Server)
using EcoLessonAPI.Data;
using EcoLessonAPI.DTOs;
using EcoLessonAPI.Models;
using EcoLessonAPI.Services;

namespace EcoLessonAPI.Controllers;

/// <summary>
/// Controller para autenticação e registro de usuários
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly EcoLessonDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        EcoLessonDbContext context,
        IPasswordService passwordService,
        IJwtService jwtService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _jwtService = jwtService;
        _logger = logger;
    }

    /// <summary>
    /// Registra um novo usuário na plataforma
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UsuarioDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UsuarioDTO>> Register([FromBody] RegisterDTO registerDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // (Sua lógica de verificação de Email/CPF está perfeita e vai funcionar)
        var emailExists = await _context.Usuarios
            .AnyAsync(u => u.EmailUsuario == registerDto.EmailUsuario);
        if (emailExists)
        {
            _logger.LogWarning("Tentativa de registro com email já existente: {Email}", registerDto.EmailUsuario);
            return BadRequest(new { message = "Email já cadastrado" });
        }

        var cpfExists = await _context.Usuarios
            .AnyAsync(u => u.Cpf == registerDto.Cpf);
        if (cpfExists)
        {
            _logger.LogWarning("Tentativa de registro com CPF já existente: {CPF}", registerDto.Cpf);
            return BadRequest(new { message = "CPF já cadastrado" });
        }

        var hashedPassword = _passwordService.HashPassword(registerDto.Senha);

        var usuario = new Usuario
        {
            Nome = registerDto.Nome,
            EmailUsuario = registerDto.EmailUsuario,
            Senha = hashedPassword,
            Cpf = registerDto.Cpf,
            Cadastro = DateTime.Now // Certo, mas DateTime.UtcNow é mais recomendado para servidores
        };

        try
        {
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Novo usuário registrado: {Email}", registerDto.EmailUsuario);
        }
        //
        // -----------------------------------------------------------------
        // MUDANÇA 3: O Bloco CATCH foi trocado para o SQL SERVER
        // -----------------------------------------------------------------
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Erro ao salvar usuário: {Email}", registerDto.EmailUsuario);
            
            // Verifica se a exceção interna é do SQL Server (em vez de Oracle)
            if (ex.InnerException is SqlException sqlEx)
            {
                // Números de erro 2601 ou 2627 são violações de constraint UNIQUE
                if (sqlEx.Number == 2601 || sqlEx.Number == 2627) 
                {
                    return BadRequest(new { message = "Email ou CPF já cadastrado (erro de banco de dados)." });
                }
            }
            
            return StatusCode(500, new { message = "Erro ao registrar usuário. Verifique os logs para mais detalhes." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao registrar usuário: {Email}", registerDto.EmailUsuario);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }

        var usuarioDto = new UsuarioDTO
        {
            IdUsuario = usuario.IdUsuario,
            Nome = usuario.Nome,
            EmailUsuario = usuario.EmailUsuario,
            Cadastro = usuario.Cadastro,
            Cpf = usuario.Cpf,
            Links = new List<LinkDTO>
            {
                new LinkDTO { Href = $"/api/v1/usuarios/{usuario.IdUsuario}", Rel = "self", Method = "GET" }
            }
        };

        // (Seu GetUsuario usa 'long', então 'usuario.IdUsuario' está correto)
        return CreatedAtAction(nameof(GetUsuario), new { id = usuario.IdUsuario }, usuarioDto);
    }

    /// <summary>
    /// Autentica um usuário e retorna um token JWT
    /// </summary>
    // (Seu método Login() está PERFEITO. Nenhuma mudança necessária.)
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDTO>> Login([FromBody] LoginDTO loginDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.EmailUsuario == loginDto.EmailUsuario);

        if (usuario == null || !_passwordService.VerifyPassword(loginDto.Senha, usuario.Senha))
        {
            _logger.LogWarning("Tentativa de login inválida para: {Email}", loginDto.EmailUsuario);
            return Unauthorized(new { message = "Email ou senha inválidos" });
        }

        var token = _jwtService.GenerateToken(usuario);
        _logger.LogInformation("Login bem-sucedido para usuário: {Email}", loginDto.EmailUsuario);

        return Ok(new AuthResponseDTO
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        });
    }

    /// <summary>
    /// Obtém dados de um usuário específico
    /// </summary>
    // (Este método não é usado pelo CreatedAtAction, mas sim o do UsuariosController. 
    // De qualquer forma, é bom corrigir o tipo de dado.)
    //
    // -----------------------------------------------------------------
    // MUDANÇA 4: Trocando 'decimal' por 'long' para ser compatível com SQL Server
    // -----------------------------------------------------------------
    [HttpGet("usuarios/{id}")]
    [ProducesResponseType(typeof(UsuarioDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioDTO>> GetUsuario(long id) // <-- MUDADO DE 'decimal'
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
        {
            return NotFound();
        }

        return Ok(new UsuarioDTO
        {
            IdUsuario = usuario.IdUsuario,
            Nome = usuario.Nome,
            EmailUsuario = usuario.EmailUsuario,
            Cadastro = usuario.Cadastro,
            Cpf = usuario.Cpf
        });
    }
}