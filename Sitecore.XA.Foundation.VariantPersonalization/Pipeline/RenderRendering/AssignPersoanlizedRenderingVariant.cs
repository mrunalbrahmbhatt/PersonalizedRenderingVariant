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
using Sitecore.Rules.Actions;
using System;
using Sitecore.Web;

namespace Sitecore.XA.Foundation.VariantPersonalization.Pipeline.RenderRendering
{
    public class AssignPersoanlizedRenderingVariant : RenderRenderingProcessor
    {
        public override void Process(RenderRenderingArgs args)
        {
            Rendering rendering = args.Rendering;
            if (rendering.Properties["PersonlizationRules"] != null && ServiceLocator.ServiceProvider.GetService<IContext>().Site.IsSxaSite())
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

            matchingRule.Actions?.Each(a => a.Apply(context));

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
