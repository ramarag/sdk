// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Collections.Generic;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.NET.TestFramework;
using Microsoft.NET.TestFramework.Assertions;
using Microsoft.NET.TestFramework.Commands;
using Xunit;
using static Microsoft.NET.TestFramework.Commands.MSBuildTest;
using System.Runtime.InteropServices;
using Microsoft.DotNet.InternalAbstractions;
using PackageInfoHelpers;
using FluentAssertions;

namespace Microsoft.NET.Publish.Tests
{
    public class GivenThatWeWantToCacheAProjectWithDependencies : SdkTest
    {
        private static string libPrefix;
        private static string runtimeos;
        private static string runtimelibos;
        private static string runtimerid;
        private static string testarch;
        private static string tfm = "netcoreapp1.0";

        static GivenThatWeWantToCacheAProjectWithDependencies()
        {
            libPrefix = "";
            runtimeos = "win7";
            runtimelibos = "win";
            var rid= RuntimeEnvironment.GetRuntimeIdentifier();
            testarch = rid.Substring(rid.LastIndexOf("-") + 1);
            runtimerid = "win7-" + testarch;

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                libPrefix = "lib";
                runtimeos = "unix";
                runtimelibos = "unix";
                runtimerid = rid;
            }

        }
        [Fact]
        public void compose_dependencies()
        {
            TestAsset simpleDependenciesAsset = _testAssetsManager
                .CopyTestAsset("SimpleCache")
                .WithSource();

            ComposeCache cacheCommand = new ComposeCache(Stage0MSBuild, simpleDependenciesAsset.TestRoot);

            var OutputFolder = Path.Combine(simpleDependenciesAsset.TestRoot, "outdir");
            var WorkingDir = Path.Combine(simpleDependenciesAsset.TestRoot, "composedir");

            cacheCommand
                .Execute($"/p:RuntimeIdentifier={runtimerid}", $"/p:TargetFramework={tfm}", $"/p:ComposeDir={OutputFolder}", $"/p:ComposeWorkingDir={WorkingDir}", $"/p:DoNotDecorateComposeDir=true")
                .Should()
                .Pass();

            DirectoryInfo cacheDirectory = new DirectoryInfo(OutputFolder);

            List<string> files_on_disk = new List < string > {
               "artifact.xml",
               $"runtime.{runtimerid}.microsoft.netcore.coredistools/1.0.1-prerelease-00001/runtimes/{runtimerid}/native/{libPrefix}coredistools{Constants.DynamicLibSuffix}",
               $"runtime.{runtimerid}.microsoft.netcore.coredistools/1.0.1-prerelease-00001/runtimes/{runtimerid}/native/coredistools.h"
               };

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && testarch != "x86")
            {
                files_on_disk.Add($"runtime.{runtimerid}.runtime.native.system/4.4.0-beta-24821-02/runtimes/{runtimerid}/native/System.Native.a");
                files_on_disk.Add($"runtime.{runtimerid}.runtime.native.system/4.4.0-beta-24821-02/runtimes/{runtimerid}/native/System.Native{Constants.DynamicLibSuffix}");
            }
            cacheDirectory.Should().OnlyHaveFiles(files_on_disk);

            //valid artifact.xml
            HashSet<PackageInfo> knownpackage = new HashSet<PackageInfo>();

            knownpackage.Add(new PackageInfo("Microsoft.NETCore.Targets", "1.2.0-beta-24821-02"));
            knownpackage.Add(new PackageInfo("System.Private.Uri", "4.4.0-beta-24821-02"));
            knownpackage.Add(new PackageInfo("Microsoft.NETCore.CoreDisTools", "1.0.1-prerelease-00001"));
            knownpackage.Add(new PackageInfo("runtime.win7.System.Private.Uri", "4.4.0-beta-24821-02"));
            knownpackage.Add(new PackageInfo("Microsoft.NETCore.Platforms", "1.2.0-beta-24821-02"));
            knownpackage.Add(new PackageInfo("runtime.win7-x64.Microsoft.NETCore.CoreDisTools", "1.0.1-prerelease-00001"));

