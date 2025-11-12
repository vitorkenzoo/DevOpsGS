namespace EcoLessonAPI.DTOs;

public class UsuarioDTO
{
    // -----------------------------------------------------------------
    // MUDANÇA 1: Trocado 'decimal' por 'long' (para o ID)
    // -----------------------------------------------------------------
    public long IdUsuario { get; set; } // <-- MUDADO DE 'decimal'
    
    public string Nome { get; set; } = string.Empty;
    public string EmailUsuario { get; set; } = string.Empty;
    public DateTime Cadastro { get; set; }
    public string Cpf { get; set; } = string.Empty; // Correto (é texto)
    public List<LinkDTO>? Links { get; set; }
}

public class UsuarioCreateDTO
{
    public string Nome { get; set; } = string.Empty; // Correto
    public string EmailUsuario { get; set; } = string.Empty; // Correto
    public string Senha { get; set; } = string.Empty; // Correto
    public string Cpf { get; set; } = string.Empty; // Correto
}

public class UsuarioUpdateDTO
{
    public string Nome { get; set; } = string.Empty; // Correto
    public string EmailUsuario { get; set; } = string.Empty; // Correto
    public string Cpf { get; set; } = string.Empty; // Correto
}