using FluentValidation;

namespace GestionFormulario.Application.Features.Respuestas;

public sealed class GuardarRespuestaRequestValidator : AbstractValidator<GuardarRespuestaRequest>
{
    public GuardarRespuestaRequestValidator()
    {
        RuleFor(x => x.FormularioId).NotEmpty();
        RuleFor(x => x.Valores).NotNull();
        RuleForEach(x => x.Valores).SetValidator(new ValorRequestValidator());
    }
}

public sealed class ValorRequestValidator : AbstractValidator<ValorRequest>
{
    public ValorRequestValidator()
    {
        RuleFor(x => x.CampoId).NotEmpty();
    }
}
