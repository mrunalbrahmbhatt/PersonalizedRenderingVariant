using Sitecore.ContentTesting.ComponentTesting;
using Sitecore.ContentTesting.Mvc.Pipelines.Response.CustomizeRendering;
using Sitecore.Diagnostics;
using Sitecore.Layouts;
using Sitecore.Mvc.Analytics.Pipelines.Response.CustomizeRendering;
using Sitecore.Mvc.Presentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.XA.Foundation.RenderingVariant.Extensions;

namespace Sitecore.XA.Foundation.RenderingVariant.Pipeline.CustomizeRendering
{
    public class SelectVariationTransferParameter : SelectVariation
    {
        protected override void ApplyVariation(CustomizeRenderingArgs args, ComponentTestContext context)
        {
            base.ApplyVariation(args, context);

            RenderingReference renderingReference = context.Components.FirstOrDefault((RenderingReference c) => c.UniqueId == context.Component.UniqueId);
            if (renderingReference != null)
            {
                ExtensionMethods.TransferRenderingParameters(args.Rendering, renderingReference);
            }
        }
    }
}