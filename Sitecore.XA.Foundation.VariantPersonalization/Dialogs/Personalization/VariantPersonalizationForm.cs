using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Extensions.XElementExtensions;
using Sitecore.Globalization;
using Sitecore.Layouts;
using Sitecore.Pipelines;
using Sitecore.Pipelines.GetPlaceholderRenderings;
using Sitecore.Pipelines.GetRenderingDatasource;
using Sitecore.Pipelines.RenderRulePlaceholder;
using Sitecore.Resources;
using Sitecore.Rules;
using Sitecore.Shell.Applications.Dialogs.Personalize;
using Sitecore.Shell.Applications.Dialogs.RulesEditor;
using Sitecore.Shell.Applications.Rules;
using Sitecore.Shell.Controls;
using Sitecore.StringExtensions;
using Sitecore.Web;
using Sitecore.Web.UI;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using Sitecore.XA.Foundation.Variants.Abstractions.Pipelines.GetVariants;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Xml.Linq;

namespace Sitecore.XA.Foundation.VariantPersonalization.Dialogs.Personalization
{
    public class VariantPersonalizationForm : DialogForm
    {
        //private readonly ID SetRenderingVariantActionId = new ID("{FA2E230E-FED2-4812-B4F6-AA6B7F065EFD}");
        /// <summary>
        /// The name of the placeholder positioned after the action.
        /// </summary>
        public const string AfterActionPlaceholderName = "afterAction";

        /// <summary>
        /// The default condition description
        /// </summary>
        protected readonly string ConditionDescriptionDefault;

        /// <summary>
        /// The default condition id string
        /// </summary>
        protected static readonly string DefaultConditionId = ItemIDs.Analytics.DefaultCondition.ToString();

        /// <summary>
        /// The default condition name
        /// </summary>
        protected readonly string ConditionNameDefault;

        /// <summary>
        /// The component personalization
        /// </summary>
        protected Web.UI.HtmlControls.Checkbox ComponentPersonalization;

        /// <summary>The rules container.</summary>
        protected Scrollbox RulesContainer;

        /// <summary>The hide rendering action id.</summary>
        private string HideRenderingActionId = RuleIds.HideRenderingActionId.ToString();

        /// <summary>The set datasource action id.</summary>
        private string SetDatasourceActionId = RuleIds.SetDatasourceActionId.ToString();


        /// <summary>The set rendering action id.</summary>
        private string SetRenderingActionId = RuleIds.SetRenderingActionId.ToString();

        /// <summary>The new condition name.</summary>
        private readonly string newConditionName = Translate.Text("Specify name...");

        /// <summary>
        /// The default condition
        /// </summary>
        private readonly XElement defaultCondition;

        /// <summary>
        /// Gets the context item.
        /// </summary>
        /// <value>The context item.</value>
        public Item ContextItem
        {
            get
            {
                ItemUri itemUri = ItemUri.Parse(ContextItemUri);
                if ( itemUri != null )
                {
                    return Database.GetItem( itemUri );
                }
                return null;
            }
        }

