using Sitecore.Diagnostics;
using Sitecore.Rules.Actions;
using Sitecore.Rules.ConditionalRenderings;
using Sitecore.Web;

namespace Sitecore.XA.Foundation.RenderingVariant.Rules.Action
{
    public class ApplyRenderingVariantParameterAction<T> : RuleAction<T> where T : ConditionalRenderingsRuleContext
    {
        private string _renderingVariantId;

        /// <summary>
        /// Gets or sets the data source.
        /// </summary>
        /// <value>The data source.</value>
        public string VariantID
        {
            get
            {
                return _renderingVariantId ?? string.Empty;
            }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                _renderingVariantId = value;
            }
        }
        public override void Apply(T ruleContext)
        {
            if (!string.IsNullOrWhiteSpace(VariantID))
            {
                Assert.ArgumentNotNull(ruleContext, "ruleContext");
                var parameters = WebUtil.ParseQueryString(ruleContext.Reference.Settings.Parameters, true);
                parameters["FieldNames"] = VariantID;
                ruleContext.Reference.Settings.Parameters = WebUtil.BuildQueryString(parameters, false, true);
            }
        }
    }
}
