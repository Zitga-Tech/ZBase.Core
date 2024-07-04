#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0051 // Remove unused private members

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ZBase.Core.Editor.ProjectSetup
{
    internal sealed class ProjectSetupWindow : OdinEditorWindow
    {
        [MenuItem("ZBase Core/Project Setup")]
        private static void OpenWindow()
        {
            var window = GetWindow<ProjectSetupWindow>();
            window.titleContent = new GUIContent("Project Setup");
            window.ShowPopup();
        }

        private ListRequest _listRequest;

        protected override void Initialize()
        {
            RefreshPackages();
        }

        [TitleGroup("Packages", GroupID = "Packages", Order = 0, Alignment = TitleAlignments.Centered)]
        [VerticalGroup("Packages/General", PaddingBottom = 5), PropertyOrder(0)]
        [ButtonGroup("Packages/General/Buttons")]
        [Button(size: ButtonSizes.Medium)]
        private void RefreshPackages()
        {
            _listRequest = Client.List(true, false);

            EditorUtility.DisplayProgressBar("Refreshing", "Fetching package list...", 0);
            EditorApplication.update -= OnRefresh;
            EditorApplication.update += OnRefresh;
        }

        [ButtonGroup("Packages/General/Buttons")]
        [Button("Packages Folder", ButtonSizes.Medium)]
        private void OpenPackagesFolder()
        {
            Process.Start(PackagesFolderFullPath);
        }

        [ButtonGroup("Packages/General/Buttons")]
        [Button("Package Manager", ButtonSizes.Medium)]
        private void OpenPackageManager()
        {
            UnityEditor.PackageManager.UI.Window.Open("");
        }

        public string PackagesFolderFullPath
            => Path.GetFullPath(Path.Combine(Application.dataPath, "../Packages"));

        [TabGroup("Packages/Tabs", "OpenUPM", Order = 1)]
        [ShowInInspector, HideLabel]
        [TableList(ShowPaging = false, AlwaysExpanded = true)]
        private List<PackageInfo> _openUPMPackages = new();

        [TabGroup("Packages/Tabs", "Tgz")]
        [ShowInInspector, HideLabel]
        [TableList(ShowPaging = false, AlwaysExpanded = true, IsReadOnly = true)]
        private List<TarballInfo> _tarballPackages = new();

        [TabGroup("Packages/Tabs", "Unity")]
        [ShowInInspector, HideLabel]
        [TableList(ShowPaging = false, AlwaysExpanded = true)]
        private List<PackageInfo> _unityPackages = new();

        private void OnRefresh()
        {
            if (_listRequest.IsCompleted == false)
            {
                EditorUtility.DisplayProgressBar("Refreshing", "Fetching package list...", 0);
                return;
            }

            EditorApplication.update -= OnRefresh;
            EditorUtility.ClearProgressBar();

            var unityPackages = new SortedSet<PackageInfo>();
            var openUPMPackages = new SortedSet<PackageInfo>();
            var tarballPackages = new SortedSet<TarballInfo>();
            var packageCollection = _listRequest.Result;

            foreach (var package in packageCollection)
            {
                switch (package.source)
                {
                    case PackageSource.LocalTarball:
                    {
                        tarballPackages.Add(new() {
                            packageName = package.name,
                            state = true,
                        });
                        break;
                    }

                    case PackageSource.Registry:
                    {
                        var registryName = package.registry.name;

                        if (string.IsNullOrEmpty(registryName) == false
                            && string.Equals(registryName, "package.openupm.com")
                        )
                        {
                            openUPMPackages.Add(new() {
                                packageName = package.name,
                                state = true,
                            });
                        }
                        else
                        {
                            unityPackages.Add(new() {
                                packageName = package.name,
                                state = true,
                            });
                        }
                        break;
                    }
                }
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies().AsSpan();
            var assembliesLength = assemblies.Length;

            for (var i = 0; i < assembliesLength; i++)
            {
                var assembly = assemblies[i];
                var attribs = assembly.GetCustomAttributes<RequiresPackageAttribute>();

                foreach (var attrib in attribs)
                {
                    switch (attrib.Registry)
                    {
                        case PackageRegistry.Unity:
                        {
                            Add(unityPackages, new PackageInfo() {
                                packageName = attrib.Package,
                                required = true,
                            });
                            break;
                        }

                        case PackageRegistry.OpenUPM:
                        {
                            Add(openUPMPackages, new PackageInfo() {
                                packageName = attrib.Package,
                                required = true,
                            });
                            break;
                        }
                    }
                }
            }

            var tarballs = Directory.GetFiles(PackagesFolderFullPath, "*.tgz").AsSpan();
            var tarballsLength = tarballs.Length;

            for (var i = 0; i < tarballsLength; i++)
            {
                var tarball = tarballs[i];
                var fileName = Path.GetFileNameWithoutExtension(tarball);
                string packageName;

                var semverIndex = fileName.LastIndexOf('-');

                if (semverIndex >= 0 && (semverIndex + 1) < fileName.Length)
                {
                    var semver = fileName[(semverIndex + 1)..];
                    
                    if (Version.TryParse(semver, out _))
                    {
                        packageName = fileName[..semverIndex];
                    }
                    else
                    {
                        packageName = fileName;
                    }
                }
                else
                {
                    packageName = fileName;
                }

                var package = new TarballInfo() {
                    packageName = packageName,
                    fileName = fileName,
                };

                if (tarballPackages.Contains(package))
                {
                    package.state = tarballPackages.Remove(package);
                }

                tarballPackages.Add(package);
            }

            _unityPackages.Clear();
            _openUPMPackages.Clear();
            _tarballPackages.Clear();

            _unityPackages.AddRange(unityPackages);
            _openUPMPackages.AddRange(openUPMPackages);
            _tarballPackages.AddRange(tarballPackages);

            static void Add(SortedSet<PackageInfo> packages, PackageInfo package)
            {
                if (packages.Contains(package))
                {
                    package.state = true;
                    packages.Remove(package);
                }

                packages.Add(package);
            }
        }

        private abstract class PackageInfoBase
        {
            [TableColumnWidth(80, false)]
            [ReadOnly, PropertyOrder(9)]
            public bool required;

            [TableColumnWidth(80, false)]
            [CustomValueDrawer(nameof(DrawInstalled))]
            [PropertyOrder(10)]
            public bool state;

            [HideInInspector]
            public PackageAction action;

            private bool CanShowNoneAddAction => state == false && action == PackageAction.Add;

            [TableColumnWidth(100, false)]
            [ButtonGroup("Action"), Button("+"), Tooltip("Add"), GUIColor("green")]
            [PropertyOrder(20)]
            [ShowIf(nameof(CanShowNoneAddAction), animate: false)]
            private void NoneAddAction()
            {
                action = PackageAction.None;
            }

            private bool CanShowAddAction => state == false && action == PackageAction.None;

            [ButtonGroup("Action"), Button("+"), Tooltip("Add")]
            [PropertyOrder(20)]
            [ShowIf(nameof(CanShowAddAction), animate: false)]
            private void AddAction()
            {
                action = PackageAction.Add;
            }

            private bool CanShowNoneRemoveAction => state && action == PackageAction.Remove;

            [ButtonGroup("Action"), Button("-"), Tooltip("Remove"), GUIColor("red")]
            [PropertyOrder(20)]
            [ShowIf(nameof(CanShowNoneRemoveAction), animate: false)]
            private void NoneRemoveAction()
            {
                action = PackageAction.None;
            }

            private bool CanShowRemoveAction => state && action == PackageAction.None;

            [ButtonGroup("Action"), Button("-"), Tooltip("Remove")]
            [PropertyOrder(20)]
            [ShowIf(nameof(CanShowRemoveAction), animate: false)]
            private void RemoveAction()
            {
                action = PackageAction.Remove;
            }

            private bool DrawInstalled(bool value)
            {
                GUI.enabled = !value;
                EditorGUILayout.LabelField(value ? "Installed" : "Not Installed");
                GUI.enabled = true;
                return value;
            }
        }

        [Serializable]
        private sealed class PackageInfo : PackageInfoBase, IEquatable<PackageInfo>, IComparable<PackageInfo>
        {
            [PropertyOrder(0), CustomValueDrawer(nameof(DrawPackageName))]
            public string packageName;

            public bool IsReadOnly => required || state;

            public int CompareTo(PackageInfo other)
                => Comparer<string>.Default.Compare(packageName, other.packageName);

            public bool Equals(PackageInfo other)
                => EqualityComparer<string>.Default.Equals(packageName, other.packageName);

            public override bool Equals(object obj)
                => obj is PackageInfo other && Equals(other);

            public override int GetHashCode()
                => HashCode.Combine(packageName);

            private string DrawPackageName(string value)
            {
                if (IsReadOnly)
                {
                    EditorGUILayout.TextField(value);
                    return value;
                }

                return EditorGUILayout.TextField(value);
            }
        }

        private sealed class TarballInfo : PackageInfoBase, IEquatable<TarballInfo>, IComparable<TarballInfo>
        {
            [PropertyOrder(0), CustomValueDrawer(nameof(DrawName))]
            public string packageName;

            [PropertyOrder(1), CustomValueDrawer(nameof(DrawName))]
            public string fileName;

            public int CompareTo(TarballInfo other)
                => Comparer<string>.Default.Compare(packageName, other.packageName);

            public bool Equals(TarballInfo other)
                => EqualityComparer<string>.Default.Equals(packageName, other.packageName);

            public override bool Equals(object obj)
                => obj is TarballInfo other && Equals(other);

            public override int GetHashCode()
                => HashCode.Combine(packageName);

            private string DrawName(string value)
            {
                EditorGUILayout.TextField(value);
                return value;
            }
        }

        private enum PackageAction : byte
        {
            [InspectorName("•")] None = 0,
            [InspectorName("+")] Add,
            [InspectorName("-")] Remove,
        }
    }
}
