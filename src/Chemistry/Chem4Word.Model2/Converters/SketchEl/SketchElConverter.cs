// ---------------------------------------------------------------------------
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
using System.Linq;
using System.Text;
using System.Windows;

namespace Chem4Word.Model2.Converters.SketchEl
{
    // Spec at https://github.com/aclarkxyz/mmi_formats/blob/master/sketchel/README.md

    // SketchEl!({#atoms},{#bonds})
    // {element}={x},{y};{charge},{unpaired}[,i{implicit}][,e{explicit}][,n{mapnum}][,...]
    // ...
    // {from}-{to}={order},{type}[,...]
    // ...
    // !End

    // Useful demos
    // https://molmatinf.com/
    // https://molmatinf.com/coordinchi/

    public class SketchElConverter
    {
        public string Description => "SketchEl Molecular Format";
        public string[] Extensions => new string[] { "*.el" };

        private Dictionary<int, Guid> _atomsLookupDictionary = new Dictionary<int, Guid>();

        public Model Import(object data)
        {
            var model = new Model();

            if (data is string file)
            {
                var lines = file.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 1)
                {
                    var firstLine = lines[0];
                    var lastLine = lines.Last();
                    if (firstLine.StartsWith("SketchEl!") && lastLine.Equals("!End"))
                    {
                        var molecule = new Molecule();
                        model.AddMolecule(molecule);
                        molecule.Parent = model;

                        ProcessFile(molecule, lines);
                    }
                    else
                    {
                        model.GeneralErrors.Add("Not a SketchEl file!");
                    }
                }
                else
                {
                    model.GeneralErrors.Add("Not a SketchEl file!");
                }
            }
            else
            {
                model.GeneralErrors.Add("Input is not a string!");
            }

            model.Relabel(true);
            model.Refresh();

            return model;
        }

        public string Export(Model model)
        {
            // SketchEl Standard bond length is 1.5 Angstoms (Å)
            model.ScaleToAverageBondLength(1.5);

            var stringBuilder = new StringBuilder();

            var atoms = model.GetAllAtoms();
            var bonds = model.GetAllBonds();

            var indexOfAtoms = new Dictionary<int, Atom>();

            stringBuilder.AppendLine($"SketchEl!({atoms.Count},{bonds.Count})");

            // {element}={x},{y};{charge},{unpaired}[,i{implicit}][,e{explicit}][,n{mapnum}][,...]
            int index = 1;
            foreach (var atom in atoms)
            {
                var elem = string.Empty;

                if (atom.Element is Element element)
                {
                    elem = element.Symbol;
                }

                if (atom.Element is FunctionalGroup functionalGroup)
                {
                    elem = functionalGroup.Name;
                }

                // Invert Y as SketchEl is upside down
                var temp = $"{elem}={SafeDouble.AsCMLString(atom.Position.X)},{SafeDouble.AsCMLString(0 - atom.Position.Y)};";
                if (atom.FormalCharge.HasValue)
                {
                    temp += $"{atom.FormalCharge},0";
                }
                else
                {
                    temp += "0,0";
                }

                if (atom.IsotopeNumber.HasValue)
                {
                    temp += $",m{atom.IsotopeNumber}";
                }

                stringBuilder.AppendLine(temp);
                indexOfAtoms.Add(index++, atom);
            }

            // {from}-{to}={order},{type}[,...]
            foreach (var bond in bonds)
            {
                var startAtom = indexOfAtoms.FirstOrDefault(a => a.Value.InternalId.Equals(bond.StartAtomInternalId));
                var endAtom = indexOfAtoms.FirstOrDefault(a => a.Value.InternalId.Equals(bond.EndAtomInternalId));
                stringBuilder.AppendLine($"{startAtom.Key}-{endAtom.Key}={BondOrderStringFromBond(bond)},{StereoStringFromBond(bond)}");
            }

            stringBuilder.AppendLine("!End");

            return stringBuilder.ToString();
        }

        private static string StereoStringFromBond(Bond bond)
        {
            string stereo;

            switch (bond.Stereo)
            {
                case BondStereo.Wedge:
                    stereo = "1";
                    break;

                case BondStereo.Hatch:
                    stereo = "2";
                    break;

                case BondStereo.Indeterminate:
                    stereo = "3";
                    break;

                default:
                    stereo = "0";
                    break;
            }

            return stereo;
        }

