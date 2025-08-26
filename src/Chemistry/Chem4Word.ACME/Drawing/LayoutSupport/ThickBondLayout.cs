﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------
using System.Windows;

namespace Chem4Word.ACME.Drawing.LayoutSupport
{
    public class ThickBondLayout: BondLayout
    {
        public Point FirstCorner; 
        public Point SecondCorner;
        public Point ThirdCorner;
        public Point FourthCorner;
    }
}
