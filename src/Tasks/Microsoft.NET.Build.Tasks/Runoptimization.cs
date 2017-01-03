// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.ProjectModel;

namespace Microsoft.NET.Build.Tasks
{
    /// <summary>
    /// Resolves the assemblies to be published for a .NET app.
    /// </summary>
    public class RunOptimization : TaskBase
    {
        private readonly List<ITaskItem> _assembliesToPublish = new List<ITaskItem>();

        [Required]
        public string ProjectPath { get; set; }

        [Required]
        public string AssetsFilePath { get; set; }

        [Required]
        public string TargetFramework { get; set; }
        [Required]
        public string RuntimeIdentifier { get; set; }

        public string PlatformLibraryName { get; set; }

        public ITaskItem[] PrivateAssetsPackageReferences { get; set; }
        
        /// <summary>
        /// All the assemblies to publish.
        /// </summary>
        [Output]
        public ITaskItem[] AssembliesToPublish
        {
            get { return _assembliesToPublish.ToArray(); }
        }

        protected override void ExecuteCore()
        {
            LockFile lockFile = new LockFileCache(BuildEngine4).GetLockFile(AssetsFilePath);            
            IEnumerable<string> privateAssetsPackageIds = PackageReferenceConverter.GetPackageIds(PrivateAssetsPackageReferences);
            IPackageResolver packageResolver = NuGetPackageResolver.CreateResolver(lockFile, ProjectPath);

            ProjectContext projectContext = lockFile.CreateProjectContext(
                NuGetUtils.ParseFrameworkName(TargetFramework),
                RuntimeIdentifier,
                PlatformLibraryName);
            //runtime.win7-x64.Microsoft.NETCore.Runtime.CoreCLR
            string coreclrlib = "runtime" + RuntimeIdentifier + ".Microsoft.NETCore.Runtime.CoreCLR";
            //runtime.win7-x64.microsoft.netcore.jit
            string jitlib = "runtime" + RuntimeIdentifier + ".microsoft.netcore.jit";

            LockFileTargetLibrary coreclr = projectContext.GetLibraries(coreclrlib);
            LockFileTargetLibrary jit = projectContext.GetLibraries(jitlib);

            string coreclrlibraryPath = packageResolver.GetPackageDirectory(coreclr.Name, coreclr.Version);
            string jitlibraryPath = packageResolver.GetPackageDirectory(jit.Name, jit.Version);
        }
    }
}
