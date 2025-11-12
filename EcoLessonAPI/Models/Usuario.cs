namespace EcoLessonAPI.Models;

public class Usuario
{
    // -----------------------------------------------------------------
    // MUDANÇA 1: Trocado 'decimal' por 'long' (para o ID da Chave Primária)
    // -----------------------------------------------------------------
    public long IdUsuario { get; set; } // <-- MUDADO DE 'decimal'
    
    public string Nome { get; set; } = string.Empty;
    public string EmailUsuario { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public DateTime Cadastro { get; set; }

    // -----------------------------------------------------------------
    // MUDANÇA 2: Trocado 'long' por 'string' (para o CPF)
    // -----------------------------------------------------------------
    public string Cpf { get; set; } = string.Empty; // <-- MUDADO DE 'long'
    
    // Navigation properties
    public virtual ICollection<Certificado> Certificados { get; set; } = new List<Certificado>();
}