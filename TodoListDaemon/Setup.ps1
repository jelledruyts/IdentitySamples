# Creates a certificate that can be used for client authentication to Azure Active Directory
# and associates the certificate with an application registered in AAD.
# IMPORTANT: Make sure to connect using an administrator account in the AAD tenant (not a Microsoft Account).
# NOTE: This must be run using Microsoft Azure PowerShell.

# Configuration.
$aadClientId = "4707dd0f-52f8-4543-a2f0-d45099e54b3f"
$certificateName = "TodoListDaemon"
$certificateFileName = [System.IO.Path]::Combine($PSScriptRoot, $certificateName + ".cer")
$makecert = "C:\Program Files (x86)\Windows Kits\8.1\bin\x64\makecert.exe"

# Connect to Azure.
Write-Host "Connecting to your Azure Active Directory..."
Write-Host "Make sure to use an administrator account in the AAD tenant (not a Microsoft Account)." -ForegroundColor Red
Connect-MsolService

# Generate a client certificate to use for authentication.
# The makecert.exe tool is part of the Windows SDK, see http://msdn.microsoft.com/en-us/library/bfsktky3.aspx.
Write-Host "Creating the certificate..."
&$makecert -r -pe -n "CN=$certificateName" -ss My -len 2048 $certificateFileName
$certificate = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2
$certificate.Import($certificateFileName)
$certificateData = [System.Convert]::ToBase64String($certificate.GetRawCertData());

# Assign the certificate to the AAD Application.
Write-Host "Assigning the certificate to the application..."
New-MsolServicePrincipalCredential -AppPrincipalId $aadClientId -Type asymmetric -Value $certificateData -StartDate $certificate.NotBefore -EndDate $certificate.NotAfter -Usage Verify

# Verify that there is indeed a key.
Write-Host "Showing currently associated credentials..."
Get-MsolServicePrincipalCredential –ServicePrincipalName $aadClientId -ReturnKeyValues $false

# NOTE: To remove a certificate again, use the following line with the ID of the key as seen in the list above.
#Remove-MsolServicePrincipalCredential -KeyIds @("<key-id>") -ServicePrincipalName $aadClientId

Write-Host "Done."