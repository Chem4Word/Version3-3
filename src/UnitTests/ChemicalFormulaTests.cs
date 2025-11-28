// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Formula;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace Chem4WordTests
{
    public class ChemicalFormulaTests
    {
        // https://andrewlock.net/creating-strongly-typed-xunit-theory-test-data-with-theorydata/
        // https://hamidmosalla.com/2020/04/05/xunit-part-8-using-theorydata-instead-of-memberdata-and-classdata/

        [Theory]
        [InlineData(0, 0, 0, "C 2 H 5 F")]
        [InlineData(1, 0, 0, "C 2 H 4 F +")]
        [InlineData(0, -1, 0, "C 2 H 4 F -")]
        [InlineData(1, 0, -2, "C 2 H 4 F -")]
        [InlineData(1, 1, 0, "C 2 H 3 F + 2")]
        [InlineData(-1, -1, 0, "C 2 H 3 F - 2")]
        [InlineData(1, 1, -1, "C 2 H 3 F +")]
        [InlineData(1, 1, -2, "C 2 H 3 F")]
        public void CalculatedFormula(int a1Charge, int a2Charge, int m1Charge, string expected)
        {
            Model model = TestHelpers.CreateSimpleMolecule();
            Atom a1 = model.GetAllAtoms().First(a => a.Id.Equals("a1")); // C
            Atom a2 = model.GetAllAtoms().First(a => a.Id.Equals("a2")); // F
            Molecule m1 = model.GetAllMolecules().First(m => m.Id.Equals("m1"));

            if (a1Charge != 0)
            {
                a1.FormalCharge = a1Charge;
            }
            if (a2Charge != 0)
            {
                a2.FormalCharge = a2Charge;
            }
            if (m1Charge != 0)
            {
                m1.FormalCharge = m1Charge;
            }

            Assert.Equal(expected, model.ConciseFormula);
        }

        [Theory]
        [InlineData(0, 0, 0, 0, 0, 0, "[ C 2 H 6 · H F ]")]
        [InlineData(1, 0, 0, 0, 0, 0, "[ C 2 H 5 + · H F ]")]
        [InlineData(1, 1, 0, 0, 0, 0, "[ C 2 H 4 + 2 · H F ]")]
        [InlineData(1, 1, 1, 0, 0, 0, "[ C 2 H 4 + 2 · H 2 F + ]")]
        [InlineData(0, -1, 0, 0, 0, 0, "[ C 2 H 5 - · H F ]")]
        [InlineData(0, 0, 0, 1, 0, 0, "[ C 2 H 6 · H F ] +")]
        [InlineData(0, 0, 0, 0, 2, 0, "[ C 2 H 6 + 2 · H F ]")]
        [InlineData(0, 0, 0, 0, 0, -1, "[ C 2 H 6 · H F - ]")]
        public void CalculatedFormulaNested(int a1Charge, int a2Charge, int a3Charge, int m1Charge, int m2Charge, int m3Charge, string expected)
        {
            Model model = TestHelpers.CreateNestedMolecule();

            Atom a1 = model.GetAllAtoms().First(a => a.Id.Equals("a1"));     // C
            Atom a2 = model.GetAllAtoms().First(a => a.Id.Equals("a2"));     // C
            Atom a3 = model.GetAllAtoms().First(a => a.Id.Equals("a3"));     // F
            Molecule m1 = model.GetAllMolecules().First(m => m.Id.Equals("m1")); // Parent
            Molecule m2 = model.GetAllMolecules().First(m => m.Id.Equals("m2")); // C-C child
            Molecule m3 = model.GetAllMolecules().First(m => m.Id.Equals("m3")); // F child

            if (a1Charge != 0)
            {
                a1.FormalCharge = a1Charge;
            }
            if (a2Charge != 0)
            {
                a2.FormalCharge = a2Charge;
            }
            if (a3Charge != 0)
            {
                a3.FormalCharge = a3Charge;
            }
            if (m1Charge != 0)
            {
                m1.FormalCharge = m1Charge;
            }
            if (m2Charge != 0)
            {
                m2.FormalCharge = m2Charge;
            }
            if (m3Charge != 0)
            {
                m3.FormalCharge = m3Charge;
            }

            Assert.Equal(expected, model.ConciseFormula);
        }

        [Fact]
        public void CalculatedFormulaDoubleNested()
        {
            Model model = TestHelpers.CreateDoubleNestedMolecule();

            string expectedConcise = "[ [ H 3 P · H 2 O ] · [ H 3 N · Y ] ]";
            string expectedUnicode = "[ [ H₃P · H₂O ] · [ H₃N · Y ] ]";

            // Act
            string actualConcise = model.ConciseFormula;
            string actualUnicode = model.UnicodeFormula;

            if (Debugger.IsAttached)
            {
                Debug.WriteLine($"Expected Concise {expectedConcise}");
                Debug.WriteLine($"Actual   Concise {actualConcise}");
                Debug.WriteLine($"Expected Unicode {expectedUnicode}");
                Debug.WriteLine($"Actual   Unicode {actualUnicode}");

                Debugger.Break();
            }
            else
            {
                // Assert(ions)
                Assert.Equal(expectedConcise, actualConcise);
                Assert.Equal(expectedUnicode, actualUnicode);
            }
        }

        [Theory]
        [InlineData("2")]
        [InlineData("Q")]
        [InlineData("Not found")]
        [InlineData("Any Old Rubbish.")]
        [InlineData("[ . ]")]
        [InlineData("Any - Old + Rubbish!")]
        public void ParseFormula_ExpectSame(string formula)
        {
            // Arrange

            // Act

            string result = FormulaHelper.ToUnicode(formula);
            if (Debugger.IsAttached)
            {
                Debug.WriteLine($"Formula:  '{formula}'");
                Debug.WriteLine($"Actual:   '{result}'");

                if (!result.Equals(formula))

                {
                    Debugger.Break();
                }
            }
            else
            {
                // Assert(ions)
                Assert.Equal(formula, result);
            }
        }

        [Theory]
        [InlineData("[C2 H6 · H1 F1] + 1", "[C₂H₆ · HF]⁺")]
        [InlineData("[C2H6 · HF]+", "[C₂H₆ · HF]⁺")]
        [InlineData("2 C 6 H 6 · C 7 H 7", "2C₆H₆ · C₇H₇")]
        [InlineData("C  6  H  6", "C₆H₆")]
        [InlineData("C 5 H 5 P 1 - · C 5 H 5 N 1 - 2 · C 5 H 5 O 1 + · C 5 H 5 Y 1 + 2", "C₅H₅P⁻ · C₅H₅N²⁻ · C₅H₅O⁺ · C₅H₅Y²⁺")]
        [InlineData("C 5 H 5 Y 1 + 2", "C₅H₅Y²⁺")]
        [InlineData("C 6 H 6 · C7 H7 F1", "C₆H₆ · C₇H₇F")]
        [InlineData("C14H15BrClNO6", "C₁₄H₁₅BrClNO₆")]
        [InlineData("C15H22N2O17P2-2", "C₁₅H₂₂N₂O₁₇P₂²⁻")]
        [InlineData("C19H26NO4+", "C₁₉H₂₆NO₄⁺")]
        [InlineData("C20H21N7O7-2", "C₂₀H₂₁N₇O₇²⁻")]
        [InlineData("C28H31N2O3+", "C₂₈H₃₁N₂O₃⁺")]
        [InlineData("C57H101O18S3-3", "C₅₇H₁₀₁O₁₈S₃³⁻")]
        [InlineData("C6H10O12P2-4", "C₆H₁₀O₁₂P₂⁴⁻")]
        [InlineData("C7 H7 F1", "C₇H₇F")]
        [InlineData("C7H6N", "C₇H₆N")]
        public void ParseFormula_ExpectUnicode(string formula, string expected)
        {
            // Arrange

            // Act
            string result = FormulaHelper.ToUnicode(formula);
            if (Debugger.IsAttached)
            {
                Debug.WriteLine($"Formula:  '{formula}'");
                Debug.WriteLine($"Expected: '{expected}'");
                Debug.WriteLine($"Actual:   '{result}'");

                if (!result.Equals(expected))

                {
                    Debugger.Break();
                }
            }
            else
            {
                // Assert(ions)
                Assert.Equal(expected, result);
            }
        }

        [Fact]
        public void MultiLevel_Nested()
        {
            // Arrange
            CMLConverter mc = new CMLConverter();
            Model model = mc.Import(ResourceHelper.GetStringResource("Multi-Level-Nested.xml"));

            string expectedConcise = "2 2 C 6 H 6 · 2 C 4 H 8";
            string expectedUnicode = "2 2 C₆H₆ · 2 C₄H₈";

            // Act
            string actualConcise = model.ConciseFormula;
            string actualUnicode = model.UnicodeFormula;

            if (Debugger.IsAttached)
            {
                Debug.WriteLine($"Expected Concise {expectedConcise}");
                Debug.WriteLine($"Actual   Concise {actualConcise}");
                Debug.WriteLine($"Expected Unicode {expectedUnicode}");
                Debug.WriteLine($"Actual   Unicode {actualUnicode}");

                Debugger.Break();
            }
            else
            {
                // Assert(ions)
                Assert.Equal(expectedConcise, actualConcise);
                Assert.Equal(expectedUnicode, actualUnicode);
            }
        }

        [Theory]
        [ClassData(typeof(BasicMoleculeScenarios))]
        public void BasicMolecule(string cmlFile, string expectedConcise, string expectedUnicode)
        {
            // Arrange
            CMLConverter mc = new CMLConverter();
            Model model = mc.Import(ResourceHelper.GetStringResource(cmlFile));

            // Act
            string actualConcise = model.ConciseFormula;
            string actualUniCode = model.UnicodeFormula;

            Debug.WriteLine($"{cmlFile}");
            Debug.WriteLine($"  Expected Concise: '{expectedConcise}'");
            Debug.WriteLine($"  Actual   Concise: '{actualConcise}'");
            Debug.WriteLine($"  Expected Unicode: '{expectedUnicode}'");
            Debug.WriteLine($"  Actual   Unicode: '{actualUniCode}'");

            Debugger.Break();

            // Assert(ions)
            if (!Debugger.IsAttached)
            {
                Assert.Equal(expectedConcise, actualConcise);
                Assert.Equal(expectedUnicode, actualUniCode);
            }
        }

        [Theory]
        [ClassData(typeof(CompactConciseFormulaScenarios))]
        public void CompactConciseFormula(string cmlFile, string expectedConcise)
        {
            // Arrange
            CMLConverter mc = new CMLConverter();
            Model model = mc.Import(ResourceHelper.GetStringResource(cmlFile));

            // Act
            FormulaHelper helper = new FormulaHelper(model);
            string actualConcise = helper.Concise(compact: true);

            Debug.WriteLine($"{cmlFile}");
            Debug.WriteLine($"  Expected Concise: '{expectedConcise}'");
            Debug.WriteLine($"  Actual   Concise: '{actualConcise}'");

            Debugger.Break();

            // Assert(ions)
            if (!Debugger.IsAttached)
            {
                Assert.Equal(expectedConcise, actualConcise);
            }
        }

        [Theory]
        [ClassData(typeof(FirstMoleculeScenarios))]
        public void FirstMolecule(string cmlFile, string expectedConcise, string expectedUnicode)
        {
            // Arrange
            CMLConverter mc = new CMLConverter();
            Model model = mc.Import(ResourceHelper.GetStringResource(cmlFile));
            Molecule molecule = model.Molecules.Values.First(m => m.Molecules.Count > 1);

            // Act
            string conciseOfChildren = molecule.ConciseFormula;
            string unicodeOfChildren = molecule.UnicodeFormula;

            Debug.WriteLine($"{cmlFile}");
            Debug.WriteLine($"  Model Concise: '{model.ConciseFormula}'");
            Debug.WriteLine($"  Model Unicode: '{model.UnicodeFormula}'");
            Debug.WriteLine($"  Expected Concise: '{expectedConcise}'");
            Debug.WriteLine($"  Actual   Concise: '{conciseOfChildren}'");
            Debug.WriteLine($"  Expected Unicode: '{expectedUnicode}'");
            Debug.WriteLine($"  Actual   Unicode: '{unicodeOfChildren}'");

            Debugger.Break();

            // Assert(ions)
            if (!Debugger.IsAttached)
            {
                Assert.Equal(expectedConcise, conciseOfChildren);
                Assert.Equal(expectedUnicode, unicodeOfChildren);
            }
        }
    }

    public class BasicMoleculeScenarios : TheoryData<string, string, string>
    {
        public BasicMoleculeScenarios()
        {
            Add("example-1.xml",
                "C 3 H 4 - 4",
                "C₃H₄⁴⁻");
            Add("example-2.xml",
                "2 C 3 H 6 · C 3 H 4 - 2",
                "2 C₃H₆ · C₃H₄²⁻");
            Add("example-3.xml",
                "C 3 H 5 + · 2 C 3 H 6",
                "C₃H₅⁺ · 2 C₃H₆");
            Add("example-4.xml",
                "C 3 H 5 + · [ 2 C 4 H 8 · 2 C 3 H 6 ]",
                "C₃H₅⁺ · [ 2 C₄H₈ · 2 C₃H₆ ]");
        }
    }

    public class CompactConciseFormulaScenarios : TheoryData<string, string>
    {
        public CompactConciseFormulaScenarios()
        {
            Add("example-1.xml", "C3H4-4");
            Add("example-2.xml", "2C3H6·C3H4-2");
            Add("example-3.xml", "C3H5+·2C3H6");
            Add("example-4.xml", "C3H5+·[2C4H8·2C3H6]");
        }
    }

    public class FirstMoleculeScenarios : TheoryData<string, string, string>
    {
        public FirstMoleculeScenarios()
        {
            Add("example-3.xml",
                "2 C 3 H 6",
                "2 C₃H₆");
            Add("example-4.xml",
                "[ 2 C 4 H 8 · 2 C 3 H 6 ]",
                "[ 2 C₄H₈ · 2 C₃H₆ ]");
        }
    }
}
