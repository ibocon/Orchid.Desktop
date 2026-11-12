#requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }

Describe 'Start-OrchidDesktopStaging' {
    BeforeAll {
        $script:ScriptPath = Join-Path -Path $PSScriptRoot -ChildPath 'Start-OrchidDesktopStaging.ps1'
    }

    It 'Staging 사전 점검을 건너뛰고 -BuildOnly 로 호출하면 정확한 MSBuild 인자로 빌드하고 실행 파일 경로를 반환한다' {
        # Given
        $FakeRepo = Join-Path -Path $TestDrive -ChildPath 'desktop-repo'
        $SrcDir = Join-Path -Path $FakeRepo -ChildPath 'src'
        $BinDir = Join-Path -Path $SrcDir -ChildPath 'bin\Debug\net10.0-windows10.0.19041.0\win-x64'
        $null = New-Item -ItemType Directory -Path $BinDir -Force
        Set-Content -LiteralPath (Join-Path -Path $SrcDir -ChildPath 'Orchid.Desktop.csproj') -Value '<Project />' -Encoding utf8NoBOM
        $FakeExe = Join-Path -Path $BinDir -ChildPath 'Orchid.Desktop.exe'
        Set-Content -LiteralPath $FakeExe -Value 'exe' -Encoding utf8NoBOM

        $ArgsCapturePath = Join-Path -Path $TestDrive -ChildPath 'dotnet-args.json'
        $FakeDotnet = Join-Path -Path $TestDrive -ChildPath 'fake-dotnet.ps1'
        @"
@(`$args) | ConvertTo-Json -Compress | Set-Content -LiteralPath '$ArgsCapturePath' -Encoding utf8NoBOM
exit 0
"@ | Set-Content -LiteralPath $FakeDotnet -Encoding utf8NoBOM

        # When
        $Result = & $script:ScriptPath `
            -RepositoryRoot $FakeRepo `
            -DotnetCommand $FakeDotnet `
            -SkipPreflight `
            -BuildOnly `
            -PassThru | Select-Object -Last 1

        # Then
        $Result.ExecutablePath | Should -Be $FakeExe
        $Result.RelayUrl | Should -Be 'https://stag.botanicsoft.com/orchid'
        $Result.Launched | Should -BeFalse
        $CapturedArgs = @(Get-Content -LiteralPath $ArgsCapturePath -Raw | ConvertFrom-Json)
        $CapturedArgs | Should -Contain '-p:WindowsPackageType=None'
        $CapturedArgs | Should -Contain '-p:OrchidRelayUrl=https://stag.botanicsoft.com/orchid'
    }
}
