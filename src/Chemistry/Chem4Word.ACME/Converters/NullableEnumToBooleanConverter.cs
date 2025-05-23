﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Windows.Data;

namespace Chem4Word.ACME.Converters
{
    public class NullableEnumToBooleanConverter : IValueConverter
    {
        // Convert enum [value] to boolean, true if matches [param]
        public object Convert(object value, Type targetType, object param, CultureInfo culture)
        {
            if (param is null)
            {
                return value is null;
            }
            return value?.Equals(param) ?? false;
        }

        // Convert boolean to enum, returning [param] if true
        public object ConvertBack(object value, Type targetType, object param, CultureInfo culture)
        {
            if (value != null)
            {
                return (bool)value ? param : Binding.DoNothing;
            }
            return null;
        }
    }
}