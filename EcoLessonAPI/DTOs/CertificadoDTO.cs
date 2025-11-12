namespace EcoLessonAPI.DTOs;

public class CertificadoDTO
{
    // -----------------------------------------------------------------
    // MUDANÇA 1: Trocado 'string' por 'long'
    // -----------------------------------------------------------------
    public long IdCertificado { get; set; } // <-- MUDADO DE 'string'
    
    public DateTime DtEmissao { get; set; }
    public string Descricao { get; set; } = string.Empty; // Correto (é texto)
    public string CodigoValidacao { get; set; } = string.Empty; // Correto (é texto)

    // -----------------------------------------------------------------
    // MUDANÇA 2: Trocado 'decimal' por 'long'
    // -----------------------------------------------------------------
    public long IdUsuario { get; set; } // <-- MUDADO DE 'decimal'

    // -----------------------------------------------------------------
    // MUDANÇA 3: Trocado 'decimal' por 'long'
    // -----------------------------------------------------------------
    public long IdCurso { get; set; } // <-- MUDADO DE 'decimal'

    public string? UsuarioNome { get; set; }
    public string? CursoNome { get; set; }
    public List<LinkDTO>? Links { get; set; }
}

public class CertificadoCreateDTO
{
    public string Descricao { get; set; } = string.Empty; // Correto (é texto)

    // -----------------------------------------------------------------
    // MUDANÇA 4: Trocado 'decimal' por 'long'
    // -----------------------------------------------------------------
    public long IdUsuario { get; set; } // <-- MUDADO DE 'decimal'

    // -----------------------------------------------------------------
    // MUDANÇA 5: Trocado 'decimal' por 'long'
    // -----------------------------------------------------------------
    public long IdCurso { get; set; } // <-- MUDADO DE 'decimal'
}

public class CertificadoUpdateDTO
{
    public string Descricao { get; set; } = string.Empty; // Correto (é texto)
}