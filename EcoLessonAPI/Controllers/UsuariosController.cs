using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient; // <-- MUDANÇA 1: ADICIONADO
using EcoLessonAPI.Data;
using EcoLessonAPI.DTOs;
using EcoLessonAPI.Models;
using EcoLessonAPI.Services;

namespace EcoLessonAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly EcoLessonDbContext _context;
    private readonly ILogger<UsuariosController> _logger;

    public UsuariosController(EcoLessonDbContext context, ILogger<UsuariosController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // (Seu [HttpGet] (GetUsuarios) está perfeito e não precisa de mudanças)
    [HttpGet]
    public async Task<ActionResult<PagedResponseDTO<UsuarioDTO>>> GetUsuarios(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var totalCount = await _context.Usuarios.CountAsync();
        var usuarios = await _context.Usuarios
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UsuarioDTO
            {
                IdUsuario = u.IdUsuario,
                Nome = u.Nome,
                EmailUsuario = u.EmailUsuario,
                Cadastro = u.Cadastro,
                Cpf = u.Cpf
            })
            .ToListAsync();

        _logger.LogInformation("Listagem de usuários - Página {Page}, Tamanho {PageSize}", page, pageSize);

        return Ok(new PagedResponseDTO<UsuarioDTO>
        {
            Data = usuarios,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    //
    // -----------------------------------------------------------------
    // MUDANÇA 2: Trocado 'decimal id' por 'long id'
    // -----------------------------------------------------------------
    [HttpGet("{id}")]
    public async Task<ActionResult<UsuarioDTO>> GetUsuario(long id) // <-- MUDADO DE 'decimal'
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
        {
            _logger.LogWarning("Usuário não encontrado: {Id}", id);
            return NotFound();
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
                new LinkDTO { Href = $"/api/v1/usuarios/{usuario.IdUsuario}", Rel = "self", Method = "GET" },
                new LinkDTO { Href = $"/api/v1/usuarios/{usuario.IdUsuario}", Rel = "update", Method = "PUT" },
                new LinkDTO { Href = $"/api/v1/usuarios/{usuario.IdUsuario}", Rel = "delete", Method = "DELETE" },
                new LinkDTO { Href = $"/api/v1/usuarios/{usuario.IdUsuario}/recomendacoes-cursos", Rel = "recomendacoes", Method = "GET" }
            }
        };

        _logger.LogInformation("Usuário recuperado: {Id}", id);
        return Ok(usuarioDto);
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioDTO>> CreateUsuario([FromBody] UsuarioCreateDTO createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // (Removendo as checagens manuais de 'CountAsync' para confiar 
        //  no try-catch, que é mais seguro contra race conditions)

        var usuario = new Usuario
        {
            Nome = createDto.Nome,
            EmailUsuario = createDto.EmailUsuario,
            Senha = createDto.Senha, // Em produção, deve ser hasheada
            Cpf = createDto.Cpf,
            Cadastro = DateTime.UtcNow // Usando UtcNow
        };

        //
        // -----------------------------------------------------------------
        // MUDANÇA 3: Adicionando try-catch para erro de Email/CPF duplicado (SQL Server)
        // -----------------------------------------------------------------
        try
        {
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Usuário criado: {Id}", usuario.IdUsuario);
      }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Erro ao criar usuário");
            
            if (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                // Verifica qual constraint falhou
                if (sqlEx.Message.Contains("UK_USUARIO_EMAIL"))
                {
                    return BadRequest(new { message = "Email já cadastrado" });
                }
                if (sqlEx.Message.Contains("UK_CPF_USUARIO"))
                {
                    return BadRequest(new { message = "CPF já cadastrado" });
                }
                return BadRequest(new { message = "Email ou CPF já cadastrado." });
            }
            
            return StatusCode(500, new { message = "Erro ao criar usuário. Verifique os logs para mais detalhes." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao criar usuário");
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

        return CreatedAtAction(nameof(GetUsuario), new { id = usuario.IdUsuario }, usuarioDto);
    }

    //
    // -----------------------------------------------------------------
    // MUDANÇA 4: Trocado 'decimal id' por 'long id'
    // -----------------------------------------------------------------
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUsuario(long id, [FromBody] UsuarioUpdateDTO updateDto) // <-- MUDADO DE 'decimal'
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
        {
            return NotFound();
        }

        usuario.Nome = updateDto.Nome;
        usuario.EmailUsuario = updateDto.EmailUsuario;
        usuario.Cpf = updateDto.Cpf;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Usuário atualizado: {Id}", id);

        return NoContent();
    }

    //
    // -----------------------------------------------------------------
    // MUDANÇA 5: Trocado 'decimal id' por 'long id'
    // -----------------------------------------------------------------
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUsuario(long id) // <-- MUDADO DE 'decimal'
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
        {
            return NotFound();
    }

        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Usuário deletado: {Id}", id);
        return NoContent();
    }
}