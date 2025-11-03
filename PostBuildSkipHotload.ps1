param(
    [string]$ProjectDir
)

$codeDir = Join-Path $ProjectDir "Code"
if (!(Test-Path $codeDir)) {
    Write-Host "Code-Verzeichnis nicht gefunden: $codeDir"
    exit
}

# Get changed files from git
$changedFiles = & git status --porcelain | ForEach-Object { $_.Substring(3) } | Where-Object { $_ -like "*.cs" }

$csFiles = Get-ChildItem -Path $codeDir -Filter "*.cs" -Recurse

foreach ($file in $csFiles) {
    $relativePath = $file.FullName.Replace($codeDir, "").TrimStart("\")
    $isChanged = $changedFiles -contains $relativePath

    $content = Get-Content $file.FullName -Raw
    $originalContent = $content

    # Regex für Klassen, die Component erben
    $classRegex = [regex]"(?m)class\s+(\w+)\s*:\s*Component"
    $matches = $classRegex.Matches($content)

    foreach ($match in $matches) {
        $className = $match.Groups[1].Value
        $classIndex = $match.Index
        $lineStart = $content.LastIndexOf("`n", $classIndex) + 1
        $beforeClass = $content.Substring(0, $lineStart)

        if ($isChanged) {
            # Remove [SkipHotload] if present
            $skipRegex = [regex]"\[SkipHotload\]\s*`n"
            $content = $skipRegex.Replace($content, "")
            Write-Host "[SkipHotload] entfernt von Klasse $className in $($file.Name) (geändert)."
        } else {
            # Add [SkipHotload] if not present
            if ($beforeClass -notmatch "\[SkipHotload\]") {
                $insert = "[SkipHotload]`n"
                $content = $content.Insert($lineStart, $insert)
                Write-Host "[SkipHotload] hinzugefügt zu Klasse $className in $($file.Name) (unverändert)."
            }
        }
    }

    if ($content -ne $originalContent) {
        Set-Content $file.FullName $content -NoNewline
    }
}

Write-Host "Post-Build: [SkipHotload] basierend auf Git-Änderungen verwaltet."