﻿using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Security;
using System.Windows.Forms;
using SolarWinds.InformationService.Contract2;
using SwqlStudio.Metadata;
using SwqlStudio.Properties;
using WeifenLuo.WinFormsUI.Docking;

namespace SwqlStudio
{
    internal class TabsFactory : ITabsFactory
    {
        private static readonly SolarWinds.Logging.Log log = new SolarWinds.Logging.Log();

        private readonly ServerList serverList = new ServerList();

        private readonly QueriesDockPanel dockPanel;
        private readonly IApplicationService applicationService;

        internal TabsFactory(QueriesDockPanel dockPanel, IApplicationService applicationService)
        {
            this.dockPanel = dockPanel;
            this.applicationService = applicationService;
        }

        public void AddTextToEditor(string text, ConnectionInfo info)
        {
            if (info == null)
                info = this.dockPanel.ActiveConnectionInfo;

            IMetadataProvider metadataProvider;
            this.serverList.TryGetProvider(info, out metadataProvider);

            CreateQueryTab(info.Title, info, metadataProvider);
            this.dockPanel.ActiveQueryTab.QueryText = text;
        }

        public void OpenActivityMonitor(string title, ConnectionInfo info)
        {
            var activityMonitorTab = new ActivityMonitorTab
            {
                ConnectionInfo = info,
                Dock = DockStyle.Fill,
                ApplicationService = this.applicationService
            };
            AddNewTab(activityMonitorTab, title);
            activityMonitorTab.Start();
        }


        public void OpenInvokeTab(string title, ConnectionInfo info, Verb verb)
        {
            var invokeVerbTab = new InvokeVerbTab
            {
                ConnectionInfo = info,
                Dock = DockStyle.Fill,
                ApplicationService = this.applicationService,
                Verb = verb
            };
            AddNewTab(invokeVerbTab, title);
        }

        /// <inheritdoc />
        public void OpenCrudTab(CrudOperation operation, ConnectionInfo info, Entity entity)
        {
            string title = entity.FullName + " - " + operation;
            var crudTab = new CrudTab(operation)
            {
                ConnectionInfo = info,
                Dock = DockStyle.Fill,
                ApplicationService = this.applicationService,
                Entity = entity
            };

            crudTab.CloseItself += (s, e) =>
            {
                this.dockPanel.RemoveTab(crudTab.Parent as DockContent);
            };

            AddNewTab(crudTab, title);
        }

        internal void AddNewQueryTab()
        {
            string msg = null;

            try
            {
                ConnectionInfo info = QueryTab.CreateConnection();
                if (info == null)
                    return;

                bool alreadyExists = false;
                ConnectionInfo found;
                alreadyExists = serverList.TryGet(info.ServerType, info.Server, info.UserName, out found);
                if (!alreadyExists)
                {
                    info.Connect();
                    var provider = serverList.Add(info);

                    info.ConnectionClosed += (sender, args) => serverList.Remove(info);
                    this.dockPanel.AddServer(provider, info);
                    found = info;
                }

                IMetadataProvider foundProvider;
                serverList.TryGetProvider(found,  out foundProvider);
                this.CreateQueryTab(found.Title, found, foundProvider);
            }
            catch (FaultException<InfoServiceFaultContract> ex)
            {
                log.Error("Failed to connect", ex);
                msg = ex.Detail.Message;
            }
            catch (SecurityNegotiationException ex)
            {
                log.Error("Failed to connect", ex);
                msg = ex.Message;
            }
            catch (FaultException ex)
            {
                log.Error("Failed to connect", ex);
                msg = (ex.InnerException != null) ? ex.InnerException.Message : ex.Message;
            }
            catch (MessageSecurityException ex)
            {
                log.Error("Failed to connect", ex);
                if (ex.InnerException != null && ex.InnerException is FaultException)
                {
                    msg = (ex.InnerException as FaultException).Message;
                }
                else
                {
                    msg = ex.Message;
                }
            }
            catch (Exception ex)
            {
                log.Error("Failed to connect", ex);
                msg = ex.Message;
            }

            if (msg != null)
            {
                msg = string.Format("Unable to connect to Information Service. {0}", msg);
                MessageBox.Show(msg, "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        internal void OpenFiles(string[] files)
        {
            var connectionInfo = this.dockPanel.ActiveConnectionInfo;
            if (connectionInfo == null)
                return;

            this.dockPanel.ColoseInitialDocument();

            // Open file(s)
            foreach (string fn in files)
            {
                QueryTab queryTab = null;
                try
                {
                    connectionInfo.Connect();

                    IMetadataProvider metadataProvider;
                    this.serverList.TryGetProvider(connectionInfo, out metadataProvider);

                    queryTab = this.CreateQueryTab(Path.GetFileName(fn), connectionInfo, metadataProvider);
                    queryTab.QueryText = File.ReadAllText(fn);
                    // Modified flag is set during loading because the document 
                    // "changes" (from nothing to something). So, clear it again.
                    queryTab.IsDirty = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.GetType().Name);
                    if (queryTab != null)
                        this.dockPanel.RemoveTab(queryTab.Parent as DockContent);
                    return;
                }

                // ICSharpCode.TextEditor doesn't have any built-in code folding
                // strategies, so I've included a simple one. Apparently, the
                // foldings are not updated automatically, so in this demo the user
                // cannot add or remove folding regions after loading the file.
                //--				editor.Document.FoldingManager.FoldingStrategy = new RegionFoldingStrategy();
                //--				editor.Document.FoldingManager.UpdateFoldings(null, null);
            }
        }

        internal void CreateTabFromPrevious()
        {
            var tab = this.dockPanel.ActiveConnectionTab;
            if (tab != null)
            {
                var connection = tab.ConnectionInfo;
                var swql = this.dockPanel.ActiveQueryTab.QueryText;
                this.AddTextToEditor(swql, connection);
            }
            else
            {
                AddNewQueryTab();
            }
        }

        private QueryTab CreateQueryTab(string title, ConnectionInfo info, IMetadataProvider provider)
        {
            var queryTab = new QueryTab
            {
                ConnectionInfo = info,
                Dock = DockStyle.Fill,
                ApplicationService = this.applicationService
            };

            queryTab.SetMetadataProvider(provider);

            AddNewTab(queryTab, title);

            info.ConnectionClosed += (sender, args) =>
            {
                this.dockPanel.RemoveTab(queryTab.Parent as DockContent);
            };

            return queryTab;
        }

        private void AddNewTab(Control childControl, string title)
        {
            var dockContent = new DockContent();
            dockContent.Icon = Resources.TextFile_16x;
            dockContent.Controls.Add(childControl);
            dockContent.Text = title;
            dockContent.Show(this.dockPanel, DockState.Document);
        }
    }
}