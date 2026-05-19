// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2.Enums;
using Chem4Word.Renderer.OoXmlV4.Helpers;
using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.Entities
{
    public class OoXmlElectronPusher
    {
        public Point StartPoint { get; set; }
        public Point FirstControlPoint { get; set; }
        public Point SecondControlPoint { get; set; }
        public Point EndPoint { get; set; }

        public Point FishHookPoint { get; set; }

        public Rect BoundingBox { get; set; }

        public string Path { get; set; }
        public ElectronPusherType PusherType { get; set; }

        public void CalculateBoundingBox()
        {
            BoundingBox = BezierUtils.CubicBezierBounds(StartPoint, FirstControlPoint, SecondControlPoint, EndPoint);
        }
    }
}
