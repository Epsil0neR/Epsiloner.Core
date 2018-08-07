using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Epsil0neR.Helpers
{
    /// <summary>
    /// Extension methods for <see cref="AppDomain"/>.
    /// </summary>
    public static class AppDomianHelpers
    {
        /// <summary>
        /// Loads all matching search pattern assemblies except assemblies.
        /// If assembly already loaded, it's OK.
        /// </summary>
        /// <param name="appDomain"></param>
        /// <param name="searchPattern"></param>
        /// <example>
        /// AppDomain.CurrentDomain.LoadAssemblies("test.*.dll");
        /// </example>
        public static void LoadAssemblies(this AppDomain appDomain, string searchPattern = null)
        {
            if (string.IsNullOrWhiteSpace(searchPattern))
                searchPattern = "*.dll";

            var ad = AppDomain.CurrentDomain;
            var path = ad.BaseDirectory;
            var matching = Directory.GetFiles(path, searchPattern);
            foreach (var file in matching)
            {
                var loaded = ad.GetAssemblies().Any(x => x.Location == file);
                if (loaded)
                    continue;

                var asm = Assembly.LoadFile(file);
                asm.DefinedTypes.ToList(); // This is hack line which loads all referenced assemblies.
            }
        }
    }
}