param(
    [string]$TileDirectory = "$env:LOCALAPPDATA\Archarina64\tiles\library",
    [Parameter(Mandatory=$true)][int]$SceneId,
    [Parameter(Mandatory=$true)][string]$Output,
    [int]$Spacing = 1200
)
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $TileDirectory -PathType Container)) { throw "Tile directory does not exist: $TileDirectory" }
if ($Spacing -lt 1 -or $Spacing -gt 100000) { throw 'Spacing must be between 1 and 100000.' }
$tiles = @()
foreach ($file in Get-ChildItem -LiteralPath $TileDirectory -Filter '*.xml' -File) {
    try { $xml = [xml](Get-Content -LiteralPath $file.FullName -Raw) } catch { continue }
    if (!$xml.SectionTile) { continue }
    $source = 0; [int]::TryParse([string]$xml.SectionTile.SourceScene, [ref]$source) | Out-Null
    $gameplayScene = 0; [int]::TryParse([string]$xml.SectionTile.Gameplay.Scene, [ref]$gameplayScene) | Out-Null
    if ($source -ne $SceneId -and $gameplayScene -ne $SceneId) { continue }
    $room = 0; [int]::TryParse([string]$xml.SectionTile.Gameplay.Room, [ref]$room) | Out-Null
    $tiles += [ordered]@{ SourceXml = (Get-Content -LiteralPath $file.FullName -Raw); X = ($tiles.Count * $Spacing); Y = 0; Z = 0; QuarterTurns = 0; EmbeddedTextures = [ordered]@{}; Room = $room; SourceFile = $file.Name }
}
if ($tiles.Count -eq 0) { throw "No extracted tile XML files matched scene $SceneId." }
$tiles = @($tiles | Sort-Object Room, SourceFile | ForEach-Object -Begin { $index = 0 } -Process { $_.X = $index * $Spacing; $index++; $_ })
$layout = [ordered]@{ Version = 1; Name = "Scene $('{0:X2}' -f $SceneId) room tiles"; Tiles = @($tiles | ForEach-Object { [ordered]@{ SourceXml = $_.SourceXml; EmbeddedTextures = $_.EmbeddedTextures; X = $_.X; Y = $_.Y; Z = $_.Z; QuarterTurns = $_.QuarterTurns } }) }
$json = $layout | ConvertTo-Json -Depth 8
if ([Text.Encoding]::UTF8.GetByteCount($json) -gt 32MB) { throw 'Generated layout exceeds the Android 32 MB document limit. Split the scene or use tile bundles individually.' }
$parent = Split-Path -Path $Output -Parent; if ($parent) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
[IO.File]::WriteAllText($Output, $json, [Text.UTF8Encoding]::new($false))
Write-Output "Created $Output with $($tiles.Count) room tile(s) from scene $SceneId. Tiles are arranged in room order; reposition them in Android."
