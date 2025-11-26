// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Formula;
using System.Collections.Generic;
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
        public void CheckCalculatedFormula(int a1Charge, int a2Charge, int m1Charge, string expected)
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
        public void CheckCalculatedFormulaNested(int a1Charge, int a2Charge, int a3Charge, int m1Charge, int m2Charge, int m3Charge, string expected)
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
        public void CheckCalculatedFormulaDoubleNested()
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

        [Theory(Skip = "Not yet implemented.")]
        // Invalid strings
        [InlineData("2", 0)]
        [InlineData("Q", 0)]
        [InlineData("Not found", 0)]
        [InlineData("Any Old Rubbish.", 0)]
        [InlineData("[ . ]", 0)]
        [InlineData("Any - Old + Rubbish!", 0)]
        [InlineData("55Any+ 999Old- -Rubbish", 0)]
        // Valid strings
        [InlineData("C  6  H  6", 2)]
        [InlineData("C7H6N", 3)]
        [InlineData("C7 H7 F1", 3)]
        [InlineData("C 5 H 5 Y 1 + 2", 4)]
        [InlineData("C28H31N2O3+", 5)]
        [InlineData("C6H10O12P2-4", 5)]
        [InlineData("C20H21N7O7-2", 5)]
        [InlineData("C57H101O18S3-3", 5)]
        [InlineData("C19H26NO4+", 5)]
        [InlineData("C15H22N2O17P2-2", 6)]
        [InlineData("C 6 H 6 · C7 H7 F1", 6)]
        [InlineData("2 C 6 H 6 · C 7 H 7", 6)]
        [InlineData("C14H15BrClNO6", 6)]
        [InlineData("[C2 H6 · H1 F1] + 1", 8)]
        [InlineData("[C2H6 · HF]+", 8)]
        [InlineData("C 5 H 5 P 1 - · C 5 H 5 N 1 - 2 · C 5 H 5 O 1 + · C 5 H 5 Y 1 + 2", 19)]
        public void ParseFormula(string formula, int count)
        {
            List<FormulaPartV2> listOfParts = FormulaHelperV2.ParseFormulaIntoParts(formula);

            if (Debugger.IsAttached)
            {
                int i = 1;
                Debug.WriteLine(formula);
                foreach (FormulaPartV2 part in listOfParts)
                {
                    Debug.WriteLine($"    #{i++} {part.PartType} {part.Text} {part.Count}");
                }
            }

            Assert.Equal(count, listOfParts.Count);
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
        [ClassData(typeof(NewMoleculeData))]
        public void New_Molecule(string cmlFile, string expectedConcise, string expectedUnicode)
        {
            // Arrange
            CMLConverter mc = new CMLConverter();
            Model model = mc.Import(ResourceHelper.GetStringResource(cmlFile));

            // Act
            FormulaHelperV2 helper = new FormulaHelperV2(model);
            string actualConcise = helper.Concise();
            string actualUniCode = helper.Unicode();

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
        [ClassData(typeof(NewMoleculeCompactData))]
        public void New_Molecule_Compact(string cmlFile, string expectedConcise, string expectedUnicode)
        {
            // Arrange
            CMLConverter mc = new CMLConverter();
            Model model = mc.Import(ResourceHelper.GetStringResource(cmlFile));

            // Act
            FormulaHelperV2 helper = new FormulaHelperV2(model);
            string actualConcise = helper.Concise(compact: true);
            string actualUniCode = helper.Unicode();

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
        [ClassData(typeof(NewFirstMoleculeData))]
        public void New_FirstMolecule(string cmlFile, string expectedConcise, string expectedUnicode)
        {
            // Arrange
            CMLConverter mc = new CMLConverter();
            Model model = mc.Import(ResourceHelper.GetStringResource(cmlFile));
            Molecule molecule = model.Molecules.Values.First(m => m.Molecules.Count > 1);

            // Act
            FormulaHelperV2 modelHelper = new FormulaHelperV2(model);
            FormulaHelperV2 helper = new FormulaHelperV2(molecule);
            string conciseOfChildren = helper.Concise();
            string unicodeOfChildren = helper.Unicode();

            Debug.WriteLine($"{cmlFile}");
            Debug.WriteLine($"  Model Concise: '{modelHelper.Concise()}'");
            Debug.WriteLine($"  Model Unicode: '{modelHelper.Unicode()}'");
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

        [Theory]
        [ClassData(typeof(NewChildMoleculeData))]
        public void New_ChildMolecule(string cmlFile, string expectedConcise, string expectedUnicode)
        {
            // Arrange
            CMLConverter mc = new CMLConverter();
            Model model = mc.Import(ResourceHelper.GetStringResource(cmlFile));
            Molecule molecule = model.Molecules.Values.First(m => m.Molecules.Count > 1);

            // Act
            FormulaHelperV2 modelHelper = new FormulaHelperV2(model);
            FormulaHelperV2 helper = new FormulaHelperV2(molecule);
            string conciseOfChildren = helper.ConciseOfChildren();
            string unicodeOfChildren = helper.UnicodeOfChildren();

            Debug.WriteLine($"{cmlFile}");
            Debug.WriteLine($"  Model Concise: '{modelHelper.Concise()}'");
            Debug.WriteLine($"  Model Unicode: '{modelHelper.Unicode()}'");
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

    public class NewMoleculeData : TheoryData<string, string, string>
    {
        public NewMoleculeData()
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

    public class NewMoleculeCompactData : TheoryData<string, string, string>
    {
        public NewMoleculeCompactData()
        {
            Add("example-1.xml",
                "C3H4-4",
                "C₃H₄⁴⁻");
            Add("example-2.xml",
                "2C3H6·C3H4-2",
                "2 C₃H₆ · C₃H₄²⁻");
            Add("example-3.xml",
                "C3H5+·2C3H6",
                "C₃H₅⁺ · 2 C₃H₆");
            Add("example-4.xml",
                "C3H5+·[2C4H8·2C3H6]",
                "C₃H₅⁺ · [ 2 C₄H₈ · 2 C₃H₆ ]");
        }
    }

    public class NewFirstMoleculeData : TheoryData<string, string, string>
    {
        public NewFirstMoleculeData()
        {
            Add("example-3.xml",
                "2 C 3 H 6",
                "2 C₃H₆");
            Add("example-4.xml",
                "[ 2 C 4 H 8 · 2 C 3 H 6 ]",
                "[ 2 C₄H₈ · 2 C₃H₆ ]");
        }
    }

    public class NewChildMoleculeData : TheoryData<string, string, string>
    {
        public NewChildMoleculeData()
        {
            Add("example-3.xml",
                "2 C 3 H 6",
                "2 C₃H₆");
            Add("example-4.xml",
                "2 C 4 H 8 · 2 C 3 H 6",
                "2 C₄H₈ · 2 C₃H₆");
        }
    }
}
