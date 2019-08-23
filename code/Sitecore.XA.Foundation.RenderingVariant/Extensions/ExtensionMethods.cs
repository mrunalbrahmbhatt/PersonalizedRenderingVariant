using Sitecore.Diagnostics;
using Sitecore.Layouts;
using Sitecore.Mvc.Presentation;

namespace Sitecore.XA.Foundation.RenderingVariant.Extensions
{
    public static class ExtensionMethods
    {
        public static void TransferRenderingParameters(Rendering rendering, RenderingReference reference)
        {
            Assert.ArgumentNotNull(rendering, "rendering");
            Assert.ArgumentNotNull(reference, "reference");
            rendering.Parameters = new RenderingParameters(reference.Settings.Parameters);
        }
        public static void TransferRenderingParameters(RenderingDefinition rendering, RenderingReference reference)
        {
            Assert.ArgumentNotNull(rendering, "rendering");
            Assert.ArgumentNotNull(reference, "reference");
            rendering.Parameters = reference.Settings.Parameters;
        }
    }
}