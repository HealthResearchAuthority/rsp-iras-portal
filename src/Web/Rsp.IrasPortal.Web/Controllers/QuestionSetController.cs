using System.Data;
using System.Text;
using System.Text.Json;
using ExcelDataReader;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Extensions;
using Rsp.IrasPortal.Web.Models;
using Rsp.Logging.Extensions;
using static Rsp.IrasPortal.Application.Constants.QuestionCategories;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "questionset:[action]")]
[Authorize(Policy = "IsAdmin")]
public class QuestionSetController(ILogger<QuestionSetController> logger, IQuestionSetService questionSetService, IValidator<QuestionSetFileModel> validator) : Controller
{
    public async Task<IActionResult> Index(QuestionSetFileModel model)
    {
        logger.LogInformationHp("called");

        await PopulateVersions(model);

        return View(model);
    }

    private async Task PopulateVersions(QuestionSetFileModel model)
    {
        var response = await questionSetService.GetVersions();
        if (response.IsSuccessStatusCode)
        {
            model.Versions = response.Content?.ToList() ?? [];
        }
    }

    public async Task<IActionResult> PreviewApplication(string versionId, string categoryId = A)
    {
        var questions = default(List<QuestionViewModel>);

        if (questions == null || questions.Count == 0)
        {
            // get the questions for the category
            var response = await questionSetService.GetQuestionsByVersion(versionId, categoryId);

            logger.LogInformationHp("called");

            // return the view if successfull
            if (response.IsSuccessStatusCode)
            {
                TempData.TryAdd(TempDataKeys.VersionId, versionId);

                // set the active stage for the category
                SetStage(categoryId);

                var questionnaire = BuildQuestionnaireViewModel(response.Content!);

                // store the questions to load again if there are validation errors on the page
                HttpContext.Session.SetString(SessionKeys.Questionnaire, JsonSerializer.Serialize(questionnaire.Questions));

                return View(nameof(PreviewApplication), questionnaire);
            }

            // return error page as api wasn't successful
            return this.ServiceError(response);
        }

        return View(nameof(PreviewApplication), new QuestionnaireViewModel
        {
            CurrentStage = categoryId,
            Questions = questions
        });
    }

    private static QuestionnaireViewModel BuildQuestionnaireViewModel(IEnumerable<QuestionsResponse> response)
    {
        // order the questions by SectionId and Sequence
        var questions = response
                .OrderBy(q => q.SectionId)
                .ThenBy(q => q.Sequence)
                .Select((question, index) => (question, index));

        var questionnaire = new QuestionnaireViewModel();

        // build the questionnaire view model
        // we need to order the questions by section and sequence
        // and also need to assign the index to the question so the multiple choice
        // answsers can be linked back to the question
        foreach (var (question, index) in questions)
        {
            questionnaire.Questions.Add(new QuestionViewModel
            {
                Index = index,
                QuestionId = question.QuestionId,
                Category = question.Category,
                SectionId = question.SectionId,
                Section = question.Section,
                Sequence = question.Sequence,
                Heading = question.Heading,
                QuestionText = question.QuestionText,
                QuestionType = question.QuestionType,
                DataType = question.DataType,
                IsMandatory = question.IsMandatory,
                IsOptional = question.IsOptional,
                Rules = question.Rules,
                Answers = question.Answers.Select(ans => new AnswerViewModel
                {
                    AnswerId = ans.AnswerId,
                    AnswerText = ans.AnswerText
                }).ToList()
            });
        }

        return questionnaire;
    }

    private (string PreviousStage, string CurrentStage, string NextStage) SetStage(string category)
    {
        (string? PreviousStage, string? CurrentStage, string NextStage) = category switch
        {
            A => ("", A, B),
            B => (A, B, C1),
            C1 => (B, C1, C2),
            C2 => (C1, C2, C3),
            C3 => (C2, C3, C4),
            C4 => (C3, C4, C5),
            C5 => (C4, C5, C6),
            C6 => (C5, C6, C7),
            C7 => (C6, C7, C8),
            C8 => (C7, C8, D),
            D => (C8, D, ""),
            _ => ("", A, B)
        };

        // store in temp data
        TempData[TempDataKeys.PreviousStage] = PreviousStage;
        TempData[TempDataKeys.CurrentStage] = CurrentStage;

        return (PreviousStage, CurrentStage, NextStage);
    }

