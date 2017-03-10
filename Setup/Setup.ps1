# Import helper functions.
Import-Module -Name $PSScriptRoot\Setup-Common -Force
Import-Module -Name $PSScriptRoot\Setup-ADFS -Force
Import-Module -Name $PSScriptRoot\Setup-AzureAD -Force

# Read configuration values from XML.
$ConfigurationFileName = "ConfigurationValues.xml"
$ConfigurationFilePath = "$PSScriptRoot\$ConfigurationFileName"
$ConfigurationValues = Get-ConfigurationValues -FileName $ConfigurationFilePath

# Present a choice menu.
Write-Host ""
Write-Host "A - Initialize daemon service client certificate"
Write-Host "      This generates a self-signed client certificate on your machine and exports it to a PFX file."
Write-Host "      This is required, otherwise the application registration will fail."
Write-Host ""
Write-Host "B - Register applications in AD FS"
Write-Host "      NOTE: This must be executed on the AD FS server directly."
Write-Host "      Please copy all files from this directory (including the client certificate PFX file) to the server"
Write-Host "      and once complete make sure to copy the ""$ConfigurationFileName"" file back to the development machine"
Write-Host "      before updating the configuration files."
Write-Host ""
Write-Host "C - Register applications in Azure Active Directory"
Write-Host "      NOTE: Make sure to log in with an admin account on Azure Active Directory."
Write-Host ""
Write-Host "D - Update configuration files inside the solution source code"
Write-Host "      This takes the configuration values from the ""$ConfigurationFileName"" XML file and updates all"
Write-Host "      relevant configuration files in the solution's source code directory."
Write-Host ""
$Choice = Read-Host -Prompt "Type your choice and press Enter"
Write-Host ""
switch ($Choice)
{
    "A"
    {
        Initialize-ClientCertificate -SubjectName $ConfigurationValues["TodoListDaemonCertificateName"]
        break
    }
    "B"
    {
        Initialize-AdfsApplicationGroup -ConfigurationValues $ConfigurationValues
        Write-Warning "Make sure to copy the ""$ConfigurationFileName"" file back into the Setup folder on the machine where the source code is located, and update the configuration source files."
        break
    }
    "C"
    {
        $TenantName = Read-Host -Prompt "Enter your Azure Active Directory tenant name"
        if ($TenantName)
        {
            Initialize-AzureAD -ConfigurationValues $ConfigurationValues -AzureADInstance "https://login.microsoftonline.com/" -TenantName $TenantName
        }
        break
    }
    "D"
    {
        # Update the configuration in the source code using the current values.
        Update-ConfigurationFiles -SourceDir "$PSScriptRoot\.." -ConfigurationValues $ConfigurationValues
        break
    }
}

# Save configuration values back to XML.
Save-ConfigurationValues -FileName $ConfigurationFilePath -ConfigurationValues $ConfigurationValues
