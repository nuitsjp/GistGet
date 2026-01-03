# Update namespaces in all C# files

$files = Get-ChildItem -Path "src\NuitsJp.GistGet" -Filter "*.cs" -Recurse
foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    # Update namespace declarations
    $content = $content -replace 'namespace GistGet\.Infrastructure\.WinGet', 'namespace NuitsJp.GistGet.Infrastructure.WinGet'
    $content = $content -replace 'namespace GistGet\.Infrastructure\.Diagnostics', 'namespace NuitsJp.GistGet.Infrastructure.Diagnostics'
    $content = $content -replace 'namespace GistGet\.Infrastructure', 'namespace NuitsJp.GistGet.Infrastructure'
    $content = $content -replace 'namespace GistGet\.Presentation', 'namespace NuitsJp.GistGet.Presentation'
    $content = $content -replace 'namespace GistGet;', 'namespace NuitsJp.GistGet;'
    # Update using directives
    $content = $content -replace 'using GistGet\.Infrastructure\.WinGet', 'using NuitsJp.GistGet.Infrastructure.WinGet'
    $content = $content -replace 'using GistGet\.Infrastructure\.Diagnostics', 'using NuitsJp.GistGet.Infrastructure.Diagnostics'
    $content = $content -replace 'using GistGet\.Infrastructure', 'using NuitsJp.GistGet.Infrastructure'
    $content = $content -replace 'using GistGet\.Presentation', 'using NuitsJp.GistGet.Presentation'
    $content = $content -replace 'using GistGet\.Resources', 'using NuitsJp.GistGet.Resources'
    $content = $content -replace 'using GistGet;', 'using NuitsJp.GistGet;'
    Set-Content -Path $file.FullName -Value $content
}

$testFiles = Get-ChildItem -Path "src\NuitsJp.GistGet.Test" -Filter "*.cs" -Recurse
foreach ($file in $testFiles) {
    $content = Get-Content $file.FullName -Raw
    # Update namespace declarations
    $content = $content -replace 'namespace GistGet\.Test\.Infrastructure\.Diagnostics', 'namespace NuitsJp.GistGet.Test.Infrastructure.Diagnostics'
    $content = $content -replace 'namespace GistGet\.Test\.Infrastructure', 'namespace NuitsJp.GistGet.Test.Infrastructure'
    $content = $content -replace 'namespace GistGet\.Test\.Presentation', 'namespace NuitsJp.GistGet.Test.Presentation'
    $content = $content -replace 'namespace GistGet\.Test', 'namespace NuitsJp.GistGet.Test'
    # Update using directives
    $content = $content -replace 'using GistGet\.Infrastructure\.WinGet', 'using NuitsJp.GistGet.Infrastructure.WinGet'
    $content = $content -replace 'using GistGet\.Infrastructure\.Diagnostics', 'using NuitsJp.GistGet.Infrastructure.Diagnostics'
    $content = $content -replace 'using GistGet\.Infrastructure', 'using NuitsJp.GistGet.Infrastructure'
    $content = $content -replace 'using GistGet\.Presentation', 'using NuitsJp.GistGet.Presentation'
    $content = $content -replace 'using GistGet;', 'using NuitsJp.GistGet;'
    Set-Content -Path $file.FullName -Value $content
}

Write-Host "Namespace update completed." -ForegroundColor Green
