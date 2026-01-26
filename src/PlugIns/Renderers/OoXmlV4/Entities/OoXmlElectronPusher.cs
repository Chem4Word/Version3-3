// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.Entities
{
    public class OoXmlElectronPusher
    {
        public Point StartPoint { get; set; }
        public Point FirstControlPoint { get; set; }
        public Point SecondControlPoint { get; set; }
        public Point EndPoint { get; set; }

        public string Path { get; set; }
    }
}
