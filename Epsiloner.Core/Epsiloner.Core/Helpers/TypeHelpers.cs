using System;
using System.Collections.Generic;
using System.Linq;

namespace Epsiloner.Helpers
{
    /// <summary>
    /// Extension methods for <see cref="Type"/>.
    /// </summary>
    public static class TypeHelpers
    {
        /// <summary>
        /// Gets a list of interfaces which implments specified type and not it's base type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEnumerable<Type> GetDirectlyImplementedInterfaces(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            IEnumerable<Type> interfaces = type.GetInterfaces();

            if (type.BaseType == null)
                return interfaces;

            var baseInterfaces = type.BaseType.GetInterfaces();
            return interfaces.Where(x => !baseInterfaces.Contains(x)).ToList();
        }
    }
}
