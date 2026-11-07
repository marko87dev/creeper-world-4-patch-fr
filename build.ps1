$ErrorActionPreference = 'Stop'
$projectDirectory = $PSScriptRoot
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compilerPath)) {
    throw 'Le compilateur C# du .NET Framework 4 (64 bits) est introuvable.'
}
$payloadHashes = @{
    'payload\assets.delta' = '131265ed3f13b2aa678b6eb01d28189b180f3e19b334042e8509a7fa24ceab3d'
    'payload\metadata.delta' = 'af4619c00f1305d2377458a5867fe201c9e26e91d4c02913e6ff7cb0db040225'
}
foreach ($relativePath in $payloadHashes.Keys) {
    $filePath = Join-Path $projectDirectory $relativePath
    if ((Get-FileHash -LiteralPath $filePath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $payloadHashes[$relativePath]) {
        throw "Le correctif binaire est absent ou modifie : $relativePath"
    }
}
[System.IO.Directory]::CreateDirectory((Join-Path $projectDirectory 'build')) | Out-Null
Push-Location -LiteralPath $projectDirectory
try {
    $compilerArguments = @(
        '/nologo', '/optimize+', '/platform:x64', '/target:winexe',
        '/out:build\CW4_Patch_FR_1.0_2.5.1.exe',
        '/win32manifest:src\app.manifest',
        '/reference:System.Windows.Forms.dll', '/reference:System.Drawing.dll',
        '/resource:payload\assets.delta,PayloadAssets',
        '/resource:payload\metadata.delta,PayloadMetadata',
        'src\Program.cs', 'src\InstallerEngine.cs', 'src\Delta.cs',
        'src\Bundle.cs', 'src\AssemblyInfo.cs'
    )
    & $compilerPath @compilerArguments
    if ($LASTEXITCODE -ne 0) { throw 'La compilation a echoue.' }
    Write-Host 'Installateur compile : build\CW4_Patch_FR_1.0_2.5.1.exe'
}
finally { Pop-Location }