            var artifact = Path.Combine(OutputFolder, "artifact.xml");
            var packagescomposed = CacheArtifactParser.Parse(artifact);
            packagescomposed.Should().Contain(pkg => knownpackage.Contains(pkg));
            
        }
        [Fact]
        public void compose_with_fxfiles()
        {
            TestAsset simpleDependenciesAsset = _testAssetsManager
                .CopyTestAsset("SimpleCache")
                .WithSource();


            ComposeCache cacheCommand = new ComposeCache(Stage0MSBuild, simpleDependenciesAsset.TestRoot);

            var OutputFolder = Path.Combine(simpleDependenciesAsset.TestRoot, "outdir");
            var WorkingDir = Path.Combine(simpleDependenciesAsset.TestRoot, "composedir");

            cacheCommand
                .Execute($"/p:RuntimeIdentifier={runtimerid}", $"/p:TargetFramework={tfm}", $"/p:ComposeDir={OutputFolder}", $"/p:ComposeWorkingDir={WorkingDir}", "/p:DoNotDecorateComposeDir=true", "/p:SkipRemovingSystemFiles=true")
                .Should()
                .Pass();

            DirectoryInfo cacheDirectory = new DirectoryInfo(OutputFolder);
            List<string> files_on_disk = new List<string> {
               "artifact.xml",
               $"runtime.{runtimerid}.microsoft.netcore.coredistools/1.0.1-prerelease-00001/runtimes/{runtimerid}/native/{libPrefix}coredistools{Constants.DynamicLibSuffix}",
               $"runtime.{runtimerid}.microsoft.netcore.coredistools/1.0.1-prerelease-00001/runtimes/{runtimerid}/native/coredistools.h",
               $"runtime.{runtimeos}.system.private.uri/4.4.0-beta-24821-02/runtimes/{runtimelibos}/lib/netstandard1.0/System.Private.Uri.dll"
               };

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && testarch != "x86")
            {
                files_on_disk.Add($"runtime.{runtimerid}.runtime.native.system/4.4.0-beta-24821-02/runtimes/{runtimerid}/native/System.Native.a");
                files_on_disk.Add($"runtime.{runtimerid}.runtime.native.system/4.4.0-beta-24821-02/runtimes/{runtimerid}/native/System.Native{Constants.DynamicLibSuffix}");
            }
            cacheDirectory.Should().OnlyHaveFiles(files_on_disk);
        }

        [Fact]
        public void compose_dependencies_noopt()
        {
            TestAsset simpleDependenciesAsset = _testAssetsManager
                .CopyTestAsset("SimpleCache")
                .WithSource();


            ComposeCache cacheCommand = new ComposeCache(Stage0MSBuild, simpleDependenciesAsset.TestRoot);

            var OutputFolder = Path.Combine(simpleDependenciesAsset.TestRoot, "outdir");
            var WorkingDir = Path.Combine(simpleDependenciesAsset.TestRoot, "composedir");

            cacheCommand
                .Execute($"/p:RuntimeIdentifier={runtimerid}", $"/p:TargetFramework={tfm}", $"/p:ComposeDir={OutputFolder}", $"/p:DoNotDecorateComposeDir=true", "/p:SkipOptimization=true", $"/p:ComposeWorkingDir={WorkingDir}", "/p:PreserveComposeWorkingDir=true")
                .Should()
                .Pass();

            DirectoryInfo cacheDirectory = new DirectoryInfo(OutputFolder);

            List<string> files_on_disk = new List<string> {
               "artifact.xml",
               $"runtime.{runtimerid}.microsoft.netcore.coredistools/1.0.1-prerelease-00001/runtimes/{runtimerid}/native/{libPrefix}coredistools{Constants.DynamicLibSuffix}",
               $"runtime.{runtimerid}.microsoft.netcore.coredistools/1.0.1-prerelease-00001/runtimes/{runtimerid}/native/coredistools.h",
               $"runtime.{runtimeos}.system.private.uri/4.4.0-beta-24821-02/runtimes/{runtimelibos}/lib/netstandard1.0/System.Private.Uri.dll"
               };

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && testarch != "x86")
            {
                files_on_disk.Add($"runtime.{runtimerid}.runtime.native.system/4.4.0-beta-24821-02/runtimes/{runtimerid}/native/System.Native.a");
                files_on_disk.Add($"runtime.{runtimerid}.runtime.native.system/4.4.0-beta-24821-02/runtimes/{runtimerid}/native/System.Native{Constants.DynamicLibSuffix}");
            }

            cacheDirectory.Should().OnlyHaveFiles(files_on_disk);
        }

        [Fact]
        public void cache_nativeonlyassets()
        {
            TestAsset simpleDependenciesAsset = _testAssetsManager
                .CopyTestAsset("UnmanagedCache")
                .WithSource();

            ComposeCache cacheCommand = new ComposeCache(Stage0MSBuild, simpleDependenciesAsset.TestRoot);

            var OutputFolder = Path.Combine(simpleDependenciesAsset.TestRoot, "outdir");
            var WorkingDir = Path.Combine(simpleDependenciesAsset.TestRoot, "composedir");
            cacheCommand
                .Execute($"/p:RuntimeIdentifier={runtimerid}", $"/p:TargetFramework={tfm}", $"/p:ComposeWorkingDir={WorkingDir}", $"/p:ComposeDir={OutputFolder}", $"/p:DoNotDecorateComposeDir=true")
                .Should()
                .Pass();

            DirectoryInfo cacheDirectory = new DirectoryInfo(OutputFolder);

            List<string> files_on_disk = new List<string> {
               "artifact.xml",
               $"runtime.{runtimerid}.microsoft.netcore.coredistools/1.0.1-prerelease-00001/runtimes/{runtimerid}/native/{libPrefix}coredistools{Constants.DynamicLibSuffix}",
               $"runtime.{runtimerid}.microsoft.netcore.coredistools/1.0.1-prerelease-00001/runtimes/{runtimerid}/native/coredistools.h"
               };

            cacheDirectory.Should().OnlyHaveFiles(files_on_disk);


        }
    }
}
