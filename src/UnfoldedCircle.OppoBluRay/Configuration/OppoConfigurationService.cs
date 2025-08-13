using System.Text.Json.Serialization.Metadata;

using UnfoldedCircle.OppoBluRay.Json;
using UnfoldedCircle.Server.Configuration;

namespace UnfoldedCircle.OppoBluRay.Configuration;

internal sealed class OppoConfigurationService(IConfiguration configuration)
    : ConfigurationService<OppoConfigurationItem>(configuration)
{
    protected override JsonTypeInfo<UnfoldedCircleConfiguration<OppoConfigurationItem>> GetSerializer()
        => OppoJsonSerializerContext.Instance.UnfoldedCircleConfigurationOppoConfigurationItem;
}