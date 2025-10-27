// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Utils;
using Chem4Word.Model2;
using IChem4Word.Contracts;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace Chem4Word.ACME
{
    public partial class EditController
    {
        /// <summary>
        /// "Normal" Constructor
        /// </summary>
        /// <param name="model">The model you want to edi. Can be empty</param>
        /// <param name="editingCanvas">EditorCanvas object which the EditController uses</param>
        /// <param name="used1DProperties">List of 1Dproperties that might be accessed</param>
        /// <param name="telemetry">Telemetry object to report issues to</param>
        public EditController(Model model, EditorCanvas editingCanvas, Canvas hostingCanvas,
                              AnnotationEditor annotationEditor, List<string> used1DProperties,
                              IChem4WordTelemetry telemetry) : base(model)
        {
            EditingCanvas = editingCanvas;
            BlockEditor = annotationEditor;
            Used1DProperties = used1DProperties;
            Telemetry = telemetry;

            _clipboardMonitor = new ClipboardMonitor();
            ClipboardMonitor.OnClipboardContentChanged += OnClipboardContentChanged_ClipboardMonitor;

            AtomOptions = new ObservableCollection<AtomOption>();
            LoadAtomOptions();
            LoadBondOptions();

            _selectedItems = new ObservableCollection<object>();
            _selectedItemsWrapper = new ReadOnlyObservableCollection<object>(_selectedItems);
            _selectedItems.CollectionChanged += OnChanged_SelectedItems;

            UndoManager = new UndoHandler(this, telemetry);

            SetupCommands();

            DefaultSettings();
        }

        /// <summary>
        /// Constructor for [X]Unit Tests
        /// Initialises the minimum objects necessary to run [X]Unit Tests
        /// </summary>
        /// <param name="model"></param>
        public EditController(Model model) : base(model)
        {
            LoadBondOptionsForUnitTest();

            _selectedItems = new ObservableCollection<object>();
            _selectedItemsWrapper = new ReadOnlyObservableCollection<object>(_selectedItems);

            UndoManager = new UndoHandler(this, null);

            SetupCommands();

            DefaultSettings();

            IsBlockEditing = false;
        }
    }
}