        /// <summary>
        /// Gets or sets the context item URI.
        /// </summary>
        /// <value>The context item URI.</value>
        public string ContextItemUri
        {
            get
            {
                return ServerProperties["ContextItemUri"] as string;
            }
            set
            {
                ServerProperties["ContextItemUri"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the device id.
        /// </summary>
        /// <value>The device id.</value>
        public string DeviceId
        {
            get
            {
                string result = ServerProperties["deviceId"] as string;
                return Assert.ResultNotNull( result );
            }
            set
            {
                Assert.IsNotNullOrEmpty( value, "value" );
                ServerProperties["deviceId"] = value;
            }
        }

        /// <summary>
        /// Gets the layout.
        /// </summary>
        /// <value>The layout.</value>
        public string Layout
        {
            get
            {
                string sessionHandle = SessionHandle;
                return Assert.ResultNotNull( WebUtil.GetSessionString( sessionHandle ) );
            }
        }

        /// <summary>
        /// Gets the layout defition.
        /// </summary>
        /// <value>The layout defition.</value>
        public LayoutDefinition LayoutDefition => LayoutDefinition.Parse( Layout );

        /// <summary>
        /// Gets or sets the rendering reference unique id.
        /// </summary>
        /// <value>The  id.</value>
        public string ReferenceId
        {
            get
            {
                string result = ServerProperties["referenceId"] as string;
                return Assert.ResultNotNull( result );
            }
            set
            {
                Assert.IsNotNullOrEmpty( value, "value" );
                ServerProperties["referenceId"] = value;
            }
        }

        /// <summary>
        /// Gets the rendering defition.
        /// </summary>
        /// <value>The rendering defition.</value>
        public RenderingDefinition RenderingDefition => Assert.ResultNotNull( LayoutDefition.GetDevice( DeviceId ).GetRenderingByUniqueId( ReferenceId ) );

        /// <summary>
        /// Gets or sets RulesSet.
        /// </summary>
        /// <value>The rules set.</value>
        public XElement RulesSet
        {
            get
            {
                string text = ServerProperties["ruleSet"] as string;
                if ( !string.IsNullOrEmpty( text ) )
                {
                    return XElement.Parse( text );
                }
                return new XElement( "ruleset", defaultCondition );
            }
            set
            {
                Assert.ArgumentNotNull( value, "value" );
                ServerProperties["ruleSet"] = value.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the session handle.
        /// </summary>
        /// <value>The session handle.</value>
        public string SessionHandle
        {
            get
            {
                string result = ServerProperties["SessionHandle"] as string;
                return Assert.ResultNotNull( result );
            }
            set
            {
                Assert.IsNotNullOrEmpty( value, "session handle" );
                ServerProperties["SessionHandle"] = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Sitecore.Shell.Applications.WebEdit.Dialogs.Personalization.PersonalizationForm" /> class.
        /// </summary>
        public VariantPersonalizationForm()
        {
            ConditionDescriptionDefault = Translate.Text( "If none of the other conditions are true, the default condition is used." );
            ConditionNameDefault = Translate.Text( "Default" );
            newConditionName = Translate.Text( "Specify name..." );
            defaultCondition = XElement.Parse( $"<rule uid=\"{DefaultConditionId}\" name=\"{ConditionNameDefault}\"><conditions><condition id=\"{RuleIds.TrueConditionId}\" uid=\"{ID.NewID.ToShortID()}\" /></conditions><actions /></rule>" );
        }

        /// <summary>
        /// Handles the toggle component click.
        /// </summary>
        protected void ComponentPersonalizationClick()
        {
            if ( !ComponentPersonalization.Checked && PersonalizeComponentActionExists() )
            {
                NameValueCollection parameters = new NameValueCollection();
                Context.ClientPage.Start( this, "ShowConfirm", parameters );
            }
            else
            {
                SheerResponse.Eval( "scTogglePersonalizeComponentSection()" );
            }
        }

        /// <summary>
        /// Deletes the ruel click.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        protected void DeleteRuleClick( string id )
        {
            Assert.ArgumentNotNull( id, "id" );
            string uId = ID.Decode(id).ToString();
            XElement rulesSet = RulesSet;
            XElement xElement = (from node in rulesSet.Elements("rule")
                                 where node.GetAttributeValue("uid") == uId
                                 select node).FirstOrDefault();
            if ( xElement != null )
            {
                xElement.Remove();
                RulesSet = rulesSet;
                SheerResponse.Remove( id + "data" );
                SheerResponse.Remove( id );
            }
        }

        /// <summary>
        /// Edits the condition.
        /// </summary>
        /// <param name="args">
        /// The arguments.
        /// </param>
        protected void EditCondition( ClientPipelineArgs args )
        {
            Assert.ArgumentNotNull( args, "args" );
            if ( string.IsNullOrEmpty( args.Parameters["id"] ) )
            {
                SheerResponse.Alert( "Please select a rule" );
                return;
            }
            string conditionId = ID.Decode(args.Parameters["id"]).ToString();
            if ( !args.IsPostBack )
            {
                RulesEditorOptions rulesEditorOptions = new RulesEditorOptions
                {
                    IncludeCommon = true,
                    RulesPath = "/sitecore/system/settings/Rules/Conditional Renderings",
                    AllowMultiple = false
                };
                XElement xElement = (from node in RulesSet.Elements("rule")
                                     where node.GetAttributeValue("uid") == conditionId
                                     select node).FirstOrDefault();
                if ( xElement != null )
                {
                    rulesEditorOptions.Value = "<ruleset>" + xElement + "</ruleset>";
                }
                rulesEditorOptions.HideActions = true;
                SheerResponse.ShowModalDialog( rulesEditorOptions.ToUrlString().ToString(), "580px", "712px", string.Empty, response: true );
                args.WaitForPostBack();
            }
            else
            {
                if ( !args.HasResult )
                {
                    return;
                }
                string result = args.Result;
                XElement xElement2 = XElement.Parse(result).Element("rule");
                XElement rulesSet = RulesSet;
                if ( xElement2 != null )
                {
                    XElement xElement3 = (from node in rulesSet.Elements("rule")
                                          where node.GetAttributeValue("uid") == conditionId
                                          select node).FirstOrDefault();
                    if ( xElement3 != null )
                    {
                        xElement3.ReplaceWith( xElement2 );
                        RulesSet = rulesSet;
                        SheerResponse.SetInnerHtml( args.Parameters["id"] + "_rule", GetRuleConditionsHtml( xElement2 ) );
                    }
                }
            }
        }

        /// <summary>
        /// Edits the rule.
        /// </summary>
        /// <param name="id">
        /// The rule.
        /// </param>
        protected void EditConditionClick( string id )
        {
            Assert.ArgumentNotNull( id, "id" );
            NameValueCollection nameValueCollection = new NameValueCollection();
            nameValueCollection["id"] = id;
            Context.ClientPage.Start( this, "EditCondition", nameValueCollection );
        }

        /// <summary>
        /// Moves the condition after the specified one.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <param name="targetId">
        /// The target id.
        /// </param>
        protected void MoveConditionAfter( string id, string targetId )
        {
            Assert.ArgumentNotNull( id, "id" );
            Assert.ArgumentNotNull( targetId, "targetId" );
            XElement rulesSet = RulesSet;
            XElement ruleById = GetRuleById(rulesSet, id);
            XElement ruleById2 = GetRuleById(rulesSet, targetId);
            if ( ruleById != null && ruleById2 != null )
            {
                ruleById.Remove();
                ruleById2.AddAfterSelf( ruleById );
                RulesSet = rulesSet;
            }
        }

        /// <summary>
        /// Moves the condition before the specified one.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <param name="targetId">
        /// The target id.
        /// </param>
        protected void MoveConditionBefore( string id, string targetId )
        {
            Assert.ArgumentNotNull( id, "id" );
            Assert.ArgumentNotNull( targetId, "targetId" );
            XElement rulesSet = RulesSet;
            XElement ruleById = GetRuleById(rulesSet, id);
            XElement ruleById2 = GetRuleById(rulesSet, targetId);
            if ( ruleById != null && ruleById2 != null )
            {
                ruleById.Remove();
                ruleById2.AddBeforeSelf( ruleById );
                RulesSet = rulesSet;
            }
        }

        /// <summary>
        /// News the condition click.
        /// </summary>
        protected void NewConditionClick()
        {
            XElement xElement = new XElement("rule");
            xElement.SetAttributeValue( "name", newConditionName );
            ID newID = ID.NewID;
            xElement.SetAttributeValue( "uid", newID );
            XElement rulesSet = RulesSet;
            rulesSet.AddFirst( xElement );
            RulesSet = rulesSet;
            string ruleSectionHtml = GetRuleSectionHtml(xElement);
            SheerResponse.Insert( "non-default-container", "afterBegin", ruleSectionHtml );
            SheerResponse.Eval( "Sitecore.CollapsiblePanel.addNew(\"" + newID.ToShortID() + "\")" );
        }

        /// <summary>
        /// The on load.
        /// </summary>
        /// <param name="e">
        /// The e.
        /// </param>
        /// <remarks>
        /// This method notifies the server control that it should perform actions common to each HTTP
        /// request for the page it is associated with, such as setting up a database query. At this
        /// stage in the page lifecycle, server controls in the hierarchy are created and initialized,
        /// view state is restored, and form controls reflect client-side data. Use the IsPostBack
        /// property to determine whether the page is being loaded in response to a client postback,
        /// or if it is being loaded and accessed for the first time.
        /// </remarks>
        protected override void OnLoad( EventArgs e )
        {
            Assert.ArgumentNotNull( e, "e" );
            base.OnLoad( e );
            if ( Context.ClientPage.IsEvent )
            {
                SheerResponse.Eval( "Sitecore.CollapsiblePanel.collapseMenus()" );
                return;
            }
            PersonalizeOptions personalizeOptions = PersonalizeOptions.Parse();
            DeviceId = personalizeOptions.DeviceId;
            ReferenceId = personalizeOptions.RenderingUniqueId;
            SessionHandle = personalizeOptions.SessionHandle;
            ContextItemUri = personalizeOptions.ContextItemUri;
            RenderingDefinition renderingDefition = RenderingDefition;
            XElement rules = renderingDefition.Rules;
            if ( rules != null )
            {
                RulesSet = rules;
            }
            if ( PersonalizeComponentActionExists() )
            {
                ComponentPersonalization.Checked = true;
            }
            RenderRules();
        }

        /// <summary>
        /// Handles a click on the OK button.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="args">
        /// The arguments.
        /// </param>
        /// <remarks>
        /// When the user clicks OK, the dialog is closed by calling
        /// the <see cref="M:Sitecore.Web.UI.Sheer.ClientResponse.CloseWindow">CloseWindow</see> method.
        /// </remarks>
        protected override void OnOK( object sender, EventArgs args )
        {
            Assert.ArgumentNotNull( sender, "sender" );
            Assert.ArgumentNotNull( args, "args" );
            XElement rulesSet = RulesSet;
            SheerResponse.SetDialogValue( rulesSet.ToString() );
            base.OnOK( sender, args );
        }

        /// <summary>Rename the rule.</summary>   
        /// <param name="message">The new message.</param>
        [HandleMessage( "rule:rename" )]
        protected void RenameRuleClick( Message message )
        {
            Assert.ArgumentNotNull( message, "message" );
            string text = message.Arguments["ruleId"];
            string value = message.Arguments["name"];
            Assert.IsNotNull( text, "id" );
            if ( !string.IsNullOrEmpty( value ) )
            {
                XElement rulesSet = RulesSet;
                XElement ruleById = GetRuleById(rulesSet, text);
                if ( ruleById != null )
                {
                    ruleById.SetAttributeValue( "name", value );
                    RulesSet = rulesSet;
                }
            }
        }

        /// <summary>
        /// Resets the datasource.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        protected void ResetDatasource( string id )
        {
            Assert.ArgumentNotNull( id, "id" );
            if ( IsComponentDisplayed( id ) )
            {
                XElement rulesSet = RulesSet;
                XElement ruleById = GetRuleById(rulesSet, id);
                if ( ruleById != null )
                {
                    RemoveAction( ruleById, SetDatasourceActionId );
                    RulesSet = rulesSet;
                    HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
                    RenderSetDatasourceAction( ruleById, htmlTextWriter );
                    SheerResponse.SetInnerHtml( id + "_setdatasource", htmlTextWriter.InnerWriter.ToString().Replace( "{ID}", id ) );
                }
            }
        }

        /// <summary>
        /// Resets the rendering.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        protected void ResetRendering( string id )
        {
            Assert.ArgumentNotNull( id, "id" );
            if ( IsComponentDisplayed( id ) )
            {
                XElement rulesSet = RulesSet;
                XElement ruleById = GetRuleById(rulesSet, id);
                if ( ruleById != null )
                {
                    RemoveAction( ruleById, SetRenderingActionId );
                    RulesSet = rulesSet;
                    HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
                    RenderSetRenderingAction( ruleById, htmlTextWriter );
                    SheerResponse.SetInnerHtml( id + "_setrendering", htmlTextWriter.InnerWriter.ToString().Replace( "{ID}", id ) );
                }
            }
        }

        /// <summary>
        /// Sets the rendering click.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        protected void SetDatasource( ClientPipelineArgs args )
        {
            Assert.ArgumentNotNull( args, "args" );
            string text = args.Parameters["id"];
            XElement rulesSet = RulesSet;
            XElement ruleById = GetRuleById(rulesSet, text);
            Assert.IsNotNull( ruleById, "rule" );
            if ( !args.IsPostBack )
            {
                XElement actionById = GetActionById(ruleById, SetRenderingActionId);
                Item item = null;
                if ( actionById != null && !string.IsNullOrEmpty( actionById.GetAttributeValue( "RenderingItem" ) ) )
                {
                    item = Client.ContentDatabase.GetItem( actionById.GetAttributeValue( "RenderingItem" ) );
                }
                else if ( !string.IsNullOrEmpty( RenderingDefition.ItemID ) )
                {
                    item = Client.ContentDatabase.GetItem( RenderingDefition.ItemID );
                }
                if ( item == null )
                {
                    SheerResponse.Alert( "Item not found." );
                    return;
                }
                Item contextItem = ContextItem;
                GetRenderingDatasourceArgs getRenderingDatasourceArgs = new GetRenderingDatasourceArgs(item)
                {
                    FallbackDatasourceRoots = new List<Item>
                {
                    Client.ContentDatabase.GetRootItem()
                },
                    ContentLanguage = contextItem?.Language,
                    ContextItemPath = ((contextItem != null) ? contextItem.Paths.FullPath : string.Empty),
                    ShowDialogIfDatasourceSetOnRenderingItem = true
                };
                XElement actionById2 = GetActionById(ruleById, SetDatasourceActionId);
                if ( actionById2 != null && !string.IsNullOrEmpty( actionById2.GetAttributeValue( "DataSource" ) ) )
                {
                    getRenderingDatasourceArgs.CurrentDatasource = actionById2.GetAttributeValue( "DataSource" );
                }
                else
                {
                    getRenderingDatasourceArgs.CurrentDatasource = RenderingDefition.Datasource;
                }
                if ( string.IsNullOrEmpty( getRenderingDatasourceArgs.CurrentDatasource ) )
                {
                    getRenderingDatasourceArgs.CurrentDatasource = contextItem.ID.ToString();
                }
                CorePipeline.Run( "getRenderingDatasource", getRenderingDatasourceArgs );
                if ( string.IsNullOrEmpty( getRenderingDatasourceArgs.DialogUrl ) )
                {
                    SheerResponse.Alert( "An error occurred." );
                    return;
                }
                SheerResponse.ShowModalDialog( getRenderingDatasourceArgs.DialogUrl, "960px", "660px", string.Empty, response: true );
                args.WaitForPostBack();
            }
            else if ( args.HasResult )
            {
                XElement xElement = GetActionById(ruleById, SetDatasourceActionId) ?? AddAction(ruleById, SetDatasourceActionId);
                xElement.SetAttributeValue( "DataSource", args.Result );
                RulesSet = rulesSet;
                HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
                RenderSetDatasourceAction( ruleById, htmlTextWriter );
                SheerResponse.SetInnerHtml( text + "_setdatasource", htmlTextWriter.InnerWriter.ToString().Replace( "{ID}", text ) );
            }
        }

        /// <summary>
        /// Sets the datasource click.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        protected void SetDatasourceClick( string id )
        {
            Assert.ArgumentNotNull( id, "id" );
            if ( IsComponentDisplayed( id ) )
            {
                NameValueCollection nameValueCollection = new NameValueCollection();
                nameValueCollection["id"] = id;
                Context.ClientPage.Start( this, "SetDatasource", nameValueCollection );
            }
        }

        /// <summary>
        /// Edits the condition.
        /// </summary>
        /// <param name="args">
        /// The arguments.
        /// </param>
        protected void SetRendering( ClientPipelineArgs args )
        {
            Assert.ArgumentNotNull( args, "args" );
            if ( !args.IsPostBack )
            {
                string placeholder = RenderingDefition.Placeholder;
                Assert.IsNotNull( placeholder, "placeholder" );
                string layout = Layout;
                GetPlaceholderRenderingsArgs getPlaceholderRenderingsArgs = new GetPlaceholderRenderingsArgs(placeholder, layout, Client.ContentDatabase, ID.Parse(DeviceId));
                getPlaceholderRenderingsArgs.OmitNonEditableRenderings = true;
                getPlaceholderRenderingsArgs.Options.ShowOpenProperties = false;
                CorePipeline.Run( "getPlaceholderRenderings", getPlaceholderRenderingsArgs );
                string dialogURL = getPlaceholderRenderingsArgs.DialogURL;
                if ( string.IsNullOrEmpty( dialogURL ) )
                {
                    SheerResponse.Alert( "An error occurred." );
                    return;
                }
                SheerResponse.ShowModalDialog( dialogURL, "720px", "470px", string.Empty, response: true );
                args.WaitForPostBack();
            }
            else if ( args.HasResult )
            {
                string id;
                if ( args.Result.IndexOf( ',' ) >= 0 )
                {
                    string[] array = args.Result.Split(',');
                    id = array[0];
                }
                else
                {
                    id = args.Result;
                }
                XElement rulesSet = RulesSet;
                string text = args.Parameters["id"];
                XElement ruleById = GetRuleById(rulesSet, text);
                Assert.IsNotNull( ruleById, "rule" );
                XElement xElement = GetActionById(ruleById, SetRenderingActionId) ?? AddAction(ruleById, SetRenderingActionId);
                xElement.SetAttributeValue( "RenderingItem", ShortID.DecodeID( id ) );
                RulesSet = rulesSet;
                HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
                RenderSetRenderingAction( ruleById, htmlTextWriter );
                SheerResponse.SetInnerHtml( text + "_setrendering", htmlTextWriter.InnerWriter.ToString().Replace( "{ID}", text ) );
            }
        }

        protected void RenderingVariantChange( string id, string varientId )
        {
            Assert.ArgumentNotNull( id, "id" );
            Assert.ArgumentNotNull( varientId, "varientId" );
            XElement rulesSet = RulesSet;
            XElement ruleById = GetRuleById(rulesSet, id);

            RemoveAction( ruleById, Constants.SetRenderingVariantActionId.ToString() );
            if ( !string.IsNullOrWhiteSpace( varientId ) )
            {
                XElement xElement = AddAction(ruleById, Constants.SetRenderingVariantActionId.ToString());
                xElement.SetAttributeValue( "VariantID", varientId );
            }
            RulesSet = rulesSet;
        }

        /// <summary>
        /// Sets the rendering click.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        protected void SetRenderingClick( string id )
        {
            Assert.ArgumentNotNull( id, "id" );
            if ( IsComponentDisplayed( id ) )
            {
                NameValueCollection nameValueCollection = new NameValueCollection();
                nameValueCollection["id"] = id;
                Context.ClientPage.Start( this, "SetRendering", nameValueCollection );
            }
        }

        /// <summary>
        /// Shows the confirm.
        /// </summary>
        /// <param name="args">
        /// The arguments.
        /// </param>
        protected void ShowConfirm( ClientPipelineArgs args )
        {
            Assert.ArgumentNotNull( args, "args" );
            if ( args.IsPostBack )
            {
                if ( args.HasResult && args.Result != "no" )
                {
                    SheerResponse.Eval( "scTogglePersonalizeComponentSection()" );
                    XElement rulesSet = RulesSet;
                    foreach ( XElement item in rulesSet.Elements( "rule" ) )
                    {
                        XElement actionById = GetActionById(item, SetRenderingActionId);
                        if ( actionById != null )
                        {
                            actionById.Remove();
                            HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
                            RenderSetRenderingAction( item, htmlTextWriter );
                            ShortID shortID = ShortID.Parse(item.GetAttributeValue("uid"));
                            Assert.IsNotNull( shortID, "ruleId" );
                            SheerResponse.SetInnerHtml( shortID + "_setrendering", htmlTextWriter.InnerWriter.ToString().Replace( "{ID}", shortID.ToString() ) );
                        }
                    }
                    RulesSet = rulesSet;
                }
                else
                {
                    ComponentPersonalization.Checked = true;
                }
            }
            else
            {
                SheerResponse.Confirm( "Personalize component settings will be removed. Are you sure you want to continue?" );
                args.WaitForPostBack();
            }
        }

        /// <summary>
        /// Switches the rendering click.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        protected void SwitchRenderingClick( string id )
        {
            Assert.ArgumentNotNull( id, "id" );
            XElement rulesSet = RulesSet;
            XElement ruleById = GetRuleById(rulesSet, id);
            if ( ruleById != null )
            {
                if ( !IsComponentDisplayed( ruleById ) )
                {
                    RemoveAction( ruleById, HideRenderingActionId );
                }
                else
                {
                    AddAction( ruleById, HideRenderingActionId );
                }
                RulesSet = rulesSet;
            }
        }

        /// <summary>
        /// Adds the action.
        /// </summary>
        /// <param name="rule">
        /// The rule.
        /// </param>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <returns>
        /// The action.
        /// </returns>
        private static XElement AddAction( XElement rule, string id )
        {
            Assert.ArgumentNotNull( rule, "rule" );
            Assert.ArgumentNotNull( id, "id" );
            XElement xElement = new XElement("action", new XAttribute("id", id), new XAttribute("uid", ID.NewID.ToShortID()));
            XElement xElement2 = rule.Element("actions");
            if ( xElement2 == null )
            {
                rule.Add( new XElement( "actions", xElement ) );
            }
            else
            {
                xElement2.Add( xElement );
            }
            return xElement;
        }

        /// <summary>
        /// Gets the action by id.
        /// </summary>
        /// <param name="rule">
        /// The rule.
        /// </param>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <returns>
        /// The action by id.
        /// </returns>
        private static XElement GetActionById( XElement rule, string id )
        {
            Assert.ArgumentNotNull( rule, "rule" );
            Assert.ArgumentNotNull( id, "id" );
            return rule.Element( "actions" )?.Elements( "action" ).FirstOrDefault( ( XElement action ) => action.GetAttributeValue( "id" ) == id );
        }

        /// <summary>
        /// Gets the rule by id.
        /// </summary>
        /// <param name="ruleSet">
        /// The rule set.
        /// </param>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <returns>
        /// The rule by id.
        /// </returns>
        private static XElement GetRuleById( XElement ruleSet, string id )
        {
            Assert.ArgumentNotNull( ruleSet, "ruleSet" );
            Assert.ArgumentNotNull( id, "id" );
            string uid = ID.Parse(id).ToString();
            return ruleSet.Elements( "rule" ).FirstOrDefault( ( XElement rule ) => rule.GetAttributeValue( "uid" ) == uid );
        }

        /// <summary>
        /// The get rule condition html.
        /// </summary>
        /// <param name="rule">
        /// The rule.
        /// </param>
        /// <returns>
        /// The get rules html.
        /// </returns>
        private static string GetRuleConditionsHtml( XElement rule )
        {
            Assert.ArgumentNotNull( rule, "rule" );
            HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
            RulesRenderer rulesRenderer = new RulesRenderer("<ruleset>" + rule + "</ruleset>")
            {
                SkipActions = true
            };
            rulesRenderer.Render( htmlTextWriter );
            return htmlTextWriter.InnerWriter.ToString();
        }

        /// <summary>
        /// Determines whether [is default condition] [the specified node].
        /// </summary>
        /// <param name="node">
        /// The node.
        /// </param>
        /// <returns>
        /// <c>true</c> if [is default condition] [the specified node]; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsDefaultCondition( XElement node )
        {
            Assert.ArgumentNotNull( node, "node" );
            return node.GetAttributeValue( "uid" ) == DefaultConditionId;
        }

        /// <summary>
        /// Removes the action.
        /// </summary>
        /// <param name="rule">
        /// The rule.
        /// </param>
        /// <param name="id">
        /// The id.
        /// </param>
        private static void RemoveAction( XElement rule, string id )
        {
            Assert.ArgumentNotNull( rule, "rule" );
            Assert.ArgumentNotNull( id, "id" );
            GetActionById( rule, id )?.Remove();
        }

        /// <summary>
        /// The add actions menu.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns></returns>
        private Menu GetActionsMenu( string id )
        {
            Assert.IsNotNullOrEmpty( id, "id" );
            Menu menu = new Menu();
            menu.ID = id + "_menu";
            string themedImageSource = Images.GetThemedImageSource("office/16x16/delete.png");
            string click = "javascript:Sitecore.CollapsiblePanel.remove(this, event, \"{0}\")".FormatWith(id);
            menu.Add( "Delete", themedImageSource, click );
            themedImageSource = string.Empty;
            click = "javascript:Sitecore.CollapsiblePanel.renameAction(\"{0}\")".FormatWith( id );
            menu.Add( "Rename", themedImageSource, click );
            MenuDivider menuDivider = menu.AddDivider();
            menuDivider.ID = "moveDivider";
            themedImageSource = Images.GetThemedImageSource( "ApplicationsV2/16x16/navigate_up.png" );
            click = "javascript:Sitecore.CollapsiblePanel.moveUp(this, event, \"{0}\")".FormatWith( id );
            MenuItem menuItem = menu.Add("Move up", themedImageSource, click);
            menuItem.ID = "moveUp";
            themedImageSource = Images.GetThemedImageSource( "ApplicationsV2/16x16/navigate_down.png" );
            click = "javascript:Sitecore.CollapsiblePanel.moveDown(this, event, \"{0}\")".FormatWith( id );
            menuItem = menu.Add( "Move down", themedImageSource, click );
            menuItem.ID = "moveDown";
            return menu;
        }

        /// <summary>
        /// The get rule section html.
        /// </summary>
        /// <param name="rule">
        /// The rule.
        /// </param>
        /// <returns>
        /// The get rule section html.
        /// </returns>
        private string GetRuleSectionHtml( XElement rule )
        {
            Assert.ArgumentNotNull( rule, "rule" );
            HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
            string text = ShortID.Parse(rule.GetAttributeValue("uid")).ToString();
            htmlTextWriter.Write( "<table id='{ID}_body' cellspacing='0' cellpadding='0' class='rule-body'>" );
            htmlTextWriter.Write( "<tbody>" );
            htmlTextWriter.Write( "<tr>" );
            htmlTextWriter.Write( "<td class='left-column'>" );
            RenderRuleConditions( rule, htmlTextWriter );
            htmlTextWriter.Write( "</td>" );
            htmlTextWriter.Write( "<td class='right-column'>" );
            RenderRuleActions( rule, htmlTextWriter );
            htmlTextWriter.Write( "</td>" );
            string value = RenderRulePlaceholder("afterAction", rule);
            htmlTextWriter.Write( value );
            htmlTextWriter.Write( "</tr>" );
            htmlTextWriter.Write( "</tbody>" );
            htmlTextWriter.Write( "</table>" );
            string panelHtml = htmlTextWriter.InnerWriter.ToString().Replace("{ID}", text);
            bool flag = IsDefaultCondition(rule);
            CollapsiblePanelRenderer.ActionsContext actionsContext = default(CollapsiblePanelRenderer.ActionsContext);
            actionsContext.IsVisible = !flag;
            CollapsiblePanelRenderer.ActionsContext actionsContext2 = actionsContext;
            if ( !flag )
            {
                actionsContext2.OnActionClick = "javascript:return Sitecore.CollapsiblePanel.showActionsMenu(this,event)";
                actionsContext2.Menu = GetActionsMenu( text );
            }
            string name = "Default";
            if ( !flag || !string.IsNullOrEmpty( rule.GetAttributeValue( "name" ) ) )
            {
                name = rule.GetAttributeValue( "name" );
            }
            CollapsiblePanelRenderer.NameContext nameContext = new CollapsiblePanelRenderer.NameContext(name);
            nameContext.Editable = !flag;
            nameContext.OnNameChanged = "javascript:return Sitecore.CollapsiblePanel.renameComplete(this,event)";
            CollapsiblePanelRenderer.NameContext nameContext2 = nameContext;
            CollapsiblePanelRenderer collapsiblePanelRenderer = new CollapsiblePanelRenderer();
            collapsiblePanelRenderer.CssClass = "rule-container";
            return collapsiblePanelRenderer.Render( text, panelHtml, nameContext2, actionsContext2 );
        }

        /// <summary>
        /// Render the placeholder for the rule.
        /// </summary>
        /// <param name="placeholderName">The name of the placeholder to render.</param>
        /// <param name="rule">The rule to render.</param>
        /// <returns>The markup for the placeholder.</returns>
        private string RenderRulePlaceholder( string placeholderName, XElement rule )
        {
            if ( ContextItem == null )
            {
                return string.Empty;
            }
            ItemUri uri = ContextItem.Uri;
            ID deviceId = ID.Parse(DeviceId);
            ID ruleSetId = ID.Parse(RenderingDefition.UniqueId);
            return RenderRulePlaceholderPipeline.Run( placeholderName, uri, deviceId, ruleSetId, rule );
        }

        /// <summary>
        /// Determines whether [is component displayed] in the specified rule.
        /// </summary>
        /// <param name="id">
        /// The rule id.
        /// </param>
        /// <returns>
        /// <c>true</c> if [is component displayed] [the specified id]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsComponentDisplayed( string id )
        {
            Assert.ArgumentNotNull( id, "id" );
            XElement rulesSet = RulesSet;
            XElement ruleById = GetRuleById(rulesSet, id);
            if ( ruleById != null && !IsComponentDisplayed( ruleById ) )
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether [is component displayed] [the specified rule].
        /// </summary>
        /// <param name="rule">
        /// The rule.
        /// </param>
        /// <returns>
        /// <c>true</c> if [is component displayed] [the specified rule]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsComponentDisplayed( XElement rule )
        {
            Assert.ArgumentNotNull( rule, "rule" );
            XElement actionById = GetActionById(rule, HideRenderingActionId);
            if ( actionById != null )
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Personalizes the component action exists.
        /// </summary>
        /// <returns>
        /// The component action exists.
        /// </returns>
        private bool PersonalizeComponentActionExists()
        {
            XElement rulesSet = RulesSet;
            return rulesSet.Elements( "rule" ).Any( ( XElement rule ) => GetActionById( rule, SetRenderingActionId ) != null );
        }

        /// <summary>
        /// Renders the hide rendering action.
        /// </summary>
        /// <param name="writer">
        /// The writer.
        /// </param>
        /// <param name="translatedText">
        /// The translated text.
        /// </param>
        /// <param name="isSelected">
        /// <c>true</c> if selected; otherwise, <c>false</c>.
        /// </param>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <param name="style">
        /// The style.
        /// </param>
        private void RenderHideRenderingAction( HtmlTextWriter writer, string translatedText, bool isSelected, int index, string style )
        {
            Assert.ArgumentNotNull( writer, "writer" );
            string str = "hiderenderingaction_{ID}_" + index.ToString(CultureInfo.InvariantCulture);
            writer.Write( "<input id='" + str + "' type='radio' name='hiderenderingaction_{ID}' onfocus='this.blur();' onchange=\"javascript:if (this.checked) { scSwitchRendering(this, event, '{ID}'); }\" " );
            if ( isSelected )
            {
                writer.Write( " checked='checked' " );
            }
            if ( !string.IsNullOrEmpty( style ) )
            {
                writer.Write( string.Format( CultureInfo.InvariantCulture, " style='{0}' ", style ) );
            }
            writer.Write( "/>" );
            writer.Write( "<label for='" + str + "' class='section-header'>" );
            writer.Write( translatedText );
            writer.Write( "</label>" );
        }

        /// <summary>
        /// Renders the picker.
        /// </summary>
        /// <param name="writer">
        /// The writer.
        /// </param>
        /// <param name="item">
        /// The item.
        /// </param>
        /// <param name="clickCommand">
        /// The click command.
        /// </param>
        /// <param name="resetCommand">
        /// The reset command.
        /// </param>
        /// <param name="prependEllipsis">
        /// if set to <c>true</c> [prepend ellipsis].
        /// </param>
        /// <param name="notSet">
        /// if set to <c>true</c> indicate the item was inferred, not set
        /// </param>
        private void RenderPicker( HtmlTextWriter writer, Item item, string clickCommand, string resetCommand, bool prependEllipsis, bool notSet = false )
        {
            Assert.ArgumentNotNull( writer, "writer" );
            Assert.ArgumentNotNull( clickCommand, "clickCommand" );
            Assert.ArgumentNotNull( resetCommand, "resetCommand" );
            string themedImageSource = Images.GetThemedImageSource((item != null) ? item.Appearance.Icon : string.Empty, ImageDimension.id16x16);
            string message = clickCommand + "(\\\"{ID}\\\")";
            string message2 = resetCommand + "(\\\"{ID}\\\")";
            string text = Translate.Text("[Not set]");
            string text2 = "item-picker";
            if ( item != null )
            {
                if ( notSet )
                {
                    text += (prependEllipsis ? ".../" : string.Empty);
                    text = text + " " + item.GetUIDisplayName();
                }
                else
                {
                    text = (prependEllipsis ? ".../" : string.Empty);
                    text += item.GetUIDisplayName();
                }
            }
            if ( (item == null) | notSet )
            {
                text2 += " not-set";
            }
            writer.Write( "<div style=\"background-image:url('{0}');background-position: left center;\" class='{1}'>", HttpUtility.HtmlEncode( themedImageSource ), text2 );
            writer.Write( "<a href='#' class='pick-button' onclick=\"{0}\" title=\"{1}\">...</a>", Context.ClientPage.GetClientEvent( message ), Translate.Text( "Select" ) );
            writer.Write( "<a href='#' class='reset-button' onclick=\"{0}\" title=\"{1}\"></a>", Context.ClientPage.GetClientEvent( message2 ), Translate.Text( "Reset" ) );
            writer.Write( "<span title=\"{0}\">{1}</span>", (item == null) ? string.Empty : item.GetUIDisplayName(), text );
            writer.Write( "</div>" );
        }

        private void RenderPicker( HtmlTextWriter writer, string datasource, string clickCommand, string resetCommand, bool prependEllipsis, bool notSet = false )
        {
            Assert.ArgumentNotNull( writer, "writer" );
            Assert.ArgumentNotNull( clickCommand, "clickCommand" );
            Assert.ArgumentNotNull( resetCommand, "resetCommand" );
            string message = clickCommand + "(\\\"{ID}\\\")";
            string message2 = resetCommand + "(\\\"{ID}\\\")";
            string text = Translate.Text("[Not set]");
            string text2 = "item-picker";
            if ( !datasource.IsNullOrEmpty() )
            {
                text = ((!notSet) ? datasource : (text + " " + datasource));
            }
            if ( datasource.IsNullOrEmpty() | notSet )
            {
                text2 += " not-set";
            }
            writer.Write( $"<div class='{text2}'>" );
            writer.Write( "<a href='#' class='pick-button' onclick=\"{0}\" title=\"{1}\">...</a>", Context.ClientPage.GetClientEvent( message ), Translate.Text( "Select" ) );
            writer.Write( "<a href='#' class='reset-button' onclick=\"{0}\" title=\"{1}\"></a>", Context.ClientPage.GetClientEvent( message2 ), Translate.Text( "Reset" ) );
            string text3 = text;
            if ( text3 != null && text3.Length > 15 )
            {
                text3 = text3.Substring( 0, 14 ) + "...";
            }
            writer.Write( "<span title=\"{0}\">{1}</span>", text, text3 );
            writer.Write( "</div>" );
        }

        /// <summary>
        /// The render rule actions.
        /// </summary>
        /// <param name="rule">
        /// The rule.
        /// </param>
        /// <param name="writer">
        /// The writer.
        /// </param>
        private void RenderRuleActions( XElement rule, HtmlTextWriter writer )
        {
            Assert.ArgumentNotNull( rule, "rule" );
            Assert.ArgumentNotNull( writer, "writer" );
            bool flag = IsComponentDisplayed(rule);
            writer.Write( "<div id='{ID}_hiderendering' class='hide-rendering'>" );
            RenderHideRenderingAction( writer, Translate.Text( "Show" ), flag, 0, null );
            RenderHideRenderingAction( writer, Translate.Text( "Hide" ), !flag, 1, "margin-left:35px;" );
            writer.Write( "</div>" );
            string text = flag ? string.Empty : " display-off";
            string text2 = ComponentPersonalization.Checked ? string.Empty : " style='display:none'";
            writer.Write( "<div id='{ID}_setrendering' class='set-rendering" + text + "'" + text2 + ">" );
            RenderSetRenderingAction( rule, writer );
            writer.Write( "</div>" );
            writer.Write( "<div id='{ID}_setdatasource' class='set-datasource" + text + "'>" );
            RenderSetDatasourceAction( rule, writer );
            writer.Write( "</div>" );

            writer.Write( "<div id='{ID}_setvariant' class='set-variant" + text + "'>" );
            RenderSetVariantAction( rule, writer );
            writer.Write( "</div>" );

        }

        private void RenderSetVariantAction( XElement rule, HtmlTextWriter writer )
        {
            Assert.ArgumentNotNull( rule, "rule" );
            Assert.ArgumentNotNull( writer, "writer" );
            string curVariantId = string.Empty;
            var availableVariants = GetAvailableVariants();
            XElement actionById = GetActionById(rule, Constants.SetRenderingVariantActionId.ToString());
            if ( actionById != null )
            {
                curVariantId = actionById.GetAttributeValue( "VariantID" );
            }

            Item item = null;
            if ( !string.IsNullOrEmpty( curVariantId ) )
            {
                item = Client.ContentDatabase.GetItem( curVariantId );
            }

            writer.Write( "<span class='section-header' unselectable='on'>" );
            writer.Write( Translate.Text( "Variant - Determines displayed fields and appearance [Shared]:" ) );
            writer.Write( "</span>" );
            writer.Write( "<select onchange=\"javascript: scRenderingVariantChange(this,event,'{ID}',this.options[this.selectedIndex].value);\">" );
            writer.Write( " <option value=\"\"></option>" );
            if ( availableVariants != null )
            {
                foreach ( var varient in availableVariants )
                {
                    if ( varient.ID.ToString() != curVariantId )
                        writer.Write( $" <option value=\"{varient.ID.ToString()}\">{varient.DisplayName}</option>" );
                    else
                        writer.Write( $" <option value=\"{varient.ID.ToString()}\" selected = selected>{varient.DisplayName}</option>" );
                }
            }
            writer.Write( "</select>" );
        }

        /// <summary>
        /// Gets available rendering variants
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Item> GetAvailableVariants()
        {
            GetVariantsArgs getVariantsArgs = new GetVariantsArgs
            {
                ContextItem = ContextItem,
                RenderingName = Client.ContentDatabase.GetItem( RenderingDefition.ItemID).Name,
                RenderingId = new ID( RenderingDefition.ItemID),
                PageTemplateId = ContextItem.TemplateID.ToString()
            };
            CorePipeline.Run( "getVariants", getVariantsArgs );
            return getVariantsArgs.Variants;
        }

        /// <summary>
        /// The render rule conditions.
        /// </summary>
        /// <param name="rule">
        /// The rule.
        /// </param>
        /// <param name="writer">
        /// The writer.
        /// </param>
        private void RenderRuleConditions( XElement rule, HtmlTextWriter writer )
        {
            Assert.ArgumentNotNull( rule, "rule" );
            Assert.ArgumentNotNull( writer, "writer" );
            bool flag = IsDefaultCondition(rule);
            if ( !flag )
            {
                Button ctl = new Button
                {
                    Header = Translate.Text("Edit rule"),
                    ToolTip = Translate.Text("Edit this rule"),
                    Class = "scButton edit-button",
                    Click = "EditConditionClick(\\\"{ID}\\\")"
                };
                writer.Write( HtmlUtil.RenderControl( ctl ) );
            }
            string str = (!flag) ? "condition-container" : "condition-container default";
            writer.Write( "<div id='{ID}_rule' class='" + str + "'>" );
            writer.Write( flag ? ConditionDescriptionDefault : GetRuleConditionsHtml( rule ) );
            writer.Write( "</div>" );
        }

        /// <summary>
        /// The render rules.
        /// </summary>
        private void RenderRules()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append( "<div id='non-default-container'>" );
            foreach ( XElement item in RulesSet.Elements( "rule" ) )
            {
                if ( IsDefaultCondition( item ) )
                {
                    stringBuilder.Append( "</div>" );
                }
                stringBuilder.Append( GetRuleSectionHtml( item ) );
            }
            RulesContainer.InnerHtml = stringBuilder.ToString();
        }

        /// <summary>
        /// Renders the set datasource action.
        /// </summary>
        /// <param name="rule">
        /// The rule.
        /// </param>
        /// <param name="writer">
        /// The writer.
        /// </param>
        private void RenderSetDatasourceAction( XElement rule, HtmlTextWriter writer )
        {
            Assert.ArgumentNotNull( rule, "rule" );
            Assert.ArgumentNotNull( writer, "writer" );
            string datasource = RenderingDefition.Datasource;
            XElement actionById = GetActionById(rule, SetDatasourceActionId);
            bool flag = true;
            if ( actionById != null )
            {
                datasource = actionById.GetAttributeValue( "DataSource" );
                flag = false;
            }
            else
            {
                datasource = string.Empty;
            }
            Item item = null;
            bool flag2 = false;
            if ( !string.IsNullOrEmpty( datasource ) )
            {
                item = Client.ContentDatabase.GetItem( datasource );
            }
            else
            {
                item = ContextItem;
                flag2 = true;
            }
            writer.Write( "<div " + ((!flag) ? string.Empty : "class='default-values'") + ">" );
            writer.Write( "<span class='section-header' unselectable='on'>" );
            writer.Write( Translate.Text( "Content:" ) );
            writer.Write( "</span>" );
            if ( item == null )
            {
                RenderPicker( writer, datasource, "SetDatasourceClick", "ResetDatasource", !flag2, flag2 );
            }
            else
            {
                RenderPicker( writer, item, "SetDatasourceClick", "ResetDatasource", !flag2, flag2 );
            }
            writer.Write( "</div>" );
        }

        /// <summary>
        /// Renders the set rendering action.
        /// </summary>
        /// <param name="rule">
        /// The rule.
        /// </param>
        /// <param name="writer">
        /// The writer.
        /// </param>
        private void RenderSetRenderingAction( XElement rule, HtmlTextWriter writer )
        {
            Assert.ArgumentNotNull( rule, "rule" );
            Assert.ArgumentNotNull( writer, "writer" );
            string text = RenderingDefition.ItemID;
            XElement actionById = GetActionById(rule, SetRenderingActionId);
            bool flag = true;
            if ( actionById != null )
            {
                string attributeValue = actionById.GetAttributeValue("RenderingItem");
                if ( !string.IsNullOrEmpty( attributeValue ) )
                {
                    text = attributeValue;
                    flag = false;
                }
            }
            writer.Write( "<div " + ((!flag) ? string.Empty : "class='default-values'") + ">" );
            if ( string.IsNullOrEmpty( text ) )
            {
                writer.Write( "</div>" );
                return;
            }
            Item item = Client.ContentDatabase.GetItem(text);
            if ( item == null )
            {
                writer.Write( "</div>" );
                return;
            }
            writer.Write( "<span class='section-header' unselectable='on'>" );
            writer.Write( Translate.Text( "Presentation:" ) );
            writer.Write( "</span>" );
            string s = Images.GetThemedImageSource(item.Appearance.Icon, ImageDimension.id48x48);
            if ( !string.IsNullOrEmpty( item.Appearance.Thumbnail ) && item.Appearance.Thumbnail != Settings.DefaultThumbnail )
            {
                string thumbnailSrc = UIUtil.GetThumbnailSrc(item, 128, 128);
                if ( !string.IsNullOrEmpty( thumbnailSrc ) )
                {
                    s = thumbnailSrc;
                }
            }
            writer.Write( "<div style=\"background-image:url('{0}')\" class='thumbnail-container'>", HttpUtility.HtmlEncode( s ) );
            writer.Write( "</div>" );
            writer.Write( "<div class='picker-container'>" );
            RenderPicker( writer, item, "SetRenderingClick", "ResetRendering", prependEllipsis: false );
            writer.Write( "</div>" );
            writer.Write( "</div>" );
        }
    }
}
