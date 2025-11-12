using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
// using Oracle.ManagedDataAccess.Client; // <-- MUDANÇA 1: REMOVIDO
using Microsoft.Data.SqlClient; // <-- MUDANÇA 2: ADICIONADO
using EcoLessonAPI.Data;
using EcoLessonAPI.DTOs;
using EcoLessonAPI.Models;

namespace EcoLessonAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CursosController : ControllerBase
{
    private readonly EcoLessonDbContext _context;
    private readonly ILogger<CursosController> _logger;

    public CursosController(EcoLessonDbContext context, ILogger<CursosController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // (Seu [HttpGet] (GetCursos) está perfeito e não precisa de mudanças, 
    // pois ele já usa EF Core padrão)
    [HttpGet]
    public async Task<ActionResult<PagedResponseDTO<CursoDTO>>> GetCursos(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var totalCount = await _context.Cursos.CountAsync();
        var cursos = await _context.Cursos
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CursoDTO
            {
                IdCurso = c.IdCurso,
                NomeCurso = c.NomeCurso,
                Descricao = c.Descricao,
                QtHoras = c.QtHoras
            })
            .ToListAsync();

        _logger.LogInformation("Listagem de cursos - Página {Page}, Tamanho {PageSize}", page, pageSize);

        return Ok(new PagedResponseDTO<CursoDTO>
        {
            Data = cursos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    //
    // -----------------------------------------------------------------
    // MUDANÇA 3: Trocado 'decimal id' por 'long id'
    // -----------------------------------------------------------------
    [HttpGet("{id}")]
    public async Task<ActionResult<CursoDTO>> GetCurso(long id) // <-- MUDADO DE 'decimal'
    {
        var curso = await _context.Cursos.FindAsync(id);
        if (curso == null)
        {
            _logger.LogWarning("Curso não encontrado: {Id}", id);
            return NotFound();
        }

        var cursoDto = new CursoDTO
        {
            IdCurso = curso.IdCurso,
            NomeCurso = curso.NomeCurso,
            Descricao = curso.Descricao,
            QtHoras = curso.QtHoras,
            Links = new List<LinkDTO>
            {
                new LinkDTO { Href = $"/api/v1/cursos/{curso.IdCurso}", Rel = "self", Method = "GET" },
                new LinkDTO { Href = $"/api/v1/cursos/{curso.IdCurso}", Rel = "update", Method = "PUT" },
                new LinkDTO { Href = $"/api/v1/cursos/{curso.IdCurso}", Rel = "delete", Method = "DELETE" }
            }
        };

        _logger.LogInformation("Curso recuperado: {Id}", id);
        return Ok(cursoDto);
    }

    [HttpPost]
    public async Task<ActionResult<CursoDTO>> CreateCurso([FromBody] CursoCreateDTO createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var curso = new Curso
        {
            // O IdCurso será gerado automaticamente pelo Azure SQL (IDENTITY)
            NomeCurso = createDto.NomeCurso,
            Descricao = createDto.Descricao,
            QtHoras = createDto.QtHoras
        };

        try
        {
            _context.Cursos.Add(curso);
            await _context.SaveChangesAsync();
            // Agora 'curso.IdCurso' tem o valor gerado pelo banco
            _logger.LogInformation("Curso criado: {Id}", curso.IdCurso);
        }
        //
        // -----------------------------------------------------------------
        // MUDANÇA 4: Bloco CATCH trocado para SQL SERVER
        // -----------------------------------------------------------------
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Erro ao criar curso");
            
            // Verificar se é erro de constraint única do SQL Server
            if (ex.InnerException is SqlException sqlEx)
            {
                // Números 2601 ou 2627 são violações de UNIQUE
                if (sqlEx.Number == 2601 || sqlEx.Number == 2627) 
                {
                    return BadRequest(new { message = "Violação de constraint única (ex: nome do curso duplicado)" });
                }
            }
            
            return StatusCode(500, new { message = "Erro ao criar curso. Verifique os logs para mais detalhes." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao criar curso");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }

        var cursoDto = new CursoDTO
        {
            IdCurso = curso.IdCurso,
            NomeCurso = curso.NomeCurso,
            Descricao = curso.Descricao,
            QtHoras = curso.QtHoras,
            Links = new List<LinkDTO>
            {
                new LinkDTO { Href = $"/api/v1/cursos/{curso.IdCurso}", Rel = "self", Method = "GET" }
            }
        };

        return CreatedAtAction(nameof(GetCurso), new { id = curso.IdCurso }, cursoDto);
    }

    //
    // -----------------------------------------------------------------
    // MUDANÇA 5: Trocado 'decimal id' por 'long id'
    // -----------------------------------------------------------------
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCurso(long id, [FromBody] CursoUpdateDTO updateDto) // <-- MUDADO DE 'decimal'
   {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var curso = await _context.Cursos.FindAsync(id);
        if (curso == null)
        {
            return NotFound();
        }

        curso.NomeCurso = updateDto.NomeCurso;
        curso.Descricao = updateDto.Descricao;
        curso.QtHoras = updateDto.QtHoras;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Curso atualizado: {Id}", id);

        return NoContent();
    }

    //
    // -----------------------------------------------------------------
    // MUDANÇA 6: Trocado 'decimal id' por 'long id'
    // -----------------------------------------------------------------
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCurso(long id) // <-- MUDADO DE 'decimal'
    {
        var curso = await _context.Cursos.FindAsync(id);
        if (curso == null)
        {
            return NotFound();
        }

        _context.Cursos.Remove(curso);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Curso deletado: {Id}", id);
        return NoContent();
    }
}