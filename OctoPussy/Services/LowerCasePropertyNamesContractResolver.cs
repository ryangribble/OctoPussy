using Newtonsoft.Json.Serialization;

namespace OctoPussy.Services
{
    public class LowerCasePropertyNamesContractResolver : DefaultContractResolver
    {
        protected override string ResolvePropertyName(string propertyName)
        {
            if (!string.IsNullOrEmpty(propertyName))
                return propertyName.ToLowerInvariant();

            return base.ResolvePropertyName(propertyName);
        }
    }
}