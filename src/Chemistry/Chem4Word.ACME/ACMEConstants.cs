// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using System;

namespace Chem4Word.ACME
{
    public static class AcmeConstants
    {
        public static string[] StandardAtoms = { "C", "H", "N", "O", "P", "S", "F", "Cl", "Br", "I", "B", "Si", "Li", "Na", "K" };
        public static string[] StandardFunctionalGroups = { "R1", "R2", "R3", "R4" };

        //selecting messages
        public const string SelectDefaultMessage =
            "Click to select; [Shift]-click to multselect; drag to select range; double-click to select molecule.";

        public const string SelStatusMessage = "Set atoms/bonds using selectors; drag to reposition; [Delete] to remove.";
        public const string SelDrawAroundMessage = "Draw around atoms and bonds to select.";
        public const string SelDragBondMessage = "Drag bond to reposition.";
        public const string SelUnlockPivotMessage = "[Shift] = unlock length; [Ctrl] = unlock angle; [Alt] = pivot.";
        public const string SelDragAtomMessage = "Drag atom to reposition";

        //messages for the status bar
        public const string UnlockStatusText = "[Shift] = unlock length; [Ctrl] = unlock angle; [Esc] = cancel.";

        public const string DefaultStatusText = "Drag a handle to resize; drag shaft to reposition.";

        //reactions status messages
        public const string ReactionTypeStdMessage = "Click to set reaction type";

        public const string EditReagentsStatusText = "Double-click box to edit reagents.";
        public const string EditConditionsStatusText = "Double-click box to edit conditions";

        //chain and drawing messages
        public const string DragChainMessage = "Drag to start sizing chain: [Esc] to cancel.";

        public const string DefaultChainMessage = "Draw a chain by clicking on an atom or free space.";
        public const string DeleteStandardMessage = "Click to remove an atom or bond.";
        public const string DefaultDrawText = "Click existing atom to sprout a chain or modify element.";
        public const string UnlockStandardMessage = "[Shift] = unlock length; [Ctrl] = unlock angle; [Esc] = cancel.";

        //ungroup warning messages
        public const string UngroupWarningMessage = "Ungroup before attempting to draw.";

        //simple drawing messages
        public const string DefaultRotateHydrogenMessage = "Click to rotate hydrogen";

        public const string DefaultSetElementMessage = "Click to set element.";
        public const string DrawSproutChainMessage = "Click to sprout chain";
        public const string DrawModifyBondMessage = "Click to modify bond";
        public const string DrawAtomMessage = "Click to draw atom";

        //ring drawing
        public const string DefaultDrawRingMessage = "Drag on atom, bond or free space to draw ring.";

        public const string ResizeRingMessage = "Drag along arrow to size ring: [Esc] to cancel";
        public const string CantDrawRingMessage = "Can't draw ring here - drag over atom or bond to draw fused ring";
        public const string DragRingFromAtomMessage = "Drag from atom to size ring.";
        public const string DragRingFromBondMessage = "Drag from bond to size ring.";
        public const string RingSpiroFuseMessage = "Click atom to spiro-fuse.";
        public const string RingTerminatingMessage = "Click atom to draw a terminating ring.";
        public const string RingFuseOnBondMessage = "Click bond to fuse a ring";
        public const string RingDrawStandaloneMessage = "Click to draw a standalone ring";
        public const string RingNoRoomMessage = "No room left to draw any more rings!";

        //layout constants
        public const int BlockTextPadding = 10;

        public const double ThumbWidth = 22;
        public const string HoverAdornerColorDef = "#FFFF8C00";     //dark orange
        public const string ThumbAdornerFillColorDef = "#FFFFA500"; //orange
        public const string Chem4WordColorDef = "#2A579A";
        public const string GroupBracketColorDef = "#FF00BFFF";           //deep sky blue
        public const string DefaultTextColor = "#000000";

        //XAML styles
        public const string GroupHandleStyle = "GroupHandleStyle";

        public const string GrabHandleStyle = "GrabHandleStyle";
        public const string ThumbStyle = "BigThumbStyle";

        public const string RotateThumbStyle = "RotateThumb";

        //metrics
        public const double AdornerBorderThickness = 1;

        public const double DefaultBondLineFactor = 1.0;
        public const double HoverAdornerThickness = 3.0;
        public const double AtomRadius = 5.0;
        public const double ExplicitHydrogenBondPercentage = 1.0;
        public const double BondThickness = ModelConstants.ScaleFactorForXaml * 0.8;
        public const double BracketThickness = BondThickness;
        public const double BracketFactor = 0.2;
        public const double GroupInflateFactor = BracketFactor / 2;

        //brushes & pens
        public const string AdornerBorderPen = "GrabHandlePen";

        public const string AdornerBorderBrush = "GrabHandleBorderBrush";
        public const string AdornerFillBrush = "ThumbFillBrush";
        public const string AtomBondSelectorBrush = "AtomBondSelectorBrush";
        public const string BlockedAdornerBrush = "BlockedAdornerBrush";
        public const string GhostBrush = "GrabHandleBorderBrush";
        public const string DrawAdornerBrush = "DrawBondBrush";
        public const string ResourceKeyProductIndicatorBrush = "ProductIndicatorBrush";
        public const string ResourceKeyReactantIndicatorBrush = "ReactantIndicatorBrush";
        public const string ResourceKeyArrowIndicatorBrush = "ArrowIndicatorBrush";
        public const string ResourceKeyIndicatorBackgroundBrush = "IndicatorBackgroundBrush";
        public const string AgosticHydrogenBondDesc = "Agostic / Hydrogen bond";
        public const string WedgeDesc = "Wedge";
        public const string HatchDesc = "Hatch";
        public const string IndeterminateDesc = "Indeterminate";
        public const string SingleDesc = "Single";
        public const string DoubleDesc = "Double";
        public const string AromaticDelocalisedDesc = "Aromatic / Delocalised";
        public const string UnspecifiedDesc = "Unspecified";
        public const string Point5Desc = "0.5";
        public const string OnePoint5Desc = "1.5";
        public const string TwoPoint5Desc = "2.5";
        public const string TripleDesc = "Triple";
        public const string NotImplementedErrMessage = "The method or operation is not implemented.";
        public const string BlockColour = "#000000";
        public const string SingleRadical = "•";
        public const string DoubleRadical = "••";

        /// <summary>
        /// Sent when the contents of the clipboard have changed.
        /// </summary>
        public const int WM_CLIPBOARDUPDATE = 0x031D;

        /// <summary>
        /// To find message-only windows, specify HWND_MESSAGE in the hwndParent parameter of the FindWindowEx function.
        /// </summary>
        public static IntPtr HWND_MESSAGE = new IntPtr(-3);

        public const double ScriptScalingFactor = 0.6;
    }
}