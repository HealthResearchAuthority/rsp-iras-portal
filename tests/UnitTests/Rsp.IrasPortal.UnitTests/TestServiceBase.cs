using Mapster;
using Moq.AutoMock;
using Rsp.IrasPortal.Web.Mapping;

namespace Rsp.IrasPortal.UnitTests;

public class TestServiceBase
{
    public AutoMocker Mocker { get; }

    public TestServiceBase()
    {
        Mocker = new AutoMocker();

        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(typeof(MappingRegister).Assembly);
    }
}