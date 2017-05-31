// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Globalization;
using NuGet.ProjectModel;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Microsoft.NET.Build.Tasks
{
    /// <summary>
    /// Resolves the assemblies to be published for a .NET app.
    /// </summary>
    public class FilterResolvedFiles : TaskBase
    {
        private readonly List<ITaskItem> _assembliesToPublish = new List<ITaskItem>();
        private readonly List<ITaskItem> _packagesResolved = new List<ITaskItem>();

        [Required]
        public string AssetsFilePath { get; set; }

        [Required]
        public ITaskItem[] ResolvedFiles { get; set; }

        [Required]
        public ITaskItem[] PackagesToPrune { get; set; }

        [Required]
        public string TargetFramework { get; set; }

        public string RuntimeIdentifier { get; set; }

        public string PlatformLibraryName { get; set; }

        public bool IsSelfContained { get; set; }

        /// <summary>
        /// All the assemblies to publish.
        /// </summary>
        [Output]
        public ITaskItem[] AssembliesToPublish
        {
            get { return _assembliesToPublish.ToArray(); }
        }
        [Output]
        public ITaskItem[] PublishedPackges
        {
            get { return _packagesResolved.ToArray(); }
        }

        protected override void ExecuteCore()
        {
            var lockFileCache = new LockFileCache(BuildEngine4);
            LockFile lockFile = lockFileCache.GetLockFile(AssetsFilePath);

            ProjectContext projectContext = lockFile.CreateProjectContext(
                NuGetUtils.ParseFrameworkName(TargetFramework),
                RuntimeIdentifier,
                PlatformLibraryName,
                IsSelfContained);

            var packageClosure =  new HashSet<PackageIdentity>();

            foreach ( var pakageItem in PackagesToPrune)
            {
                var pkgName = pakageItem.ItemSpec;
                if (!string.IsNullOrEmpty(pkgName))
                {
                    packageClosure.UnionWith(projectContext.GetTransitiveList(pkgName));
                }
            }

            var packagesToPublish = new HashSet<PackageIdentity>();
            foreach (var resolvedFile in ResolvedFiles)
            {
                var pkgName = resolvedFile.GetMetadata(MetadataKeys.PackageName);
                var pkgVersion = resolvedFile.GetMetadata(MetadataKeys.PackageVersion);

                if (!string.IsNullOrEmpty(pkgName) && !string.IsNullOrEmpty(pkgVersion))
                {
                    var resolvedPkg = new PackageIdentity(pkgName, NuGetVersion.Parse(pkgVersion));
                    if (!packageClosure.Contains(resolvedPkg))
                    {
                        _assembliesToPublish.Add(resolvedFile);
                        packagesToPublish.Add(resolvedPkg);
                    }
                }
            }

            foreach (var resolvedPkg in packagesToPublish)
            {
                TaskItem item = new TaskItem(resolvedPkg.Id);
                item.SetMetadata("Version", resolvedPkg.Version.ToString());
                _packagesResolved.Add(item);
            }
        }
    }
}
