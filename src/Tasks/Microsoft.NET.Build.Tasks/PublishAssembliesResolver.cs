// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using NuGet.Frameworks;
using NuGet.ProjectModel;
using PackageInfoHelpers;

namespace Microsoft.NET.Build.Tasks
{
public class PublishAssembliesResolver
    {
        private static ConcurrentDictionary<PackageInfo, byte> allResolvedPackages = new ConcurrentDictionary<PackageInfo, byte>();
        private static volatile bool hasallResolvedLibrariesBeenDumped = false;
        private readonly IPackageResolver _packageResolver;
        private IEnumerable<string> _privateAssetPackageIds;
        private bool _preserveCacheLayout;
        private bool _doNotTrackPackageAsResolved;

        public PublishAssembliesResolver(IPackageResolver packageResolver)
        {
            _packageResolver = packageResolver;
        }

        public PublishAssembliesResolver WithPrivateAssets(IEnumerable<string> privateAssetPackageIds)
        {
            _privateAssetPackageIds = privateAssetPackageIds;
            return this;
        }
        public PublishAssembliesResolver WithPreserveCacheLayout(bool preserveCacheLayout)
        {
            _preserveCacheLayout = preserveCacheLayout;
            return this;
        }

        public PublishAssembliesResolver WithPackageTracking(bool doNotTrackPackageAsResolved)
        {
            _doNotTrackPackageAsResolved = doNotTrackPackageAsResolved;
            return this;
        }
        

        public static IEnumerable<PackageInfo> GetResolvedPackageList()
        {
            return allResolvedPackages.Keys;
        }
        public IEnumerable<ResolvedFile> Resolve(ProjectContext projectContext)
        {
            List<ResolvedFile> results = new List<ResolvedFile>();

            if (hasallResolvedLibrariesBeenDumped)
            {
                throw new BuildErrorException("DumpResolved has been called and we are still resolving the packages");
            }

            foreach (LockFileTargetLibrary targetLibrary in projectContext.GetRuntimeLibraries(_privateAssetPackageIds))
            {
                if (targetLibrary.Type != "package")
                {
                    continue;
                }

                if (!_doNotTrackPackageAsResolved)
                {
                    allResolvedPackages.TryAdd(new PackageInfo(targetLibrary.Name, targetLibrary.Version.ToString()), default(byte));
                }

                string pkgRoot;
                string libraryPath = _packageResolver.GetPackageDirectory(targetLibrary.Name, targetLibrary.Version, out pkgRoot);

                results.AddRange(GetResolvedFiles(targetLibrary.RuntimeAssemblies, libraryPath, pkgRoot, AssetType.Runtime));
                results.AddRange(GetResolvedFiles(targetLibrary.NativeLibraries, libraryPath, pkgRoot, AssetType.Native));

                foreach (LockFileRuntimeTarget runtimeTarget in targetLibrary.RuntimeTargets.FilterPlaceHolderFiles())
                {
                    if (string.Equals(runtimeTarget.AssetType, "native", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(runtimeTarget.AssetType, "runtime", StringComparison.OrdinalIgnoreCase))
                    {
                        string sourcePath = Path.Combine(libraryPath, runtimeTarget.Path);
                        AssetType _assetType = AssetType.None;
                        Enum.TryParse<AssetType>(runtimeTarget.AssetType, true, out _assetType);

                        results.Add(
                            new ResolvedFile(
                                sourcePath: sourcePath,
                                destinationSubDirectory: GetDestinationSubDirectory(sourcePath,
                                                                                    pkgRoot,
                                                                                    GetRuntimeTargetDestinationSubDirectory(runtimeTarget)),
                                assetType: _assetType));
                    }
                }

                foreach (LockFileItem resourceAssembly in targetLibrary.ResourceAssemblies.FilterPlaceHolderFiles())
                {
                    string locale;
                    string sourcePath = Path.Combine(libraryPath, resourceAssembly.Path);
                    if (!resourceAssembly.Properties.TryGetValue("locale", out locale))
                    {
                        locale = null;
                    }

                    locale = GetDestinationSubDirectory(sourcePath, pkgRoot, locale);

                    results.Add(
                        new ResolvedFile(
                            sourcePath: sourcePath,
                            destinationSubDirectory: locale,
                            assetType: AssetType.Resources));
                }
            }

            return results;
        }

        private IEnumerable<ResolvedFile> GetResolvedFiles(IEnumerable<LockFileItem> items, string libraryPath, string pkgRoot, AssetType assetType)
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
            if (_preserveCacheLayout && pkgRoot != null)
            {
                if (! libraryPath.StartsWith(pkgRoot))
                {
                    throw new BuildErrorException(Strings.IncorrectPackageRoot, pkgRoot, libraryPath);
                }

                destpath = Path.GetDirectoryName(libraryPath.Substring(pkgRoot.Length));
            }
            return destpath;
        }
    }
}
