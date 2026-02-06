using Mapster;
using Moq.AutoMock;
using Rsp.Portal.Web.Mapping;

namespace Rsp.Portal.UnitTests;

public class TestServiceBase
{
    public AutoMocker Mocker { get; }

    public TestServiceBase()
    {
        Mocker = new AutoMocker();

        var config = new TypeAdapterConfig();
        config.RuleMap.Clear();
        config.Scan(typeof(MappingRegister).Assembly);
    }
}