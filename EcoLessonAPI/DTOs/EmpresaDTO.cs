namespace EcoLessonAPI.DTOs;

public class EmpresaDTO
{
    // -----------------------------------------------------------------
    // MUDANÇA 1: Trocado 'decimal' por 'long' (para o ID)
    // -----------------------------------------------------------------
    public long IdEmpresa { get; set; } // <-- MUDADO DE 'decimal'
    
    public string RazaoSocial { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string EmailEmpresa { get; set; } = string.Empty;
    public List<LinkDTO>? Links { get; set; }
}

public class EmpresaCreateDTO
{
    public string RazaoSocial { get; set; } = string.Empty; // Correto (é texto)
    public string Cnpj { get; set; } = string.Empty; // Correto (é texto)
    public string EmailEmpresa { get; set; } = string.Empty; // Correto (é texto)
}

public class EmpresaUpdateDTO
{
    public string RazaoSocial { get; set; } = string.Empty; // Correto (é texto)
    public string Cnpj { get; set; } = string.Empty; // Correto (é texto)
    public string EmailEmpresa { get; set; } = string.Empty; // Correto (é texto)
}