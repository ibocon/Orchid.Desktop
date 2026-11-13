// keep: CultureInfo language culture resolution
using System.Globalization;
// keep: Regex marker constant and compound key patterns
using System.Text.RegularExpressions;
// keep: XDocument resx loading
using System.Xml.Linq;
// keep: FluentAssertions assertions
using FluentAssertions;
// keep: Orchid.Desktop service namespace
using Orchid.Desktop.Service;
// keep: xUnit Fact attribute
using Xunit;

namespace Orchid.UnitTests.Desktop;

// allow: SIZE_OK — 단일 지역화 계약 테스트 파일로 4개 [Fact]와 Peony.Mobile 규약 헬퍼를 함께 둔다 (태스크 계약).
/// <summary>
/// Peony.Mobile 지역화 규약을 준용한 Orchid.Desktop의 언어 컬처 해석,
/// resx 키 정합, 리소스 props 레지스트리 계약을 검증한다.
/// </summary>
public sealed partial class DesktopLocalizationAndResourceContractTest
{
    private static readonly string[] CoreResourceNames =
    [
        "ResourceShared",
        "DesktopAppResource",
        "LoginPanelResource",
        "PairingPanelResource",
        "ClipboardListResource",
    ];

    /// <summary>
    /// <see cref="LanguageCultureResolver"/>는 영어/한국어를 고정 컬처로 변환하고
    /// System 설정은 설치 컬처 2자리 코드로 ko-KR/en-US를 판별하며
    /// <see cref="LanguageCultureResolver.Apply"/>는 스레드 풀 기본 컬처까지 전환한다.
    /// </summary>
    [Fact]
    public void ResolveShouldReturnKoKrForKoreanAndEnUsForEnglishOrUnsupportedCulture()
    {
        // Given & When & Then
        // ✅ 명시 선택은 고정 컬처로, System은 설치 컬처의 2자리 코드로 매핑한다 (미지원 언어는 en-US 폴백)
        LanguageCultureResolver.Resolve(Language.English).Name.Should().Be("en-US");
        LanguageCultureResolver.Resolve(Language.Korean).Name.Should().Be("ko-KR");
        LanguageCultureResolver.Resolve(Language.System, new CultureInfo("ko-KR")).Name.Should().Be("ko-KR");
        LanguageCultureResolver.Resolve(Language.System, new CultureInfo("ko")).Name.Should().Be("ko-KR");
        LanguageCultureResolver.Resolve(Language.System, new CultureInfo("en-US")).Name.Should().Be("en-US");
        LanguageCultureResolver.Resolve(Language.System, new CultureInfo("ja-JP")).Name.Should().Be("en-US");

        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo? originalDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        CultureInfo? originalDefaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;
        try
        {
            // ✅ Apply는 현재 스레드와 스레드 풀 기본 CurrentCulture/CurrentUICulture를 모두 설정한다
            LanguageCultureResolver.Apply(Language.English);
            CultureInfo.CurrentCulture.Name.Should().Be("en-US");
            CultureInfo.CurrentUICulture.Name.Should().Be("en-US");
            CultureInfo.DefaultThreadCurrentCulture.Should().Be(new CultureInfo("en-US"));
            CultureInfo.DefaultThreadCurrentUICulture.Should().Be(new CultureInfo("en-US"));

            LanguageCultureResolver.Apply(Language.Korean);
            CultureInfo.CurrentCulture.Name.Should().Be("ko-KR");
            CultureInfo.CurrentUICulture.Name.Should().Be("ko-KR");
            CultureInfo.DefaultThreadCurrentCulture.Should().Be(new CultureInfo("ko-KR"));
            CultureInfo.DefaultThreadCurrentUICulture.Should().Be(new CultureInfo("ko-KR"));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
            CultureInfo.DefaultThreadCurrentCulture = originalDefaultCulture;
            CultureInfo.DefaultThreadCurrentUICulture = originalDefaultUiCulture;
        }
    }

