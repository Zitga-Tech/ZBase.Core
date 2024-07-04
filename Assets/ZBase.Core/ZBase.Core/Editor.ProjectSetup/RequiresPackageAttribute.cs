#if UNITY_EDITOR

using System;
using System.Diagnostics.CodeAnalysis;

namespace ZBase.Core.Editor.ProjectSetup
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class RequiresPackageAttribute : Attribute
    {
        public PackageRegistry Registry { get; }

        public string Package { get; }

        public RequiresPackageAttribute(PackageRegistry registry, [NotNull] string package)
        {
            Registry = registry;
            Package = package;
        }
    }

    public enum PackageRegistry
    {
        Unity,
        OpenUPM,
    }
}

#endif
