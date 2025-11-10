// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
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
        /// Creates a ChemistryDataObject from a model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="dataType"></param>
        /// <returns></returns>
        public static ChemistryDataObject CreateFromModel(Model model, string dataType)
        {
            var dto = new ChemistryDataObject
            {
                DataType = dataType,
                Name = model.QuickName,
                Formula = model.ConciseFormula,
                MolWeight = model.MolecularWeight
            };

            if (dataType.Equals("cml"))
            {
                var cc = new CMLConverter();
                dto.Chemistry = Encoding.UTF8.GetBytes(cc.Export(model));
            }
            else
            {
                var pbc = new ProtocolBufferConverter();
                dto.Chemistry = pbc.Export(model);
            }

            // Lists of ChemistryNameDataObject for TreeView
            foreach (var property in model.GetUniqueNames())
            {
                var chemistryNameDataObject = CreateNamesFromModel(property);
                dto.Names.Add(chemistryNameDataObject);
            }

            foreach (var property in model.GetUniqueFormulae())
            {
                var chemistryNameDataObject = CreateNamesFromModel(property);
                dto.Formulae.Add(chemistryNameDataObject);
            }

            foreach (var property in model.GetUniqueCaptions())
            {
                var chemistryNameDataObject = CreateNamesFromModel(property);
                dto.Captions.Add(chemistryNameDataObject);
            }

            return dto;

            // Local Function
            ChemistryNameDataObject CreateNamesFromModel(TextualProperty textualProperty)
            {
                var nameDataObject = new ChemistryNameDataObject
                {
                    Name = textualProperty.Value
                };

                if (textualProperty.FullType.Contains(":"))
                {
                    var parts = textualProperty.FullType.Split(':');
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

        public static ChemistryObject CreateFromDto(ChemistryDataObject chemistryDto)
        {
            var obj = new ChemistryObject
            {
                Id = chemistryDto.Id,
                Chemistry = chemistryDto.Chemistry,
                Formula = chemistryDto.Formula,
                Name = chemistryDto.Name,
                MolecularWeight = chemistryDto.MolWeight,
                Tags = chemistryDto.Tags.Select(t => t.Text).ToList(),

                // Lists of ChemistryNameDataObject for TreeView
                Names = chemistryDto.Names,
                Formulae = chemistryDto.Formulae,
                Captions = chemistryDto.Captions
            };

            // List of captions and names to be shown inline in the custom UserControl ChemistryItem
            var listOfNames = chemistryDto.Captions.Select(c => c.Name).ToList();
            listOfNames.AddRange(chemistryDto.Names.Select(t => t.Name).ToList());

            obj.ChemicalNames = new List<string>();
            foreach (var name in listOfNames.Distinct())
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
