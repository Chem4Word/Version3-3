﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Helpers;
using System.Windows;

namespace Chem4Word.ACME
{
    /// <summary>
    /// Rolls up Bond Stereo and Order into a single class to facilitate binding.
    /// Deals with combinations of Orders and Stereo
    /// </summary>
    public class BondOption : DependencyObject
    {
        public int Id
        {
            get { return (int)GetValue(IdProperty); }
            set { SetValue(IdProperty, value); }
        }

        public static readonly DependencyProperty IdProperty =
            DependencyProperty.Register("Id", typeof(int), typeof(BondOption), new PropertyMetadata(default(int)));

        public string Order
        {
            get { return (string)GetValue(OrderProperty); }
            set { SetValue(OrderProperty, value); }
        }

        public static readonly DependencyProperty OrderProperty =
            DependencyProperty.Register("Order", typeof(string), typeof(BondOption),
                                        new PropertyMetadata(Globals.OrderSingle));

        public BondStereo? Stereo
        {
            get { return (BondStereo?)GetValue(BondStereoEnumsProperty); }
            set { SetValue(BondStereoEnumsProperty, value); }
        }

        public static readonly DependencyProperty BondStereoEnumsProperty =
            DependencyProperty.Register("Stereo", typeof(BondStereo?), typeof(BondOption),
                                        new PropertyMetadata(null));

        public System.Windows.Media.Drawing BondGraphic
        {
            get { return (System.Windows.Media.Drawing)GetValue(BondGraphicProperty); }
            set { SetValue(BondGraphicProperty, value); }
        }

        public static readonly DependencyProperty BondGraphicProperty =
            DependencyProperty.Register("BondGraphic", typeof(System.Windows.Media.Drawing), typeof(BondOption),
                                        new PropertyMetadata(default(System.Windows.Media.Drawing)));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(string), typeof(BondOption),
                                        new PropertyMetadata(default(string)));

        public string Description
        {
            get
            {
                switch (Order)
                {
                    case Globals.OrderZero:
                        return "Agostic / Hydrogen bond";

                    case Globals.OrderSingle:
                        {
                            switch (Stereo)
                            {
                                case BondStereo.Wedge:
                                    return "Wedge";

                                case BondStereo.Hatch:
                                    return "Hatch";

                                case BondStereo.Indeterminate:
                                    return "Indeterminate";

                                default:
                                    return "Single";
                            }
                        }

                    case Globals.OrderDouble:
                        {
                            switch (Stereo)
                            {
                                case BondStereo.Indeterminate:
                                    {
                                        return "Indeterminate";
                                    }
                                default:
                                    return "Double";
                            }
                        }
                    case Globals.OrderAromatic:
                        return "Aromatic / Delocalised";

                    case Globals.OrderOther:
                        return "Unspecified";

                    case Globals.OrderPartial01:
                        return "0.5";

                    case Globals.OrderPartial12:
                        return "1.5";

                    case Globals.OrderPartial23:
                        return "2.5";

                    case Globals.OrderTriple:
                        return "Triple";

                    default:
                        return "";
                }
            }
        }

        public override string ToString()
        {
            return $"{Order} - {Stereo}";
        }
    }
}