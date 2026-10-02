param([string]$TileXml, [string]$BundlePath)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($TileXml) -or [string]::IsNullOrWhiteSpace($BundlePath)) { throw 'Usage: make-tile-bundle.ps1 -TileXml <tile.xml> -BundlePath <tile.archtile>' }
if (!(Test-Path -LiteralPath $TileXml -PathType Leaf)) { throw "Tile XML does not exist: $TileXml" }
$xml = [xml](Get-Content -LiteralPath $TileXml -Raw)
$root = Split-Path -Path $TileXml -Parent
$paths = @($xml.SelectNodes('//Material/map_Kd|//Material/map_Ka') | ForEach-Object { $_.InnerText } | Where-Object { $_ } | Select-Object -Unique)
$resolved = @{}
foreach ($path in $paths) {
    $candidate = $path
    if (!(Test-Path -LiteralPath $candidate -PathType Leaf)) { $candidate = Join-Path $root $path }
    if (!(Test-Path -LiteralPath $candidate -PathType Leaf)) { throw "Referenced texture is missing: $path" }
    if ([IO.Path]::GetExtension($candidate) -ine '.png') { throw "Only PNG textures are supported: $candidate" }
    $resolved[[IO.Path]::GetFileName($candidate)] = (Resolve-Path -LiteralPath $candidate).Path
}
$temporary = Join-Path ([IO.Path]::GetTempPath()) ('archarina-bundle-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path (Join-Path $temporary 'textures') -Force | Out-Null
Copy-Item -LiteralPath $TileXml -Destination (Join-Path $temporary 'tile.xml')
foreach ($pair in $resolved.GetEnumerator()) { Copy-Item -LiteralPath $pair.Value -Destination (Join-Path $temporary 'textures' $pair.Key) }
try {
    $parent = Split-Path -Path $BundlePath -Parent
    if ($parent) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
    if (Test-Path -LiteralPath $BundlePath) { Remove-Item -LiteralPath $BundlePath -Force }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::CreateFromDirectory($temporary, $BundlePath, [IO.Compression.CompressionLevel]::Optimal, $false)
} finally { Remove-Item -LiteralPath $temporary -Recurse -Force -ErrorAction SilentlyContinue }
Write-Output "Created $BundlePath with $($resolved.Count) texture(s). Copy it to Android and use Import tile."
