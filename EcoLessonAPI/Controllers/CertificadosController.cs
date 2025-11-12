using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
// using Oracle.ManagedDataAccess.Client; // <-- MUDANÇA 1: REMOVIDO
using Microsoft.Data.SqlClient; // <-- MUDANÇA 2: ADICIONADO
using System.Linq;
using EcoLessonAPI.Data;
using EcoLessonAPI.DTOs;
using EcoLessonAPI.Models;

namespace EcoLessonAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CertificadosController : ControllerBase
{
    private readonly EcoLessonDbContext _context;
    private readonly ILogger<CertificadosController> _logger;

    public CertificadosController(EcoLessonDbContext context, ILogger<CertificadosController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // (Seu [HttpGet] (GetCertificados) está perfeito e não precisa de mudanças, 
    // pois ele já usa EF Core padrão)
    [HttpGet]
    public async Task<ActionResult<PagedResponseDTO<CertificadoDTO>>> GetCertificados(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var totalCount = await _context.Certificados.CountAsync();
        var certificados = await _context.Certificados
            .Include(c => c.Usuario)
            .Include(c => c.Curso)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CertificadoDTO
            {
                IdCertificado = c.IdCertificado,
                DtEmissao = c.DtEmissao,
                Descricao = c.Descricao,
                CodigoValidacao = c.CodigoValidacao,
                IdUsuario = c.IdUsuario,
                IdCurso = c.IdCurso,
                UsuarioNome = c.Usuario != null ? c.Usuario.Nome : null,
                CursoNome = c.Curso != null ? c.Curso.NomeCurso : null
            })
            .ToListAsync();

        _logger.LogInformation("Listagem de certificados - Página {Page}, Tamanho {PageSize}", page, pageSize);

        return Ok(new PagedResponseDTO<CertificadoDTO>
        {
            Data = certificados,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    //
    // -----------------------------------------------------------------
    // MUDANÇA 3: Trocado 'string id' por 'long id'
    // -----------------------------------------------------------------
    [HttpGet("{id}")]
    public async Task<ActionResult<CertificadoDTO>> GetCertificado(long id)
    {
        var certificado = await _context.Certificados
            .Include(c => c.Usuario)
            .Include(c => c.Curso)
            .FirstOrDefaultAsync(c => c.IdCertificado == id); // <-- Agora compara long com long
        
        if (certificado == null)
        {
            _logger.LogWarning("Certificado não encontrado: {Id}", id);
            return NotFound();
        }

        var certificadoDto = new CertificadoDTO
        {
            IdCertificado = certificado.IdCertificado,
            DtEmissao = certificado.DtEmissao,
            Descricao = certificado.Descricao,
            CodigoValidacao = certificado.CodigoValidacao,
            IdUsuario = certificado.IdUsuario,
            IdCurso = certificado.IdCurso,
            UsuarioNome = certificado.Usuario?.Nome,
            CursoNome = certificado.Curso?.NomeCurso,
            Links = new List<LinkDTO>
            {
                new LinkDTO { Href = $"/api/v1/certificados/{certificado.IdCertificado}", Rel = "self", Method = "GET" },
                new LinkDTO { Href = $"/api/v1/certificados/{certificado.IdCertificado}", Rel = "update", Method = "PUT" },
                new LinkDTO { Href = $"/api/v1/certificados/{certificado.IdCertificado}", Rel = "delete", Method = "DELETE" },
                new LinkDTO { Href = $"/api/v1/usuarios/{certificado.IdUsuario}", Rel = "usuario", Method = "GET" },
                new LinkDTO { Href = $"/api/v1/cursos/{certificado.IdCurso}", Rel = "curso", Method = "GET" }
            }
        };

        _logger.LogInformation("Certificado recuperado: {Id}", id);
        return Ok(certificadoDto);
    }

    //
    // -----------------------------------------------------------------
    // MUDANÇA 4: Lógica de 'CreateCertificado' 100% REESCRITA
    // -----------------------------------------------------------------
    [HttpPost]
    public async Task<ActionResult<CertificadoDTO>> CreateCertificado([FromBody] CertificadoCreateDTO createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Validar IDs (sua lógica está perfeita)
        var usuario = await _context.Usuarios.FindAsync(createDto.IdUsuario);
        if (usuario == null)
        {
            return BadRequest(new { message = "Usuário não encontrado" });
        }

        var curso = await _context.Cursos.FindAsync(createDto.IdCurso);
        if (curso == null)
        {
            return BadRequest(new { message = "Curso não encontrado" });
        }

        // REMOÇÃO: Toda a lógica de gerar ID manual (timestamp + random) foi removida.
        // O Azure SQL vai gerar o ID automaticamente (IDENTITY).

        var codigoValidacao = Guid.NewGuid().ToString("N").Substring(0, 20).ToUpper();
        
        var certificado = new Certificado
        {
            // IdCertificado NÃO é mais definido aqui. O banco de dados vai criar.
            DtEmissao = DateTime.UtcNow, // Usando UtcNow (melhor prática)
            Descricao = createDto.Descricao ?? string.Empty,
            CodigoValidacao = codigoValidacao,
            IdUsuario = createDto.IdUsuario,
            IdCurso = createDto.IdCurso
        };

        try
        {
            _context.Certificados.Add(certificado);
            await _context.SaveChangesAsync();

            // Agora 'certificado.IdCertificado' foi preenchido pelo banco de dados
            _logger.LogInformation("Certificado criado: {Id}", certificado.IdCertificado);
        }
        //
        // -----------------------------------------------------------------
        // MUDANÇA 5: Bloco CATCH trocado para SQL SERVER
        // -----------------------------------------------------------------
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Erro ao criar certificado. IdUsuario: {IdUsuario}, IdCurso: {IdCurso}", 
                createDto.IdUsuario, createDto.IdCurso);
            
            // Verificar se é erro de constraint (única ou FK) do SQL Server
            if (ex.InnerException is SqlException sqlEx)
            {
                _logger.LogError("Erro SQL: Number={Number}, Message={Message}", sqlEx.Number, sqlEx.Message);
                
                if (sqlEx.Number == 2601 || sqlEx.Number == 2627) // Erro de UNIQUE constraint
                {
                    return BadRequest(new { message = "ID ou Código de Validação do certificado já existe" });
                }
                if (sqlEx.Number == 547) // Erro de FOREIGN KEY constraint
                {
                    return BadRequest(new { message = "Usuário ou curso não encontrado" });
                }
            }
            
            return StatusCode(500, new { message = "Erro ao criar certificado. Verifique os logs para mais detalhes." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao criar certificado");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }

        var certificadoDto = new CertificadoDTO
        {
            IdCertificado = certificado.IdCertificado,
            DtEmissao = certificado.DtEmissao,
            Descricao = certificado.Descricao,
            CodigoValidacao = certificado.CodigoValidacao,
            IdUsuario = certificado.IdUsuario,
            IdCurso = certificado.IdCurso,
            UsuarioNome = usuario.Nome,
            CursoNome = curso.NomeCurso,
            Links = new List<LinkDTO>
            {
                new LinkDTO { Href = $"/api/v1/certificados/{certificado.IdCertificado}", Rel = "self", Method = "GET" }
            }
        };

        return CreatedAtAction(nameof(GetCertificado), new { id = certificado.IdCertificado }, certificadoDto);
    }

    //
    // -----------------------------------------------------------------
    // MUDANÇA 6: Trocado 'string id' por 'long id'
    // -----------------------------------------------------------------
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCertificado(long id, [FromBody] CertificadoUpdateDTO updateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var certificado = await _context.Certificados.FindAsync(id);
        if (certificado == null)
        {
            return NotFound();
        }

        certificado.Descricao = updateDto.Descricao;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Certificado atualizado: {Id}", id);

        return NoContent();
    }

    //
    // -----------------------------------------------------------------
    // MUDANÇA 7: Trocado 'string id' por 'long id'
    // -----------------------------------------------------------------
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCertificado(long id)
    {
        var certificado = await _context.Certificados.FindAsync(id);
        if (certificado == null)
        {
            return NotFound();
        }

        _context.Certificados.Remove(certificado);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Certificado deletado: {Id}", id);
        return NoContent();
    }
}