    [HttpPost]
    public async Task<IActionResult> PublishVersion(string versionId)
    {
        logger.LogInformationHp("called");

        var response = await questionSetService.PublishVersion(versionId);

        if (response.IsSuccessStatusCode)
        {
            TempData["PublishSuccess"] = true;
            TempData["PublishedVersionId"] = versionId;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Upload(QuestionSetFileModel model)
    {
        logger.LogInformationHp("called");

        var file = model.Upload;

        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("Upload", "Please upload a file");
            await PopulateVersions(model);
            return View(nameof(Index), model);
        }

        string[] allowedExtensions = [".xlsx", ".xlsb", ".xls"];

        if (!allowedExtensions.Contains(Path.GetExtension(file.FileName).ToLower()))
        {
            ModelState.AddModelError("Upload", $"Please upload a file of type: {string.Join(", ", allowedExtensions)}");
            await PopulateVersions(model);
            return View(nameof(Index), model);
        }

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var stream = file.OpenReadStream();
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var result = reader.AsDataSet(new ExcelDataSetConfiguration()
        {
            ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
            {
                UseHeaderRow = true
            }
        });

        List<DataTable> moduleSheets = [];
        DataTable rulesSheet = new();
        DataTable answerOptionsSheet = new();
        DataTable contentsSheet = new();

        var fileName = Path.GetFileNameWithoutExtension(file.FileName);

        foreach (var sheetName in SheetNames.All)
        {
            var sheet = result.Tables[sheetName];

            if (sheet == null)
            {
                ModelState.AddModelError("Upload", $"Sheet '{sheetName}' cannot be found");
                continue;
            }

            if (HasValidColumns(sheetName, sheet))
            {
                if (sheetName == SheetNames.Rules)
                {
                    rulesSheet = sheet;
                }
                else if (sheetName == SheetNames.AnswerOptions)
                {
                    answerOptionsSheet = sheet;
                }
                else if (sheetName == SheetNames.Contents)
                {
                    contentsSheet = sheet;
                }
                else
                {
                    moduleSheets.Add(sheet);
                }
            }
        }

        if (ModelState.ErrorCount > 0)
        {
            await PopulateVersions(model);
            return View(nameof(Index), model);
        }

        foreach (DataRow category in contentsSheet.Rows)
        {
            var categoryId = category.Field<string>(ContentsColumns.Tab);

            if (categoryId == null || !SheetNames.Modules.Contains(categoryId))
            {
                continue;
            }

            var categoryDto = new CategoryDto
            {
                CategoryId = categoryId,
                CategoryName = category.Field<string>(ContentsColumns.Category) ?? "",
                VersionId = fileName
            };

            model.CategoryDtos.Add(categoryDto);
        }

        foreach (DataRow answerOption in answerOptionsSheet.Rows)
        {
            var answerOptionId = answerOption.Field<string>(AnswerOptionsColumns.OptionId);

            if (answerOptionId == null)
            {
                continue;
            }

            var answerDto = new AnswerOptionDto
            {
                OptionId = answerOptionId,
                OptionText = answerOption.Field<string>(AnswerOptionsColumns.OptionText) ?? "",
                VersionId = fileName
            };

            model.AnswerOptionDtos.Add(answerDto);
        }

        foreach (var sheet in moduleSheets)
        {
            foreach (DataRow question in sheet.Rows)
            {
                var questionId = question.Field<string>(ModuleColumns.QuestionId);

                if (questionId == null)
                {
                    continue;
                }

                if (questionId.StartsWith("IQT"))
                {
                    var sectionDto = new SectionDto
                    {
                        SectionId = questionId,
                        QuestionCategoryId = question.Field<string>(ModuleColumns.Category) ?? "",
                        SectionName = question.Field<string>(ModuleColumns.QuestionText) ?? "",
                        VersionId = fileName
                    };

                    model.SectionDtos.Add(sectionDto);

                    continue;
                }

                var conformance = question.Field<string>(ModuleColumns.Conformance);

                var questionDto = new QuestionDto
                {
                    QuestionId = questionId,
                    Category = question.Field<string>(ModuleColumns.Category) ?? "",
                    SectionId = question.Field<string>(ModuleColumns.Section) ?? "",
                    Section = question.Field<string>(ModuleColumns.Section) ?? "",
                    Sequence = Convert.ToInt32(question[ModuleColumns.Sequence]),
                    Heading = Convert.ToString(question[ModuleColumns.Heading]),
                    QuestionText = question.Field<string>(ModuleColumns.QuestionText) ?? "",
                    QuestionType = question.Field<string>(ModuleColumns.QuestionType) ?? "",
                    DataType = question.Field<string>(ModuleColumns.DataType) ?? "",
                    IsMandatory = conformance == "Mandatory",
                    IsOptional = conformance == "Optional",
                    VersionId = fileName
                };

                var answersString = question.Field<string>(ModuleColumns.Answers) ?? "";

                questionDto.Answers =
                    answersString
                    .Split(',')
                    .Where(answer => answer.StartsWith("OPT"))
                    .Select(answer => new AnswerDto
                    {
                        AnswerId = answer,
                        AnswerText = "",
                        VersionId = fileName
                    })
                    .ToList();

                questionDto.Rules = GetRules(rulesSheet, questionId, fileName);

                model.QuestionDtos.Add(questionDto);
            }
        }

        var isValid = await ValidateQuestions(model);

        if (!isValid)
        {
            await PopulateVersions(model);
            return View(nameof(Index), model);
        }

        VersionDto version = new VersionDto()
        {
            VersionId = fileName,
            CreatedAt = DateTime.UtcNow,
            PublishedBy = null,
            PublishedAt = null,
            IsDraft = true,
            IsPublished = false,
        };

        var response = await questionSetService.CreateQuestions(
            new QuestionSetDto
            {
                Version = version,
                Categories = model.CategoryDtos,
                Sections = model.SectionDtos,
                AnswerOptions = model.AnswerOptionDtos,
                Questions = model.QuestionDtos
            });

        ViewBag.Success = response.IsSuccessStatusCode;

        await PopulateVersions(model);
        return View(nameof(Index), model);
    }

    private static List<RuleDto> GetRules(DataTable rulesSheet, string questionId, string fileName)
    {
        var groupedRules = rulesSheet
            .AsEnumerable()
            .Where(row => row.Field<string>(RulesColumns.QuestionId) == questionId)
            .GroupBy(row => Convert.ToInt32(row[RulesColumns.RuleId]));

        var rules = groupedRules
            .Select(group => new RuleDto
            {
                QuestionId = group.First().Field<string>(RulesColumns.QuestionId) ?? "",
                Sequence = Convert.ToInt32(group.First()[RulesColumns.Sequence]),
                ParentQuestionId = group.First().Field<string>(RulesColumns.ParentQuestionId) ?? "",
                Mode = group.First().Field<string>(RulesColumns.Mode) ?? "",
                Description = group.First().Field<string>(RulesColumns.Description) ?? "",
                VersionId = fileName,
                Conditions = group
                    .Select(condition => new ConditionDto
                    {
                        Mode = condition.Field<string>(RulesColumns.ConditionMode) ?? "",
                        Operator = condition.Field<string>(RulesColumns.ConditionOperator) ?? "",
                        Value = condition.Field<string>(RulesColumns.ConditionValue),
                        Negate = condition.Field<bool>(RulesColumns.ConditionNegate),
                        ParentOptions = condition.Field<string>(RulesColumns.ConditionParentOptions)?.Split(",").ToList() ?? [],
                        OptionType = condition.Field<string>(RulesColumns.ConditionOptionType) ?? "",
                        Description = condition.Field<string>(RulesColumns.ConditionDescription),
                        IsApplicable = true,
                    })
                    .ToList()
            })
            .ToList();

        return rules;
    }

    private async Task<bool> ValidateQuestions(QuestionSetFileModel model)
    {
        var context = new ValidationContext<QuestionSetFileModel>(model);

        context.RootContextData["questionDtos"] = model.QuestionDtos;

        var result = await validator.ValidateAsync(context);

        if (!result.IsValid)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("Upload", error.ErrorMessage);
            }

            return false;
        }

        return true;
    }

    private bool HasValidColumns(string sheetName, DataTable sheet)
    {
        IEnumerable<string> requiredColumns = sheetName switch
        {
            SheetNames.AnswerOptions => AnswerOptionsColumns.All,
            SheetNames.Rules => RulesColumns.All,
            SheetNames.Contents => ContentsColumns.All,
            _ => ModuleColumns.All,
        };
        var missingColumns = requiredColumns.Where(column => !sheet.Columns.Contains(column));

        foreach (var missingColumn in missingColumns)
        {
            ModelState.AddModelError("Upload", $"Sheet '{sheetName}' does not contain column '{missingColumn}'");
        }

        return !missingColumns.Any();
    }
}