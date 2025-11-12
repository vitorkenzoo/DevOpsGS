using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcoLessonAPI.DTOs;
using EcoLessonAPI.Services;

namespace EcoLessonAPI.Controllers;

[ApiController]
[Route("api/v1/usuarios/{id}/recomendacoes-cursos")]
[Authorize]
public class RecomendacaoController : ControllerBase
{
    private readonly IRecomendacaoService _recomendacaoService;
    private readonly ILogger<RecomendacaoController> _logger;

    public RecomendacaoController(
        IRecomendacaoService recomendacaoService,
        ILogger<RecomendacaoController> logger)
    {
        _recomendacaoService = recomendacaoService;
        _logger = logger;
    }

    //
    // -----------------------------------------------------------------
    // MUDANÇA 1: Trocado 'decimal id' por 'long id'
    // -----------------------------------------------------------------
    [HttpGet]
    public async Task<ActionResult<List<CursoDTO>>> GetRecomendacoes(long id, [FromQuery] int topN = 5) // <-- MUDADO DE 'decimal'
    {
        if (topN < 1 || topN > 20) topN = 5;

        try
        {
            var recomendacoes = await _recomendacaoService.ObterRecomendacoesAsync(id, topN); // <-- 'id' agora é 'long'
            _logger.LogInformation("Recomendações geradas para usuário {Id}: {Count} cursos", id, recomendacoes.Count);
            return Ok(recomendacoes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar recomendações para usuário {Id}", id);
            return StatusCode(500, new { message = "Erro ao gerar recomendações" });
        }
    }
}