using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Rsp.IrasPortal.Configuration.FeatureFolders;

public class FeatureControllerModelConvention : IControllerModelConvention
{
    private readonly string _folderName;
    private readonly Func<ControllerModel, string?> _nameDerivationStrategy;

    public FeatureControllerModelConvention(FeatureFolderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _folderName = options.FeatureFolderName;

        _nameDerivationStrategy = options.DeriveFeatureFolderName ?? DeriveFeatureFolderName;
    }

    public void Apply(ControllerModel controller)
    {
        ArgumentNullException.ThrowIfNull(controller);

        var featureName = _nameDerivationStrategy(controller);

        if (!string.IsNullOrEmpty(featureName))
        {
            controller.Properties.Add("feature", featureName);
        }
    }

    private string? DeriveFeatureFolderName(ControllerModel model)
    {
        var @namespace = model.ControllerType.Namespace;

        var result = @namespace?
                                .Replace(".Controllers", string.Empty)
                                .Split('.')
                                .SkipWhile(s => s != _folderName)
                                .Aggregate("", Path.Combine);

        return result ?? _folderName;
    }
}