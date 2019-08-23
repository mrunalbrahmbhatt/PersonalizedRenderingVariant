using Microsoft.Extensions.DependencyInjection;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.DependencyInjection;
using Sitecore.Layouts;
using Sitecore.Pipelines;
using Sitecore.Pipelines.ExecutePageEditorAction;
using Sitecore.StringExtensions;
using Sitecore.Web;
using Sitecore.XA.Feature.Composites;
using Sitecore.XA.Feature.Composites.Services;
using Sitecore.XA.Foundation.Abstractions.Configuration;
using Sitecore.XA.Foundation.Presentation.Layout;
using Sitecore.XA.Foundation.Presentation.Services;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sitecore.XA.Foundation.RenderingVariant.Pipeline.ExecutePageEditorAction
{
    public class FillCompositeRenderingDatasourceCustom
    {
        public IRenderingParametersService RenderingParametersService
        {
            get;
            set;
        }

        public FillCompositeRenderingDatasourceCustom()
        {
            RenderingParametersService = ServiceLocator.ServiceProvider.GetService<IRenderingParametersService>();
        }

        public void Process(PipelineArgs args)
        {
            if (ServiceLocator.ServiceProvider.GetService<IConfiguration<CompositesConfiguration>>().GetConfiguration().OnPageEditingEnabled)
            {
                IInsertRenderingArgs insertRenderingsArgs = args as IInsertRenderingArgs;
                ServiceLocator.ServiceProvider.GetService<IOnPageEditingContextService>().ExperienceEditorAction = (args as IPageEditorActionArgs)?.Name;
                if (insertRenderingsArgs != null && insertRenderingsArgs.Datasource == null && WebUtil.GetFormValue("datasource").IsNullOrEmpty() && Regex.IsMatch(insertRenderingsArgs.PlaceholderKey, "section-(title|content)") && insertRenderingsArgs.Layout.Devices.Cast<DeviceDefinition>().FirstOrDefault((DeviceDefinition d) => d.ID == insertRenderingsArgs.Device.ID) != null)
                {
                    LayoutModel layoutModel = new LayoutModel(insertRenderingsArgs.Layout.ToXml());
                    Item compositeDatasourceItem = ServiceLocator.ServiceProvider.GetService<ICompositeService>().GetCompositeDatasourceItem(insertRenderingsArgs.PlaceholderKey, insertRenderingsArgs.ContextItem, layoutModel, new ID(insertRenderingsArgs.Device.ID));
                    insertRenderingsArgs.Datasource = compositeDatasourceItem;
                }
            }
        }
    }

}