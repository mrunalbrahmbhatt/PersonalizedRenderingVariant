using Sitecore.Analytics;
using Sitecore.Diagnostics;
using Sitecore.Layouts;
using Sitecore.Mvc.Analytics.Pipelines.Response.CustomizeRendering;
using Sitecore.Mvc.Presentation;
using Sitecore.Web;
using Sitecore.Data.Items;
using Sitecore.Analytics.Pipelines.GetRenderingRules;
using Sitecore.Rules.ConditionalRenderings;
using Sitecore.Rules;
using System.Collections.Generic;

namespace Sitecore.XA.Foundation.VariantPersonalization.Pipeline.CustomizeRendering
{
    public class ResolvePersonalizedRenderingVariant : Personalize
    {
        public ResolvePersonalizedRenderingVariant():base()
        {

        }
        public override void Process(CustomizeRenderingArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (!args.IsCustomized && Tracker.IsActive)
            {
                Evaluate(args);
            }
        }

        protected override void Evaluate(CustomizeRenderingArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            Item item = args.PageContext.Item;
            if (item != null)
            {
                RenderingReference renderingReference = CustomizeRenderingProcessor.GetRenderingReference(args.Rendering, Context.Language, args.PageContext.Database);
                GetRenderingRulesArgs getRenderingRulesArgs = new GetRenderingRulesArgs(item, renderingReference);
                GetRenderingRulesPipeline.Run(getRenderingRulesArgs);
                RuleList<ConditionalRenderingsRuleContext> ruleList = getRenderingRulesArgs.RuleList;
                if (ruleList != null && ruleList.Count != 0)
                {
                    List<RenderingReference> references = new List<RenderingReference>
                {
                    renderingReference
                };
                    ConditionalRenderingsRuleContext conditionalRenderingsRuleContext = new ConditionalRenderingsRuleContext(references, renderingReference)
                    {
                        Item = item
                    };
                    conditionalRenderingsRuleContext.Parameters["mvc.rendering"] = args.Rendering;
                    RunRules(ruleList, conditionalRenderingsRuleContext);
                    ApplyActions(args, conditionalRenderingsRuleContext);
                    args.IsCustomized = true;
                }
            }
        }

        protected override void ApplyChanges(Rendering rendering, RenderingReference reference)
        {
            Assert.ArgumentNotNull(rendering, "rendering");
            Assert.ArgumentNotNull(reference, "reference");
            TransferRenderingVariant(rendering, reference);
        }
        private static void TransferRenderingVariant(Rendering rendering, RenderingReference reference)
        {
            Assert.ArgumentNotNull(rendering, "rendering");
            Assert.ArgumentNotNull(reference, "reference");
            if (reference.RenderingItem != null)
            {
                var parameters = WebUtil.ParseQueryString(reference.Settings.Parameters, true);
                rendering.Parameters["FieldNames"] = parameters["FieldNames"];
            }
        }
    }
}
