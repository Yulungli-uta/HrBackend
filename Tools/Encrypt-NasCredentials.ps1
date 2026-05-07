param(
    [Parameter(Mandatory=$true)]
    [string]$Username,

    [Parameter(Mandatory=$true)]
    [SecureString]$Password,

    [string]$Domain = "",

    # Debe coincidir con FileManagement:EncryptionKey en appsettings.json
    [string]$Key = "K7mQ9xP2vL8rT5nB3zY6cW1sA4dF0hJ2"
)

function Encrypt-Value([string]$plain, [string]$k) {
    # Mismo algoritmo que CredentialEncryptor.cs:
    # Key = UTF8(key), IV = SHA256(key)[0..15], AES-256 CBC PKCS7
    $keyBytes = [Text.Encoding]::UTF8.GetBytes($k)

    $sha  = [Security.Cryptography.SHA256]::Create()
    $iv   = ($sha.ComputeHash($keyBytes))[0..15]

    $aes  = [Security.Cryptography.Aes]::Create()
    $aes.Key     = $keyBytes
    $aes.IV      = $iv
    $aes.Mode    = [Security.Cryptography.CipherMode]::CBC
    $aes.Padding = [Security.Cryptography.PaddingMode]::PKCS7

    $enc = $aes.CreateEncryptor()
    $ms  = New-Object IO.MemoryStream
    $cs  = New-Object Security.Cryptography.CryptoStream($ms, $enc,
               [Security.Cryptography.CryptoStreamMode]::Write)
    $sw  = New-Object IO.StreamWriter($cs)
    $sw.Write($plain)
    $sw.Flush()
    $cs.FlushFinalBlock()

    [Convert]::ToBase64String($ms.ToArray())
}

# Extraer contraseña del SecureString sin exponerla en memoria de texto claro más de lo necesario
$bstr    = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password)
$passPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)

$encUser   = Encrypt-Value $Username $Key
$encPass   = Encrypt-Value $passPlain $Key
$encDomain = if ($Domain) { Encrypt-Value $Domain $Key } else { $null }

Write-Host ""
Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host "  Valores encriptados para appsettings.json" -ForegroundColor Cyan
Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Reemplaza el bloque FileManagement en appsettings.json:" -ForegroundColor Yellow
Write-Host ""
Write-Host '  "FileManagement": {'
Write-Host '    "UseImpersonation": true,'
Write-Host "    `"EncryptionKey`": `"$Key`","
Write-Host '    "NetworkCredentials": {'
Write-Host "      `"Username`": `"$encUser`","
Write-Host "      `"Password`": `"$encPass`","
if ($encDomain) {
    Write-Host "      `"Domain`": `"$encDomain`""
} else {
    Write-Host '      "Domain": ""'
}
Write-Host '    }'
Write-Host '  }'
Write-Host ""
