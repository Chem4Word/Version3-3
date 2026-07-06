// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Models;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.ProtocolBuffers;
using IChem4Word.Contracts.Dto;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chem4Word.Helpers
{
    public static class DtoHelper
    {
        /// <summary>
        /// Creates a ChemistryDataTransferObject from a model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="dataType"></param>
        /// <returns></returns>
        public static ChemistryDataTransferObject CreateFromModel(Model model, string dataType)
        {
            ChemistryDataTransferObject dto = new ChemistryDataTransferObject
            {
                DataType = dataType,
                SubstanceName = model.QuickName,
                ConciseFormula = model.ConciseFormula,
                UnicodeFormula = model.UnicodeFormula,
                MolWeight = model.MolecularWeight
            };

            if (dataType.Equals("cml"))
            {
                CMLConverter cc = new CMLConverter();
                dto.Chemistry = Encoding.UTF8.GetBytes(cc.Export(model));
            }
            else
            {
                ProtocolBufferConverter pbc = new ProtocolBufferConverter();
                dto.Chemistry = pbc.Export(model);
            }

            // Lists of ChemistryNameDataObject for TreeView
            foreach (TextualProperty property in model.GetUniqueNames())
            {
                ChemistryNameDataObject chemistryNameDataObject = CreateNamesFromModel(property);
                dto.Names.Add(chemistryNameDataObject);
            }

            foreach (TextualProperty property in model.GetUniqueFormulae())
            {
                ChemistryNameDataObject chemistryNameDataObject = CreateNamesFromModel(property);
                dto.Formulae.Add(chemistryNameDataObject);
            }

            foreach (TextualProperty property in model.GetUniqueCaptions())
            {
                ChemistryNameDataObject chemistryNameDataObject = CreateNamesFromModel(property);
                dto.Captions.Add(chemistryNameDataObject);
            }

            if (!string.IsNullOrEmpty(model.SubstanceName))
            {
                dto.SubstanceName = model.SubstanceName;
            }

            if (model.SubstanceTags.Any())
            {
                foreach (string tag in model.SubstanceTags)
                {
                    dto.Tags.Add(new ChemistryTagDataObject { Text = tag });
                }
            }

            return dto;

            // Local Function
            ChemistryNameDataObject CreateNamesFromModel(TextualProperty textualProperty)
            {
                ChemistryNameDataObject nameDataObject = new ChemistryNameDataObject
                {
                    Name = textualProperty.Value
                };

                if (textualProperty.FullType.Contains(":"))
                {
                    string[] parts = textualProperty.FullType.Split(':');
                    nameDataObject.NameSpace = parts[0];
                    nameDataObject.Tag = parts[1];
                }
                else
                {
                    nameDataObject.NameSpace = "chem4word";
                    nameDataObject.Tag = textualProperty.FullType;
                }

                return nameDataObject;
            }
        }

        public static ChemistryObject CreateFromDto(ChemistryDataTransferObject chemistryDto)
        {
            ChemistryObject obj = new ChemistryObject
            {
                Id = chemistryDto.Id,
                Chemistry = chemistryDto.Chemistry,
                ConciseFormula = chemistryDto.ConciseFormula,
                UnicodeFormula = chemistryDto.UnicodeFormula,
                SubstanceName = chemistryDto.SubstanceName,
                MolecularWeight = chemistryDto.MolWeight,
                Tags = chemistryDto.Tags.Select(t => t.Text).ToList(),

                // Lists of ChemistryNameDataObject for TreeView
                Names = chemistryDto.Names,
                Formulae = chemistryDto.Formulae,
                Captions = chemistryDto.Captions
            };

            // List of captions and names to be shown inline in the custom UserControl ChemistryItem
            List<string> listOfNames = chemistryDto.Captions.Select(c => c.Name).ToList();
            listOfNames.AddRange(chemistryDto.Names.Select(t => t.Name).ToList());

            obj.ChemicalNames = new List<string>();
            foreach (string name in listOfNames.Distinct())
            {
                // Is long enough, not a special string, not a number
                if (name.Length > 3
                    && !name.ToLower().Equals("unable to calculate")
                    && !name.ToLower().Equals("not found")
                    && !name.ToLower().Equals("not requested")
                    && !decimal.TryParse(name, out _))
                {
                    obj.ChemicalNames.Add(name);
                }
            }

            if (chemistryDto.DataType.Equals("cml"))
            {
                obj.Chemistry = Encoding.UTF8.GetString(chemistryDto.Chemistry);
            }
            else
            {
                obj.Chemistry = chemistryDto.Chemistry;
            }

            obj.Initializing = false;

            return obj;
        }
    }
}
