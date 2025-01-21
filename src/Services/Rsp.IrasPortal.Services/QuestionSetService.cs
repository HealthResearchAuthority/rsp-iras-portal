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
public class QuestionSetService(IQuestionSetServiceClient client, IQuestionSetBuilder questionSetBuilder) : IQuestionSetService
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
    public async Task<ServiceResponse<IEnumerable<QuestionsResponse>>> GetQuestions(string categoryId, string sectionId)
    {
        var apiResponse = await client.GetQuestions(categoryId, sectionId);
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

    /// <inheritdoc/>
    public async Task<ServiceResponse<IEnumerable<QuestionsResponse>>> GetQuestionsByVersion(string versionId, string categoryId, string sectionId)
    {
        // get all questions for the category
        var apiResponse = await client.GetQuestionsByVersion(versionId, categoryId, sectionId);

        // convert to service response
        return apiResponse.ToServiceResponse();
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<IEnumerable<QuestionSectionsResponse>>> GetQuestionSections()
    {
        var apiResponse = await client.GetQuestionSections();
        return apiResponse.ToServiceResponse();
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<QuestionSectionsResponse>> GetPreviousQuestionSection(string sectionId)
    {
        var apiResponse = await client.GetPreviousQuestionSection(sectionId);
        return apiResponse.ToServiceResponse();
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<QuestionSectionsResponse>> GetNextQuestionSection(string sectionId)
    {
        var apiResponse = await client.GetNextQuestionSection(sectionId);
        return apiResponse.ToServiceResponse();
    }

    /// <inheritdoc/>
    public async Task<ServiceResponse<IEnumerable<CategoryDto>>> GetQuestionCategories()
    {
        var apiResponse = await client.GetQuestionCategories();
        return apiResponse.ToServiceResponse();
    }

    /// <inheritdoc/>
    public ServiceResponse<QuestionSetDto> ProcessQuestionSetFile(IFormFile file)
    {
        ServiceResponse<QuestionSetDto> errorResponse = new()
        {
            Error = "An error occured while processing your file",
            StatusCode = HttpStatusCode.BadRequest
        };

        List<string> allowedExtensions = [".xlsx", ".xlsb", ".xls"];
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

        var questionSetDto = questionSetBuilder
            .WithVersion(version)
            .WithCategories(contentsTable)
            .WithQuestions(moduleTables, rulesTable, answerOptionsTable)
            .Build();

        return new ServiceResponse<QuestionSetDto>
        {
            Content = questionSetDto,
            StatusCode = HttpStatusCode.OK
        };
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

    /// <summary>
    /// Validates the column headings in the uploaded files to ensure they match the expected strings
    /// </summary>
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
}