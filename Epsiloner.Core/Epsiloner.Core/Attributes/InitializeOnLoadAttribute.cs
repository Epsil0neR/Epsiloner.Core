using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Epsiloner.Attributes
{
    /// <summary>
    /// Specifies which types should execute static constructor when assembly is loaded.
    /// Note: <see cref="Initialize"/> method must be invoked outside to execute static constructors for all existing attributes and also handle new assemblies load.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class InitializeOnLoadAttribute : Attribute
    {
        private static readonly Type AttrType = typeof(InitializeOnLoadAttribute);

        /// <inheritdoc />
        public InitializeOnLoadAttribute(Type type)
        {
            Type = type;
        }

        /// <summary>
        /// Type, which static constructor should be executed.
        /// </summary>
        public Type Type { get; }


        /// <summary>
        /// Checks all existing assemblies and runs static costructors for found assemblies.
        /// </summary>
        public static void Initialize()
        {
            //To prevent multiple event handlers, first we remove existing handler, only then we add handler to always have only 1 active handler.
            AppDomain.CurrentDomain.AssemblyLoad -= CurrentDomainOnAssemblyLoad;
            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomainOnAssemblyLoad;

            //Proceed all loaded assemblies.
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
                ProceedAssembly(assembly);
        }

        private static void CurrentDomainOnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            ProceedAssembly(args.LoadedAssembly);
        }

        private static void ProceedAssembly(Assembly assembly)
        {
            foreach (InitializeOnLoadAttribute attr in assembly.GetCustomAttributes(AttrType, false))
                RuntimeHelpers.RunClassConstructor(attr.Type.TypeHandle); //Static constructor for same type will be executed only once.
        }
    }
}

