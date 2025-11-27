// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Behaviors;
using Chem4Word.ACME.Commands;
using Chem4Word.ACME.Commands.Editing;
using Chem4Word.ACME.Commands.Grouping;
using Chem4Word.ACME.Commands.Layout.Alignment;
using Chem4Word.ACME.Commands.Layout.Flipping;
using Chem4Word.ACME.Commands.PropertyEdit;
using Chem4Word.ACME.Commands.Reactions;
using Chem4Word.ACME.Commands.Sketching;
using Chem4Word.ACME.Commands.Undo;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Enums;
using Chem4Word.ACME.Utils;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Wpf;
using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Formula;
using IChem4Word.Contracts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using static Chem4Word.Model2.ModelGlobals;

namespace Chem4Word.ACME
{
    /// <summary>
    /// The master brain of ACME. All editing operations arise from this class.
    /// </summary>
    ///
    /// We use commands to perform instantaneous operations, such as deletion.
    /// We use behaviors to put the editor into one mode or another
    ///
    /// The sheer size of this class has necessitated that we break it down into
    /// partial class definitions.
    /// Check out the folder EditController for the other parts.

    public partial class EditController : Controller
    {
        private static readonly string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static readonly string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        #region Fields

        private Dictionary<int, BondOption> _bondOptions = new Dictionary<int, BondOption>();
        private int? _selectedBondOptionId;

        private BaseEditBehavior _activeBehavior;

        private double _currentBondLength;

        #endregion Fields

        #region Properties

        /// <summary>
        /// Telemetry interface
        /// </summary>
        private IChem4WordTelemetry Telemetry { get; }

        /// <summary>
        /// Indicates whether there is a model currently being loaded
        /// </summary>
        public bool Loading { get; set; }

        /// <summary>
        /// Bond order, as selected in the Bond dropdown
        /// </summary>
        public string CurrentBondOrder
        {
            get { return _bondOptions[_selectedBondOptionId.Value].Order; }
        }

        /// <summary>
        /// Current bond stereo, as selected in the Bond dropdown
        /// </summary>
        public BondStereo CurrentStereo
        {
            get { return _bondOptions[_selectedBondOptionId.Value].Stereo.Value; }
        }

        /// <summary>
        /// Quick metric for bond thickness when editing
        /// </summary>
        public double EditBondThickness
        {
            get { return AcmeConstants.BondThickness * AcmeConstants.DefaultBondLineFactor; }
        }

        /// <summary>
        /// The main undo Manager for this editor
        /// </summary>
        public UndoHandler UndoManager { get; }

        /// <summary>
        /// Stipulates the default bond length for the model
        /// </summary>
        public double CurrentBondLength
        {
            get { return _currentBondLength; }
            set
            {
                _currentBondLength = value;
                OnPropertyChanged();
                double scaled = value * ModelConstants.ScaleFactorForXaml;
                // Decide if we need to rescale to current drawing
                if (!Loading && Math.Abs(Model.MeanBondLength - scaled) > 2.5)
                {
                    SetAverageBondLength(scaled);
                }
            }
        }

        /// <summary>
        /// Refers to the actual hosting control
        /// associated with this controller
        /// </summary>
        public Editor EditorControl { get; set; }

        /// <summary>
        /// The ChemistryCanvas embedded in the EditorControl
        /// </summary>
        public EditorCanvas EditingCanvas { get; set; }

        /// <summary>
        /// The little Rich text editor for editing reaction information
        /// </summary>
        public AnnotationEditor BlockEditor { get; }

        public List<string> Used1DProperties { get; set; }

        public int? SelectedBondOptionId
        {
            get
            {
                List<int> selectedBondTypes = (from bt in SelectedBondOptions
                                               select bt.Id).Distinct().ToList();

                switch (selectedBondTypes.Count)
                {
                    // Nothing selected, return last value selected
                    case 0:
                        return _selectedBondOptionId;

                    case 1:
                        return selectedBondTypes[0];
                    // More than one selected !
                    default:
                        return null;
                }
            }
            set
            {
                _selectedBondOptionId = value;
                if (value != null)
                {
                    SetBondOption(_selectedBondOptionId, SelectedItems.OfType<Bond>().ToArray());
                }
            }
        }

