﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Model2.Enums;
using Chem4Word.Model2.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace Chem4Word.Model2.Converters.MDL
{
    public class CtabProcessor : SdFileBase
    {
        public static int doubletRadicalConvention = 4;

        private Molecule _molecule;

        private Dictionary<int, Atom> atomByNumber;
        private Dictionary<int, Bond> bondByNumber;
        private Dictionary<Atom, int> numberByAtom;

        public override SdfState ImportFromStream(StreamReader reader, Molecule molecule, out string message)
        {
            _molecule = molecule;
            atomByNumber = new Dictionary<int, Atom>();
            bondByNumber = new Dictionary<int, Bond>();
            numberByAtom = new Dictionary<Atom, int>();

            message = null;
            SdfState result = SdfState.Null;

            try
            {
                while (!reader.EndOfStream)
                {
                    MDLCounts counts = ReadCtabHeader(reader);
                    if (!string.IsNullOrEmpty(counts.Message))
                    {
                        if (counts.Message.Contains("Unsupported"))
                        {
                            result = SdfState.Unsupported;
                        }
                        else
                        {
                            result = SdfState.Error;
                        }
                        message = counts.Message;
                        break;
                    }
                    else
                    {
                        ReadAtoms(reader, counts.Atoms);
                        ReadBonds(reader, counts.Bonds);
                        result = ReadCtabFooter(reader);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                message = ex.Message;
                result = SdfState.Error;
            }

            return result;
        }

        public void ExportToStream(StreamWriter writer, List<Atom> atoms, List<Bond> bonds, string creator)
        {
            atomByNumber = new Dictionary<int, Atom>();
            bondByNumber = new Dictionary<int, Bond>();
            numberByAtom = new Dictionary<Atom, int>();

            WriteHeader(writer, atoms.Count, bonds.Count, creator);
            WriteAtoms(writer, atoms);
            WriteBonds(writer, bonds);
            WriteProperties(writer, atoms);
            writer.WriteLine(MDLConstants.M_END);
        }

        #region Reading Into Model

        private MDLCounts ReadCtabHeader(StreamReader reader)
        {
            MDLCounts result = new MDLCounts();

            // Read Line #1 - Title
            string title = SdFileConverter.GetNextLine(reader);
            if (!string.IsNullOrEmpty(title))
            {
                if (title.ToUpper().StartsWith("$MDL"))
                {
                    _molecule.Errors.Add("RGFiles are currently not supported");
                    throw new InvalidDataException("RGFiles are currently not supported");
                }
                if (title.ToUpper().StartsWith("$RXN"))
                {
                    _molecule.Errors.Add("RXNFiles are currently not supported");
                    throw new InvalidDataException("RXNFiles are currently not supported");
                }
                if (title.ToUpper().StartsWith("$RDFILE"))
                {
                    _molecule.Errors.Add("RDFiles are currently not supported");
                    throw new InvalidDataException("RDFiles are currently not supported");
                }
                if (title.ToUpper().StartsWith("<XDFILE>"))
                {
                    _molecule.Errors.Add("XDFiles are currently not supported");
                    throw new InvalidDataException("XDFiles are currently not supported");
                }
            }

            // Read and discard Line #2 - Header
            SdFileConverter.GetNextLine(reader);

            // Read and discard Line #3 - Comment
            SdFileConverter.GetNextLine(reader);

            // Read Line #4 - Counts
            string counts = SdFileConverter.GetNextLine(reader);

            if (counts.ToLower().Contains("v2000"))
            {
                try
                {
                    result.Atoms = ParseInteger(counts, 0, 3);
                    result.Bonds = ParseInteger(counts, 3, 3);
                    result.Version = GetSubString(counts, 34, 5).ToUpper();
                }
                catch (Exception ex)
                {
                    _molecule.Errors.Add($"Exception {ex.Message}");
                    result.Message = $"Exception {ex.Message}";
                    Debug.WriteLine(ex.Message);
                }
            }
            else
            {
                result.Message = "Line containing atom and bond counts not found!";
            }

            return result;
        }

        private SdfState ReadCtabFooter(StreamReader reader)
        {
            SdfState result = SdfState.Error;

            while (!reader.EndOfStream)
            {
                string line = SdFileConverter.GetNextLine(reader);
                if (!string.IsNullOrEmpty(line))
                {
                    if (line.StartsWith(MDLConstants.M_CHG)
                        || line.StartsWith(MDLConstants.M_ISO)
                        || line.StartsWith(MDLConstants.M_RAD))
                    {
                        ReadAtomPropertyLine(line);
                    }

                    if (line.Equals(MDLConstants.M_END))
                    {
                        result = SdfState.EndOfCtab;
                        break;
                    }
                }
            }

            return result;
        }

        private void ReadAtoms(StreamReader reader, int atoms)
        {
            int idx = 0;
            while (!reader.EndOfStream && idx < atoms)
            {
                string line = SdFileConverter.GetNextLine(reader);
                if (!string.IsNullOrEmpty(line))
                {
                    // create atom
                    Atom thisAtom = new Atom();

                    double x = SafeDouble.Parse(GetSubString(line, 0, 9));
                    double y = SafeDouble.Parse(GetSubString(line, 10, 9));
                    double z = SafeDouble.Parse(GetSubString(line, 20, 9));

                    // Inverting Y co-ordinate to make it the right way up.
                    thisAtom.Position = new Point(x, 0 - y);

                    // element type
                    string elType = GetSubString(line, 31, 3);
                    var ok = AtomHelpers.TryParse(elType, true, out var eb);
                    if (ok)
                    {
                        if (eb is Element || eb is FunctionalGroup)
                        {
                            thisAtom.Element = eb;
                        }
                    }
                    else
                    {
                        _molecule.Errors.Add($"{elType} at Line {SdFileConverter.LineNumber} is not a valid Element");
                        _molecule.Errors.Add($"{line}");
                    }

                    // isotope
                    int delta = ParseInteger(line, 34, 2);
                    if (delta != 0)
                    {
                        thisAtom.IsotopeNumber = delta;
                    }

                    // charge
                    int ch = ParseInteger(line, 36, 3);
                    thisAtom.FormalCharge = FormalChargeFromMolfile(ch);
                    thisAtom.DoubletRadical = DoubletRadicalFromMolfile(ch);

                    // field 3 is atom parity (a *write-only* field, so not used)
                    // int parity = ParseInteger(line, 39, 3)

                    // field 4 hydrogen count
                    // int hCount = ParseInteger(line, 42, 3)

                    // field 5 stereoCareBox
                    // int field5 = ParseInteger(line, 45, 3)

                    // field 6 valency/oxidation state
                    // int oxState = ParseInteger(line, 48, 3)

                    // field 7 H0 designator
                    // int hZero = ParseInteger(line, 51, 3)

                    // atom-atom mapping
                    int atomMap = ParseInteger(line, 60, 3);
                    if (atomMap != 0)
                    {
                        // ToDo: [DCD] What to do here ???
                    }

                    // inversion/retention flag
                    // int hZero = ParseInteger(line, 63, 3)

                    // exact change flag
                    // int hZero = ParseInteger(line, 66, 3)

                    idx++;

                    // Add atom to molecule
                    thisAtom.Id = $"a{idx}";
                    _molecule.AddAtom(thisAtom);
                    thisAtom.Parent = _molecule;
                    atomByNumber.Add(idx, thisAtom);
                }
            }
        }

        private void ReadBonds(StreamReader reader, int bonds)
        {
            int idx = 0;
            while (!reader.EndOfStream && idx < bonds)
            {
                string line = SdFileConverter.GetNextLine(reader);
                if (!string.IsNullOrEmpty(line))
                {
                    // create bond

                    int atomNumber1 = ParseInteger(line, 0, 3);
                    Atom atom1 = atomByNumber[atomNumber1];

                    if (atom1 == null)
                    {
                        _molecule.Warnings.Add($"Cannot resolve atomNumber {atomNumber1} at line {SdFileConverter.LineNumber}");
                        _molecule.Warnings.Add($"{line}");
                    }

                    int atomNumber2 = ParseInteger(line, 3, 3);
                    Atom atom2 = atomByNumber[atomNumber2];

                    if (atom2 == null)
                    {
                        _molecule.Warnings.Add($"Cannot resolve atomNumber {atomNumber2} at line {SdFileConverter.LineNumber}");
                        _molecule.Warnings.Add($"{line}");
                    }
                    idx++;

                    Bond thisBond = new Bond();
                    thisBond.StartAtomInternalId = atom1.InternalId;
                    thisBond.EndAtomInternalId = atom2.InternalId;

                    // Check for duplicate bond
                    var duplicates = (from b in _molecule.Bonds
                                      where b.StartAtomInternalId == thisBond.StartAtomInternalId && b.EndAtomInternalId == thisBond.EndAtomInternalId
                                            || b.EndAtomInternalId == thisBond.StartAtomInternalId && b.StartAtomInternalId == thisBond.EndAtomInternalId
                                      select b).Any();
                    if (duplicates)
                    {
                        _molecule.Errors.Add($"Duplicate bond at line {SdFileConverter.LineNumber}: atoms [{atomNumber1} & {atomNumber2}] are already connected by a bond.");
                        _molecule.Warnings.Add($"{line}");
                    }

                    // Bond Order
                    string order = GetSubString(line, 6, 3);
                    if (!string.IsNullOrEmpty(order))
                    {
                        int bondOrder = ParseInteger(order);
                        if (bondOrder <= 4)
                        {
                            thisBond.Order = BondOrder(bondOrder);
                        }
                        else
                        {
                            thisBond.Order = BondOrder(bondOrder);
                            _molecule.Warnings.Add($"Unsupported Bond Type of {order} at Line {SdFileConverter.LineNumber}");
                        }
                    }

                    // stereo
                    string stereo = GetSubString(line, 9, 3);
                    if (!string.IsNullOrEmpty(stereo))
                    {
                        thisBond.Stereo = BondStereoFromMolfile(ParseInteger(stereo));
                    }

                    // Check for bond with same start and end atom
                    var invalid = thisBond.StartAtomInternalId == thisBond.EndAtomInternalId;
                    if (!invalid)
                    {
                        // add bond to molecule
                        _molecule.AddBond(thisBond);
                        thisBond.Parent = _molecule;
                        bondByNumber.Add(idx, thisBond);
                    }
                    else
                    {
                        _molecule.Warnings.Add("Bond is skipped as it's invalid");
                        _molecule.Warnings.Add($"{line}");
                    }
                }
            }
        }

        #endregion Reading Into Model

        #region Exporting From Model

        private void WriteHeader(StreamWriter writer, int atoms, int bonds, string creator)
        {
            // Line 1 - Molecule Name (80)
            writer.WriteLine("");

            // Line 2
            // 01234567890123456789012345678901234567890123456789012345678901234567890123456789
            //           1         2         3         4         5         6         7
            // IIPPPPPPPPMMDDYYHHmmddSSssssssssssEEEEEEEEEEEERRRRRR
            // A2<--A8--><---A10-->A2I2<--F10.5-><---F12.5--><-I6->
            // I == User Initials
            // P == Program Name
            // MMDDYYHHmm == Date and Time
            // d == dimensional codes
            // Ss == scaling factors
            // E == Energy
            // R == registry number
            writer.WriteLine($"  Chem4Wrd{SafeDate.ToMdlHeaderTime(DateTime.UtcNow)}");

            // Line 3 - Comments (80)
            writer.WriteLine(Truncate80(creator));

            // Counts line
            // 01234567890123456789012345678901234567890123456789012345678901234567890123456789
            //           1         2         3         4         5         6         7
            // aaabbblll---cccsss------------mmm-vvvvv
            //   6  5  0     0  0              3 V2000
            // a == number of atoms
            // b == number of bonds
            // l == number of atom lists
            // c == chiral flag; 0=not chiral, 1=chiral
            // m == number of additional properties
            // v == version number
            writer.WriteLine($"{OutputMDLInt(atoms)}{OutputMDLInt(bonds)}  0     0  0              0 V2000");
        }

        private string Truncate80(string info)
        {
            string result = string.Empty;

            if (!string.IsNullOrEmpty(info))
            {
                result = info.Length > 80
                    ? info.Substring(0, 80)
                    : info;
            }

            return result;
        }

        private void WriteAtoms(StreamWriter writer, List<Atom> atoms)
        {
            int i = 0;
            foreach (var atom in atoms)
            {
                writer.WriteLine(CreateAtomLine(atom));
                numberByAtom.Add(atom, ++i);
            }
        }

        private void WriteBonds(StreamWriter writer, List<Bond> bonds)
        {
            foreach (var bond in bonds)
            {
                writer.WriteLine(CreateBondLine(bond));
            }
        }

        private string CreateAtomLine(Atom atom)
        {
            // Atoms
            // 01234567890123456789012345678901234567890123456789012345678901234567890123456789
            //           1         2         3         4         5         6         7
            // xxxxx.xxxxyyyyy.yyyyzzzzz.zzzz aaaddcccssshhhbbbvvvHHH------mmmnnneee
            // x,y,z = atom coordinates
            // a = atom symbol
            // d = mass difference; -3, -2, -1, 0, 1, 2, 3, 4 (0 if value beyond these limits)
            // c = charge; 0 = uncharged or value other than these, 1 = +3, 2 = +2, 3 = +1, 4 = doublet radical, 5 = -1, 6 = -2, 7 = -3
            // s = stereo parity; 0 = not stereo, 1 = odd, 2 = even, 3 = either or unmarked stereo center
            // h = hydrogen count +1; 1 = H0, 2 = H1, 3 = H2, 4 = H3, 5 = H4
            // b = stereo care box; 0 = ignore stereo configuration of this double bond atom, 1 = stereo configuration of double bond atom must match
            // v = valence; 0 = no marking (default) (1 to 14) = (1 to 14) 15 = zero valence
            // H = H0 designator; 0 = not specified, 1 = no H atoms allowed
            // m = atom to atom mapping
            // n = inversion/retention flag
            // e = exact change flag

            StringBuilder sb = new StringBuilder();

            // x,y,z
            double x = atom.Position.X;
            sb.Append(OutputMDLFloat(x));
            // Invert Y as MOLFile is upside down
            double y = 0 - atom.Position.Y;
            sb.Append(OutputMDLFloat(y));
            double z = 0;
            sb.Append(OutputMDLFloat(z));
            sb.Append(" ");
            // symbol
            sb.Append(FormatAtomSymbol(atom));
            // mass difference
            sb.Append(" 0");
            // charge
            sb.Append(FormatAtomCharge(atom));
            // stereo parity
            sb.Append("  0");
            //hydrogen count
            sb.Append("  0");
            // stereo care box
            sb.Append("  0");
            // valence
            sb.Append("  0");
            // H0 designator
            sb.Append("  0");
            // m = atom to atom mapping
            sb.Append("  0");
            // inversion/retention flag
            sb.Append("  0");
            // exact change flag
            sb.Append("  0");

            return sb.ToString();
        }

        private string CreateBondLine(Bond bond)
        {
            // Bonds
            // 01234567890123456789012345678901234567890123456789012345678901234567890123456789
            //           1         2         3         4         5         6         7
            // 111222tttsss---rrrccc
            // 1 == 1st atom number
            // 2 == 2nd atom number
            // t = bond type; 1 = Single, 2 = Double, 3 = Triple, 4 = Aromatic, 5 = Single or Double, 6 = Single or Aromatic, 7 = Double or Aromatic, 8 = Any
            // s = bond stereo; Single bonds: 0 = not stereo, 1 = Up, 4 = Either, 6 = Down, Double bonds: 0 = Use x-, y-, z-coords from atom block to determine cis or trans, 3 = Cis or trans (either) double bond
            // r = bond topology; 0 = Either, 1 = Ring, 2 = Chain
            // c = reacting center status; 0 = unmarked, 1 = a center, -1 = not a center, Additional: 2 = no change, 4 = bond made/broken, 8 = bond order changes 12 = 4+8 (both made/broken and changes); 5 = (4 + 1), 9 = (8 + 1), and 13 = (12 + 1) are also possible

            StringBuilder sb = new StringBuilder();

            sb.Append(OutputMDLInt(numberByAtom[bond.StartAtom]));
            sb.Append(OutputMDLInt(numberByAtom[bond.EndAtom]));
            // bond type
            sb.Append(OutputMDLInt(MdlBondType(bond.Order)));
            // bond stereo
            sb.Append(OutputMDLInt(MdlBondStereo(bond.Stereo)));
            // not used
            sb.Append("   ");
            // bond topology
            sb.Append("  0");
            // reacting center statu
            sb.Append("  0");

            return sb.ToString();
        }

        private static int MdlBondStereo(BondStereo code)
        {
            int stereo;

            switch (code)
            {
                case BondStereo.None:
                    stereo = 0;
                    break;

                case BondStereo.Wedge:
                    stereo = 1;
                    break;

                case BondStereo.Hatch:
                    stereo = 6;
                    break;

                case BondStereo.Indeterminate:
                    stereo = 4;
                    break;

                default:
                    stereo = 0;
                    break;
            }
            return stereo;
        }

        public static int MdlBondType(string bondOrder)
        {
            // bond type; 1 = Single, 2 = Double, 3 = Triple, 4 = Aromatic, 5 = Single or Double, 6 = Single or Aromatic, 7 = Double or Aromatic, 8 = Any

            int result;

            switch (bondOrder)
            {
                case Globals.OrderZero:
                case Globals.OrderOther:
                    result = 0;
                    break;

                case Globals.OrderPartial01:
                    result = 0;
                    break;

                case Globals.OrderSingle:
                    result = 1;
                    break;

                case Globals.OrderPartial12:
                    result = 0;
                    break;

                case Globals.OrderAromatic:
                    result = 4;
                    break;

                case Globals.OrderDouble:
                    result = 2;
                    break;

                case Globals.OrderPartial23:
                    result = 0;
                    break;

                case Globals.OrderTriple:
                    result = 3;
                    break;

                default:
                    result = 0;
                    break;
            }

            return result;
        }

        private string FormatAtomCharge(Atom atom)
        {
            // field 2 charge
            /*
                0 = uncharged or value other than these, 1 = +3, 2 = +2, 3 = +1,
                4 = doublet radical, 5 = -1, 6 = -2, 7 = -3
             */
            string chString = "  0";

            if (atom.FormalCharge != null)
            {
                int fCharge = atom.FormalCharge.Value;
                int mdlCharge = 0;
                if (fCharge == 0)
                {
                    mdlCharge = 0;
                }
                else if (fCharge > 0 && fCharge < 5)
                {
                    mdlCharge = 4 - fCharge;
                }
                else if (fCharge > -4)
                {
                    mdlCharge = 4 - fCharge;
                }
                chString = OutputMDLInt(mdlCharge);
            }

            return chString;
        }

        private string FormatAtomSymbol(Atom atom)
        {
            var elementType = "";
            if (atom.Element is Element element)
            {
                elementType = element.Symbol;
            }

            // Bit of a bodge, but it ensures that it can be re-loaded without exploding
            if (atom.Element is FunctionalGroup functionalGroup)
            {
                elementType = functionalGroup.Name;
                if (elementType.Length > 3)
                {
                    elementType = "*";
                }
            }

            if (string.IsNullOrEmpty(elementType))
            {
                Debugger.Break();
            }

            // Add three spaces the trim back to three characters
            return $"{elementType}   ".Substring(0, 3);
        }

        private void WriteProperties(StreamWriter writer, List<Atom> atoms)
        {
            string p = CreateAtomPropertyLines(MDLConstants.M_CHG, atoms);
            if (!string.IsNullOrEmpty(p))
            {
                writer.WriteLine(p);
            }
            p = CreateAtomPropertyLines(MDLConstants.M_ISO, atoms);
            if (!string.IsNullOrEmpty(p))
            {
                writer.WriteLine(p);
            }
            p = CreateAtomPropertyLines(MDLConstants.M_RAD, atoms);
            if (!string.IsNullOrEmpty(p))
            {
                writer.WriteLine(p);
            }
        }

        #endregion Exporting From Model

        private string CreateAtomPropertyLines(string propertyType, List<Atom> atoms)
        {
            List<int> values = new List<int>();
            List<int> atomNumbers = new List<int>();

            foreach (Atom atom in atoms)
            {
                int atomNumber = numberByAtom[atom];

                int fCharge = 0;
                if (atom.FormalCharge != null)
                {
                    fCharge = atom.FormalCharge.Value;
                }
                double isotope = 0.0;
                if (atom.IsotopeNumber != null)
                {
                    isotope = atom.IsotopeNumber.Value;
                }
                int spin = 0;
                if (atom.SpinMultiplicity != null)
                {
                    spin = atom.SpinMultiplicity.Value;
                }

                if (propertyType == MDLConstants.M_CHG && fCharge != 0)
                {
                    values.Add(atom.FormalCharge.Value);
                    atomNumbers.Add(atomNumber);
                }
                else if (propertyType == MDLConstants.M_ISO && isotope > 0.0001)
                {
                    values.Add(atom.IsotopeNumber.Value);
                    atomNumbers.Add(atomNumber);
                }
                else if (propertyType == MDLConstants.M_RAD && spin > 0.0001)
                {
                    values.Add(atom.SpinMultiplicity.Value);
                    atomNumbers.Add(atomNumber);
                }
            }

            int count = atomNumbers.Count;

            StringBuilder output = new StringBuilder();

            var lineFeedRequired = false;

            for (int i = 0; i < (float)count / 8f; i++)
            {
                int thisLineCount = (count - i * 8) > 8 ? 8 : count - i * 8;
                if (lineFeedRequired)
                {
                    output.AppendLine("");
                }
                output.Append(propertyType + "  " + thisLineCount);
                for (int j = 0; j < thisLineCount; j++)
                {
                    string atomNumber = OutputMDLInt(atomNumbers[j + i * 8]);
                    string value = OutputMDLInt(values[j + i * 8]);
                    output.Append(" " + atomNumber + " " + value);
                }

                lineFeedRequired = true;
            }

            return output.ToString().TrimEnd();
        }

        private void ReadAtomPropertyLine(String line)
        {
            string propertyType = line.Substring(0, 6);

            int nFields = ParseInteger(line, 7, 3);

            for (int i = 0; i < nFields; i++)
            {
                int startAt = 8 * i + 10;

                int atomNumber = ParseInteger(line, startAt, 3);
                int value = ParseInteger(line, startAt + 4, 3);

                // update the relevant property of the atom
                if (propertyType.Equals(MDLConstants.M_CHG))
                {
                    atomByNumber[atomNumber].FormalCharge = value;
                }
                else if (propertyType.Equals(MDLConstants.M_ISO))
                {
                    atomByNumber[atomNumber].IsotopeNumber = value;
                }
                else if (propertyType.Equals(MDLConstants.M_RAD))
                {
                    atomByNumber[atomNumber].SpinMultiplicity = value;
                }
            }
        }

        private static int FormalChargeFromMolfile(int chargemunge)
        {
            // ------------------------------------------------------------------------------------------------------------
            // 0 = uncharged or value other than these, 1 = +3, 2 = +2, 3 = +1, 4 = doublet radical, 5 = -1, 6 = -2, 7 = -3
            // ------------------------------------------------------------------------------------------------------------
            // Translates from the molfile enumerated atomic charge cum doublet radical to the model formal charge
            int formalCharge = 0;

            #region Translate from mdl to our model

            switch (chargemunge)
            {
                case 1:
                    formalCharge = 3;
                    break;

                case 2:
                    formalCharge = 2;
                    break;

                case 3:
                    formalCharge = 1;
                    break;

                case 4:
                    formalCharge = 0;
                    break;

                case 5:
                    formalCharge = -1;
                    break;

                case 6:
                    formalCharge = -2;
                    break;

                case 7:
                    formalCharge = -3;
                    break;
            }

            #endregion Translate from mdl to our model

            return formalCharge;
        }

        private static bool DoubletRadicalFromMolfile(int chargemunge)
        {
            //Translates from the molfile enumerated atomic charge cum doublet radical to the model doublet radical
            return (chargemunge == doubletRadicalConvention);
        }

        private static bool ElementExists(String element)
        {
            bool result = Globals.PeriodicTable.Elements.ContainsKey(element);
            return result;
        }

        private static string BondOrder(int molNumber)
        {
            string order = string.Empty;

            return Bond.OrderValueToOrder(molNumber);
        }

        private static string CmlStereoBond(int molNumber)
        {
            string stereo;

            if (molNumber == 1)
            {
                stereo = "W";
            }
            else if (molNumber == 6)
            {
                stereo = "H";
            }
            else
            {
                stereo = string.Empty;
            }

            return stereo;
        }

        private string GetSubString(String line, int start, int length)
        {
            string result = null;

            if (line != null && line.Length >= start + length)
            {
                result = line.Substring(start, length).Trim();
            }

            return result;
        }

        #region MDL Number Support

        private static BondStereo BondStereoFromMolfile(int molfileBondStereo)
        {
            //Translates from a molfile convention bond stereochemistry number to the enumeration used in the model
            //The opposite of BondStereoFromModel
            BondStereo modelStereo = 0;

            //MOLFILE: 0 is NOT stereo, 1 is UP,    6 is DOWN,  4 is EITHER
            //MODEL  : 0 is None,       1 is Wedge, 2 is Hatch, 3 is Indeterminate
            switch (molfileBondStereo)
            {
                case 0:
                    modelStereo = BondStereo.None;
                    break;

                case 1:
                    modelStereo = BondStereo.Wedge;
                    break;

                case 4:
                    modelStereo = BondStereo.Indeterminate;
                    break;

                case 6:
                    modelStereo = BondStereo.Hatch;
                    break;

                default:
                    modelStereo = BondStereo.None;
                    break;
            }
            return modelStereo;
        }

        private static int BondStereoFromModel(BondStereo modelBondStereo)
        {
            //Translates from the model enumerated bond stereochemistry to the molfile convention
            //The opposite of BondStereoFromMolfile
            int molfileStereo = 0;

            //MODEL  : 0 is None,       1 is Wedge, 2 is Hatch, 3 is Indeterminate
            //MOLFILE: 0 is NOT stereo, 1 is UP,    6 is DOWN,  4 is EITHER
            switch (modelBondStereo)
            {
                case BondStereo.None:
                    molfileStereo = 0;
                    break;

                case BondStereo.Wedge:
                    molfileStereo = 1;
                    break;

                case BondStereo.Hatch:
                    molfileStereo = 6;
                    break;

                case BondStereo.Indeterminate:
                    molfileStereo = 4;
                    break;

                default:
                    molfileStereo = 4;
                    break;
            }
            return molfileStereo;
        }

        private static string OutputMDLFloat(double value)
        {
            string s = SafeDouble.AsCMLString(value);
            while (s.Length < 10)
            {
                s = " " + s;
            }
            return s;
        }

        private static string OutputMDLInt(int intgr)
        {
            string s = "" + intgr;
            while (s.Length < 3)
            {
                s = " " + s;
            }
            return s;
        }

        private int ParseInteger(String line, int start, int length)
        {
            int n = 0;

            string s = GetSubString(line, start, length);
            if (!string.IsNullOrEmpty(s))
            {
                n = ParseInteger(s);
            }

            return n;
        }

        private static int ParseInteger(String s)
        {
            int i = 0;
            int l = s.Length;
            s = s.Trim();

            // trim leading zeros
            while (l > 1 && s[0] == '0')
            {
                s = s.Substring(1);
                l--;
            }

            if (!string.IsNullOrEmpty(s))
            {
                i = int.Parse(s);
            }

            return i;
        }

        #endregion MDL Number Support
    }
}