# Script PowerShell pour exécuter l'analyse des résultats de la campagne de tests

param (
    [string]$LogsDir = "../results/logs",
    [string]$OutputDir = "../results/analysis",
    [string]$FinalReportPath = "../results/final_analysis_report.md"
)

# Vérifier que les répertoires existent
if (-not (Test-Path $LogsDir)) {
    Write-Error "Le répertoire des logs n'existe pas: $LogsDir"
    exit 1
}

# Créer le répertoire de sortie s'il n'existe pas
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
    Write-Host "Répertoire de sortie créé: $OutputDir"
}

# Exécuter le script d'analyse Python
Write-Host "Exécution de l'analyse des résultats..."
python generate_analysis_report.py --log-dir $LogsDir --output-dir $OutputDir

# Vérifier si l'analyse a réussi
if ($LASTEXITCODE -ne 0) {
    Write-Error "L'analyse a échoué avec le code de sortie $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "Analyse terminée. Rapport généré: $OutputDir\analysis_report.md"
Write-Host "Visualisations générées dans: $OutputDir\visualizations"

# Afficher un résumé des résultats
Write-Host "`n=== Résumé des Résultats ==="
Write-Host "Modèles testés:"
Get-Content "$OutputDir\analysis_report.md" | Select-String -Pattern "^\| [^|]+ \| [0-9]+\.[0-9]+% \| [0-9]+\.[0-9]+ \| [0-9]+\.[0-9]+ \| \$[0-9]+\.[0-9]+ \| [0-9]+ \|$" | ForEach-Object {
    $line = $_ -replace '^\|', '' -replace '\|$', ''
    $parts = $line -split '\|' | ForEach-Object { $_.Trim() }
    Write-Host "- $($parts[0]): Taux de réussite = $($parts[1]), Coût moyen = $($parts[4])"
}

Write-Host "`nSeuils de complexité:"
$complexityLevels = @("Trivial", "Simple", "Medium", "Hard")
foreach ($level in $complexityLevels) {
    Write-Host "- Niveau $level:"
    Get-Content "$OutputDir\analysis_report.md" | Select-String -Pattern "^\| [^|]+ \| $level \| [0-9]+\.[0-9]+% \| (Oui|Non) \|$" | ForEach-Object {
        $line = $_ -replace '^\|', '' -replace '\|$', ''
        $parts = $line -split '\|' | ForEach-Object { $_.Trim() }
        Write-Host "  * $($parts[0]): $($parts[2]) - $($parts[3])"
    }
}

Write-Host "`n=== Fin de l'Analyse ==="