        /// <summary>
        /// List of matching Bond Options for the selected bonds in the canvas
        /// </summary>
        private List<BondOption> SelectedBondOptions
        {
            get
            {
                IEnumerable<Bond> selectedBonds = SelectedItems.OfType<Bond>();

                IEnumerable<BondOption> selbonds = (from Bond selbond in selectedBonds
                                                    select new BondOption { Order = selbond.Order, Stereo = selbond.Stereo }).Distinct();

                IEnumerable<BondOption> selOptions = from BondOption bo in _bondOptions.Values
                                                     join selbond1 in selbonds
                                                         on new { bo.Order, bo.Stereo } equals new { selbond1.Order, selbond1.Stereo }
                                                     select new BondOption { Id = bo.Id, Order = bo.Order, Stereo = bo.Stereo };

                return selOptions.ToList();
            }
        }

        /// <summary>
        /// The currently edit behaviour as indicated by the toolbar buttons
        /// E.g. Draw, Select, Ring etc
        /// Each button is associated with a behaviour defined in XAML
        /// and connected directly to its tag
        /// </summary>
        public BaseEditBehavior ActiveBehavior
        {
            get { return _activeBehavior; }
            set
            {
                if (_activeBehavior != null)
                {
                    _activeBehavior.Detach();
                    _activeBehavior = null;
                }

                _activeBehavior = value;
                if (_activeBehavior != null)
                {
                    _activeBehavior.Attach(EditingCanvas);
                    SendStatus((_activeBehavior.CurrentStatus.message, TotUpMolFormulae(), TotUpSelectedMwt()));
                }

                OnPropertyChanged();
            }
        }

        /// <summary>
        /// List of available atom options for the atom dropdown
        /// </summary>
        public ObservableCollection<AtomOption> AtomOptions { get; set; }

        /// <summary>
        /// Has the chemistry in the editor been modified?
        /// </summary>
        public bool IsDirty
        {
            get
            {
                return UndoManager.CanUndo;
            }
        }

        /// <summary>
        /// Has any setting been changed by the user?
        /// </summary>
        public bool HasChangedSettings { get; set; }

        /// <summary>
        /// Default text for the status bar at the bottom of the editor
        /// when editing reaction textI
        /// </summary>
        private const string EditingTextStatus =
            "[Shift-Enter] = new line; [Enter] = save text; [Esc] = cancel editing. ";

        /// <summary>
        /// Default status message when something is selected
        /// </summary>
        private const string DefaultStatusMessage = "Drag to reposition; [Delete] to remove.";

        #endregion Properties

        #region Events

        /// <summary>
        /// Raised when the following method sends a new status message
        /// </summary>
        public event EventHandler<WpfEventArgs> OnFeedbackChange;

        /// <summary>
        /// Call this from a method which changes the current state
        /// of the user's interaction with the editor
        /// </summary>
        /// <param name="value">ValueTuple consisting of the message, the formula and the molecular weight</param>
        internal void SendStatus((string message, string formula, string molecularWeight) value)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                WpfEventArgs args = new WpfEventArgs { Message = value.message };

                bool hasReactions = Model.ReactionSchemes.Any() &&
                                     Model.ReactionSchemes.First().Value.Reactions.Count > 0;
                bool moleculesSelected = SelectionType == SelectionTypeCode.Molecule;

                if (hasReactions && moleculesSelected || !hasReactions)
                {
                    args.Formula = value.formula;
                    args.MolecularWeight = value.molecularWeight;
                }

