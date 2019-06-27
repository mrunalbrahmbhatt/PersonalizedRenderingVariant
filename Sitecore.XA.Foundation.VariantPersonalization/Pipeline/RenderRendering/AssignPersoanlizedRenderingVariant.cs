using Microsoft.Extensions.DependencyInjection;
using Sitecore.DependencyInjection;
using Sitecore.Diagnostics;
using Sitecore.Extensions.XElementExtensions;
using Sitecore.Mvc.Analytics.Pipelines.Response.CustomizeRendering;
using Sitecore.Mvc.Pipelines.Response.RenderRendering;
using Sitecore.Mvc.Presentation;
using Sitecore.XA.Foundation.Abstractions;
using Sitecore.XA.Foundation.Multisite.Extensions;
using System.Linq;
using System.Xml.Linq;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Sitecore.Analytics.Pipelines.GetRenderingRules;
using Sitecore.Rules.ConditionalRenderings;
using Sitecore.Globalization;
using Sitecore.Data;
using Sitecore.Rules;
using System.Collections.Generic;
using Sitecore.Mvc.Extensions;
using Sitecore.Mvc.Analytics.Presentation;
using Sitecore.Analytics.Pipelines.RenderingRuleEvaluated;

namespace Sitecore.XA.Foundation.VariantPersonalization.Pipeline.RenderRendering
{
    public class AssignPersoanlizedRenderingVariant : RenderRenderingProcessor
    {
        public override void Process(RenderRenderingArgs args)
        {
            Rendering rendering = args.Rendering;
            if (rendering.Properties["PersonlizationRules"] != null && ServiceLocator.ServiceProvider.GetService<IContext>().Site.IsSxaSite())
            {
                Evaluate(new CustomizeRenderingArgs(rendering));
            }
        }


        protected virtual void Evaluate(CustomizeRenderingArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            Item item = args.PageContext.Item;
            if (item != null)
            {
                RenderingReference renderingReference = GetRenderingReference(args.Rendering, args.Rendering.Item.Language, args.Rendering.Item.Database);
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


        protected virtual void ApplyChanges(Rendering rendering, RenderingReference reference)
        {
            Assert.ArgumentNotNull(rendering, "rendering");
            Assert.ArgumentNotNull(reference, "reference");
            //TransferRenderingItem(rendering, reference);
            //TransferDataSource(rendering, reference);
        }

        protected static RenderingReference GetRenderingReference(Rendering rendering, Language language, Database database)
        {
            Assert.IsNotNull(rendering, "rendering");
            Assert.IsNotNull(language, "language");
            Assert.IsNotNull(database, "database");
            string text = rendering.Properties["RenderingXml"];
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }
            XElement element = XElement.Parse(text);
            return new RenderingReference(element.ToXmlNode(), language, database);
        }

        private static XElement GetActionById(XElement rule, string id)
        {
            Assert.ArgumentNotNull(rule, "rule");
            Assert.ArgumentNotNull(id, "id");
            return rule.Element("actions")?.Elements("action").FirstOrDefault((XElement action) => action.GetAttributeValue("id") == id);
        }

        protected virtual void ApplyActions(CustomizeRenderingArgs args, ConditionalRenderingsRuleContext context)
        {
            Assert.ArgumentNotNull(args, "args");
            Assert.ArgumentNotNull(context, "context");
            RenderingReference renderingReference = context.References.Find((RenderingReference r) => r.UniqueId == context.Reference.UniqueId);
            if (renderingReference == null)
            {
                args.Renderer = new EmptyRenderer();
            }
            else
            {
                ApplyChanges(args.Rendering, renderingReference);
            }
        }

        

        protected virtual void RunRules(RuleList<ConditionalRenderingsRuleContext> rules, ConditionalRenderingsRuleContext context)
        {
            Assert.ArgumentNotNull(rules, "rules");
            Assert.ArgumentNotNull(context, "context");
            if (!RenderingRuleEvaluatedPipeline.IsEmpty())
            {
                rules.Evaluated += RulesEvaluatedHandler;
            }
            rules.RunFirstMatching(context);
        }

        private void RulesEvaluatedHandler(RuleList<ConditionalRenderingsRuleContext> ruleList, ConditionalRenderingsRuleContext ruleContext, Rule<ConditionalRenderingsRuleContext> rule)
        {
            RenderingRuleEvaluatedArgs args = new RenderingRuleEvaluatedArgs(ruleList, ruleContext, rule);
            RenderingRuleEvaluatedPipeline.Run(args);
        }

    }
}
