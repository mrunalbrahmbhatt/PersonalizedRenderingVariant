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
using System.Linq;
using Sitecore.Mvc.Extensions;
using Sitecore.Data;
using Sitecore.Mvc.Analytics.Presentation;
using Sitecore.Analytics.Pipelines.RenderingRuleEvaluated;
using Sitecore.Rules.Actions;
using System;
using Sitecore.XA.Foundation.VariantPersonalization.Rules.Action;

namespace Sitecore.XA.Foundation.VariantPersonalization.Pipeline.CustomizeRendering
{
    public class ResolvePersonalizedRenderingVariant : Personalize
    {
        private string _VariantId = string.Empty;

        public ResolvePersonalizedRenderingVariant() : base()
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

        protected override void ApplyActions(CustomizeRenderingArgs args, ConditionalRenderingsRuleContext context)
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
                TransferRenderingVariant(args.Rendering, renderingReference, _VariantId);
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

        protected override void RunRules(RuleList<ConditionalRenderingsRuleContext> rules, ConditionalRenderingsRuleContext context)
        {
            Assert.ArgumentNotNull(rules, "rules");
            Assert.ArgumentNotNull(context, "context");
            if (!RenderingRuleEvaluatedPipeline.IsEmpty())
            {
                rules.Evaluated += RulesEvaluatedHandler;
                rules.Applied += RulesAppliedHandler;
            }
            rules.RunFirstMatching(context);
        }

        private void RulesAppliedHandler(RuleList<ConditionalRenderingsRuleContext> ruleList, ConditionalRenderingsRuleContext ruleContext, RuleAction<ConditionalRenderingsRuleContext> action)
        {
            if (action.GetType().Name.StartsWith("SetRenderingVariantAction"))
            {
                _VariantId = (action as SetRenderingVariantAction<ConditionalRenderingsRuleContext>).VariantID;
            }
        }

        private void RulesEvaluatedHandler(RuleList<ConditionalRenderingsRuleContext> ruleList, ConditionalRenderingsRuleContext ruleContext, Rule<ConditionalRenderingsRuleContext> rule)
        {
            RenderingRuleEvaluatedArgs args = new RenderingRuleEvaluatedArgs(ruleList, ruleContext, rule);
            RenderingRuleEvaluatedPipeline.Run(args);
        }

        //public override void Process(CustomizeRenderingArgs args)
        //{
        //    Assert.ArgumentNotNull(args, "args");
        //    if (Tracker.IsActive)
        //    {
        //        Evaluate(args);
        //    }
        //}

        //protected override void Evaluate(CustomizeRenderingArgs args)
        //{
        //    Assert.ArgumentNotNull(args, "args");
        //    Item item = args.PageContext.Item;
        //    if (item != null)
        //    {
        //        RenderingReference renderingReference = CustomizeRenderingProcessor.GetRenderingReference(args.Rendering, Context.Language, args.PageContext.Database);
        //        GetRenderingRulesArgs getRenderingRulesArgs = new GetRenderingRulesArgs(item, renderingReference);
        //        GetRenderingRulesPipeline.Run(getRenderingRulesArgs);
        //        RuleList<ConditionalRenderingsRuleContext> ruleList = getRenderingRulesArgs.RuleList;
        //        if (ruleList != null && ruleList.Count != 0)
        //        {
        //            List<RenderingReference> references = new List<RenderingReference>
        //        {
        //            renderingReference
        //        };
        //            ConditionalRenderingsRuleContext conditionalRenderingsRuleContext = new ConditionalRenderingsRuleContext(references, renderingReference)
        //            {
        //                Item = item
        //            };
        //            conditionalRenderingsRuleContext.Parameters["mvc.rendering"] = args.Rendering;
        //            RunRules(ruleList, conditionalRenderingsRuleContext);
        //            ApplyActions(args, conditionalRenderingsRuleContext);
        //            args.IsCustomized = true;
        //            //var matchingRule = GetMatchingRule(ruleList, conditionalRenderingsRuleContext);
        //            //ApplyActions(args, conditionalRenderingsRuleContext, matchingRule);
        //        }
        //    }
        //}

        //protected override void ApplyActions(CustomizeRenderingArgs args, ConditionalRenderingsRuleContext context)
        //{
        //    Assert.ArgumentNotNull(args, "args");
        //    Assert.ArgumentNotNull(context, "context");
        //    RenderingReference renderingReference = context.References.Find((RenderingReference r) => r.UniqueId == context.Reference.UniqueId);
        //    if (renderingReference == null)
        //    {
        //        args.Renderer = new EmptyRenderer();
        //    }
        //    else
        //    {
        //        ApplyChanges(args.Rendering, renderingReference);
        //    }
        //}

        ////protected virtual void ApplyActions(CustomizeRenderingArgs args, ConditionalRenderingsRuleContext context, Rule<ConditionalRenderingsRuleContext> matchingRule)
        ////{
        ////    Assert.ArgumentNotNull(args, "args");
        ////    Assert.ArgumentNotNull(context, "context");
        ////    Assert.ArgumentNotNull(matchingRule, "matchingRule");

        ////    matchingRule.Actions?.Each(a => a.Apply(context));

        ////    var parameters = WebUtil.ParseQueryString(context.Reference.Settings.Parameters, true);
        ////    if (!string.IsNullOrWhiteSpace(parameters["FieldNames"]))
        ////    {
        ////        args.Rendering.Parameters["FieldNames"] = parameters["FieldNames"];
        ////    }
        ////}
        //protected virtual Rule<ConditionalRenderingsRuleContext> GetMatchingRule(RuleList<ConditionalRenderingsRuleContext> rules, ConditionalRenderingsRuleContext context)
        //{
        //    Assert.ArgumentNotNull(rules, "rules");
        //    Assert.ArgumentNotNull(context, "context");

        //    var matchingRule = rules.Rules.FirstOrDefault(r => r.Evaluate(context)) ?? rules.Rules.FirstOrDefault(r => r.UniqueId == ID.Null);
        //    return matchingRule;
        //}

        //protected override void ApplyChanges(Rendering rendering, RenderingReference reference)
        //{
        //    Assert.ArgumentNotNull(rendering, "rendering");
        //    Assert.ArgumentNotNull(reference, "reference");
        //    TransferRenderingVariant(rendering, reference);
        //}
        private static void TransferRenderingVariant(Rendering rendering, RenderingReference reference, string variantId)
        {
            Assert.ArgumentNotNull(rendering, "rendering");
            Assert.ArgumentNotNull(reference, "reference");
            if (reference.RenderingItem != null && !string.IsNullOrWhiteSpace(variantId))
            {
                var parameters = WebUtil.ParseQueryString(reference.Settings.Parameters, true);
                //rendering.Parameters["FieldNames"] = "{4C961442-BEA7-4759-9AA2-A8DF74357451}";//parameters["FieldNames"];
                rendering.Parameters["FieldNames"] = variantId;
            }
        }
    }
}
