using Chem4Word.ACME.Drawing.Text;
using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Chem4Word.ACME.Drawing.Visuals
{
    /// <summary>
    /// Draws a symbol indicating a single electron or an electron pair, attached to an atom
    /// </summary>
    public class ElectronVisual : AtomVisual
    {
        #region Properties

        public AtomVisual ParentVisual { get; protected set; }
        public DrawingContext Context { get; set; }

        public Electron ParentElectron { get; set; }
        public AtomTextMetrics ParentMetrics { get; set; }
        public AtomTextMetrics Metrics { get; protected set; }
        public AtomTextMetrics HydrogenMetrics { get; set; }
        public AtomTextMetrics ChargeMetrics { get; set; }
        public AtomTextMetrics ElectronMetrics { get; set; }

        #endregion Properties

        #region Constructors

        public ElectronVisual(AtomVisual parentVisual,
                           DrawingContext drawingContext, AtomTextMetrics mainAtomMetrics, AtomTextMetrics hMetrics, AtomTextMetrics chargeMetrics)
        {
            Context = drawingContext;
            ParentVisual = parentVisual;
            ParentMetrics = mainAtomMetrics;
            HydrogenMetrics = hMetrics;
            ChargeMetrics = chargeMetrics;
        }

        public override void Render()
        {
            Point center = ParentVisual.Position;

            //establish a vector from the centre of the atom to the planned position for the bounding box
            
            //first, work out from the placement property what the vector is
            double offsetAngle = 45 * (int)(ParentElectron.Placement.Value);
            Matrix rotator = new Matrix();
            rotator.Rotate(offsetAngle);

            //make it long enough to clear the atom symbol
            Vector placementVector = 100 * GeometryTool.ScreenNorth * rotator;
            
            //and intersect it with the convex hull to find the edge point
            var endPoint = ParentVisual.GetIntersection(center, center + placementVector);


            //now extend it by the standoff distance plus the size of the electron symbol
            //this is the centre of the electron symbol
            //if we're drawing a radical, then draw a simple dot
            //otherwise, draw two dots offset by a perpendicular to the main vector
        }

        #endregion Constructors
    }
}