    /// <summary>
    /// 중립 resx는 Universal Clipboard Bridge 영문 문구를,
    /// ko-KR resx는 모두클립 번역 문구를 제공해야 한다.
    /// </summary>
    [Fact]
    public void NeutralResxShouldProvideUniversalClipboardBridgeAndKoKrResxShouldProvideTranslatedStrings()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        XDocument sharedNeutral = XDocument.Load(
            Path.Combine(repoRoot, "src", "Service", "Localization", "ResourceShared.resx"));
        XDocument sharedKorean = XDocument.Load(
            Path.Combine(repoRoot, "src", "Service", "Localization", "ResourceShared.ko-KR.resx"));
        XDocument desktopNeutral = XDocument.Load(
            Path.Combine(repoRoot, "src", "Component", "DesktopApp", "DesktopAppResource.resx"));
        XDocument desktopKorean = XDocument.Load(
            Path.Combine(repoRoot, "src", "Component", "DesktopApp", "DesktopAppResource.ko-KR.resx"));
        XDocument loginNeutral = XDocument.Load(
            Path.Combine(repoRoot, "src", "Component", "LoginPanel", "LoginPanelResource.resx"));
        XDocument loginKorean = XDocument.Load(
            Path.Combine(repoRoot, "src", "Component", "LoginPanel", "LoginPanelResource.ko-KR.resx"));

        // When & Then
        // ✅ ResourceShared: 앱 이름과 창 제목이 문화권별 문구를 제공한다
        FindResxValue(sharedNeutral, "AppName").Should().Be("Universal Clipboard Bridge");
        FindResxValue(sharedKorean, "AppName").Should().Be("모두클립");
        FindResxValue(sharedNeutral, "WindowTitle").Should().Be("Orchid - Universal Clipboard Bridge");
        FindResxValue(sharedKorean, "WindowTitle").Should().Be("모두클립 - PC 모바일 클립보드");

        // ✅ DesktopAppResource: 앱 이름과 로그아웃 문구가 문화권별 문구를 제공한다
        FindResxValue(desktopNeutral, "AppName").Should().Be("Universal Clipboard Bridge");
        FindResxValue(desktopKorean, "AppName").Should().Be("모두클립");
        FindResxValue(desktopNeutral, "SignOutLabel").Should().Be("Sign out");
        FindResxValue(desktopKorean, "SignOutLabel").Should().Be("로그아웃");

        // ✅ LoginPanelResource: 로그인 제목은 문화권별 문구를 사용한다
        FindResxValue(loginNeutral, "Title").Should().Be("Sign in to Universal Clipboard Bridge");
        FindResxValue(loginKorean, "Title").Should().Be("모두클립 로그인");

