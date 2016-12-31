// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using NuGet.Frameworks;
using NuGet.ProjectModel;

namespace Microsoft.NET.Build.Tasks
{
    public class PublishAssembliesResolver
    {
        private readonly IPackageResolver _packageResolver;
        private IEnumerable<string> _privateAssetPackageIds;
        private bool _preserveCacheLayout;

        public PublishAssembliesResolver(IPackageResolver packageResolver)
        {
            _packageResolver = packageResolver;
        }

        public PublishAssembliesResolver WithPrivateAssets(IEnumerable<string> privateAssetPackageIds)
        {
            _privateAssetPackageIds = privateAssetPackageIds;
            return this;
        }
        public PublishAssembliesResolver PreserveCacheLayout(string cond)
        {
            _preserveCacheLayout = String.IsNullOrEmpty(cond) ? false : cond.ToLowerInvariant().Equals("true");
            return this;
        }

        public IEnumerable<ResolvedFile> Resolve(ProjectContext projectContext)
        {
            List<ResolvedFile> results = new List<ResolvedFile>();

            foreach (LockFileTargetLibrary targetLibrary in projectContext.GetRuntimeLibraries(_privateAssetPackageIds))
            {
                string pkgRoot;
                string libraryPath = _packageResolver.GetPackageDirectory(targetLibrary.Name, targetLibrary.Version, out pkgRoot);

                results.AddRange(GetResolvedFiles(targetLibrary.RuntimeAssemblies, libraryPath, pkgRoot, "runtime"));
                results.AddRange(GetResolvedFiles(targetLibrary.NativeLibraries, libraryPath, pkgRoot, "native"));

                foreach (LockFileRuntimeTarget runtimeTarget in targetLibrary.RuntimeTargets.FilterPlaceHolderFiles())
                {
                    if (string.Equals(runtimeTarget.AssetType, "native", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(runtimeTarget.AssetType, "runtime", StringComparison.OrdinalIgnoreCase))
                    {
                        string srcpath = Path.Combine(libraryPath, runtimeTarget.Path);

                        results.Add(
                            new ResolvedFile(
                                sourcePath: srcpath,
                                destinationSubDirectory: GetDestinationSubDirectory(srcpath,
                                                                                    pkgRoot,
                                                                                    GetRuntimeTargetDestinationSubDirectory(runtimeTarget)),
                                assetType:runtimeTarget.AssetType));
                    }
                }

                foreach (LockFileItem resourceAssembly in targetLibrary.ResourceAssemblies.FilterPlaceHolderFiles())
                {
                    string locale;
                    if (!resourceAssembly.Properties.TryGetValue("locale", out locale))
                    {
                        locale = null;
                    }

                    results.Add(
                        new ResolvedFile(
                            sourcePath: Path.Combine(libraryPath, resourceAssembly.Path),
                            destinationSubDirectory: locale,
                            assetType: "resources"));
                }
            }

            return results;
        }

        private IEnumerable<ResolvedFile> GetResolvedFiles(IEnumerable<LockFileItem> items, string libraryPath, string pkgRoot, string assetType)
        {
            foreach (LockFileItem item in items.FilterPlaceHolderFiles())
            {
                string srcpath = Path.Combine(libraryPath, item.Path);

                yield return new ResolvedFile(
                    sourcePath: srcpath,
                    destinationSubDirectory: GetDestinationSubDirectory(srcpath, pkgRoot),
                    assetType: assetType);
            }
        }

        private static string GetRuntimeTargetDestinationSubDirectory(LockFileRuntimeTarget runtimeTarget)
        {

            if (!string.IsNullOrEmpty(runtimeTarget.Runtime))
            {
                return Path.GetDirectoryName(runtimeTarget.Path);
            }

            return null;
        }

        private string GetDestinationSubDirectory(string libraryPath, string pkgRoot, string destpath = null)
        {

            if (_preserveCacheLayout)
            {
                destpath = Path.GetDirectoryName(libraryPath.Replace(pkgRoot, ""));
            }
            return destpath;
        }
    }
}
