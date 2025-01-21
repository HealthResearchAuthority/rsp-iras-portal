using System.Data;
using Microsoft.AspNetCore.Http;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.QuestionSetServiceTests;

public class ProcessQuestionSetFileTests : TestServiceBase<QuestionSetService>
{
    private readonly Mock<IQuestionSetServiceClient> _questionSetServiceClient;

    public ProcessQuestionSetFileTests()
    {
        _questionSetServiceClient = Mocker.GetMock<IQuestionSetServiceClient>();
    }

    [Fact]
    public void ProcessQuestionSetFile_Should_Return_Error_Response_For_Invalid_File_Extension()
    {
        var invalidFileMock = new Mock<IFormFile>();
        invalidFileMock.Setup(f => f.FileName).Returns("InvalidFileExtension.txt");

        var result = Sut.ProcessQuestionSetFile(invalidFileMock.Object);

        result.ShouldNotBeNull();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public void ProcessQuestionSetFile_Should_Return_Success_Response_For_Valid_File()
    {
        var currentDirectoryPath = Directory.GetCurrentDirectory();
        var filePath = Path.Combine(currentDirectoryPath, "Data/ValidQuestionSet.xlsx");
        var fileStream = new FileStream(filePath, FileMode.Open);
        IFormFile file = new FormFile(fileStream, 0, fileStream.Length, fileStream.Name, fileStream.Name);

        var questionSetBuilder = Mocker.GetMock<IQuestionSetBuilder>();

        questionSetBuilder
            .Setup(qsb => qsb.WithVersion(It.IsAny<string>()))
            .Returns(questionSetBuilder.Object);

        questionSetBuilder
            .Setup(qsb => qsb.WithCategories(It.IsAny<DataTable>()))
            .Returns(questionSetBuilder.Object);

        questionSetBuilder
            .Setup(qsb => qsb.WithQuestions(It.IsAny<List<DataTable>>(), It.IsAny<DataTable>(), It.IsAny<DataTable>()))
            .Returns(questionSetBuilder.Object);

        var fakeQuestionSetDto = new QuestionSetDto();
        questionSetBuilder
            .Setup(qsb => qsb.Build())
            .Returns(fakeQuestionSetDto);

        var result = Sut.ProcessQuestionSetFile(file);

        result.ShouldNotBeNull();
        result.IsSuccessStatusCode.ShouldBeTrue();
    }

    [Fact]
    public void ProcessQuestionSetFile_Should_Return_Error_Response_For_Invalid_File_With_Missing_Sheets()
    {
        var currentDirectoryPath = Directory.GetCurrentDirectory();
        var filePath = Path.Combine(currentDirectoryPath, "Data/InvalidQuestionSetMissingSheets.xlsx");
        var fileStream = new FileStream(filePath, FileMode.Open);
        IFormFile file = new FormFile(fileStream, 0, fileStream.Length, fileStream.Name, fileStream.Name);

        var result = Sut.ProcessQuestionSetFile(file);

        result.ShouldNotBeNull();
        result.IsSuccessStatusCode.ShouldBeFalse();
    }

    [Fact]
    public void ProcessQuestionSetFile_Should_Return_Error_Response_For_Invalid_File_With_Missing_Columns()
    {
        var currentDirectoryPath = Directory.GetCurrentDirectory();
        var filePath = Path.Combine(currentDirectoryPath, "Data/InvalidQuestionSetMissingColumns.xlsx");
        var fileStream = new FileStream(filePath, FileMode.Open);
        IFormFile file = new FormFile(fileStream, 0, fileStream.Length, fileStream.Name, fileStream.Name);

        var result = Sut.ProcessQuestionSetFile(file);

        result.ShouldNotBeNull();
        result.IsSuccessStatusCode.ShouldBeFalse();
    }
}