using FluentValidation;
using TechLibrary.Communication.Requests;

namespace TechLibrary.Api.UseCases.Users.Register
{
    public class RegisterUserValidator : AbstractValidator<RequestUserJson>
    {
        public RegisterUserValidator()
        {
            RuleFor(request => request.Name).NotEmpty().WithMessage("O nome é obrigatorio");
            RuleFor(request => request.Email).NotEmpty().EmailAddress().WithMessage("O email é obrigatorio e deve ser um email válido");
            RuleFor(request => request.Password).NotEmpty().MinimumLength(6).WithMessage("Senha obrigatoria");

        }
    }
}
