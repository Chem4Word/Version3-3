// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Chem4Word.Model2.Converters.Smiles
{
    internal sealed class Parser
    {
        private readonly string _s;
        private readonly List<string> _warnings;
        private readonly ParsedSmiles _result = new ParsedSmiles();
        private readonly Dictionary<int, (int atom, char? symbol, int slot)> _openRings = new Dictionary<int, (int, char?, int)>();
        private int _i;

        private Parser(string smiles, List<string> warnings)
        {
            _s = smiles;
            _warnings = warnings;
        }

        public static ParsedSmiles Parse(string smiles, List<string> warnings)
        {
            Parser parser = new Parser(smiles, warnings);
            parser.Run();
            parser._result.FindBridges();
            return parser._result;
        }

        private void Run()
        {
            int previous = -1;
            char? pending = null;
            Stack<int> branches = new Stack<int>();

            while (_i < _s.Length)
            {
                char c = _s[_i];
                switch (c)
                {
                    case '(':
                        if (previous < 0)
                        {
                            Fail("Branch has no atom to attach to");
                        }
                        if (pending.HasValue)
                        {
                            Fail("Bond symbol must be inside the branch");
                        }
                        branches.Push(previous);
                        _i++;
                        break;

                    case ')':
                        if (branches.Count == 0)
                        {
                            Fail("Unmatched ')'");
                        }
                        if (pending.HasValue)
                        {
                            Fail("Bond symbol is not followed by an atom");
                        }
                        previous = branches.Pop();
                        _i++;
                        break;

                    case '.':
                        if (pending.HasValue)
                        {
                            Fail("Bond symbol is not followed by an atom");
                        }
                        previous = -1;
                        _i++;
                        break;

                    case '-':
                    case '=':
                    case '#':
                    case '$':
                    case ':':
                    case '/':
                    case '\\':
                        if (pending.HasValue)
                        {
                            Fail("Two bond symbols in a row");
                        }
                        pending = c;
                        _i++;
                        break;

                    case '>':
                        Fail("Reaction SMILES are not supported");
                        break;

                    default:
                        if (char.IsDigit(c) || c == '%')
                        {
                            if (previous < 0)
                            {
                                Fail("Ring closure has no atom to attach to");
                            }
                            RingClosure(previous, ReadRingNumber(), pending);
                            pending = null;
                            break;
                        }

                        SmilesAtom atom = c == '[' ? ReadBracketAtom() : ReadOrganicAtom();
                        int index = _result.Atoms.Count;
                        _result.Atoms.Add(atom);

                        if (previous >= 0)
                        {
                            AddBond(previous, index, pending);
                            atom.HasPrevious = true;
                        }
                        else if (pending.HasValue)
                        {
                            Fail("Bond symbol has no preceding atom");
                        }

                        if (atom.HydrogenCount > 0)
                        {
                            atom.Neighbours.Add(SmilesConverter.ImplicitNeighbour);
                        }

                        pending = null;
                        previous = index;
                        break;
                }
            }

            if (pending.HasValue)
            {
                Fail("Bond symbol is not followed by an atom");
            }
            if (branches.Count > 0)
            {
                Fail("Unclosed branch");
            }
            if (_openRings.Count > 0)
            {
                Fail($"Unclosed ring {string.Join(", ", _openRings.Keys)}");
            }
            if (_result.Atoms.Count == 0)
            {
                Fail("No atoms found");
            }
        }

        private void AddBond(int from, int to, char? symbol)
        {
            _result.Bonds.Add(MakeBond(from, to, symbol));
            _result.Atoms[from].Neighbours.Add(to);
            _result.Atoms[to].Neighbours.Add(from);
        }

        private SmilesBond MakeBond(int from, int to, char? symbol)
        {
            SmilesBond bond = new SmilesBond { From = from, To = to };
            switch (symbol)
            {
                case null:
                    bond.Aromatic = _result.Atoms[from].Aromatic && _result.Atoms[to].Aromatic;
                    break;

                case '=':
                    bond.Order = 2;
                    break;

                case '#':
                    bond.Order = 3;
                    break;

                case '$':
                    bond.Order = 4;
                    break;

                case ':':
                    bond.Aromatic = true;
                    break;

                case '/':
                    bond.Direction = 1;
                    break;

                case '\\':
                    bond.Direction = -1;
                    break;
            }
            return bond;
        }

        private void RingClosure(int atom, int number, char? symbol)
        {
            SmilesAtom current = _result.Atoms[atom];
            if (!_openRings.TryGetValue(number, out (int atom, char? symbol, int slot) open))
            {
                _openRings[number] = (atom, symbol, current.Neighbours.Count);
                current.Neighbours.Add(SmilesConverter.PendingRingBond);
                return;
            }

            _openRings.Remove(number);
            if (open.atom == atom)
            {
                Fail($"Ring {number} closes on the atom that opened it");
            }
            if (_result.Bonds.Any(b => b.From == open.atom && b.To == atom || b.From == atom && b.To == open.atom))
            {
                Fail($"Ring {number} duplicates an existing bond");
            }
            if (open.symbol.HasValue && symbol.HasValue && OrderKey(open.symbol.Value) != OrderKey(symbol.Value))
            {
                Fail($"Ring {number} has conflicting bond symbols");
            }

            // A / or \ reads from the atom carrying it towards the ring partner.
            SmilesBond bond = IsDirectional(symbol) && !IsDirectional(open.symbol)
                ? MakeBond(atom, open.atom, symbol)
                : MakeBond(open.atom, atom, open.symbol ?? symbol);
            _result.Bonds.Add(bond);

            _result.Atoms[open.atom].Neighbours[open.slot] = atom;
            current.Neighbours.Add(open.atom);
        }

        private static bool IsDirectional(char? symbol) => symbol == '/' || symbol == '\\';

        private static char OrderKey(char symbol) => symbol == '/' || symbol == '\\' ? '-' : symbol;

        private int ReadRingNumber()
        {
            if (_s[_i] != '%')
            {
                return _s[_i++] - '0';
            }

            _i++;
            if (_i < _s.Length && _s[_i] == '(')
            {
                _i++;
                int? value = ReadNumber();
                if (!value.HasValue || _i >= _s.Length || _s[_i] != ')')
                {
                    Fail("Malformed %(n) ring number");
                }
                _i++;
                return value.Value;
            }

            if (_i + 1 >= _s.Length || !char.IsDigit(_s[_i]) || !char.IsDigit(_s[_i + 1]))
            {
                Fail("'%' must be followed by two digits");
            }
            int number = (_s[_i] - '0') * 10 + (_s[_i + 1] - '0');
            _i += 2;
            return number;
        }

        private SmilesAtom ReadOrganicAtom()
        {
            char c = _s[_i];
            if (c == '*')
            {
                _i++;
                return new SmilesAtom { Symbol = "*" };
            }

            if (Next(1) == "Cl" || Next(1) == "Br")
            {
                string symbol = Next(1);
                _i += 2;
                return new SmilesAtom { Symbol = symbol };
            }

            if ("BCNOPSFI".IndexOf(c) >= 0)
            {
                _i++;
                return new SmilesAtom { Symbol = c.ToString() };
            }

            if ("bcnops".IndexOf(c) >= 0)
            {
                _i++;
                return new SmilesAtom { Symbol = char.ToUpperInvariant(c).ToString(), Aromatic = true };
            }

            Fail($"Unexpected character '{c}'");
            return null;
        }

        private SmilesAtom ReadBracketAtom()
        {
            _i++;
            SmilesAtom atom = new SmilesAtom { Bracket = true, Isotope = ReadNumber() };

            ReadElement(atom);

            if (Peek() == '@')
            {
                _i++;
                atom.Chirality = 1;
                if (Peek() == '@')
                {
                    _i++;
                    atom.Chirality = 2;
                }
                else if (Next(2) == "TH1" || Next(2) == "TH2")
                {
                    atom.Chirality = _s[_i + 2] - '0';
                    _i += 3;
                }
                else if (_i + 1 < _s.Length && char.IsUpper(_s[_i]) && char.IsUpper(_s[_i + 1]))
                {
                    _warnings.Add($"Stereo class @{_s.Substring(_i, 2)} is not supported and was ignored");
                    _i += 2;
                    ReadNumber();
                    atom.Chirality = 0;
                }
            }

            if (Peek() == 'H')
            {
                _i++;
                atom.HydrogenCount = ReadNumber() ?? 1;
            }

            if (Peek() == '+' || Peek() == '-')
            {
                char sign = _s[_i++];
                int? magnitude = ReadNumber();
                int charge = magnitude ?? 1;
                while (!magnitude.HasValue && Peek() == sign)
                {
                    _i++;
                    charge++;
                }
                atom.Charge = sign == '+' ? charge : -charge;
            }

            if (Peek() == ':')
            {
                // Atom class (reaction mapping); not stored in the model.
                _i++;
                if (!ReadNumber().HasValue)
                {
                    Fail("Atom class must be a number");
                }
            }

            if (Peek() != ']')
            {
                Fail("Expected ']'");
            }
            _i++;

            if (atom.Chirality != 0 && atom.HydrogenCount > 1)
            {
                _warnings.Add($"Stereo on [{atom.Symbol}H{atom.HydrogenCount}] ignored");
                atom.Chirality = 0;
            }

            return atom;
        }

        private void ReadElement(SmilesAtom atom)
        {
            char c = Peek();
            if (c == '*')
            {
                _i++;
                atom.Symbol = "*";
                return;
            }

            if (char.IsLower(c))
            {
                string two = Next(1);
                if (two == "se" || two == "as" || two == "te")
                {
                    _i += 2;
                    atom.Symbol = char.ToUpperInvariant(two[0]) + two.Substring(1);
                }
                else if ("bcnops".IndexOf(c) >= 0)
                {
                    _i++;
                    atom.Symbol = char.ToUpperInvariant(c).ToString();
                }
                else
                {
                    Fail($"Unknown aromatic symbol '{c}'");
                }
                atom.Aromatic = true;
                return;
            }

            if (char.IsUpper(c))
            {
                string two = Next(1);
                if (two != null && char.IsLower(two[1]) && IsElement(two))
                {
                    _i += 2;
                    atom.Symbol = two;
                    return;
                }
                if (IsElement(c.ToString()))
                {
                    _i++;
                    atom.Symbol = c.ToString();
                    return;
                }
            }

            Fail("Unknown element");
        }

        private static bool IsElement(string symbol) =>
            ModelGlobals.PeriodicTable.HasElement(symbol) && !SmilesConverter.PseudoElements.Contains(symbol);

        private char Peek() => _i < _s.Length ? _s[_i] : '\0';

        // The text from the current position up to and including _i + extra, or null if past the end.
        private string Next(int extra) => _i + extra < _s.Length ? _s.Substring(_i, extra + 1) : null;

        private int? ReadNumber()
        {
            int start = _i;
            while (_i < _s.Length && char.IsDigit(_s[_i]))
            {
                _i++;
            }
            return _i > start ? int.Parse(_s.Substring(start, _i - start)) : (int?)null;
        }

        private void Fail(string message)
        {
            throw new FormatException($"{message} at position {Math.Min(_i, _s.Length) + 1}");
        }
    }
}
