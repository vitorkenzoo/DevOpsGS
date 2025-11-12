namespace EcoLessonAPI.Models;

public class Curso
{
    // -----------------------------------------------------------------
    // MUDANÇA 1: Trocado 'decimal' por 'long' (para o ID da Chave Primária)
    // -----------------------------------------------------------------
    public long IdCurso { get; set; } // <-- MUDADO DE 'decimal'
    
    public string NomeCurso { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;

    // -----------------------------------------------------------------
    // MUDANÇA 2: Trocado 'decimal' por 'int' (para Quantidade de Horas)
    // -----------------------------------------------------------------
    public int QtHoras { get; set; }
    public virtual ICollection<Certificado> Certificados { get; set; } = new List<Certificado>();
}