using Chem4Word.Core.Helpers;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Benchmarks
{
    public static class Common
    {
        public static Model LoadCml(string cmlFile)
        {
            string resource = ResourceHelper.GetStringResource(Assembly.GetExecutingAssembly(), cmlFile);
            if (!string.IsNullOrEmpty(resource))
            {
                return new CMLConverter().Import(resource);
            }

            return new Model();
        }

        public static List<string> GetPaths(Model model)
        {
            List<string> paths = new List<string>();

            paths.AddRange(model.GetAllAtoms().Select(a => a.Path));
            paths.AddRange(model.GetAllBonds().Select(a => a.Path));
            paths.AddRange(model.GetAllMolecules().Select(a => a.Path));

            Random rng = new Random();
            int n = paths.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (paths[n], paths[k]) = (paths[k], paths[n]);
            }

            return paths;
        }
    }
}
