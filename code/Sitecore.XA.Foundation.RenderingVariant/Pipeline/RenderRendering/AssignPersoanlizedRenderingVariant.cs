using Microsoft.Extensions.DependencyInjection;
using Sitecore.Analytics.Pipelines.GetRenderingRules;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.DependencyInjection;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Layouts;
using Sitecore.Mvc.Analytics.Pipelines.Response.CustomizeRendering;
using Sitecore.Mvc.Extensions;
using Sitecore.Mvc.Pipelines.Response.RenderRendering;
using Sitecore.Mvc.Presentation;
using Sitecore.Rules;
using Sitecore.Rules.ConditionalRenderings;
using Sitecore.Web;
using Sitecore.XA.Foundation.Abstractions;
using Sitecore.XA.Foundation.Multisite.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Sitecore.XA.Foundation.RenderingVariant.Pipeline.RenderRendering
{
    public class AssignPersoanlizedRenderingVariant : RenderRenderingProcessor
    {
        public override void Process(RenderRenderingArgs args)
        {
            Rendering rendering = args.Rendering;
            var siteContext = ServiceLocator.ServiceProvider.GetService<IContext>().Site;
            if (rendering.Properties["PersonlizationRules"] != null && siteContext.IsSxaSite() && siteContext.DisplayMode != Sites.DisplayMode.Edit)
            {
                Evaluate(new CustomizeRenderingArgs(args.Rendering));
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
                    var matchingRule = GetMatchingRule(ruleList, conditionalRenderingsRuleContext);
                    ApplyActions(args, conditionalRenderingsRuleContext, matchingRule);
                }
            }
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

        /// <summary>
        /// Executes all the action against matched Rule.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="context"></param>
        /// <param name="matchingRule"></param>
        protected virtual void ApplyActions(CustomizeRenderingArgs args, ConditionalRenderingsRuleContext context, Rule<ConditionalRenderingsRuleContext> matchingRule)
        {
            Assert.ArgumentNotNull(args, "args");
            Assert.ArgumentNotNull(context, "context");
            Assert.ArgumentNotNull(matchingRule, "matchingRule");
            if (matchingRule?.Actions?.Count <= 0)
                return;

            matchingRule.Actions?.Where(a => a.GetType().Name.StartsWith("ApplyRenderingVariantParameterAction"))?.Each(a => a.Apply(context));

            var parameters = WebUtil.ParseQueryString(context.Reference.Settings.Parameters, true);
            if (!string.IsNullOrWhiteSpace(parameters["FieldNames"]))
            {
                args.Rendering.Parameters["FieldNames"] = parameters["FieldNames"];
            }
        }

        /// <summary>
        /// Gets the matching rule from the rulelist
        /// </summary>
        /// <param name="rules"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        protected virtual Rule<ConditionalRenderingsRuleContext> GetMatchingRule(RuleList<ConditionalRenderingsRuleContext> rules, ConditionalRenderingsRuleContext context)
        {
            Assert.ArgumentNotNull(rules, "rules");
            Assert.ArgumentNotNull(context, "context");

            var matchingRule = rules.Rules.FirstOrDefault(r => r.Evaluate(context)) ?? rules.Rules.FirstOrDefault(r => r.UniqueId == ID.Null);
            return matchingRule;
        }
    }
}