        private static string BondOrderStringFromBond(Bond bond)
        {
            string bondOrder;

            switch (bond.Order)
            {
                case ModelConstants.OrderSingle:
                    bondOrder = "1";
                    break;

                case ModelConstants.OrderDouble:
                    bondOrder = "2";
                    break;

                case ModelConstants.OrderTriple:
                    bondOrder = "3";
                    break;

                default:
                    bondOrder = "0";
                    break;
            }

            return bondOrder;
        }

        private void ProcessFile(Molecule molecule, string[] lines)
        {
            // Transform header line
            //  From "SketchEl!(1,2)"
            //  To "1,2"
            var counts = lines[0].Replace("SketchEl!(", "").Replace(")", "");
            var parts = counts.Split(',');
            var atoms = int.Parse(parts[0]);
            var bonds = int.Parse(parts[1]);

            // Check that there are the correct number of lines
            if (lines.Length == atoms + bonds + 2)
            {
                var index = 0;

                for (var i = 0; i < atoms; i++)
                {
                    index++;
                    ExtractAtom(molecule, lines[index], index);
                }

                if (!molecule.Errors.Any())
                {
                    for (var i = 0; i < bonds; i++)
                    {
                        index++;
                        ExtractBond(molecule, lines[index], index);
                    }
                }
            }
            else
            {
                molecule.Errors.Add("Invalid number of lines in the file!");
            }
        }

