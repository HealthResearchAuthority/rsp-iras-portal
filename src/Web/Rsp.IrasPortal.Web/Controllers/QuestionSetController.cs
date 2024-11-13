using System.Data;
using System.Text;
using ExcelDataReader;
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
public class QuestionSetController(ILogger<QuestionSetController> logger, IQuestionSetService questionSetService) : Controller
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
        DataTable rulesSheet;
        DataTable answerOptionsSheet;

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

        var questionDtos = new List<QuestionDto>();

        foreach (var sheet in moduleSheets)
        {
            foreach (DataRow question in sheet.Rows)
            {
                var questionId = Convert.ToString(question[ModuleColumns.QuestionId]);

                if (questionId == null || Convert.IsDBNull(questionId))
                {
                    continue;
                }

                if (questionId.StartsWith("IQT"))
                {
                    continue;
                }

                var conformance = Convert.ToString(question[ModuleColumns.Conformance]);

                var questionDto = new QuestionDto
                {
                    QuestionId = questionId!,
                    Category = Convert.ToString(question[ModuleColumns.Category])!,
                    SectionId = Convert.ToString(question[ModuleColumns.Section])!,
                    Section = Convert.ToString(question[ModuleColumns.Section])!,
                    Sequence = Convert.ToInt32(question[ModuleColumns.Sequence]),
                    Heading = Convert.ToString(question[ModuleColumns.Heading])!,
                    QuestionText = Convert.ToString(question[ModuleColumns.QuestionText])!,
                    QuestionType = Convert.ToString(question[ModuleColumns.QuestionType])!,
                    DataType = Convert.ToString(question[ModuleColumns.DataType])!,
                    IsMandatory = conformance == "Mandatory" || conformance == "Conditional mandatory",
                    IsOptional = conformance == "Optional",
                    Rules = []
                };

                var answersString = Convert.ToString(question[ModuleColumns.Answers]);

                if (answersString == null || Convert.IsDBNull(answersString) || answersString.Length < 3)
                {
                    questionDto.Answers = [];
                }
                else
                {
                    var answers = answersString.Split(',');
                    questionDto.Answers = answers.Where(answer => answer.StartsWith("OPT")).Select(answer => new AnswerDto
                    {
                        AnswerId = answer,
                        AnswerText = answer,
                    }).ToList();
                }

                questionDtos.Add(questionDto);
            }
        }

        var response = await questionSetService.CreateQuestions(questionDtos);

        if (response.IsSuccessStatusCode)
        {
            ViewBag.Success = true;
        }

        return View(model);
    }

    private bool HasValidColumns(string sheetName, DataTable sheet)
    {
        IEnumerable<string> requiredColumns = sheetName switch
        {
            SheetNames.AnswerOptions => AnswerOptionsColumns.All,
            SheetNames.Rules => RulesColumns.All,
            _ => ModuleColumns.All,
        };
        var missingColumns = requiredColumns.Where(column => !sheet.Columns.Contains(column));

        foreach (var missingColumn in missingColumns)
        {
            ModelState.AddModelError("Upload", $"Sheet '{sheetName}' does not contain column {missingColumn}");
        }

        return !missingColumns.Any();
    }
}