                OnFeedbackChange?.Invoke(this, args);
            }
            catch (Exception exception)
            {
                Telemetry.Write(module, "Exception", exception.ToString());
            }
        }

        #endregion Events

        #region Methods

        /// <summary>
        /// Initializes the editor with default settings
        /// </summary>
        private void DefaultSettings()
        {
            _selectedBondOptionId = 1;
            _selectedReactionType = ReactionType.Normal;
            _selectedElement = ModelGlobals.PeriodicTable.C;
            SelectionIsSubscript = false;
            SelectionIsSuperscript = false;
        }

        /// <summary>
        /// Initializes and sets up all command objects for the application.
        /// </summary>
        /// <remarks>This method assigns instances of various command objects to their respective
        /// properties. These commands are used to perform actions such as undo/redo, editing, alignment, and  property
        /// modifications within the application. Each command is initialized with a reference  to the current context
        /// to enable its functionality.</remarks>
        private void SetupCommands()
        {
            RedoCommand = new RedoCommand(this);
            UndoCommand = new UndoCommand(this);
            AddAtomCommand = new AddAtomCommand(this);
            CopyCommand = new CopyCommand(this);
            CutCommand = new CutCommand(this);
            PasteCommand = new PasteCommand(this);
            FlipVerticalCommand = new FlipVerticalCommand(this);
            FlipHorizontalCommand = new FlipHorizontalCommand(this);
            AddHydrogensCommand = new AddHydrogensCommand(this);
            RemoveHydrogensCommand = new RemoveHydrogensCommand(this);
            FuseCommand = new FuseCommand(this);
            GroupCommand = new GroupCommand(this);
            UnGroupCommand = new UnGroupCommand(this);
            SettingsCommand = new SettingsCommand(this);
            PickElementCommand = new PickElementCommand(this);

            AlignBottomsCommand = new AlignBottomsCommand(this);
            AlignMiddlesCommand = new AlignMiddlesCommand(this);
            AlignTopsCommand = new AlignTopsCommand(this);

            AlignLeftsCommand = new AlignLeftsCommand(this);
            AlignCentresCommand = new AlignCentresCommand(this);
            AlignRightsCommand = new AlignRightsCommand(this);

            EditConditionsCommand = new EditConditionsCommand(this);
            EditReagentsCommand = new EditReagentsCommand(this);
            AssignReactionRolesCommand = new AssignReactionRolesCommand(this);
            ClearReactionRolesCommand = new ClearReactionRolesCommand(this);

            EditSelectionPropertiesCommand = new EditSelectionPropertiesCommand(this);
            EditActiveAtomPropertiesCommand = new EditActiveAtomPropertiesCommand(this);
            EditActiveBondPropertiesCommand = new EditActiveBondPropertiesCommand(this);
            PropertiesCommand = new EditSelectionPropertiesCommand(this);

            FlipBondStereoCommand = new FlipBondStereoCommand(this);
        }

        /// <summary>
        /// Loads and initializes the available atom options, including standard options, model-specific options, and
        /// functional groups.
        /// </summary>
        /// <remarks>This method clears any existing atom options before loading new ones. It sequentially
        /// loads standard atom options,  model-specific atom options, and functional groups associated with the model.
        /// This ensures that the atom options  are fully refreshed and up-to-date.</remarks>
        private void LoadAtomOptions()
        {
            ClearAtomOptions();
            LoadStandardAtomOptions();
            LoadModelAtomOptions();
            LoadModelFGs();
        }

        /// <summary>
        /// Gets rid of any existing atop options defined in the dropdown
        /// </summary>
        private void ClearAtomOptions()
        {
            int limit = AtomOptions.Count - 1;
            for (int i = limit; i >= 0; i--)
            {
                AtomOptions.RemoveAt(i);
            }
        }

        /// <summary>
        /// Loads all functional groups associated with the current Model
        /// </summary>
        private void LoadModelFGs()
        {
            IEnumerable<ElementBase> modelFGs = (from a in Model.GetAllAtoms()
                                                 where a.Element is FunctionalGroup && !(from ao in AtomOptions
                                                                                         select ao.Element).Contains(a.Element)
                                                 orderby a.SymbolText
                                                 select a.Element).Distinct();

            IEnumerable<AtomOption> newOptions = from mfg in modelFGs
                                                 select new AtomOption((mfg as FunctionalGroup));
            foreach (AtomOption newOption in newOptions)
            {
                AtomOptions.Add(newOption);
            }
        }

        /// <summary>
        /// Loads any additional elements specified in the model as atom options
        /// </summary>
        /// <param name="addition"></param>
        private void LoadModelAtomOptions()
        {
            IEnumerable<ElementBase> modelElements = (from a in Model.GetAllAtoms()
                                                      where a.Element is Element && !(from ao in AtomOptions
                                                                                      select ao.Element).Contains(a.Element)
                                                      orderby a.SymbolText
                                                      select a.Element).Distinct();

            IEnumerable<AtomOption> newOptions = from e in ModelGlobals.PeriodicTable.ElementsSource
                                                 join me in modelElements
                                                     on e equals me
                                                 select new AtomOption(e);

            foreach (AtomOption newOption in newOptions)
            {
                AtomOptions.Add(newOption);
            }
        }

        /// <summary>
        /// Loads the bog-standard atom options into the main dropdown
        /// </summary>
        private void LoadStandardAtomOptions()
        {
            foreach (string atom in AcmeConstants.StandardAtoms)
            {
                AtomOptions.Add(new AtomOption(ModelGlobals.PeriodicTable.Elements[atom]));
            }

            foreach (string fg in AcmeConstants.StandardFunctionalGroups)
            {
                AtomOptions.Add(new AtomOption(FunctionalGroupsList.FirstOrDefault(f => f.Name.Equals(fg))));
            }
        }

        /// <summary>
        /// Loads up the bond options into the main dropdown
        /// </summary>
        private void LoadBondOptions()
        {
            BondOption[] storedOptions = (BondOption[])EditingCanvas.FindResource("BondOptions");
            for (int i = 1; i <= storedOptions.Length; i++)
            {
                _bondOptions[i] = storedOptions[i - 1];
            }
        }

        /// <summary>
        /// Testing initialization method to load up bond options
        /// </summary>
        private void LoadBondOptionsForUnitTest()
        {
            _bondOptions = new Dictionary<int, BondOption>
                           {
                               {
                                   1,
                                   new BondOption
                                   {
                                       Id = 1, Order = ModelConstants.OrderSingle, Stereo = BondStereo.None
                                   }
                               },
                               {
                                   2,
                                   new BondOption
                                   {
                                       Id = 2, Order = ModelConstants.OrderDouble, Stereo = BondStereo.None
                                   }
                               },
                               {
                                   3,
                                   new BondOption
                                   {
                                       Id = 3, Order = ModelConstants.OrderTriple, Stereo = BondStereo.None
                                   }
                               }
                           };
        }

        /// <summary>
        /// Writes a message to telemetry
        /// </summary>
        /// <param name="source">Calling procedure name</param>
        /// <param name="level">Error level</param>
        /// <param name="message">Actual error message</param>
        private void WriteTelemetry(string source, string level, string message)
        {
            Telemetry?.Write(source, level, message);
        }

        /// <summary>
        /// Writes an exception to telemetry
        /// </summary>
        /// <param name="source"></param>
        /// <param name="exception"></param>
        private void WriteTelemetryException(string source, Exception exception)
        {
            if (Telemetry != null)
            {
                Telemetry.Write(source, "Exception", exception.Message);
                Telemetry.Write(source, "Exception", exception.StackTrace);
            }
            else
            {
                RegistryHelper.StoreException(source, exception);
            }

            Debugger.Break();
        }

        /// <summary>
        /// Checks the integrity of the model after an editor operation
        /// </summary>
        /// <param name="module">Name of calling module in which this was invoked</param>
        private void CheckModelIntegrity(string module)
        {
#if DEBUG
            List<string> integrity = Model.CheckIntegrity();
            if (integrity.Count > 0)
            {
                Telemetry?.Write(module, "Integrity", string.Join(Environment.NewLine, integrity));
            }
#endif
        }

        #endregion Methods

        //totals up the mwt of all the molecules in the selection
        public string TotUpSelectedMwt()
        {
            string selectedMWT = SafeDouble.AsCMLString(Model.MolecularWeight);
            //set the molecular weight if you can

            double mwt = 0;
            foreach (Molecule molecule in SelectedItems.Where(m => m is Molecule))
            {
                mwt += molecule.MolecularWeight;
            }

            if (mwt > 0d)
            {
                selectedMWT = SafeDouble.AsCMLString(mwt);
            }
            else
            {
                if (Model.MolecularWeight > 0d)
                {
                    selectedMWT = SafeDouble.AsCMLString(Model.MolecularWeight);
                }
                else
                {
                    selectedMWT = "";
                }
            }

            return selectedMWT;
        }

        //totals up all the formulae of all the selected molecules

        public string TotUpMolFormulae()
        {
            Collection<string> formulae = new Collection<string>();

            // Some molecules are selected
            foreach (Molecule molecule in SelectedItems.Where(m => m is Molecule))
            {
                formulae.Add(molecule.UnicodeFormula);
            }

            if (formulae.Count > 0)
            {
                return string.Join(", ", formulae.ToArray());
            }

            // No molecules are selected
            foreach (Molecule molecule in Model.Molecules.Values)
            {
                formulae.Add(molecule.UnicodeFormula);
            }

            if (formulae.Count > 0)
            {
                return string.Join(", ", formulae.ToArray());
            }

            return string.Empty;
        }
    }
}
