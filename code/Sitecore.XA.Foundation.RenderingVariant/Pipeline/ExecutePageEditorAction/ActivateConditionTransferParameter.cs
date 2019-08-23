using Sitecore.ContentTesting.Pipelines.ExecutePageEditorAction;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Layouts;
using Sitecore.Sites;
using Sitecore.XA.Foundation.RenderingVariant.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sitecore.XA.Foundation.RenderingVariant.Pipeline.ExecutePageEditorAction
{
    public class ActivateConditionTransferParameter: ActivateCondition
    {
        protected override RenderingReference DoActivateCondition(RenderingDefinition rendering, ID conditionID, Language lang, Database database, Item item, SiteContext site)
        {
            var reference = base.DoActivateCondition(rendering, conditionID, lang, database, item, site);
            ExtensionMethods.TransferRenderingParameters(rendering, reference);
            return reference;
        }
    }
}
