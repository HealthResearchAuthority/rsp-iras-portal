using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;
using Rsp.Portal.UnitTests;

namespace Rsp.IrasPortal.UnitTests.Services.ProjectCollaboratorServiceTests;

public class ProjectCollaboratorTests : TestServiceBase<ProjectCollaboratorService>
{
    [Theory]
    [AutoData]
    public async Task GetProjectCollaborators_Should_Return_Response_When_Client_Returns_Success(
        string projectRecordId,
        List<ProjectCollaboratorResponse> collaborators)
    {
        var apiResponse = Mock.Of<IApiResponse<IEnumerable<ProjectCollaboratorResponse>>>(r =>
            r.IsSuccessStatusCode &&
            r.StatusCode == HttpStatusCode.OK &&
            r.Content == collaborators);

        var client = Mocker.GetMock<IProjectCollaboratorServiceClient>();
        client.Setup(c => c.GetProjectCollaborators(projectRecordId)).Returns(Task.FromResult(apiResponse));

        var result = await Sut.GetProjectCollaborators(projectRecordId);

        var model = result.ShouldBeOfType<ServiceResponse<IEnumerable<ProjectCollaboratorResponse>>>();
        model.IsSuccessStatusCode.ShouldBeTrue();
        model.StatusCode.ShouldBe(HttpStatusCode.OK);
        model.Content.ShouldBe(collaborators);

        client.Verify(c => c.GetProjectCollaborators(projectRecordId), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task SaveProjectCollaborator_Should_Return_Response_When_Client_Returns_Success(
        ProjectCollaboratorRequest request,
        ProjectCollaboratorResponse response)
    {
        var apiResponse = Mock.Of<IApiResponse<ProjectCollaboratorResponse>>(r =>
            r.IsSuccessStatusCode &&
            r.StatusCode == HttpStatusCode.OK &&
            r.Content == response);

        var client = Mocker.GetMock<IProjectCollaboratorServiceClient>();
        client.Setup(c => c.SaveProjectCollaborator(request)).Returns(Task.FromResult(apiResponse));

        var result = await Sut.SaveProjectCollaborator(request);

        var model = result.ShouldBeOfType<ServiceResponse<ProjectCollaboratorResponse>>();
        model.IsSuccessStatusCode.ShouldBeTrue();
        model.StatusCode.ShouldBe(HttpStatusCode.OK);
        model.Content.ShouldBe(response);

        client.Verify(c => c.SaveProjectCollaborator(request), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task UpdateCollaboratorAccess_Should_Return_Response_When_Client_Returns_Success(
        UpdateCollaboratorAccessRequest request)
    {
        var apiResponse = Mock.Of<IApiResponse>(r =>
            r.IsSuccessStatusCode &&
            r.StatusCode == HttpStatusCode.OK);

        var client = Mocker.GetMock<IProjectCollaboratorServiceClient>();
        client.Setup(c => c.UpdateCollaboratorAccess(request)).Returns(Task.FromResult(apiResponse));

        var result = await Sut.UpdateCollaboratorAccess(request);

        var model = result.ShouldBeOfType<ServiceResponse>();
        model.IsSuccessStatusCode.ShouldBeTrue();
        model.StatusCode.ShouldBe(HttpStatusCode.OK);

        client.Verify(c => c.UpdateCollaboratorAccess(request), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task RemoveProjectCollaborator_Should_Return_Response_When_Client_Returns_Success(string projectCollaboratorId)
    {
        var apiResponse = Mock.Of<IApiResponse>(r =>
            r.IsSuccessStatusCode &&
            r.StatusCode == HttpStatusCode.OK);

        var client = Mocker.GetMock<IProjectCollaboratorServiceClient>();
        client.Setup(c => c.RemoveProjectCollaborator(projectCollaboratorId)).Returns(Task.FromResult(apiResponse));

        var result = await Sut.RemoveProjectCollaborator(projectCollaboratorId);

        var model = result.ShouldBeOfType<ServiceResponse>();
        model.IsSuccessStatusCode.ShouldBeTrue();
        model.StatusCode.ShouldBe(HttpStatusCode.OK);

        client.Verify(c => c.RemoveProjectCollaborator(projectCollaboratorId), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task GetCollaboratorProjects_Should_Return_Response_When_Client_Returns_Success(
        string userId,
        List<CollaboratorProjectResponse> projects)
    {
        var apiResponse = Mock.Of<IApiResponse<IEnumerable<CollaboratorProjectResponse>>>(r =>
            r.IsSuccessStatusCode &&
            r.StatusCode == HttpStatusCode.OK &&
            r.Content == projects);

        var client = Mocker.GetMock<IProjectCollaboratorServiceClient>();
        client.Setup(c => c.GetCollaboratorProjects(userId)).Returns(Task.FromResult(apiResponse));

        var result = await Sut.GetCollaboratorProjects(userId);

        var model = result.ShouldBeOfType<ServiceResponse<IEnumerable<CollaboratorProjectResponse>>>();
        model.IsSuccessStatusCode.ShouldBeTrue();
        model.StatusCode.ShouldBe(HttpStatusCode.OK);
        model.Content.ShouldBe(projects);

        client.Verify(c => c.GetCollaboratorProjects(userId), Times.Once);
    }
}