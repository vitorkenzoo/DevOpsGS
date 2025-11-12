namespace EcoLessonAPI.Models;

public class Certificado
{
    public long IdCertificado { get; set; } // <-- MUDADO DE 'string'
    
    public DateTime DtEmissao { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string CodigoValidacao { get; set; } = string.Empty;
    public long IdUsuario { get; set; } // <-- MUDADO DE 'decimal'
    public long IdCurso { get; set; } 
    public virtual Usuario? Usuario { get; set; }
    public virtual Curso? Curso { get; set; }
}