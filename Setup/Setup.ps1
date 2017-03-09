# Import helper functions.
Import-Module -Name $PSScriptRoot\Setup-Common -Force
Import-Module -Name $PSScriptRoot\Setup-ADFS -Force
Import-Module -Name $PSScriptRoot\Setup-AzureAD -Force

# Read configuration values from XML.
$ConfigurationFileName = "$PSScriptRoot\ConfigurationValues.xml"
$ConfigurationValues = Get-ConfigurationValues -FileName $ConfigurationFileName

# Present a choice menu.
Write-Host ""
Write-Host "A - Initialize daemon service client certificate"
Write-Host "B - Register applications in AD FS (NOTE: this must be executed on the AD FS server)"
Write-Host "C - Register applications in Azure Active Directory"
Write-Host "D - Update configuration files inside the solution source code based on the current configuration values"
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
Save-ConfigurationValues -FileName $ConfigurationFileName -ConfigurationValues $ConfigurationValues
