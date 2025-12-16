// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Enums;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Chem4Word.ACME.Converters.CompassControl
{
    public class CompassTypeInverseToVisibility : IValueConverter
    {
        // Optional: specify which enum value should be visible
        public CompassControlType TargetType { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CompassControlType controlType)
            {
                return controlType == CompassControlType.FunctionalGroups
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                //Debug.WriteLine("Value of {0} is {1}", parameter?.ToString(), value);
            }

            return value;
        }
    }
}
