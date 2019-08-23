
using Sitecore.Layouts;
using Sitecore.Mvc.Analytics.Pipelines.Response.CustomizeRendering;
using Sitecore.Rules.ConditionalRenderings;
using Sitecore.XA.Foundation.RenderingVariant.Extensions;

namespace Sitecore.XA.Foundation.RenderingVariant.Pipeline.CustomizeRendering
{
    public class PersonalizeTransferParameter:Personalize
    {
        protected override void ApplyActions(CustomizeRenderingArgs args, ConditionalRenderingsRuleContext context)
        {
            base.ApplyActions(args, context);
            RenderingReference renderingReference = context.References.Find((RenderingReference r) => r.UniqueId == context.Reference.UniqueId);
            if (renderingReference != null)
            {
                ExtensionMethods.TransferRenderingParameters(args.Rendering, renderingReference);
            }
        }
    }
}