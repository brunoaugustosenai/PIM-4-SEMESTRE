using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Naxapa.Web.Models;

public sealed class SetupAdminViewModel
{
    [Required, StringLength(150)]
    [Display(Name = "Nome do estabelecimento")]
    public string Estabelecimento { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "Código do estabelecimento")]
    public string? CodigoEstabelecimento { get; set; }

    [Required(ErrorMessage = "Informe o CPF ou CNPJ da empresa.")]
    [StringLength(18)]
    [Display(Name = "CPF ou CNPJ da empresa")]
    public string Documento { get; set; } = string.Empty;

    [Required, StringLength(150)]
    [Display(Name = "Seu nome")]
    public string Nome { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 3)]
    [Display(Name = "Usuário")]
    public string UserName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(128, MinimumLength = 10)]
    [Display(Name = "Senha")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "As senhas não conferem.")]
    [Display(Name = "Confirmar senha")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class LoginViewModel
{
    [Required]
    [Display(Name = "Usuário ou e-mail")]
    public string Login { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Manter conectado")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public sealed class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password)]
    [Display(Name = "Senha atual")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(128, MinimumLength = 10)]
    [Display(Name = "Nova senha")]
    public string NewPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(NewPassword), ErrorMessage = "As senhas não conferem.")]
    [Display(Name = "Confirmar nova senha")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class ProfileViewModel
{
    public long UsuarioId { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Telefone { get; init; }
    public string Perfil { get; init; } = string.Empty;
    public string Estabelecimento { get; init; } = string.Empty;
    public IReadOnlyList<SelectListItem> Estabelecimentos { get; init; } = [];
}

public sealed class UserFormViewModel
{
    public long? UsuarioId { get; set; }

    [Required, StringLength(150)]
    public string Nome { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 3)]
    [Display(Name = "Usuário")]
    public string UserName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Telefone { get; set; }

    [DataType(DataType.Password), StringLength(128, MinimumLength = 10)]
    [Display(Name = "Senha inicial")]
    public string? Password { get; set; }

    [Required]
    [Display(Name = "Perfil")]
    public long PerfilId { get; set; }

    public bool Ativo { get; set; } = true;

    public IReadOnlyList<SelectListItem> Perfis { get; set; } = [];
}

public sealed class ResetPasswordViewModel
{
    public long UsuarioId { get; set; }
    public string Usuario { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(128, MinimumLength = 10)]
    [Display(Name = "Nova senha")]
    public string NewPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(NewPassword), ErrorMessage = "As senhas não conferem.")]
    [Display(Name = "Confirmar senha")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed record UserRow(long Id, string Nome, string UserName, string Email, string Perfil, bool Ativo, DateTime? UltimoAcesso);

public sealed class ApiTokenRequest
{
    [Required]
    public string Login { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public long? EstabelecimentoId { get; set; }
}

public sealed record ApiTokenResponse(
    string Token,
    string TokenType,
    DateTime ExpiresAtUtc,
    long UserId,
    string UserName,
    long EstabelecimentoId,
    string Estabelecimento);
