// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.Renderer.OoXmlV4.OoXml
{
    public class OoXmlConstants
    {
        // https://startbigthinksmall.wordpress.com/2010/02/05/unit-converter-and-specification-search-for-ooxmlwordml-development/
        // http://lcorneliussen.de/raw/dashboards/ooxml/

        // V3 == 0.75 -> ACS == 0.6
        // This makes bond line width equal to ACS Guide of 0.6pt
        public const double AcsLineWidth = 0.6;

        public const double AcsLineWidthEmus = AcsLineWidth * EmusPerWordPoint;

        public const double CmlCharacterMargin = 1.25;

        public const double CsSuperscriptRaiseFactor = 0.3;

        // This character is used to replace any which have not been extracted to Arial.json
        public const char DefaultCharacter = '⊠';

        // Margins are in CML Points
        public const double DrawingMargin = 5;

        // Fixed values
        public const int EmusPerWordPoint = 12700;

        // https://www.compart.com/en/unicode/U+22A0

        // 5 is a good value to use (Use 0 to compare with AMC diagrams)

        // margin in cml pixels

        public const double LineShrinkPixels = 1.75;

        // Percentage of average (median) bond length
        // V3 == 0.2 -> ACS == 0.18
        public const double MultipleBondOffsetPercentage = 0.18;

        public const double SubscriptDropFactor = 0.75;
        public const double SubscriptScaleFactor = 0.6;
        public const double BracketOffsetPercentage = 0.2;

        // cml pixels
        // V3 == 9500 -> V3.1 [ACS] == 9144
        // This makes cml bond length of 20 equal ACS guide 0.2" (0.508cm)
        public const double EmusPerCmlPoint = 9144;
    }
}