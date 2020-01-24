$ErrorActionPreference = 'Stop'

$outPath = New-TemporaryFile
dotnet outdated -o "$outPath" src/Quelimb/Quelimb.csproj

$outPath.Refresh()
if ($outPath.Length -eq 0) { return }

$j = Get-Content $outPath | ConvertFrom-Json
$deps = $j.Projects | ForEach-Object {
    $_.TargetFrameworks |
        Where-Object { $_.Name -eq '.NETStandard,Version=v2.1' } |
        ForEach-Object { $_.Dependencies | Write-Output }
}

$deps | ForEach-Object {
    Write-Host "Add $($_.Name) $($_.LatestVersion)"
    dotnet add test/Quelimb.Tests/Quelimb.Tests.csproj package --version $_.LatestVersion $_.Name
}
