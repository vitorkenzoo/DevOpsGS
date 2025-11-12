namespace EcoLessonAPI.Models;

public class Empresa
{
    // -----------------------------------------------------------------
    // MUDANÇA 1: Trocado 'decimal' por 'long' (para o ID da Chave Primária)
    // -----------------------------------------------------------------
    public long IdEmpresa { get; set; } // <-- MUDADO DE 'decimal'
    
    public string RazaoSocial { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string EmailEmpresa { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual ICollection<Vaga> Vagas { get; set; } = new List<Vaga>();
}