using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
// using Oracle.ManagedDataAccess.Client; // <-- MUDANÇA 1: REMOVIDO
using Microsoft.Data.SqlClient; // <-- MUDANÇA 2: ADICIONADO
using EcoLessonAPI.Data;
using EcoLessonAPI.DTOs;
using EcoLessonAPI.Models;

namespace EcoLessonAPI.Controllers;

/// <summary>
/// Controller para gerenciamento de vagas de emprego
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class VagasController : ControllerBase
{
    private readonly EcoLessonDbContext _context;
    private readonly ILogger<VagasController> _logger;

    public VagasController(EcoLessonDbContext context, ILogger<VagasController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lista vagas de emprego com paginação
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponseDTO<VagaDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponseDTO<VagaDTO>>> GetVagas(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        //
        // -----------------------------------------------------------------
        // MUDANÇA 3: Trocado 'decimal?' por 'long?'
        // -----------------------------------------------------------------
        [FromQuery] long? empresaId = null) // <-- MUDADO DE 'decimal?'
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var query = _context.Vagas.Include(v => v.Empresa).AsQueryable();
        
        if (empresaId.HasValue)
        {
            query = query.Where(v => v.IdEmpresa == empresaId.Value);
        }

        var totalCount = await query.CountAsync();
        var vagas = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new VagaDTO
            {
                IdVaga = v.IdVaga,
                NomeVaga = v.NomeVaga,
                DescricaoVaga = v.DescricaoVaga,
                Salario = v.Salario,
                DtPublicacao = v.DtPublicacao,
                IdEmpresa = v.IdEmpresa,
                EmpresaNome = v.Empresa != null ? v.Empresa.RazaoSocial : null
            })
            .ToListAsync();

        _logger.LogInformation("Listagem de vagas - Página {Page}, Tamanho {PageSize}", page, pageSize);

        return Ok(new PagedResponseDTO<VagaDTO>
        {
            Data = vagas,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    /// <summary>
    /// Obtém uma vaga específica por ID
    /// </summary>
    //
    // -----------------------------------------------------------------
    // MUDANÇA 4: Trocado 'decimal id' por 'long id'
    // -----------------------------------------------------------------
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(VagaDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VagaDTO>> GetVaga(long id) // <-- MUDADO DE 'decimal'
    {
        var vaga = await _context.Vagas
            .Include(v => v.Empresa)
            .FirstOrDefaultAsync(v => v.IdVaga == id);
        
        if (vaga == null)
        {
            _logger.LogWarning("Vaga não encontrada: {Id}", id);
            return NotFound();
        }

        var vagaDto = new VagaDTO
        {
            IdVaga = vaga.IdVaga,
            NomeVaga = vaga.NomeVaga,
            DescricaoVaga = vaga.DescricaoVaga,
            Salario = vaga.Salario,
            DtPublicacao = vaga.DtPublicacao,
            IdEmpresa = vaga.IdEmpresa,
            EmpresaNome = vaga.Empresa?.RazaoSocial,
            Links = new List<LinkDTO>
            {
                new LinkDTO { Href = $"/api/v1/vagas/{vaga.IdVaga}", Rel = "self", Method = "GET" },
                new LinkDTO { Href = $"/api/v1/vagas/{vaga.IdVaga}", Rel = "update", Method = "PUT" },
                new LinkDTO { Href = $"/api/v1/vagas/{vaga.IdVaga}", Rel = "delete", Method = "DELETE" },
                new LinkDTO { Href = $"/api/v1/empresas/{vaga.IdEmpresa}", Rel = "empresa", Method = "GET" }
            }
        };

        _logger.LogInformation("Vaga recuperada: {Id}", id);
        return Ok(vagaDto);
    }

    /// <summary>
    /// Cria uma nova vaga de emprego
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(VagaDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VagaDTO>> CreateVaga([FromBody] VagaCreateDTO createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // (Vamos assumir que o DTO 'VagaCreateDTO' também foi corrigido para usar 'long IdEmpresa')
        var empresa = await _context.Empresas.FindAsync(createDto.IdEmpresa);
        if (empresa == null)
        {
            return BadRequest(new { message = "Empresa não encontrada" });
        }

        var vaga = new Vaga
        {
            NomeVaga = createDto.NomeVaga,
            DescricaoVaga = createDto.DescricaoVaga,
            Salario = createDto.Salario,
            DtPublicacao = DateTime.UtcNow, // Usando UtcNow
            IdEmpresa = createDto.IdEmpresa
        };

        try
        {
            _context.Vagas.Add(vaga);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Vaga criada: {Id}", vaga.IdVaga);
        }
        //
        // -----------------------------------------------------------------
        // MUDANÇA 5: Bloco CATCH trocado para SQL SERVER
        // -----------------------------------------------------------------
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Erro ao criar vaga");
            
            if (ex.InnerException is SqlException sqlEx)
            {
                if (sqlEx.Number == 2601 || sqlEx.Number == 2627) // UNIQUE
                {
                    return BadRequest(new { message = "Violação de constraint única (ex: nome da vaga duplicado)" });
                }
                if (sqlEx.Number == 547) // FOREIGN KEY
                {
                    // (Isso não deve acontecer por causa da sua checagem manual, mas é bom ter)
                    return BadRequest(new { message = "Empresa não encontrada" });
                }
            }
            
            return StatusCode(500, new { message = "Erro ao criar vaga. Verifique os logs para mais detalhes." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao criar vaga");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }

        var vagaDto = new VagaDTO
        {
            IdVaga = vaga.IdVaga,
            NomeVaga = vaga.NomeVaga,
            DescricaoVaga = vaga.DescricaoVaga,
            Salario = vaga.Salario,
            DtPublicacao = vaga.DtPublicacao,
            IdEmpresa = vaga.IdEmpresa,
            EmpresaNome = empresa.RazaoSocial,
            Links = new List<LinkDTO>
            {
                new LinkDTO { Href = $"/api/v1/vagas/{vaga.IdVaga}", Rel = "self", Method = "GET" }
            }
        };

        return CreatedAtAction(nameof(GetVaga), new { id = vaga.IdVaga }, vagaDto);
    }

    /// <summary>
    /// Atualiza uma vaga existente
    /// </summary>
    //
    // -----------------------------------------------------------------
    // MUDANÇA 6: Trocado 'decimal id' por 'long id'
    // -----------------------------------------------------------------
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateVaga(long id, [FromBody] VagaUpdateDTO updateDto) // <-- MUDADO DE 'decimal'
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var vaga = await _context.Vagas.FindAsync(id);
        if (vaga == null)
        {
            return NotFound();
        }

        vaga.NomeVaga = updateDto.NomeVaga;
        vaga.DescricaoVaga = updateDto.DescricaoVaga;
        vaga.Salario = updateDto.Salario;
        vaga.IdEmpresa = updateDto.IdEmpresa; // (Assumindo que VagaUpdateDTO também usa 'long')

        await _context.SaveChangesAsync();
        _logger.LogInformation("Vaga atualizada: {Id}", id);

        return NoContent();
    }

    /// <summary>
    /// Deleta uma vaga
    /// </summary>
    //
    // -----------------------------------------------------------------
    // MUDANÇA 7: Trocado 'decimal id' por 'long id'
    // -----------------------------------------------------------------
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVaga(long id) // <-- MUDADO DE 'decimal'
    {
        var vaga = await _context.Vagas.FindAsync(id);
        if (vaga == null)
      {
            return NotFound();
        }

        _context.Vagas.Remove(vaga);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Vaga deletada: {Id}", id);
        return NoContent();
    }
}