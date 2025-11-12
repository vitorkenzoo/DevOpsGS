namespace EcoLessonAPI.Models;

public class Vaga
{
    public long IdVaga { get; set; } // CORRETO (ID é long)

    // -----------------------------------------------------------------
    // MUDANÇA 1: Trocado 'long' por 'string' (Nome da Vaga é texto)
    // -----------------------------------------------------------------
    public string NomeVaga { get; set; } = string.Empty; // <-- MUDADO DE 'long'
    
    // -----------------------------------------------------------------
    // MUDANÇA 2: Trocado 'long' por 'string' (Descrição é texto)
    // -----------------------------------------------------------------
    public string DescricaoVaga { get; set; } = string.Empty; // <-- MUDADO DE 'long'

    // -----------------------------------------------------------------
    // MUDANÇA 3: Trocado 'long' por 'decimal?' (Salário tem centavos)
    // -----------------------------------------------------------------
    public decimal? Salario { get; set; } // <-- MUDADO DE 'long' (e '?' o torna opcional)
    
    public DateTime DtPublicacao { get; set; }
    public long IdEmpresa { get; set; } // CORRETO (FK é long)
    
    // Navigation properties
    public virtual Empresa? Empresa { get; set; }
}