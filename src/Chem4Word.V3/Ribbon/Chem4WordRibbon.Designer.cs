using Chem4Word.UI;

namespace Chem4Word
{
    partial class CustomRibbon : Microsoft.Office.Tools.Ribbon.RibbonBase
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        public CustomRibbon()
            : base(Globals.Factory.GetRibbonFactory())
        {
            InitializeComponent();
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.Chem4WordV3 = this.Factory.CreateRibbonTab();
            this.GroupInputOutput = this.Factory.CreateRibbonGroup();
            this.ImportFromFile = this.Factory.CreateRibbonButton();
            this.WebSearchMenu = this.Factory.CreateRibbonMenu();
            this.ExportToFile = this.Factory.CreateRibbonButton();
            this.ExportAll = this.Factory.CreateRibbonMenu();
            this.ExportAllToCML = this.Factory.CreateRibbonButton();
            this.ExportAllToSDfiles = this.Factory.CreateRibbonButton();
            this.GroupLibrary = this.Factory.CreateRibbonGroup();
            this.ShowLibrary = this.Factory.CreateRibbonToggleButton();
            this.ManageLibraries = this.Factory.CreateRibbonButton();
            this.BuyLibrary = this.Factory.CreateRibbonButton();
            this.EditLibrary = this.Factory.CreateRibbonButton();
            this.SaveToLibrary = this.Factory.CreateRibbonButton();
            this.GroupStructure = this.Factory.CreateRibbonGroup();
            this.EditStructure = this.Factory.CreateRibbonButton();
            this.ArrangeMolecules = this.Factory.CreateRibbonButton();
            this.ShowAsMenu = this.Factory.CreateRibbonMenu();
            this.EditLabels = this.Factory.CreateRibbonButton();
            this.ViewCml = this.Factory.CreateRibbonButton();
            this.ShowNavigator = this.Factory.CreateRibbonToggleButton();
            this.GroupOptions = this.Factory.CreateRibbonGroup();
            this.ChangeOptions = this.Factory.CreateRibbonButton();
            this.HelpMenu = this.Factory.CreateRibbonMenu();
            this.ShowAbout = this.Factory.CreateRibbonButton();
            this.ShowHome = this.Factory.CreateRibbonButton();
            this.Donate = this.Factory.CreateRibbonButton();
            this.ShowSystemInfo = this.Factory.CreateRibbonButton();
            this.CheckNow = this.Factory.CreateRibbonButton();
            this.ReadManual = this.Factory.CreateRibbonButton();
            this.YouTube = this.Factory.CreateRibbonButton();
            this.ButtonsDisabled = this.Factory.CreateRibbonButton();
            this.Update = this.Factory.CreateRibbonButton();
            this.Chem4WordV3.SuspendLayout();
            this.GroupInputOutput.SuspendLayout();
            this.GroupLibrary.SuspendLayout();
            this.GroupStructure.SuspendLayout();
            this.GroupOptions.SuspendLayout();
            this.SuspendLayout();
            // 
            // Chem4WordV3
            // 
            this.Chem4WordV3.Groups.Add(this.GroupInputOutput);
            this.Chem4WordV3.Groups.Add(this.GroupLibrary);
            this.Chem4WordV3.Groups.Add(this.GroupStructure);
            this.Chem4WordV3.Groups.Add(this.GroupOptions);
            this.Chem4WordV3.Label = "Chem4Word V3";
            this.Chem4WordV3.Name = "Chem4WordV3";
            // 
            // GroupInputOutput
            // 
            this.GroupInputOutput.Items.Add(this.ImportFromFile);
            this.GroupInputOutput.Items.Add(this.WebSearchMenu);
            this.GroupInputOutput.Items.Add(this.ExportToFile);
            this.GroupInputOutput.Items.Add(this.ExportAll);
            this.GroupInputOutput.Label = "External";
            this.GroupInputOutput.Name = "GroupInputOutput";
            // 
            // ImportFromFile
            // 
            this.ImportFromFile.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.ImportFromFile.Enabled = false;
            this.ImportFromFile.Image = global::Chem4Word.Properties.Resources.Import;
            this.ImportFromFile.Label = "Import";
            this.ImportFromFile.Name = "ImportFromFile";
            this.ImportFromFile.ScreenTip = "Import a structure from a chemistry file";
            this.ImportFromFile.ShowImage = true;
            this.ImportFromFile.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OnClick_Import);
            // 
            // WebSearchMenu
            // 
            this.WebSearchMenu.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.WebSearchMenu.Dynamic = true;
            this.WebSearchMenu.Enabled = false;
            this.WebSearchMenu.Image = global::Chem4Word.Properties.Resources.WebSearch;
            this.WebSearchMenu.Label = "Web Search";
            this.WebSearchMenu.Name = "WebSearchMenu";
            this.WebSearchMenu.ScreenTip = "Search public repositories for chemical structures";
            this.WebSearchMenu.ShowImage = true;
            this.WebSearchMenu.ItemsLoading += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OnLoading_SearchItems);
            // 
            // ExportToFile
            // 
            this.ExportToFile.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.ExportToFile.Enabled = false;
            this.ExportToFile.Image = global::Chem4Word.Properties.Resources.Export;
            this.ExportToFile.Label = "Export";
            this.ExportToFile.Name = "ExportToFile";
            this.ExportToFile.ScreenTip = "Export the selected structure to a chemistry file";
            this.ExportToFile.ShowImage = true;
            this.ExportToFile.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OnClick_Export);
            // 
            // ExportAll
            // 
            this.ExportAll.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.ExportAll.Enabled = false;
            this.ExportAll.Image = global::Chem4Word.Properties.Resources.ExportAll;
            this.ExportAll.Items.Add(this.ExportAllToCML);
            this.ExportAll.Items.Add(this.ExportAllToSDfiles);
            this.ExportAll.Label = "Export All";
            this.ExportAll.Name = "ExportAll";
            this.ExportAll.ShowImage = true;
            // 
            // ExportAllToCML
            // 
            this.ExportAllToCML.Image = global::Chem4Word.Properties.Resources.Cml;
            this.ExportAllToCML.Label = "CML Files";
            this.ExportAllToCML.Name = "ExportAllToCML";
            this.ExportAllToCML.ScreenTip = "Export all structures as CML";
            this.ExportAllToCML.ShowImage = true;
            this.ExportAllToCML.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OnClick_ExportAllToCML);
            // 
            // ExportAllToSDfiles
            // 
            this.ExportAllToSDfiles.Image = global::Chem4Word.Properties.Resources.SDFile;
            this.ExportAllToSDfiles.Label = "SD Files";
            this.ExportAllToSDfiles.Name = "ExportAllToSDfiles";
            this.ExportAllToSDfiles.ScreenTip = "Export all structures as SDF";
            this.ExportAllToSDfiles.ShowImage = true;
            this.ExportAllToSDfiles.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OnClick_ExportAllToSDFiles);
            // 
            // GroupLibrary
            // 
            this.GroupLibrary.Items.Add(this.ShowLibrary);
            this.GroupLibrary.Items.Add(this.ManageLibraries);
            this.GroupLibrary.Items.Add(this.BuyLibrary);
            this.GroupLibrary.Items.Add(this.EditLibrary);
            this.GroupLibrary.Items.Add(this.SaveToLibrary);
            this.GroupLibrary.Label = "Libraries";
            this.GroupLibrary.Name = "GroupLibrary";
            // 
            // ShowLibrary
            // 
            this.ShowLibrary.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.ShowLibrary.Enabled = false;
            this.ShowLibrary.Image = global::Chem4Word.Properties.Resources.Library_Find;
            this.ShowLibrary.Label = "Show";
            this.ShowLibrary.Name = "ShowLibrary";
            this.ShowLibrary.ScreenTip = "Show your currently selected library to import a structure from";
            this.ShowLibrary.ShowImage = true;
            this.ShowLibrary.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OnClick_ShowLibrary);
            // 
            // ManageLibraries
            // 
            this.ManageLibraries.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.ManageLibraries.Enabled = false;
            this.ManageLibraries.Image = global::Chem4Word.Properties.Resources.Library_Toggle;
            this.ManageLibraries.Label = "Manage";
            this.ManageLibraries.Name = "ManageLibraries";
            this.ManageLibraries.ScreenTip = "Manage your libraries";
            this.ManageLibraries.ShowImage = true;
            this.ManageLibraries.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OnClick_ManageLibraries);
            // 
            // BuyLibrary
            // 
            this.BuyLibrary.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.BuyLibrary.Enabled = false;
            this.BuyLibrary.Image = global::Chem4Word.Properties.Resources.Library_Buy;
            this.BuyLibrary.Label = "Download or Buy";
            this.BuyLibrary.Name = "BuyLibrary";
            this.BuyLibrary.ScreenTip = "Download or buy a library (most are free!)";
            this.BuyLibrary.ShowImage = true;
            this.BuyLibrary.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OnClick_BuyLibrary);
            // 
            // EditLibrary
            // 
            this.EditLibrary.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.EditLibrary.Enabled = false;
            this.EditLibrary.Image = global::Chem4Word.Properties.Resources.Library_Edit;
            this.EditLibrary.Label = "Edit Library";
            this.EditLibrary.Name = "EditLibrary";
            this.EditLibrary.ScreenTip = "Edit your currently selected library";
            this.EditLibrary.ShowImage = true;
            this.EditLibrary.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OnClick_EditLibrary);
            // 
            // SaveToLibrary
            // 
            this.SaveToLibrary.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.SaveToLibrary.Enabled = false;
            this.SaveToLibrary.Image = global::Chem4Word.Properties.Resources.Library_Save;
            this.SaveToLibrary.Label = "Save to Library";
            this.SaveToLibrary.Name = "SaveToLibrary";
            this.SaveToLibrary.ScreenTip = "Save the selected structure to your currently selected Library";
            this.SaveToLibrary.ShowImage = true;
            this.SaveToLibrary.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OnClick_SaveToLibrary);
            // 
            // GroupStructure
            // 
            this.GroupStructure.Items.Add(this.EditStructure);
            this.GroupStructure.Items.Add(this.ArrangeMolecules);
            this.GroupStructure.Items.Add(this.ShowAsMenu);
            this.GroupStructure.Items.Add(this.EditLabels);
            this.GroupStructure.Items.Add(this.ViewCml);
            this.GroupStructure.Items.Add(this.ShowNavigator);
            this.GroupStructure.Label = "Structures";
            this.GroupStructure.Name = "GroupStructure";
            // 
            // EditStructure
            // 
            this.EditStructure.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.EditStructure.Description = "Description";
            this.EditStructure.Enabled = false;
            this.EditStructure.Image = global::Chem4Word.Properties.Resources.Draw;
            this.EditStructure.Label = "Draw";
            this.EditStructure.Name = "EditStructure";
            this.EditStructure.ScreenTip = "Edit the selected structure or Draw a new structure";
            this.EditStructure.ShowImage = true;
            this.EditStructure.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OnClick_DrawOrEdit);
            // 
            // ArrangeMolecules
            // 
            this.ArrangeMolecules.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.ArrangeMolecules.Enabled = false;
            this.ArrangeMolecules.Image = global::Chem4Word.Properties.Resources.Separate_Molecules;
            this.ArrangeMolecules.Label = "Arrange";
            this.ArrangeMolecules.Name = "ArrangeMolecules";
            this.ArrangeMolecules.ScreenTip = "Arrange the structure so that the molecules do not overlap";
            this.ArrangeMolecules.ShowImage = true;
            this.ArrangeMolecules.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OnClick_Separate);
            // 
            // ShowAsMenu
            // 
            this.ShowAsMenu.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.ShowAsMenu.Dynamic = true;
            this.ShowAsMenu.Enabled = false;
            this.ShowAsMenu.Image = global::Chem4Word.Properties.Resources.View_As;
            this.ShowAsMenu.Label = "Show As";
            this.ShowAsMenu.Name = "ShowAsMenu";
            this.ShowAsMenu.ScreenTip = "Show the selected structure as Text (1D) or Drawing (2D)";
            this.ShowAsMenu.ShowImage = true;
            this.ShowAsMenu.ItemsLoading += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OnLoading_ViewAsItems);
            // 
            // EditLabels
            // 
            this.EditLabels.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.EditLabels.Enabled = false;
            this.EditLabels.Image = global::Chem4Word.Properties.Resources.Edit_Labels;
            this.EditLabels.Label = "Edit Labels";
            this.EditLabels.Name = "EditLabels";
            this.EditLabels.ScreenTip = "View or Edit the selected structure\'s Text (1D) labels";
            this.EditLabels.ShowImage = true;
            this.EditLabels.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OnClick_EditLabels);
            // 
            // ViewCml
            // 
            this.ViewCml.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.ViewCml.Enabled = false;
            this.ViewCml.Image = global::Chem4Word.Properties.Resources.Cml;
            this.ViewCml.Label = "View CML";
            this.ViewCml.Name = "ViewCml";
            this.ViewCml.ScreenTip = "View the CML data stored in this document for the selected structure";
            this.ViewCml.ShowImage = true;
            this.ViewCml.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OnClick_ViewCml);
            // 
            // ShowNavigator
            // 
            this.ShowNavigator.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.ShowNavigator.Enabled = false;
            this.ShowNavigator.Image = global::Chem4Word.Properties.Resources.Navigator_Toggle;
            this.ShowNavigator.Label = "Navigate";
            this.ShowNavigator.Name = "ShowNavigator";
            this.ShowNavigator.ScreenTip = "Navigate the chemical structures in this document";
            this.ShowNavigator.ShowImage = true;
            this.ShowNavigator.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OnClick_Navigator);
            // 
            // GroupOptions
            // 
            this.GroupOptions.Items.Add(this.ChangeOptions);
            this.GroupOptions.Items.Add(this.HelpMenu);
            this.GroupOptions.Items.Add(this.Update);
            this.GroupOptions.Label = "System";
            this.GroupOptions.Name = "GroupOptions";
            // 
            // ChangeOptions
            // 
            this.ChangeOptions.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.ChangeOptions.Enabled = false;
            this.ChangeOptions.Image = global::Chem4Word.Properties.Resources.Settings;
            this.ChangeOptions.Label = "Settings";
            this.ChangeOptions.Name = "ChangeOptions";
            this.ChangeOptions.ScreenTip = "Change Chem4Word settings";
            this.ChangeOptions.ShowImage = true;
            this.ChangeOptions.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OnClick_Options);
            // 
            // HelpMenu
            // 
            this.HelpMenu.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.HelpMenu.Image = global::Chem4Word.Properties.Resources.Help;
            this.HelpMenu.Items.Add(this.ShowAbout);
            this.HelpMenu.Items.Add(this.ShowHome);
            this.HelpMenu.Items.Add(this.Donate);
            this.HelpMenu.Items.Add(this.ShowSystemInfo);
            this.HelpMenu.Items.Add(this.CheckNow);
            this.HelpMenu.Items.Add(this.ReadManual);
            this.HelpMenu.Items.Add(this.YouTube);
            this.HelpMenu.Items.Add(this.ButtonsDisabled);
            this.HelpMenu.Label = "Help";
            this.HelpMenu.Name = "HelpMenu";
            this.HelpMenu.ScreenTip = "Get Chem4Word Help";
            this.HelpMenu.ShowImage = true;
            // 
            // ShowAbout
            // 
            this.ShowAbout.Image = global::Chem4Word.Properties.Resources.Information;
            this.ShowAbout.Label = "About";
            this.ShowAbout.Name = "ShowAbout";
            this.ShowAbout.ShowImage = true;
            this.ShowAbout.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OnClick_ShowAbout);
            // 
            // ShowHome
            // 
            this.ShowHome.Image = global::Chem4Word.Properties.Resources.Home;
            this.ShowHome.Label = "Chem4Word Home";
            this.ShowHome.Name = "ShowHome";
            this.ShowHome.ShowImage = true;
            this.ShowHome.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OnClick_ShowHome);
            // 
            // Donate
            // 
            this.Donate.Image = global::Chem4Word.Properties.Resources.Pound;
            this.Donate.Label = "Donate";
            this.Donate.Name = "Donate";
            this.Donate.ShowImage = true;
            this.Donate.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OnClick_Donate);
            // 
            // ShowSystemInfo
            // 
            this.ShowSystemInfo.Image = global::Chem4Word.Properties.Resources.About;
            this.ShowSystemInfo.Label = "System Info";
            this.ShowSystemInfo.Name = "ShowSystemInfo";
            this.ShowSystemInfo.ShowImage = true;
            this.ShowSystemInfo.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OnClick_ShowSystemInfo);
            // 
            // CheckNow
            // 
            this.CheckNow.Image = global::Chem4Word.Properties.Resources.SmallTick;
            this.CheckNow.Label = "Check for Updates";
            this.CheckNow.Name = "CheckNow";
            this.CheckNow.ShowImage = true;
            this.CheckNow.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OnClick_CheckForUpdates);
            // 
            // ReadManual
            // 
            this.ReadManual.Description = "Read the User Manual";
            this.ReadManual.Image = global::Chem4Word.Properties.Resources.Manual;
            this.ReadManual.Label = "User Manual";
            this.ReadManual.Name = "ReadManual";
            this.ReadManual.ShowImage = true;
            this.ReadManual.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OnClick_ReadManual);
            // 
            // YouTube
            // 
            this.YouTube.Image = global::Chem4Word.Properties.Resources.YouTube;
            this.YouTube.Label = "YouTube Videos";
            this.YouTube.Name = "YouTube";
            this.YouTube.ShowImage = true;
            this.YouTube.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OnClick_YouTube);
            // 
            // ButtonsDisabled
            // 
            this.ButtonsDisabled.Image = global::Chem4Word.Properties.Resources.Locked;
            this.ButtonsDisabled.Label = "Buttons Disabled ...";
            this.ButtonsDisabled.Name = "ButtonsDisabled";
            this.ButtonsDisabled.ShowImage = true;
            this.ButtonsDisabled.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OnClick_ButtonsDisabled);
            // 
            // Update
            // 
            this.Update.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.Update.Enabled = false;
            this.Update.Image = global::Chem4Word.Properties.Resources.Shield_Good;
            this.Update.Label = "Update";
            this.Update.Name = "Update";
            this.Update.ScreenTip = "Update Chem4Word";
            this.Update.ShowImage = true;
            this.Update.Visible = false;
            this.Update.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.OnClick_Update);
            // 
            // CustomRibbon
            // 
            this.Name = "CustomRibbon";
            this.RibbonType = "Microsoft.Word.Document";
            this.Tabs.Add(this.Chem4WordV3);
            this.Load += new Microsoft.Office.Tools.Ribbon.RibbonUIEventHandler(this.OnLoad_CustomRibbon);
            this.Chem4WordV3.ResumeLayout(false);
            this.Chem4WordV3.PerformLayout();
            this.GroupInputOutput.ResumeLayout(false);
            this.GroupInputOutput.PerformLayout();
            this.GroupLibrary.ResumeLayout(false);
            this.GroupLibrary.PerformLayout();
            this.GroupStructure.ResumeLayout(false);
            this.GroupStructure.PerformLayout();
            this.GroupOptions.ResumeLayout(false);
            this.GroupOptions.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        internal Microsoft.Office.Tools.Ribbon.RibbonTab Chem4WordV3;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup GroupStructure;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton EditStructure;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton ChangeOptions;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup GroupInputOutput;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton ImportFromFile;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton ExportToFile;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton ViewCml;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup GroupOptions;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton EditLabels;
        internal Microsoft.Office.Tools.Ribbon.RibbonMenu ShowAsMenu;
        internal Microsoft.Office.Tools.Ribbon.RibbonToggleButton ShowNavigator;
        internal Microsoft.Office.Tools.Ribbon.RibbonMenu WebSearchMenu;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup GroupLibrary;
        internal Microsoft.Office.Tools.Ribbon.RibbonToggleButton ShowLibrary;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton SaveToLibrary;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton ArrangeMolecules;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton Update;
        internal Microsoft.Office.Tools.Ribbon.RibbonMenu HelpMenu;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton ShowAbout;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton ShowHome;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton CheckNow;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton ReadManual;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton YouTube;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton ButtonsDisabled;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton ShowSystemInfo;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton ManageLibraries;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton BuyLibrary;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton EditLibrary;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton Donate;
        internal Microsoft.Office.Tools.Ribbon.RibbonMenu ExportAll;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton ExportAllToCML;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton ExportAllToSDfiles;
    }

    partial class ThisRibbonCollection
    {
        internal CustomRibbon CustomRibbon
        {
            get { return this.GetRibbon<CustomRibbon>(); }
        }
    }
}
