#if UNITY_EDITOR

using ZBase.Core.Editor.ProjectSetup;

[assembly: RequiresPackage(PackageRegistry.Unity, "com.unity.collections")]
[assembly: RequiresPackage(PackageRegistry.Unity, "com.unity.editorcoroutines")]
[assembly: RequiresPackage(PackageRegistry.Unity, "com.unity.ide.rider")]
[assembly: RequiresPackage(PackageRegistry.Unity, "com.unity.ide.visualstudio")]
[assembly: RequiresPackage(PackageRegistry.Unity, "com.unity.logging")]
[assembly: RequiresPackage(PackageRegistry.Unity, "com.unity.mathematics")]

[assembly: RequiresPackage(PackageRegistry.OpenUPM, "com.annulusgames.unity-codegen")]
[assembly: RequiresPackage(PackageRegistry.OpenUPM, "com.cysharp.unitask")]
[assembly: RequiresPackage(PackageRegistry.OpenUPM, "com.draconware-dev.span-extensions.net.unity")]
[assembly: RequiresPackage(PackageRegistry.OpenUPM, "com.gilzoide.easy-project-settings")]
[assembly: RequiresPackage(PackageRegistry.OpenUPM, "com.zbase.foundation")]
[assembly: RequiresPackage(PackageRegistry.OpenUPM, "com.zbase.foundation.aliasing")]
[assembly: RequiresPackage(PackageRegistry.OpenUPM, "com.zbase.foundation.enum-extensions")]
[assembly: RequiresPackage(PackageRegistry.OpenUPM, "org.nuget.system.runtime.compilerservices.unsafe")]

#endif
