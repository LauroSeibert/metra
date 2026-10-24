using Microsoft.AspNetCore.Identity;

namespace Erp.Infrastructure.Identity;

/// <summary>Mensagens do Identity em pt-BR, prontas para a tela (as originais vêm em inglês).</summary>
internal static class IdentityMessages
{
    public static string Translate(IdentityError error) => error.Code switch
    {
        nameof(IdentityErrorDescriber.PasswordTooShort) => "A senha precisa de ao menos 8 caracteres.",
        nameof(IdentityErrorDescriber.PasswordRequiresDigit) => "A senha precisa de ao menos um número.",
        nameof(IdentityErrorDescriber.PasswordRequiresLower) => "A senha precisa de ao menos uma letra minúscula.",
        nameof(IdentityErrorDescriber.PasswordRequiresUpper) => "A senha precisa de ao menos uma letra maiúscula.",
        nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric) => "A senha precisa de ao menos um símbolo.",
        nameof(IdentityErrorDescriber.PasswordRequiresUniqueChars) => "A senha precisa de mais caracteres diferentes.",
        nameof(IdentityErrorDescriber.PasswordMismatch) => "Senha atual incorreta.",
        nameof(IdentityErrorDescriber.InvalidEmail) or nameof(IdentityErrorDescriber.InvalidUserName) => "E-mail inválido.",
        nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName) => "E-mail já cadastrado.",
        nameof(IdentityErrorDescriber.ConcurrencyFailure) => "O registro foi alterado por outra pessoa. Recarregue e tente de novo.",
        _ => "Não foi possível concluir a operação.",
    };
}
