using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Epsil0neR.Helpers
{
    /// <summary>
    /// Extension methods for <see cref="Assembly"/>.
    /// </summary>
    public static class AssemblyHelpers
    {
        /// <summary>
        /// Gets all derived types from specified <paramref name="baseType" /> in speicified <paramref name="assembly" />.
        /// </summary>
        /// <param name="assembly">Assembly where to search derived types</param>
        /// <param name="baseType">Base type</param>
        /// <returns></returns>
        public static IEnumerable<Type> FindDerivedTypes(this Assembly assembly, Type baseType)
        {
            return assembly.GetTypes().Where(t => t != baseType && baseType.IsAssignableFrom(t)).ToList();
        }

        /// <summary>
        /// Gets all loaded to current <see cref="AppDomain"/> assemblies which has references to specified <paramref name="assembly"/>.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static IEnumerable<Assembly> GetDependentAssemblies(this Assembly assembly)
        {
            return AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetNamesOfAssembliesReferencedBy().Contains(assembly.FullName)).ToList();
        }

        /// <summary>
        /// Gets names of assemblies which is referenced by <paramref name="assembly"/>.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetNamesOfAssembliesReferencedBy(this Assembly assembly)
        {
            return assembly.GetReferencedAssemblies().Select(assemblyName => assemblyName.FullName).ToList();
        }
    }
}
