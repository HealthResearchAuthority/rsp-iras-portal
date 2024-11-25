using System.Data;
using System.Text;
using ExcelDataReader;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Models;
using Rsp.Logging.Extensions;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "questionset:[action]")]
[Authorize(Policy = "IsAdmin")]
public class QuestionSetController(ILogger<QuestionSetController> logger, IQuestionSetService questionSetService, IValidator<QuestionSetFileModel> validator) : Controller
{
    public IActionResult Upload()
    {
        logger.LogInformationHp("called");

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Upload(QuestionSetFileModel model)
    {
        logger.LogInformationHp("called");

        var file = model.Upload;

        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("Upload", "Please upload a file");
            return View(model);
        }

        string[] allowedExtensions = [".xlsx", ".xlsb", ".xls"];

        if (!allowedExtensions.Contains(Path.GetExtension(file.FileName).ToLower()))
        {
            ModelState.AddModelError("Upload", $"Please upload a file of type: {string.Join(", ", allowedExtensions)}");
            return View(model);
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
            return View(model);
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
                OptionText = answerOption.Field<string>(AnswerOptionsColumns.OptionText) ?? ""
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
                    })
                    .ToList();

                questionDto.Rules = GetRules(rulesSheet, questionId);

                model.QuestionDtos.Add(questionDto);
            }
        }

        var isValid = await ValidateQuestions(model);

        if (!isValid)
        {
            return View(model);
        }

        var response = await questionSetService.CreateQuestions(
            new QuestionSetDto
            {
                Categories = model.CategoryDtos,
                Sections = model.SectionDtos,
                AnswerOptions = model.AnswerOptionDtos,
                Questions = model.QuestionDtos
            });

        ViewBag.Success = response.IsSuccessStatusCode;

        return View(model);
    }

    private static List<RuleDto> GetRules(DataTable rulesSheet, string questionId)
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