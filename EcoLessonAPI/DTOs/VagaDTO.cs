namespace EcoLessonAPI.DTOs;

public class VagaDTO
{
    // -----------------------------------------------------------------
    // MUDANÇA 1: Trocado 'decimal' por 'long' (para o ID)
    // -----------------------------------------------------------------
    public long IdVaga { get; set; } // <-- MUDADO DE 'decimal'
    
    public string NomeVaga { get; set; } = string.Empty;
    public string DescricaoVaga { get; set; } = string.Empty;

    // -----------------------------------------------------------------
    // MUDANÇA 2: Trocado 'decimal' por 'decimal?' (para aceitar nulo)
    // -----------------------------------------------------------------
    public decimal? Salario { get; set; } // <-- MUDADO DE 'decimal'

    public DateTime DtPublicacao { get; set; }

    // -----------------------------------------------------------------
    // MUDANÇA 3: Trocado 'decimal' por 'long' (para o ID)
    // -----------------------------------------------------------------
    public long IdEmpresa { get; set; } // <-- MUDADO DE 'decimal'

    public string? EmpresaNome { get; set; }
    public List<LinkDTO>? Links { get; set; }
}

public class VagaCreateDTO
{
    public string NomeVaga { get; set; } = string.Empty;
    public string DescricaoVaga { get; set; } = string.Empty;

    // -----------------------------------------------------------------
    // MUDANÇA 4: Trocado 'decimal' por 'decimal?' (para aceitar nulo)
    // -----------------------------------------------------------------
    public decimal? Salario { get; set; } // <-- MUDADO DE 'decimal'

    // -----------------------------------------------------------------
    // MUDANÇA 5: Trocado 'decimal' por 'long' (para o ID)
    // -----------------------------------------------------------------
    public long IdEmpresa { get; set; } // <-- MUDADO DE 'decimal'
}

public class VagaUpdateDTO
{
    public string NomeVaga { get; set; } = string.Empty;
    public string DescricaoVaga { get; set; } = string.Empty;

    // -----------------------------------------------------------------
    // MUDANÇA 6: Trocado 'decimal' por 'decimal?' (para aceitar nulo)
    // -----------------------------------------------------------------
    public decimal? Salario { get; set; } // <-- MUDADO DE 'decimal'

    // -----------------------------------------------------------------
    // MUDANÇA 7: Trocado 'decimal' por 'long' (para o ID)
    // -----------------------------------------------------------------
    public long IdEmpresa { get; set; } // <-- MUDADO DE 'decimal'
}