        private void ExtractAtom(Molecule molecule, string line, int index)
        {
            // {element}={x},{y};{charge},{unpaired}[,i{implicit}][,e{explicit}][,n{mapnum}][,...]

            Debug.WriteLine($"Processing atom line #{index} - {line}");

            var parts = line.Split(new[] { '=', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 5)
            {
                molecule.Errors.Add($"Atom line #{index} - Does not contain enough parts");
            }
            else
            {
                // Process first 5 required parts
                var atom = new Atom();
                var element = UnEscape(parts[0]);
                // Remove formatting
                var lookup = element.Replace("|", "").Replace("{", "").Replace("}", "");
                var ok = AtomHelpers.TryParse(lookup, true, out var eb);
                if (ok)
                {
                    if (eb is Element || eb is FunctionalGroup)
                    {
                        atom.Element = eb;
                    }
                }
                else
                {
                    atom.Element = ModelGlobals.FunctionalGroupsList.FirstOrDefault(n => n.Name.Equals("??"));
                    molecule.Warnings.Add($"Atom line #{index} - Element or Functional Group {element}, replaced with '??'");
                }

                // Invert Y as SketchEl is upside down
                atom.Position = new Point(SafeDouble.Parse(parts[1]), 0 - SafeDouble.Parse(parts[2]));

                atom.FormalCharge = int.Parse(parts[3]);

                Debug.WriteLine($"We don't use unpaired {parts[4]}");

                // Process the remaining optional parts
                for (var i = 5; i < parts.Length; i++)
                {
                    switch (parts[i].Substring(0, 1).ToLower())
                    {
                        case "m":
                            atom.IsotopeNumber = int.Parse(parts[i].Substring(1));
                            break;

                        case "a":
                            if (parts[i].StartsWith("aSketchEl!"))
                            {
                                var temp = UnEscape(parts[i].Substring(1));
                                Debug.WriteLine("Abbreviation :-");
                                Debug.WriteLine(temp);
                            }
                            break;

                        default:
                            // We don't need to implement "e" "n" "x" "y" "z"
                            Debug.WriteLine(parts[i]);
                            break;
                    }
                }

                molecule.AddAtom(atom);
                atom.Parent = molecule;

                _atomsLookupDictionary.Add(_atomsLookupDictionary.Count + 1, atom.InternalId);
            }
        }

        private string Escape(string unEscaped)
        {
            var result = string.Empty;

            // Strip out <CR> leaving <LF>
            var temp = unEscaped.Replace("\r", "");
            for (var counter = 0; counter < temp.Length; counter++)
            {
                switch (temp[counter])
                {
                    case '\n':
                    case '=':
                    case ';':
                    case ',':
                        var ii = (int)temp[counter];
                        result += $@"\{ii:X4}";
                        break;

                    default:
                        result += temp[counter];
                        break;
                }
            }

            return result;
        }

        private string UnEscape(string escaped)
        {
            var result = string.Empty;

            for (var counter = 0; counter < escaped.Length; counter++)
            {
                if (escaped[counter] == '\\')
                {
                    var hex = string.Empty;
                    for (var i = 0; i < 4; i++)
                    {
                        counter++;
                        hex += escaped[counter];
                    }

                    var ii = Convert.ToUInt32($"0x{hex}", 16);
                    result += Convert.ToChar(ii);
                }
                else
                {
                    result += escaped[counter];
                }
            }

            return result;
        }

        private void ExtractBond(Molecule molecule, string line, int index)
        {
            // {from}-{to}={order},{type}[,...]

            Debug.WriteLine($"Processing bond line #{line}");
            var parts = line.Split(new[] { '=', '-', ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 4)
            {
                molecule.Errors.Add($"Bond line #{index} - Does not contain enough parts");
            }
            else
            {
                var startAtomInternalId = Guid.Empty;
                var endAtomInternalId = Guid.Empty;

                var startAtomFound = int.TryParse(parts[0], out var start)
                                     && _atomsLookupDictionary.TryGetValue(start, out startAtomInternalId);
                var endAtomFound = int.TryParse(parts[1], out var end)
                                   && _atomsLookupDictionary.TryGetValue(end, out endAtomInternalId);

                if (startAtomFound && endAtomFound)
                {
                    var bond = new Bond
                    {
                        StartAtomInternalId = startAtomInternalId,
                        EndAtomInternalId = endAtomInternalId
                    };

                    // Force bond order of 4 to zero bond
                    if (parts[2].Equals("4"))
                    {
                        bond.Order = "0";
                        molecule.Warnings.Add($"Bond line #{index} - Order set to zero - was {parts[2]}");
                    }
                    else
                    {
                        // Numeric value is automatically converted to S, D or T by the model
                        bond.Order = parts[2];
                    }

                    switch (parts[3])
                    {
                        case "0":
                            bond.Stereo = BondStereo.None;
                            break;

                        case "1":
                            bond.Stereo = BondStereo.Wedge;
                            if (!bond.Order.Equals("S"))
                            {
                                bond.Order = "S";
                                molecule.Warnings.Add($"Bond line #{index} - Order set to Single - BondStereo.Wedge does not support Order of {parts[2]}");
                            }
                            break;

                        case "2":
                            bond.Stereo = BondStereo.Hatch;
                            if (!bond.Order.Equals("S"))
                            {
                                bond.Order = "S";
                                molecule.Warnings.Add($"Bond line #{index} - Order set to Single - BondStereo.Hatch does not support Order of {parts[2]}");
                            }
                            break;

                        case "3":
                            bond.Stereo = BondStereo.Indeterminate;
                            if (!bond.Order.Equals("S"))
                            {
                                bond.Order = "S";
                                molecule.Warnings.Add($"Bond line #{index} - Order set to Single - BondStereo.Indeterminate does not support Order of {parts[2]}");
                            }
                            break;
                    }

                    for (var i = 3; i < parts.Length; i++)
                    {
                        switch (parts[i].Substring(0, 1).ToLower())
                        {
                            default:
                                // We don't need to implement "x" "y"
                                Debug.WriteLine(parts[i]);
                                break;
                        }
                    }

                    if (!bond.StartAtomInternalId.Equals(bond.EndAtomInternalId))
                    {
                        molecule.AddBond(bond);
                        bond.Parent = molecule;
                    }
                    else
                    {
                        molecule.Warnings.Add($"Bond line #{index} - Skipped as StartAtom == EndAtom");
                    }
                }
                else
                {
                    if (!startAtomFound)
                    {
                        molecule.Errors.Add($"Bond line #{index} - Start atom {parts[0]} was not found");
                    }
                    if (!endAtomFound)
                    {
                        molecule.Errors.Add($"Bond line #{index} - End atom {parts[1]} was not found");
                    }
                }
            }
        }
    }
}