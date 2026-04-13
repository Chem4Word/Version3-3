// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace Chem4Word.ACME.Converters
{
    public class ElementConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context,
            Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(ElementBase);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            try
            {
                string s = value as string;
                if (string.IsNullOrEmpty(s))
                {
                    // We should never get here
                    string message = $"Value is null or empty !";
                    Debug.WriteLine(message);
                    Debugger.Break();

                    return "";
                }
                else
                {
                    return ModelGlobals.PeriodicTable.Elements[s];
                }
            }
            catch
            {
                return null;
            }
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return (value as ElementBase)?.Symbol;
        }
    }
}
