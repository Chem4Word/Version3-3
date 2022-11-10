// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Models;
using Chem4Word.Model2;
using IChem4Word.Contracts.Dto;
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
        /// <param name="data"></param>
        /// <param name="dataType"></param>
        /// <returns></returns>
        public static ChemistryDataObject CreateFromModel(Model model, byte[] data, string dataType)
        {
            var dto = new ChemistryDataObject
            {
                Chemistry = data,
                DataType = dataType,
                Name = model.QuickName,
                Formula = model.ConciseFormula,
                MolWeight = model.MolecularWeight,
            };

            // Lists of ChemistryNameDataObject for TreeView
            foreach (var property in model.GetAllNames())
            {
                var chemistryNameDataObject = CreateNamesFromModel(property);
                dto.Names.Add(chemistryNameDataObject);
            }

            foreach (var property in model.GetAllFormulae())
            {
                var chemistryNameDataObject = CreateNamesFromModel(property);
                dto.Formulae.Add(chemistryNameDataObject);
            }

            foreach (var property in model.GetAllCaptions())
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
                Cml = Encoding.UTF8.GetString(chemistryDto.Chemistry),
                Formula = chemistryDto.Formula,
                Name = chemistryDto.Name,
                MolecularWeight = chemistryDto.MolWeight,
                Tags = chemistryDto.Tags.Select(t => t.Text).ToList(),

                // List of names to be shown inline in custom UserControl ChemistryItem
                ChemicalNames = chemistryDto.Names.Select(t => t.Name).Distinct().ToList(),

                // Lists of ChemistryNameDataObject for TreeView
                Names = chemistryDto.Names,
                Formulae = chemistryDto.Formulae,
                Captions = chemistryDto.Captions
            };

            obj.Initializing = false;

            return obj;
        }
    }
}