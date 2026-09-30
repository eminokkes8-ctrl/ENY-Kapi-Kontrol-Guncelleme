$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$stage = Join-Path ([System.IO.Path]::GetTempPath()) ('ENY_UpdatePackage_' + [guid]::NewGuid().ToString('N'))
$package = Join-Path $root 'ENY_Kapi_Kontrol_Update.zip'
$checksumFile = $package + '.sha256'
$sdk = Join-Path $root 'sdk'

if (-not (Test-Path -LiteralPath (Join-Path $root 'ENY_Kapi_Kontrol.exe'))) {
    throw 'ENY_Kapi_Kontrol.exe bulunamadi. Once build.bat calistirin.'
}
if (-not (Test-Path -LiteralPath (Join-Path $sdk 'HCNetSDK.dll'))) {
    throw 'sdk\HCNetSDK.dll bulunamadi.'
}

try {
    $sdkFiles = Get-ChildItem -LiteralPath $sdk -Filter '*.dll' -File
    $componentDirectory = Join-Path $sdk 'HCNetSDKCom'
    $componentFiles = Get-ChildItem -LiteralPath $componentDirectory -Filter '*.dll' -File
    if ($sdkFiles.Count -eq 0 -or $componentFiles.Count -eq 0) {
        throw 'Hikvision SDK DLL dosyalari eksik.'
    }

    $componentStage = Join-Path $stage 'HCNetSDKCom'
    New-Item -ItemType Directory -Path $componentStage -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $root 'ENY_Kapi_Kontrol.exe') -Destination $stage
    Copy-Item -Path (Join-Path $sdk '*.dll') -Destination $stage
    Copy-Item -Path (Join-Path $componentDirectory '*.dll') -Destination $componentStage

    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $package -CompressionLevel Optimal -Force
    $hash = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash.ToLowerInvariant()
    [System.IO.File]::WriteAllText($checksumFile, $hash)

    Write-Output "Paket: $package"
    Write-Output "SHA-256: $checksumFile"
    Write-Output "Hash: $hash"
}
finally {
    if (Test-Path -LiteralPath $stage) {
        Remove-Item -LiteralPath $stage -Recurse -Force
    }
}