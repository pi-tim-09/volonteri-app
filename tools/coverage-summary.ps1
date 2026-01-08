param(
    [string]$Root = "."
)

$covs = Get-ChildItem -Path $Root -Recurse -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue
if (-not $covs -or $covs.Count -eq 0) {
    Write-Error "No coverage.cobertura.xml files found under '$Root'. Run: dotnet test --collect:'XPlat Code Coverage'"
    exit 1
}

$linesCovered = 0
$linesValid = 0
$branchesCovered = 0
$branchesValid = 0

foreach ($cov in $covs) {
    [xml]$x = Get-Content -Path $cov.FullName
    $c = $x.coverage

    $linesCovered += [int]$c."lines-covered"
    $linesValid += [int]$c."lines-valid"
    $branchesCovered += [int]$c."branches-covered"
    $branchesValid += [int]$c."branches-valid"
}

$lineRate = if ($linesValid -gt 0) { [math]::Round(($linesCovered / $linesValid) * 100, 2) } else { 0 }
$branchRate = if ($branchesValid -gt 0) { [math]::Round(($branchesCovered / $branchesValid) * 100, 2) } else { 0 }

Write-Host "Coverage summary (aggregated Cobertura totals)"
Write-Host "  Line coverage:   $lineRate% ($linesCovered/$linesValid)"
Write-Host "  Branch coverage: $branchRate% ($branchesCovered/$branchesValid)"
