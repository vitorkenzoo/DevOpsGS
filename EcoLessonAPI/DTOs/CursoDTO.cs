namespace EcoLessonAPI.DTOs;

public class CursoDTO
{
    // -----------------------------------------------------------------
    // MUDANÇA 1: Trocado 'decimal' por 'long' (para o ID)
    // -----------------------------------------------------------------
    public long IdCurso { get; set; } // <-- MUDADO DE 'decimal'
    
    public string NomeCurso { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;

    // -----------------------------------------------------------------
    // MUDANÇA 2: Trocado 'decimal' por 'int' (para Horas)
    // -----------------------------------------------------------------
    public int QtHoras { get; set; } // <-- MUDADO DE 'decimal'
    
    public List<LinkDTO>? Links { get; set; }
}

public class CursoCreateDTO
{
    public string NomeCurso { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;

    // -----------------------------------------------------------------
    // MUDANÇA 3: Trocado 'decimal' por 'int' (para Horas)
    // -----------------------------------------------------------------
    public int QtHoras { get; set; } // <-- MUDADO DE 'decimal'
}

public class CursoUpdateDTO
{
    public string NomeCurso { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;

    // -----------------------------------------------------------------
    // MUDANÇA 4: Trocado 'decimal' por 'int' (para Horas)
    // -----------------------------------------------------------------
    public int QtHoras { get; set; } // <-- MUDADO DE 'decimal'
}