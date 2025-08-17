using NuitsJp.GistGet.Business;
using NuitsJp.GistGet.Models;
using Shouldly;

namespace NuitsJp.GistGet.Tests.Business.Models;

public class PackageYamlConverterTests
{
    [Fact]
    public void ToYaml_WithEmptyCollection_ShouldReturnEmptyPackagesYaml()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var collection = new PackageCollection();

        // Act
        var yaml = converter.ToYaml(collection);

        // Assert
        yaml.ShouldNotBeNullOrEmpty();
        yaml.Trim().ShouldBe("{}"); // 辞書形式では空の辞書
    }

    [Fact]
    public void ToYaml_WithSinglePackage_ShouldSerializeCorrectly()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var collection = new PackageCollection();
        var package = new PackageDefinition("AkelPad.AkelPad", "4.9.8");
        collection.Add(package);

        // Act
        var yaml = converter.ToYaml(collection);

        // Assert
        yaml.ShouldNotBeNullOrEmpty();
        yaml.ShouldContain("AkelPad.AkelPad:"); // 辞書形式ではパッケージIDがキー
        yaml.ShouldContain("version: 4.9.8");
    }

    [Fact]
    public void ToYaml_WithMultiplePackages_ShouldSerializeInSortedOrder()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var collection = new PackageCollection();
        collection.Add(new PackageDefinition("Zoom.Zoom"));
        collection.Add(new PackageDefinition("AkelPad.AkelPad"));
        collection.Add(new PackageDefinition("Microsoft.VisualStudioCode"));

        // Act
        var yaml = converter.ToYaml(collection);

        // Assert
        yaml.ShouldNotBeNullOrEmpty();
        var lines = yaml.Split('\n');
        var packageLines = lines.Where(line => line.Contains(":") && !line.Contains("  ")).ToArray(); // トップレベルキー行を取得
        packageLines.Length.ShouldBe(3);
        packageLines[0].ShouldContain("AkelPad.AkelPad:");
        packageLines[1].ShouldContain("Microsoft.VisualStudioCode:");
        packageLines[2].ShouldContain("Zoom.Zoom:");
    }

    [Fact]
    public void ToYaml_WithFullPackageProperties_ShouldSerializeAllProperties()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var collection = new PackageCollection();
        var package = new PackageDefinition(
            "AkelPad.AkelPad",
            "4.9.8",
            true,
            "x64",
            "user",
            "winget",
            "--force"
        );
        collection.Add(package);

        // Act
        var yaml = converter.ToYaml(collection);

        // Assert
        yaml.ShouldContain("AkelPad.AkelPad:"); // 辞書形式ではIDがキー
        yaml.ShouldContain("version: 4.9.8");
        yaml.ShouldContain("uninstall: true");
        yaml.ShouldContain("architecture: x64");
        yaml.ShouldContain("scope: user");
        yaml.ShouldContain("source: winget");
        yaml.ShouldContain("custom: --force");
    }

    [Fact]
    public void FromYaml_WithValidYaml_ShouldDeserializeCorrectly()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var yaml = """
                   packages:
                     - id: AkelPad.AkelPad
                       version: 4.9.8
                     - id: Microsoft.VisualStudioCode
                   """;

        // Act
        var collection = converter.FromYaml(yaml);

        // Assert
        collection.Count.ShouldBe(2);

        var akelPad = collection.FindById("AkelPad.AkelPad");
        akelPad.ShouldNotBeNull();
        akelPad.Id.ShouldBe("AkelPad.AkelPad");
        akelPad.Version.ShouldBe("4.9.8");

        var vsCode = collection.FindById("Microsoft.VisualStudioCode");
        vsCode.ShouldNotBeNull();
        vsCode.Id.ShouldBe("Microsoft.VisualStudioCode");
        vsCode.Version.ShouldBeNull();
    }

    [Fact]
    public void FromYaml_WithEmptyPackages_ShouldReturnEmptyCollection()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var yaml = "packages: []";

        // Act
        var collection = converter.FromYaml(yaml);

        // Assert
        collection.Count.ShouldBe(0);
    }

    [Fact]
    public void FromYaml_WithInvalidYaml_ShouldThrowArgumentException()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var invalidYaml = "invalid: yaml: content";

        // Act & Assert
        Should.Throw<ArgumentException>(() => converter.FromYaml(invalidYaml));
    }

    [Fact]
    public void FromYaml_WithNullOrEmptyInput_ShouldReturnEmptyCollection()
    {
        // Arrange
        var converter = new PackageYamlConverter();

        // Act & Assert
        converter.FromYaml(null!).Count.ShouldBe(0);
        converter.FromYaml(string.Empty).Count.ShouldBe(0);
        converter.FromYaml("   ").Count.ShouldBe(0);
    }

    [Fact]
    public void RoundTrip_ShouldPreservePackageData()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var originalCollection = new PackageCollection();
        originalCollection.Add(new PackageDefinition("AkelPad.AkelPad", "4.9.8", true, "x64", "user", "winget",
            "--force"));
        originalCollection.Add(new PackageDefinition("Microsoft.VisualStudioCode"));

        // Act
        var yaml = converter.ToYaml(originalCollection);
        var deserializedCollection = converter.FromYaml(yaml);

        // Assert
        deserializedCollection.Count.ShouldBe(originalCollection.Count);

        var originalAkelPad = originalCollection.FindById("AkelPad.AkelPad")!;
        var deserializedAkelPad = deserializedCollection.FindById("AkelPad.AkelPad")!;

        deserializedAkelPad.Id.ShouldBe(originalAkelPad.Id);
        deserializedAkelPad.Version.ShouldBe(originalAkelPad.Version);
        deserializedAkelPad.Uninstall.ShouldBe(originalAkelPad.Uninstall);
        deserializedAkelPad.Architecture.ShouldBe(originalAkelPad.Architecture);
        deserializedAkelPad.Scope.ShouldBe(originalAkelPad.Scope);
        deserializedAkelPad.Source.ShouldBe(originalAkelPad.Source);
        deserializedAkelPad.Custom.ShouldBe(originalAkelPad.Custom);
    }

    [Fact]
    public void RoundTrip_EmptyCollection_ShouldPreserveEmptyState()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var originalCollection = new PackageCollection();

        // Act
        var yaml = converter.ToYaml(originalCollection);
        var deserializedCollection = converter.FromYaml(yaml);

        // Assert
        originalCollection.Count.ShouldBe(0);
        deserializedCollection.Count.ShouldBe(0);
        yaml.Trim().ShouldBe("{}");
    }

    [Fact]
    public void RoundTrip_SinglePackageMinimalFields_ShouldPreserveIdOnly()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var originalCollection = new PackageCollection();
        originalCollection.Add(new PackageDefinition("Microsoft.PowerToys"));

        // Act
        var yaml = converter.ToYaml(originalCollection);
        var deserializedCollection = converter.FromYaml(yaml);

        // Assert
        deserializedCollection.Count.ShouldBe(1);
        var package = deserializedCollection.FindById("Microsoft.PowerToys")!;
        package.Id.ShouldBe("Microsoft.PowerToys");
        package.Version.ShouldBeNull();
        package.Uninstall.ShouldBeNull();
        package.Architecture.ShouldBeNull();
        package.Scope.ShouldBeNull();
        package.Source.ShouldBeNull();
        package.Custom.ShouldBeNull();
    }

    [Fact]
    public void RoundTrip_MultiplePackagesVariedFields_ShouldPreserveAllData()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var originalCollection = new PackageCollection();

        // Package with minimal fields
        originalCollection.Add(new PackageDefinition("A.MinimalPackage"));

        // Package with some fields
        originalCollection.Add(new PackageDefinition("B.PartialPackage", "1.0.0", architecture: "x64"));

        // Package with all fields
        originalCollection.Add(new PackageDefinition("C.FullPackage", "2.0.0", true, "x86", "machine", "msstore",
            "--quiet"));

        // Act
        var yaml = converter.ToYaml(originalCollection);
        var deserializedCollection = converter.FromYaml(yaml);

        // Assert
        deserializedCollection.Count.ShouldBe(3);

        // Verify minimal package
        var minimal = deserializedCollection.FindById("A.MinimalPackage")!;
        minimal.Id.ShouldBe("A.MinimalPackage");
        minimal.Version.ShouldBeNull();
        minimal.Uninstall.ShouldBeNull();
        minimal.Architecture.ShouldBeNull();
        minimal.Scope.ShouldBeNull();
        minimal.Source.ShouldBeNull();
        minimal.Custom.ShouldBeNull();

        // Verify partial package
        var partial = deserializedCollection.FindById("B.PartialPackage")!;
        partial.Id.ShouldBe("B.PartialPackage");
        partial.Version.ShouldBe("1.0.0");
        partial.Architecture.ShouldBe("x64");
        partial.Uninstall.ShouldBeNull();
        partial.Scope.ShouldBeNull();
        partial.Source.ShouldBeNull();
        partial.Custom.ShouldBeNull();

        // Verify full package
        var full = deserializedCollection.FindById("C.FullPackage")!;
        full.Id.ShouldBe("C.FullPackage");
        full.Version.ShouldBe("2.0.0");
        full.Uninstall.ShouldBe(true);
        full.Architecture.ShouldBe("x86");
        full.Scope.ShouldBe("machine");
        full.Source.ShouldBe("msstore");
        full.Custom.ShouldBe("--quiet");
    }

    [Fact]
    public void RoundTrip_WithOptionalFieldsCombinations_ShouldPreserveExactData()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var originalCollection = new PackageCollection();

        // Various combinations of optional fields
        originalCollection.Add(new PackageDefinition("Test.VersionOnly", "1.0"));
        originalCollection.Add(new PackageDefinition("Test.UninstallOnly", uninstall: true));
        originalCollection.Add(new PackageDefinition("Test.ArchOnly", architecture: "arm64"));
        originalCollection.Add(new PackageDefinition("Test.ScopeOnly", scope: "user"));
        originalCollection.Add(new PackageDefinition("Test.SourceOnly", source: "winget"));
        originalCollection.Add(new PackageDefinition("Test.CustomOnly", custom: "--override"));
        originalCollection.Add(new PackageDefinition("Test.VersionAndScope", "2.0", scope: "machine"));

        // Act
        var yaml = converter.ToYaml(originalCollection);
        var deserializedCollection = converter.FromYaml(yaml);

        // Assert
        deserializedCollection.Count.ShouldBe(7);

        var versionOnly = deserializedCollection.FindById("Test.VersionOnly")!;
        versionOnly.Version.ShouldBe("1.0");
        versionOnly.Uninstall.ShouldBeNull();

        var uninstallOnly = deserializedCollection.FindById("Test.UninstallOnly")!;
        uninstallOnly.Uninstall.ShouldBe(true);
        uninstallOnly.Version.ShouldBeNull();

        var archOnly = deserializedCollection.FindById("Test.ArchOnly")!;
        archOnly.Architecture.ShouldBe("arm64");
        archOnly.Version.ShouldBeNull();

        var scopeOnly = deserializedCollection.FindById("Test.ScopeOnly")!;
        scopeOnly.Scope.ShouldBe("user");
        scopeOnly.Version.ShouldBeNull();

        var sourceOnly = deserializedCollection.FindById("Test.SourceOnly")!;
        sourceOnly.Source.ShouldBe("winget");
        sourceOnly.Version.ShouldBeNull();

        var customOnly = deserializedCollection.FindById("Test.CustomOnly")!;
        customOnly.Custom.ShouldBe("--override");
        customOnly.Version.ShouldBeNull();

        var versionAndScope = deserializedCollection.FindById("Test.VersionAndScope")!;
        versionAndScope.Version.ShouldBe("2.0");
        versionAndScope.Scope.ShouldBe("machine");
        versionAndScope.Uninstall.ShouldBeNull();
    }

    [Fact]
    public void RoundTrip_WithEmptyStringFields_ShouldTreatAsNull()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var originalCollection = new PackageCollection();

        // Package with empty string fields (should be treated as null)
        originalCollection.Add(new PackageDefinition("Test.EmptyFields", "", null, "", "", "", ""));

        // Act
        var yaml = converter.ToYaml(originalCollection);
        var deserializedCollection = converter.FromYaml(yaml);

        // Assert
        deserializedCollection.Count.ShouldBe(1);
        var package = deserializedCollection.FindById("Test.EmptyFields")!;

        // Empty strings should be serialized as null and deserialized as null
        package.Version.ShouldBeNull();
        package.Uninstall.ShouldBeNull();
        package.Architecture.ShouldBeNull();
        package.Scope.ShouldBeNull();
        package.Source.ShouldBeNull();
        package.Custom.ShouldBeNull();

        // YAML should not contain empty string fields (OmitNull configuration)
        yaml.ShouldNotContain("version:");
        yaml.ShouldNotContain("uninstall:");
        yaml.ShouldNotContain("architecture:");
        yaml.ShouldNotContain("scope:");
        yaml.ShouldNotContain("source:");
        yaml.ShouldNotContain("custom:");
    }

    [Fact]
    public void FromYaml_WithMixedYamlTestData_ShouldDeserializeCorrectly()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        // yaml-specification.mdに基づくテストデータ（現在の配列形式）
        var yaml = """
                   packages:
                     - id: 7zip.7zip
                     - id: Microsoft.VisualStudioCode
                       version: 1.85.0
                       architecture: x64
                       scope: machine
                       source: winget
                       custom: /VERYSILENT /NORESTART
                     - id: Zoom.Zoom
                       uninstall: true
                   """;

        // Act
        var collection = converter.FromYaml(yaml);

        // Assert
        collection.Count.ShouldBe(3);

        // 7zip.7zip (パラメータなし)
        var sevenZip = collection.FindById("7zip.7zip");
        sevenZip.ShouldNotBeNull();
        sevenZip.Version.ShouldBeNull();
        sevenZip.Uninstall.ShouldBeNull();

        // Microsoft.VisualStudioCode (複数パラメータあり)
        var vscode = collection.FindById("Microsoft.VisualStudioCode");
        vscode.ShouldNotBeNull();
        vscode.Version.ShouldBe("1.85.0");
        vscode.Architecture.ShouldBe("x64");
        vscode.Scope.ShouldBe("machine");
        vscode.Source.ShouldBe("winget");
        vscode.Custom.ShouldBe("/VERYSILENT /NORESTART");

        // Zoom.Zoom (アンインストール指定)
        var zoom = collection.FindById("Zoom.Zoom");
        zoom.ShouldNotBeNull();
        zoom.Uninstall.ShouldBe(true);
    }

    [Fact]
    public void FromYaml_WithEmptyYamlTestData_ShouldReturnEmptyCollection()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var yaml = "packages: []";

        // Act
        var collection = converter.FromYaml(yaml);

        // Assert
        collection.Count.ShouldBe(0);
    }

    [Fact]
    public void FromYaml_WithDictionaryFormat_ShouldDeserializeCorrectly()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        // yaml-specification.mdで指定されている辞書形式のYAML
        var yaml = """
                   7zip.7zip:
                   Microsoft.VisualStudioCode:
                     allowHashMismatch: true
                     architecture: x64
                     custom: /VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders
                     force: true
                     header: 'Authorization: Bearer xxx'
                     installerType: exe
                     locale: en-US
                     location: C:\Program Files\Microsoft VS Code
                     log: C:\Temp\vscode_install.log
                     mode: silent
                     override: /SILENT
                     scope: machine
                     skipDependencies: true
                     version: 1.85.0
                     confirm: true
                     whatIf: true
                     uninstall: false
                   Git.Git:
                     version: 2.43.0
                   PowerShell.PowerShell:
                     scope: user
                   Zoom.Zoom:
                     uninstall: true
                   """;

        // Act
        var collection = converter.FromYaml(yaml);

        // Assert
        collection.Count.ShouldBe(5);

        // 7zip.7zip (パラメータなし)
        var sevenZip = collection.FindById("7zip.7zip");
        sevenZip.ShouldNotBeNull();
        sevenZip.Version.ShouldBeNull();
        sevenZip.Uninstall.ShouldBeNull();

        // Microsoft.VisualStudioCode (全プロパティあり)
        var vscode = collection.FindById("Microsoft.VisualStudioCode");
        vscode.ShouldNotBeNull();
        vscode.Version.ShouldBe("1.85.0");
        vscode.AllowHashMismatch.ShouldBe(true);
        vscode.Architecture.ShouldBe("x64");
        vscode.Custom.ShouldBe("/VERYSILENT /NORESTART /MERGETASKS=!runcode,addcontextmenufiles,addcontextmenufolders");
        vscode.Force.ShouldBe(true);
        vscode.Header.ShouldBe("Authorization: Bearer xxx");
        vscode.InstallerType.ShouldBe("exe");
        vscode.Locale.ShouldBe("en-US");
        vscode.Location.ShouldBe("C:\\Program Files\\Microsoft VS Code");
        vscode.Log.ShouldBe("C:\\Temp\\vscode_install.log");
        vscode.Mode.ShouldBe("silent");
        vscode.Override.ShouldBe("/SILENT");
        vscode.Scope.ShouldBe("machine");
        vscode.SkipDependencies.ShouldBe(true);
        vscode.Confirm.ShouldBe(true);
        vscode.WhatIf.ShouldBe(true);
        vscode.Uninstall.ShouldBe(false);

        // Git.Git (バージョンのみ)
        var git = collection.FindById("Git.Git");
        git.ShouldNotBeNull();
        git.Version.ShouldBe("2.43.0");

        // PowerShell.PowerShell (スコープのみ)
        var powershell = collection.FindById("PowerShell.PowerShell");
        powershell.ShouldNotBeNull();
        powershell.Scope.ShouldBe("user");

        // Zoom.Zoom (アンインストール指定)
        var zoom = collection.FindById("Zoom.Zoom");
        zoom.ShouldNotBeNull();
        zoom.Uninstall.ShouldBe(true);
    }

    [Fact]
    public void FromYaml_WithEmptyDictionaryFormat_ShouldReturnEmptyCollection()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var yaml = ""; // 空のYAMLは空のコレクションを返すべき

        // Act
        var collection = converter.FromYaml(yaml);

        // Assert
        collection.Count.ShouldBe(0);
    }

    [Fact]
    public void ToYaml_WithDictionaryFormat_ShouldSerializeToSpecificationFormat()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var collection = new PackageCollection();

        // パラメータなしのパッケージ
        collection.Add(new PackageDefinition("7zip.7zip"));

        // 一部プロパティを持つパッケージ
        collection.Add(new PackageDefinition(
            "Microsoft.VisualStudioCode",
            version: "1.85.0",
            uninstall: false,
            architecture: "x64",
            scope: "machine",
            source: null,
            custom: "/VERYSILENT /NORESTART",
            allowHashMismatch: true,
            force: true,
            header: "Authorization: Bearer xxx"
        ));

        // アンインストール指定
        collection.Add(new PackageDefinition("Zoom.Zoom", uninstall: true));

        // Act
        var yaml = converter.ToYaml(collection);

        // Assert - yaml-specification.mdの辞書形式になっていることを確認
        yaml.ShouldNotContain("packages:"); // 配列形式ではない
        yaml.ShouldNotContain("- id:"); // 配列形式ではない

        // 辞書形式の構造確認
        yaml.ShouldContain("7zip.7zip:");
        yaml.ShouldContain("Microsoft.VisualStudioCode:");
        yaml.ShouldContain("Zoom.Zoom:");

        // プロパティの確認
        yaml.ShouldContain("version: 1.85.0");
        yaml.ShouldContain("allowHashMismatch: true");
        yaml.ShouldContain("architecture: x64");
        yaml.ShouldContain("custom: /VERYSILENT /NORESTART");
        yaml.ShouldContain("force: true");
        yaml.ShouldContain("header: 'Authorization: Bearer xxx'");
        yaml.ShouldContain("scope: machine");
        yaml.ShouldContain("uninstall: true");
        yaml.ShouldContain("uninstall: false");

        // パラメータなしのパッケージは空の値
        var lines = yaml.Split('\n');
        var sevenZipLine = lines.FirstOrDefault(line => line.Contains("7zip.7zip:"));
        sevenZipLine.ShouldNotBeNull();
        sevenZipLine.Trim().ShouldBe("7zip.7zip:");
    }

    [Fact]
    public void ToYaml_WithEmptyCollection_ShouldReturnEmptyYaml()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var collection = new PackageCollection();

        // Act
        var yaml = converter.ToYaml(collection);

        // Assert
        yaml.ShouldNotBeNull();
        yaml.Trim().ShouldBe("{}"); // 空の辞書
    }

    [Fact]
    public void RoundTrip_WithAllPropertiesDictionaryFormat_ShouldPreserveAllData()
    {
        // Arrange
        var converter = new PackageYamlConverter();
        var originalCollection = new PackageCollection();

        // 全プロパティを持つパッケージを作成
        originalCollection.Add(new PackageDefinition(
            "Microsoft.VisualStudioCode",
            version: "1.85.0",
            uninstall: false,
            architecture: "x64",
            scope: "machine",
            source: "winget",
            custom: "/VERYSILENT /NORESTART",
            allowHashMismatch: true,
            force: true,
            header: "Authorization: Bearer xxx",
            installerType: "exe",
            locale: "en-US",
            location: "C:\\Program Files\\Microsoft VS Code",
            log: "C:\\Temp\\vscode_install.log",
            mode: "silent",
            overrideArgs: "/SILENT",
            skipDependencies: true,
            confirm: true,
            whatIf: true
        ));

        // パラメータなしのパッケージ
        originalCollection.Add(new PackageDefinition("7zip.7zip"));

        // アンインストール指定のパッケージ
        originalCollection.Add(new PackageDefinition("Zoom.Zoom", uninstall: true));

        // Act
        var yaml = converter.ToYaml(originalCollection);
        var deserializedCollection = converter.FromYaml(yaml);

        // Assert
        deserializedCollection.Count.ShouldBe(originalCollection.Count);

        // ソート順が保持されていることを確認
        var originalSorted = originalCollection.ToSortedList();
        var deserializedSorted = deserializedCollection.ToSortedList();

        for (int i = 0; i < originalSorted.Count; i++)
        {
            var original = originalSorted[i];
            var deserialized = deserializedSorted[i];

            deserialized.Id.ShouldBe(original.Id);
            deserialized.Version.ShouldBe(original.Version);
            deserialized.Uninstall.ShouldBe(original.Uninstall);
            deserialized.Architecture.ShouldBe(original.Architecture);
            deserialized.Scope.ShouldBe(original.Scope);
            deserialized.Source.ShouldBe(original.Source);
            deserialized.Custom.ShouldBe(original.Custom);
            deserialized.AllowHashMismatch.ShouldBe(original.AllowHashMismatch);
            deserialized.Force.ShouldBe(original.Force);
            deserialized.Header.ShouldBe(original.Header);
            deserialized.InstallerType.ShouldBe(original.InstallerType);
            deserialized.Locale.ShouldBe(original.Locale);
            deserialized.Location.ShouldBe(original.Location);
            deserialized.Log.ShouldBe(original.Log);
            deserialized.Mode.ShouldBe(original.Mode);
            deserialized.Override.ShouldBe(original.Override);
            deserialized.SkipDependencies.ShouldBe(original.SkipDependencies);
            deserialized.Confirm.ShouldBe(original.Confirm);
            deserialized.WhatIf.ShouldBe(original.WhatIf);
        }

        // YAML形式が辞書形式であることを確認
        yaml.ShouldNotContain("packages:");
        yaml.ShouldNotContain("- id:");
        yaml.ShouldContain("7zip.7zip:");
        yaml.ShouldContain("Microsoft.VisualStudioCode:");
        yaml.ShouldContain("Zoom.Zoom:");
    }
}