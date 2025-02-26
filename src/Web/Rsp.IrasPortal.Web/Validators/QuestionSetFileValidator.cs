﻿using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

[ExcludeFromCodeCoverage]
public class QuestionSetFileValidator : AbstractValidator<QuestionSetFileModel>
{
    public QuestionSetFileValidator()
    {
        RuleForEach(x => x.QuestionDtos)
            .SetValidator(new QuestionDtoValidator());

        // '
        // RuleForEach(x => x.AnswerOptions)
        //    .SetValidator(new AnswerDtoValidator());'
    }
}