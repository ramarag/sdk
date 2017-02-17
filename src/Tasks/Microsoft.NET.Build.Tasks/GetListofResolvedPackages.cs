// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.ProjectModel;
using PackageInfoHelpers;

namespace Microsoft.NET.Build.Tasks
{
    /// <summary>
    /// Resolves the assemblies to be published for a .NET app.
    /// </summary>
    public class GetListofResolvedPackages : TaskBase
    {
        private readonly List<ITaskItem> _listofResolvePackages = new List<ITaskItem>();

        /// <summary>
        /// All the Packages that were resolved
        /// </summary>
        [Output]
        public ITaskItem[] PackagesResolved
        {
            get { return _listofResolvePackages.ToArray(); }
        }

        protected override void ExecuteCore()
        {

            IEnumerable<PackageInfo> resolvedPackages =  PublishAssembliesResolver.GetResolvedPackageList();

            foreach (PackageInfo resolvedPackage in resolvedPackages)
            {
                TaskItem item = new TaskItem(resolvedPackage.Name);
                item.SetMetadata("Version", resolvedPackage.Version);
                _listofResolvePackages.Add(item);
            }
        }
    }
}
