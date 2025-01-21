using System.Data;
using System.Net;
using System.Text;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services.Extensions;

namespace Rsp.IrasPortal.Services;

/// <inheritdoc/>
public class QuestionSetService(IQuestionSetServiceClient client) : IQuestionSetService
{
    /// <inheritdoc/>
    public async Task<ServiceResponse<IEnumerable<QuestionsResponse>>> GetQuestions()
    {
        // get all questions
        var apiResponse = await client.GetQuestions();

        // convert to service response
        return apiResponse.ToServiceResponse();
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<IEnumerable<QuestionsResponse>>> GetQuestions(string categoryId)
    {
        // get all questions for the category
        var apiResponse = await client.GetQuestions(categoryId);

        // convert to service response
        return apiResponse.ToServiceResponse();
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<IEnumerable<QuestionsResponse>>> GetQuestionsByVersion(string versionId)
    {
        // get all questions
        var apiResponse = await client.GetQuestionsByVersion(versionId);

        // convert to service response
        return apiResponse.ToServiceResponse();
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<IEnumerable<QuestionsResponse>>> GetQuestionsByVersion(string versionId, string categoryId)
    {
        // get all questions for the category
        var apiResponse = await client.GetQuestionsByVersion(versionId, categoryId);

        // convert to service response
        return apiResponse.ToServiceResponse();
    }

    public ServiceResponse<QuestionSetDto> ProcessQuestionSetFile(IFormFile file)
    {
        List<string> allowedExtensions = [".xlsx", ".xlsb", ".xls"];
        ServiceResponse<QuestionSetDto> errorResponse = new()
        {
            Error = "An error occured while processing your file",
            StatusCode = HttpStatusCode.BadRequest
        };

        if (!allowedExtensions.Contains(Path.GetExtension(file.FileName).ToLower()))
        {
            return errorResponse.WithReason($"Please upload a file of type: {string.Join(", ", allowedExtensions)}");
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

        List<DataTable> moduleTables = [];
        DataTable rulesTable = new();
        DataTable answerOptionsTable = new();
        DataTable contentsTable = new();

        foreach (var sheetName in SheetNames.All)
        {
            var table = result.Tables[sheetName];

            if (table == null)
            {
                return errorResponse.WithReason($"Sheet '{sheetName}' cannot be found");
            }

            var tableColumnError = ValidateColumns(sheetName, table);

            if (tableColumnError != null)
            {
                return errorResponse.WithReason(tableColumnError);
            }

            switch (sheetName)
            {
                case SheetNames.Rules:
                    rulesTable = table;
                    break;

                case SheetNames.AnswerOptions:
                    answerOptionsTable = table;
                    break;

                case SheetNames.Contents:
                    contentsTable = table;
                    break;

                default:
                    moduleTables.Add(table);
                    break;
            }
        }

        var version = Path.GetFileNameWithoutExtension(file.FileName);
        var versionDto = new VersionDto()
        {
            VersionId = version,
            CreatedAt = DateTime.UtcNow,
            PublishedBy = null,
            PublishedAt = null,
            IsDraft = true,
            IsPublished = false,
        };
        var answerOptionsDict = BuildAnswerOptionsDictionary(answerOptionsTable);
        var categoryDtos = BuildCategoryDtos(contentsTable, version);
        var questionDtos = BuildQuestionDtos(moduleTables, rulesTable, answerOptionsDict, version);

        return new ServiceResponse<QuestionSetDto>
        {
            Content = new QuestionSetDto
            {
                Questions = questionDtos,
                Categories = categoryDtos,
                Version = versionDto,
            },
            StatusCode = HttpStatusCode.OK,
        };
    }

    private static Dictionary<string, string> BuildAnswerOptionsDictionary(DataTable answerOptionsTable)
    {
        Dictionary<string, string> result = [];

        foreach (DataRow answerOption in answerOptionsTable.Rows)
        {
            var answerOptionId = answerOption.Field<string>(AnswerOptionsColumns.OptionId);
            var answerOptionText = answerOption.Field<string>(AnswerOptionsColumns.OptionText);

            if (answerOptionId != null && answerOptionText != null)
            {
                result.Add(answerOptionId, answerOptionText);
            }
        }

        return result;
    }

    private static List<CategoryDto> BuildCategoryDtos(DataTable contentsTable, string version)
    {
        List<CategoryDto> categoryDtos = [];

        foreach (DataRow category in contentsTable.Rows)
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
                VersionId = version
            };

            categoryDtos.Add(categoryDto);
        }

        return categoryDtos;
    }

    private static List<QuestionDto> BuildQuestionDtos(List<DataTable> moduleTables, DataTable rulesTable, Dictionary<string, string> answerOptionsDict, string version)
    {
        List<QuestionDto> questionDtos = [];

        foreach (var table in moduleTables)
        {
            string sectionName = "";

            foreach (DataRow question in table.Rows)
            {
                var questionId = question.Field<string>(ModuleColumns.QuestionId);

                if (questionId == null || !questionId.StartsWith("IQ"))
                {
                    continue;
                }

                if (questionId.StartsWith("IQT"))
                {
                    sectionName = question.Field<string>(ModuleColumns.QuestionText) ?? "";
                    continue;
                }

                var conformance = question.Field<string>(ModuleColumns.Conformance);

                var questionDto = new QuestionDto
                {
                    QuestionId = questionId,
                    Category = question.Field<string>(ModuleColumns.Category) ?? "",
                    SectionId = question.Field<string>(ModuleColumns.Section) ?? "",
                    Section = sectionName ?? "",
                    Sequence = Convert.ToInt32(question[ModuleColumns.Sequence]),
                    Heading = Convert.ToString(question[ModuleColumns.Heading]),
                    QuestionText = question.Field<string>(ModuleColumns.QuestionText) ?? "",
                    QuestionType = question.Field<string>(ModuleColumns.QuestionType) ?? "",
                    DataType = question.Field<string>(ModuleColumns.DataType) ?? "",
                    IsMandatory = conformance == "Mandatory",
                    IsOptional = conformance == "Optional",
                    VersionId = version
                };

                var answersString = question.Field<string>(ModuleColumns.Answers) ?? "";
                questionDto.Answers = answersString
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Where(answerOptionsDict.ContainsKey)
                    .Select(answerOptionId => new AnswerDto
                    {
                        AnswerId = answerOptionId,
                        AnswerText = answerOptionsDict[answerOptionId],
                        VersionId = version
                    })
                    .ToList();

                questionDto.Rules = GetRules(rulesTable, questionId, version);

                questionDtos.Add(questionDto);
            }
        }

        return questionDtos;
    }

    private static List<RuleDto> GetRules(DataTable rulesTable, string questionId, string version)
    {
        var groupedRules = rulesTable
            .AsEnumerable()
            .Where(row => row.Field<string>(RulesColumns.QuestionId) == questionId)
            .GroupBy(row => Convert.ToInt32(row[RulesColumns.RuleId]));

        var rules = groupedRules
            .Select(group => new RuleDto
            {
                QuestionId = group.First().Field<string>(RulesColumns.QuestionId) ?? "",
                Sequence = Convert.ToInt32(group.First()[RulesColumns.Sequence]),
                ParentQuestionId = group.First().Field<string>(RulesColumns.ParentQuestionId),
                Mode = group.First().Field<string>(RulesColumns.Mode) ?? "",
                Description = group.First().Field<string>(RulesColumns.Description) ?? "",
                VersionId = version,
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

    private static string? ValidateColumns(string sheetName, DataTable sheet)
    {
        IEnumerable<string> requiredColumns = sheetName switch
        {
            SheetNames.AnswerOptions => AnswerOptionsColumns.All,
            SheetNames.Rules => RulesColumns.All,
            SheetNames.Contents => ContentsColumns.All,
            _ => ModuleColumns.All,
        };

        var missingColumns = requiredColumns.Where(column => !sheet.Columns.Contains(column));

        return missingColumns.Any()
            ? $"Sheet '{sheetName}' is missing columns: {string.Join(", ", missingColumns)}"
            : null;
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse> AddQuestionSet(QuestionSetDto questionSet)
    {
        var apiResponse = await client.AddQuestionSet(questionSet);

        return apiResponse.ToServiceResponse();
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<IEnumerable<VersionDto>>> GetVersions()
    {
        // get all versions
        var apiResponse = await client.GetVersions();

        // convert to service response
        return apiResponse.ToServiceResponse();
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse> PublishVersion(string versionId)
    {
        // publish version
        var apiResponse = await client.PublishVersion(versionId);

        // convert to service response
        return apiResponse.ToServiceResponse();
    }
}