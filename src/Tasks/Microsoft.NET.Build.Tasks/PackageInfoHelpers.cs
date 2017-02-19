// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using NuGet.Frameworks;
using NuGet.ProjectModel;

namespace PackageInfoHelpers
{
    public class PackageInfo : IEquatable<PackageInfo>
    {
        public string Name { get; }

        public string Version { get; }

        public PackageInfo(string _Name, string _Version)
        {
            Name = _Name;
            Version = _Version;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() | Version.GetHashCode();
        }
        public bool Equals(PackageInfo pkg)
        {
            return Name.Equals(pkg.Name) && Version.Equals(pkg.Version);
        }

        public override bool Equals(object obj)
        {
            var temp = obj as PackageInfo;
            return temp == null ? false : Equals(temp);
        }
    }

    public class CacheArtifactParser
    {
        public static HashSet<PackageInfo> Parse(string filterProjFilesList)
        {
            var listofFilterFiles = filterProjFilesList.Split(';');
            var listofPackages = new HashSet<PackageInfo>();

            foreach (var filterFile in listofFilterFiles)
            {
                var doc = XDocument.Load(filterFile);

                foreach (var pkginfo in doc.Elements("Project").Elements("ItemGroup").Elements("PackageReference"))
                {

                    var pkgname = pkginfo.Attribute("Include");
                    var version = pkginfo.Attribute("Version");

                    if (pkgname != null && version != null)
                    {
                        listofPackages.Add(new PackageInfo(pkgname.Value, version.Value));
                    }
                }
            }

            return listofPackages;
        }
    }
}