        // ✅ LoginPanelResource: 설명/소셜 로그인 문구는 중립 영문과 ko-KR 번역이 서로 다르다
        foreach (string key in new[] { "Description", "GoogleText", "KakaoText", "MicrosoftText" })
        {
            string? neutralValue = FindResxValue(loginNeutral, key);
            string? koreanValue = FindResxValue(loginKorean, key);
            neutralValue.Should().NotBeNullOrWhiteSpace(because: $"LoginPanelResource.{key}는 중립 resx에 값이 있어야 한다.");
            koreanValue.Should().NotBeNullOrWhiteSpace(because: $"LoginPanelResource.{key}는 ko-KR resx에 번역이 있어야 한다.");
            koreanValue.Should().NotBe(neutralValue, because: $"LoginPanelResource.{key}는 ko-KR 번역문이 영문 원문과 달라야 한다.");
        }
    }

    /// <summary>
    /// 모든 중립 resx는 marker 상수와 키 정합을 유지하고,
    /// ko-KR 형제 resx와 동일한 키 집합을 가지며,
    /// 복합 파스칼 키를 값으로 그대로 노출하지 않는다.
    /// </summary>
    [Fact]
    public void AllResourceMarkersAndResxFilesShouldMaintainKeyParityWithoutPlaceholders()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        DirectoryInfo sourceRoot = new(Path.Combine(repoRoot, "src"));
        List<FileInfo> neutralResxFiles = EnumerateNeutralResxFiles(sourceRoot).ToList();

        // When & Then
        // ✅ ResourceShared, DesktopAppResource, LoginPanelResource, PairingPanelResource, ClipboardListResource 중립 resx가 존재한다
        neutralResxFiles.Count.Should().BeGreaterThanOrEqualTo(5);
        HashSet<string> neutralNames = neutralResxFiles
            .Select(file => Path.GetFileNameWithoutExtension(file.Name))
            .ToHashSet(StringComparer.Ordinal);
        neutralNames.Should().Contain(
            CoreResourceNames,
            because: "다섯 핵심 리소스의 중립 resx가 모두 존재해야 한다.");

        foreach (FileInfo resxFile in neutralResxFiles)
        {
            string relativePath = Path.GetRelativePath(sourceRoot.FullName, resxFile.FullName);
            HashSet<string> neutralKeys = ReadResxKeys(resxFile.FullName);

            // ✅ marker .cs의 모든 public const string 상수는 중립 resx에 키로 존재한다
            string markerPath = Path.ChangeExtension(resxFile.FullName, ".cs");
            if (File.Exists(markerPath))
            {
                IEnumerable<string> missingMarkerKeys = ReadMarkerConstantNames(markerPath)
                    .Where(name => !neutralKeys.Contains(name))
                    .Order(StringComparer.Ordinal);
                missingMarkerKeys.Should().BeEmpty(
                    because: $"{relativePath}의 marker 상수는 모두 중립 resx에 값이 있어야 한다.");
            }

            // ✅ 중립 resx는 ko-KR 형제 resx와 동일한 키 집합을 유지한다
            string koreanResxPath = Path.Combine(
                resxFile.DirectoryName!,
                $"{Path.GetFileNameWithoutExtension(resxFile.Name)}.ko-KR.resx");
            File.Exists(koreanResxPath).Should().BeTrue(
                because: $"{relativePath}는 ko-KR 지역화 resx를 가져야 한다.");
            HashSet<string> koreanKeys = ReadResxKeys(koreanResxPath);
            neutralKeys.SetEquals(koreanKeys).Should().BeTrue(
                because: $"{relativePath}와 ko-KR resx의 키 집합은 동일해야 한다. " +
                         $"ko-KR 누락: [{string.Join(", ", neutralKeys.Except(koreanKeys).Order())}], " +
                         $"ko-KR 잉여: [{string.Join(", ", koreanKeys.Except(neutralKeys).Order())}]");

            // ✅ 중립 값은 복합 파스칼 키 플레이스홀더를 그대로 두지 않는다 (en-US 문화에서 키가 노출된다)
            XDocument document = XDocument.Load(resxFile.FullName);
            document.Root!
                .Elements("data")
                .Where(IsCompoundKeyPlaceholder)
                .Select(element => (string?)element.Attribute("name") ?? string.Empty)
                .Should().BeEmpty(
                    because: $"{relativePath}의 중립 값은 en-US 사용자에게 보이는 문구여야 한다.");
        }
    }

    /// <summary>
    /// Orchid.Desktop.Resources.props 레지스트리와 sibling props 파일이
    /// Peony.Mobile resx-props 규약(임포트 등록, 섹션별 정렬, LogicalName/CustomToolNamespace)을 따른다.
    /// </summary>
    [Fact]
    public void ResourcePropsAndRegistryShouldFollowPeonyMobileConventions()
    {
        // Given
        string repoRoot = FindRepositoryRoot();
        DirectoryInfo sourceRoot = new(Path.Combine(repoRoot, "src"));
        FileInfo registryFile = new(Path.Combine(sourceRoot.FullName, "Orchid.Desktop.Resources.props"));
        string projectText = File.ReadAllText(Path.Combine(sourceRoot.FullName, "Orchid.Desktop.csproj"));

        // When & Then
        // ✅ 리소스 레지스트리가 존재하고 csproj에 정확히 한 번 import 된다
        registryFile.Exists.Should().BeTrue(because: "Orchid.Desktop 리소스 레지스트리가 존재해야 한다.");
        Regex.Count(projectText, Regex.Escape("<Import Project=\"Orchid.Desktop.Resources.props\" />"))
            .Should().Be(1, because: "csproj은 리소스 레지스트리를 정확히 한 번 import 해야 한다.");

        // ✅ 각 중립 resx는 Resource 접미사를 제거한 이름의 sibling props를 가진다 (ResourceShared은 그대로)
        List<FileInfo> neutralResxFiles = EnumerateNeutralResxFiles(sourceRoot).ToList();
        IEnumerable<string> missingProps = neutralResxFiles
            .Where(resx => !File.Exists(GetExpectedPropsPath(resx)))
            .Select(resx => Path.GetRelativePath(sourceRoot.FullName, resx.FullName));
        missingProps.Should().BeEmpty(because: "각 중립 resx는 sibling props 파일을 가져야 한다.");

        // ✅ 모든 sibling props는 레지스트리에 등록된다
        List<string> imports = LoadRegistryImports(registryFile).ToList();
        HashSet<string> importedPaths = imports.ToHashSet(StringComparer.OrdinalIgnoreCase);
        IEnumerable<string> unregisteredProps = EnumerateResourceProps(sourceRoot)
            .Where(props => !importedPaths.Contains(ToRegistryPath(sourceRoot, props)))
            .Select(props => Path.GetRelativePath(sourceRoot.FullName, props.FullName));
        unregisteredProps.Should().BeEmpty(
            because: "모든 리소스 props는 Orchid.Desktop.Resources.props에 등록되어야 한다.");

        // ✅ 레지스트리 Import는 섹션(Component\, Service\) 안에서 알파벳 정렬을 유지한다
        List<string> violatingSections = imports
            .GroupBy(GetSection)
            .Where(section => !section.SequenceEqual(
                section.OrderBy(NormalizeRegistryPathForSort, StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase))
            .Select(section => section.Key)
            .ToList();
        violatingSections.Should().BeEmpty(because: "레지스트리 Import는 섹션별 알파벳 정렬이어야 한다.");

        // ✅ 각 EmbeddedResource의 LogicalName/CustomToolNamespace은 고정 패턴을 따른다
        List<string> patternViolations = [];
        foreach (FileInfo propsFile in EnumerateResourceProps(sourceRoot))
        {
            XDocument document = XDocument.Load(propsFile.FullName);
            foreach (XElement embedded in document.Descendants("EmbeddedResource"))
            {
                string updatePath = embedded.Attribute("Update")?.Value ?? string.Empty;
                string resourceName = Path.GetFileNameWithoutExtension(
                    updatePath.Replace('\\', Path.DirectorySeparatorChar));
                string? logicalName = embedded.Element("LogicalName")?.Value;
                string? customToolNamespace = embedded.Element("CustomToolNamespace")?.Value;

                if (!string.Equals(logicalName, $"Orchid.Desktop.Component.{resourceName}.resources", StringComparison.Ordinal))
                {
                    patternViolations.Add($"{propsFile.Name}: LogicalName='{logicalName}'");
                }

                if (!string.Equals(customToolNamespace, "Orchid.Desktop.Component", StringComparison.Ordinal))
                {
                    patternViolations.Add($"{propsFile.Name}: CustomToolNamespace='{customToolNamespace}'");
                }
            }
        }

        patternViolations.Should().BeEmpty(
            because: "LogicalName은 'Orchid.Desktop.Component.{리소스}.resources', " +
                     "CustomToolNamespace은 'Orchid.Desktop.Component'이어야 한다.");

        // ✅ 각 Razor 컴포넌트는 대응하는 Resource marker로 IStringLocalizer를 주입한다
        foreach ((string componentRelativePath, string markerName) in new[]
        {
            (Path.Combine("src", "Component", "DesktopApp", "DesktopApp.razor"), "DesktopAppResource"),
            (Path.Combine("src", "Component", "LoginPanel", "LoginPanel.razor"), "LoginPanelResource"),
            (Path.Combine("src", "Component", "PairingPanel", "PairingPanel.razor"), "PairingPanelResource"),
            (Path.Combine("src", "Component", "ClipboardList", "ClipboardList.razor"), "ClipboardListResource"),
        })
        {
            string component = File.ReadAllText(Path.Combine(repoRoot, componentRelativePath));
            component.Should().Contain(
                $"IStringLocalizer<{markerName}>",
                because: $"{componentRelativePath}는 {markerName}로 IStringLocalizer를 주입해야 한다.");
        }
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Orchid.Desktop.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Orchid.Desktop.slnx not found.");
    }

    private static string? FindResxValue(XDocument document, string key)
    {
        return document.Root!
            .Elements("data")
            .Where(element => (string?)element.Attribute("name") == key)
            .Select(element => (string?)element.Element("value"))
            .FirstOrDefault();
    }

    private static IEnumerable<FileInfo> EnumerateNeutralResxFiles(DirectoryInfo sourceRoot)
    {
        return sourceRoot
            .EnumerateFiles("*.resx", SearchOption.AllDirectories)
            .Where(file => !IsInOutputDirectory(file) && !IsCultureResource(file))
            .OrderBy(file => file.FullName, StringComparer.Ordinal);
    }

    private static IEnumerable<FileInfo> EnumerateResourceProps(DirectoryInfo sourceRoot)
    {
        return sourceRoot
            .EnumerateFiles("*.props", SearchOption.AllDirectories)
            .Where(file => !IsInOutputDirectory(file)
                && !file.Name.Equals("Orchid.Desktop.Resources.props", StringComparison.Ordinal));
    }

    private static HashSet<string> ReadResxKeys(string resxPath)
    {
        XDocument document = XDocument.Load(resxPath);
        return document.Root!
            .Elements("data")
            .Select(element => (string?)element.Attribute("name") ?? string.Empty)
            .Where(name => name.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static IEnumerable<string> ReadMarkerConstantNames(string markerPath)
    {
        string source = File.ReadAllText(markerPath);
        return PublicConstStringRegex()
            .Matches(source)
            .Select(match => match.Groups["name"].Value);
    }

    private static bool IsCompoundKeyPlaceholder(XElement element)
    {
        string name = (string?)element.Attribute("name") ?? string.Empty;
        string value = ((string?)element.Element("value") ?? string.Empty).Trim();
        return name.Length > 0
            && string.Equals(name, value, StringComparison.Ordinal)
            && CompoundKeyRegex().IsMatch(name);
    }

    private static string GetExpectedPropsPath(FileInfo resxFile)
    {
        string resourceName = Path.GetFileNameWithoutExtension(resxFile.Name);
        string propsName = resourceName.EndsWith("Resource", StringComparison.Ordinal)
            ? $"{resourceName[..^"Resource".Length]}.props"
            : $"{resourceName}.props";

        return Path.Combine(resxFile.DirectoryName!, propsName);
    }

    private static IEnumerable<string> LoadRegistryImports(FileInfo registryFile)
    {
        XDocument registry = XDocument.Load(registryFile.FullName);
        return registry
            .Descendants("Import")
            .Select(element => element.Attribute("Project")?.Value ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(NormalizeRegistryPath);
    }

    private static string ToRegistryPath(DirectoryInfo sourceRoot, FileInfo file)
    {
        return NormalizeRegistryPath(Path.GetRelativePath(sourceRoot.FullName, file.FullName));
    }

    private static string NormalizeRegistryPath(string path)
    {
        return path.Replace('/', '\\');
    }

    private static string NormalizeRegistryPathForSort(string path)
    {
        return path.Replace('\\', '/');
    }

    private static bool IsInOutputDirectory(FileInfo file)
    {
        string normalizedPath = file.FullName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        return normalizedPath.Contains(
                $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase)
            || normalizedPath.Contains(
                $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCultureResource(FileInfo file)
    {
        string fileName = Path.GetFileNameWithoutExtension(file.Name);
        return fileName.Contains('.', StringComparison.Ordinal);
    }

    private static string GetSection(string importPath)
    {
        string normalized = importPath.Replace('\\', '/');
        int slash = normalized.IndexOf('/');
        return slash >= 0 ? normalized[..slash] : normalized;
    }

    [GeneratedRegex(@"public\s+const\s+string\s+(?<name>\w+)\s*=")]
    private static partial Regex PublicConstStringRegex();

    [GeneratedRegex(@"^[A-Z][a-z0-9]+(?:[A-Z_][A-Za-z0-9]*)+$")]
    private static partial Regex CompoundKeyRegex();
}
