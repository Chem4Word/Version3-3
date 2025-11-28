// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Media;

/* BondLayouts are simple classes that define the shape of a bond visual.
   They simplify the transfer of information into and out of drawing routines.
   You can either use the Point properties and draw primitives directly from those,
   or use the DefinedGeometry and draw that directly   */

namespace Chem4Word.ACME.Drawing.LayoutSupport
{
    public class WedgeBondLayout : BondLayout
    {
        public Point FirstCorner;
        public Point SecondCorner;
        public bool Outlined; //is the bond end drawn as a line?

        public StreamGeometry GetOutline()
        {
            var streamGeometry = new StreamGeometry();

            using (var sgc = streamGeometry.Open())
            {
                sgc.BeginFigure(Start, true, true);
                sgc.LineTo(FirstCorner, Outlined, true);
                sgc.LineTo(End, Outlined, true);
                sgc.LineTo(SecondCorner, Outlined, true);

                sgc.Close();
            }

            streamGeometry.Freeze();
            return streamGeometry;
        }
    }
}