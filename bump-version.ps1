param([Parameter(Mandatory = $true)][string]$Bump)

# Rewrites <ApplicationDisplayVersion> in the csproj. That element is the only
# place the launcher version lives: Android versionName and the APK file name
# built by build-release.cmd both come from it.
$csproj = Join-Path $PSScriptRoot 'SMAPIGameLoader\SMAPIGameLoader.csproj'
$text = [System.IO.File]::ReadAllText($csproj)

$m = [regex]::Match($text, '<ApplicationDisplayVersion>(\d+)\.(\d+)\.(\d+)</ApplicationDisplayVersion>')
if (-not $m.Success) {
    Write-Error 'ApplicationDisplayVersion x.y.z not found in SMAPIGameLoader.csproj'
    exit 1
}

$old = $m.Groups[1].Value + '.' + $m.Groups[2].Value + '.' + $m.Groups[3].Value
$major = [int]$m.Groups[1].Value
$minor = [int]$m.Groups[2].Value
$patch = [int]$m.Groups[3].Value

if ($Bump -match '^\d+\.\d+\.\d+$') {
    $parts = $Bump.Split('.')
    $major = [int]$parts[0]
    $minor = [int]$parts[1]
    $patch = [int]$parts[2]
}
elseif ($Bump -ieq 'major') { $major++; $minor = 0; $patch = 0 }
elseif ($Bump -ieq 'minor') { $minor++; $patch = 0 }
elseif ($Bump -ieq 'patch') { $patch++ }
else {
    Write-Error "invalid argument '$Bump' (expected major | minor | patch | x.y.z)"
    exit 1
}

$new = "$major.$minor.$patch"
if ($new -eq $old) {
    Write-Host "[bump] already $old"
    exit 0
}

[System.IO.File]::WriteAllText(
    $csproj,
    $text.Remove($m.Index, $m.Length).Insert($m.Index, "<ApplicationDisplayVersion>$new</ApplicationDisplayVersion>"),
    (New-Object System.Text.UTF8Encoding($true))  # keep the file's UTF-8 BOM
)
Write-Host "[bump] $old -> $new"
