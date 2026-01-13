using Blog.Application.Contracts.Posts;
using FluentValidation;

namespace Blog.Application.Validation;

public class CreatePostRequestValidator : AbstractValidator<CreatePostRequest>
{
    public CreatePostRequestValidator()
    {
        RuleFor(x => x.AuthorId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MinimumLength(3).MaximumLength(200);
        RuleFor(x => x.Content).NotEmpty().MinimumLength(10);
    }
}
