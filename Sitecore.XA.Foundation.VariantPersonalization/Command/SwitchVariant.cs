using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Sitecore.Shell.Applications.WebEdit.Commands;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;
using Sitecore.XA.Foundation.SitecoreExtensions.Extensions;
using System;
using System.Collections.Specialized;

namespace Sitecore.XA.Foundation.VariantPersonalization.Command
{
    class SwitchVariant : WebEditCommand
    {
        public override void Execute(CommandContext context)
        {
            string formValue = WebUtil.GetFormValue("scLayout");
            string xml = WebEditUtil.ConvertJSONLayoutToXML(formValue);
            string id = ShortID.Decode(WebUtil.GetFormValue("scDeviceID"));
            LayoutDefinition layoutDefinition = LayoutDefinition.Parse(xml);
            if (layoutDefinition == null)
            {
                ReturnLayout();
                return;
            }
            DeviceDefinition device = layoutDefinition.GetDevice(id);
            if (device == null)
            {
                ReturnLayout();
                return;
            }
            string uniqueId = Guid.Parse(context.Parameters["renderingUid"]).ToString("B").ToUpperInvariant();
            RenderingDefinition renderingByUniqueId = device.GetRenderingByUniqueId(uniqueId);
            if (renderingByUniqueId == null)
            {
                ReturnLayout();
                return;
            }
            if (string.IsNullOrEmpty(renderingByUniqueId.Parameters))
            {
                if (!string.IsNullOrEmpty(renderingByUniqueId.ItemID))
                {
                    RenderingItem renderingItem = Client.ContentDatabase.GetItem(renderingByUniqueId.ItemID);
                    renderingByUniqueId.Parameters = ((renderingItem != null) ? renderingItem.Parameters : string.Empty);
                }
                else
                {
                    renderingByUniqueId.Parameters = string.Empty;
                }
            }
            NameValueCollection nameValueCollection = WebUtil.ParseUrlParameters(renderingByUniqueId.Parameters);
            string input = nameValueCollection["FieldNames"];
            string text = context.Parameters["variant"];
            if (Guid.TryParse(input, out Guid result) && result == Guid.Parse(text))
            {
                ReturnLayout();
                return;
            }
            nameValueCollection["FieldNames"] = text;
            renderingByUniqueId.Parameters = new UrlString(nameValueCollection.EscapeDataValues()).GetUrl();
            formValue = WebEditUtil.ConvertXMLLayoutToJSON(layoutDefinition.ToXml());
            ReturnLayout(formValue);
        }

        protected virtual void ReturnLayout(string layout = null)
        {
            SheerResponse.SetAttribute("scLayoutDefinition", "value", layout ?? string.Empty);
            if (!string.IsNullOrEmpty(layout))
            {
                SheerResponse.Eval("window.parent.Sitecore.PageModes.ChromeManager.handleMessage('chrome:rendering:propertiescompleted');");
            }
        }
    }
}
