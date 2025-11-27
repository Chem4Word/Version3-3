// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2.Enums;
using System;

namespace Chem4Word.Model2.Formula
{
    public class FormulaPart
    {
        public FormulaPartType PartType { get; }

        public string Text { get; set; }

        public int Count { get; set; }

        public int Index { get; set; }

        public string Value
        {
            get
            {
                string value;

                switch (PartType)
                {
                    case FormulaPartType.Charge:
                        value = string.Empty;
                        int absCharge = Math.Abs(Count);

                        if (absCharge > 0)
                        {
                            if (Count > 0 )
                            {
                                value = Count > 1 ? $"+ {absCharge}" : "+";
                            }
                            if (Count < 0)
                            {
                                value = Count < -1 ? $"- {absCharge}" : "-";
                            }
                        }
                        break;

                    case FormulaPartType.ChildMolecule:
                        value = Count == 1
                            ? $"{Text}"
                            : $"{Count} {Text}";
                        break;

                    default:
                        value = Count == 1
                            ? $"{Text}"
                            : $"{Text} {Count}";
                        break;
                }

                return value;
            }
        }

        public FormulaPart(FormulaPartType partType, string text, int count)
        {
            PartType = partType;
            Text = text;
            Count = count;
        }

        public FormulaPart(FormulaPartType partType, int index, string text, int count)
        {
            PartType = partType;
            Index = index;
            Text = text;
            Count = count;
        }
        public override string ToString()
        {
            return $"{Value} - {PartType}";
        }
    }
}
