// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using Chem4Word.Model2.Enums;
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
                                        new PropertyMetadata(ModelConstants.OrderSingle));

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
                    case ModelConstants.OrderZero:
                        return AcmeConstants.AgosticHydrogenBondDesc;

                    case ModelConstants.OrderSingle:
                        {
                            switch (Stereo)
                            {
                                case BondStereo.Wedge:
                                    return AcmeConstants.WedgeDesc;

                                case BondStereo.Hatch:
                                    return AcmeConstants.HatchDesc;

                                case BondStereo.Indeterminate:
                                    return AcmeConstants.IndeterminateDesc;

                                default:
                                    return AcmeConstants.SingleDesc;
                            }
                        }

                    case ModelConstants.OrderDouble:
                        {
                            switch (Stereo)
                            {
                                case BondStereo.Indeterminate:
                                    {
                                        return AcmeConstants.IndeterminateDesc;
                                    }
                                default:
                                    return AcmeConstants.DoubleDesc;
                            }
                        }
                    case ModelConstants.OrderAromatic:
                        return AcmeConstants.AromaticDelocalisedDesc;

                    case ModelConstants.OrderOther:
                        return AcmeConstants.UnspecifiedDesc;

                    case ModelConstants.OrderPartial01:
                        return AcmeConstants.Point5Desc;

                    case ModelConstants.OrderPartial12:
                        return AcmeConstants.OnePoint5Desc;

                    case ModelConstants.OrderPartial23:
                        return AcmeConstants.TwoPoint5Desc;

                    case ModelConstants.OrderTriple:
                        return AcmeConstants.TripleDesc;

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