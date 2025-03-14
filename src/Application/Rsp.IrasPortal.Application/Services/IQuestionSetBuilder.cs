using System.Data;
using Rsp.IrasPortal.Application.DTOs;

namespace Rsp.IrasPortal.Application.Services;

public interface IQuestionSetBuilder
{
    /// <summary>
    /// Attaches version data to a question set
    /// </summary>
    /// <param name="version">Version Id</param>
    public IQuestionSetBuilder WithVersion(string version);

    /// <summary>
    /// Attaches category data to a question set
    /// </summary>
    /// <param name="contentsTable">The contents table from the uploaded question set excel</param>
    public IQuestionSetBuilder WithCategories(DataTable contentsTable);

    /// <summary>
    /// Attaches question data to a question set
    /// </summary>
    /// <param name="moduleTables">A list of module tables from the uploaded question set excel</param>
    /// <param name="rulesTable">The rules table from the uploaded question set excel</param>
    /// <param name="answerOptionsTable">The answer options table from the uploaded question set excel</param>
    /// <returns></returns>
    public IQuestionSetBuilder WithQuestions(List<DataTable> moduleTables, DataTable rulesTable, DataTable answerOptionsTable);

    /// <summary>
    /// Constructs the question set DTO
    /// </summary>
    public QuestionSetDto Build();
}