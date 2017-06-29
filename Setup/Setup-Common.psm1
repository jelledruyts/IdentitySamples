function Get-ConfigurationValues ($FileName)
{
    # Read an XML file into an ordered dictionary where the XML element names are the configuration keys and the inner text is the value.
    Write-Host "Reading setup configuration file from ""$FileName"""
    $ConfigurationValues = [ordered]@{}
    [xml]$Xml = Get-Content -Path $FileName
    $Xml.ConfigurationValues.ChildNodes | Where-Object { $_.NodeType -eq "Element" } | ForEach-Object { $ConfigurationValues.Add($_.Name, $_.InnerText) }
    return $ConfigurationValues
}

function Save-ConfigurationValues ($FileName, $ConfigurationValues)
{
    Write-Host "Saving setup configuration file to ""$FileName"""
    $Xml = New-Object System.Xml.XmlDocument
    $RootElement = $Xml.CreateElement("ConfigurationValues")
    $RootElement = $Xml.AppendChild($RootElement)
    $ConfigurationValues.GetEnumerator() | ForEach-Object {
        $ConfigurationValueElement = $Xml.CreateElement($_.Key)
        $ConfigurationValueElement.InnerText = $_.Value
        $ConfigurationValueElement = $RootElement.AppendChild($ConfigurationValueElement)
    }
    $Xml.Save($FileName)
}

function Get-StringReplacedValue ($Text, $Pattern, $Key, $Value)
{
    # Replace the pattern using a regular expression.
    $Find = $Pattern -replace "_KEY_", $Key -replace "_VALUE_", ")(.*?)(?<post>"
    $Find = "(?<pre>$Find)"
    $Replace = "`${pre}$Value`${post}"
    $Text = $Text -replace $Find, $Replace
    return $Text.TrimEnd()
}

function Update-ConfigurationFile ($FileName, $ConfigurationValues, $Pattern, $QuoteChar, $UnquotedConfigurationKeys)
{
    # Read the entire configuration file.
    $FileContent = Get-Content -Raw $FileName

    # Replace each occurrence of the pattern for every configuration value.
    $ConfigurationValues.GetEnumerator() | ForEach-Object {
        $Key = $_.Key
        $Value = $_.Value
        if ($UnquotedConfigurationKeys.Contains($Key) -eq $false)
        {
            $Value = $QuoteChar + $Value + $QuoteChar
        }
        $FileContent = Get-StringReplacedValue -Text $FileContent -Pattern $Pattern -Key $Key -Value $Value
    }

    # Write the updated configuration file.
    Write-Host "Updating application configuration file ""$FileName"""
    Set-Content $FileName -Value $FileContent -Encoding UTF8
}

function Update-ConfigurationFiles ($SourceDir, $ConfigurationValues)
{
    # Define the configuration files to update per file type.
    $XmlConfigurationFiles = @(
        "$SourceDir\TodoListWebApi\Web.config"
        "$SourceDir\TodoListWebApp\Web.config"
        "$SourceDir\TodoListConsole\App.config"
        "$SourceDir\TodoListDaemon\App.config"
        "$SourceDir\TodoListWpf\App.config"
    )
    $JavaScriptConfigurationFiles = @(
        "$SourceDir\TodoListWebSpa\scripts\appConfig.js"
    )
    $CsharpConfigurationFiles = @(
        "$SourceDir\TodoListUniversalWindows10\AppConfiguration.cs"
    )
    $JsonConfigurationFiles = @(
        "$SourceDir\TaxonomyWebApi\appsettings.json"
        "$SourceDir\TodoListWebCore\appsettings.json"
    )

    # Don't quote configuration values with the keys below.
    $UnquotedConfigurationKeys = @(
        "StsSupportsLogOut"
        "CanValidateAuthority"
    )

    $XmlConfigurationFiles | ForEach-Object { Update-ConfigurationFile -FileName $_ -ConfigurationValues $ConfigurationValues -Pattern '<add key="_KEY_" value="_VALUE_" />' -QuoteChar "" -UnquotedConfigurationKeys $UnquotedConfigurationKeys }
    $JavaScriptConfigurationFiles | ForEach-Object { Update-ConfigurationFile -FileName $_ -ConfigurationValues $ConfigurationValues -Pattern '"?_KEY_"?: _VALUE_,?\r\n' -QuoteChar "'" -UnquotedConfigurationKeys $UnquotedConfigurationKeys }
    $CsharpConfigurationFiles | ForEach-Object { Update-ConfigurationFile -FileName $_ -ConfigurationValues $ConfigurationValues -Pattern '_KEY_ = _VALUE_;' -QuoteChar '"' -UnquotedConfigurationKeys $UnquotedConfigurationKeys }
    $JsonConfigurationFiles | ForEach-Object { Update-ConfigurationFile -FileName $_ -ConfigurationValues $ConfigurationValues -Pattern '"?_KEY_"?: _VALUE_,?\r\n' -QuoteChar '"' -UnquotedConfigurationKeys $UnquotedConfigurationKeys }
}

function Get-ClientCertificate ($CertificateFileName)
{
    Write-Host "Reading client certificate from file ""$CertificateFileName"""
    $SecurePfxPassword = Read-Host -AsSecureString -Prompt "Enter the password for the certificate PFX file"
    $Certificate = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($CertificateFileName, $SecurePfxPassword)
    return $Certificate
}

function Initialize-ClientCertificate ($SubjectName)
{
    Write-Host "Generating client certificate with subject name ""$SubjectName"""
    $StartDate = (Get-Date).AddDays(-1)
    $EndDate = (Get-Date).AddYears(10)
    $Certificate = New-SelfSignedCertificate -CertStoreLocation Cert:\CurrentUser\My -Subject $SubjectName -KeyExportPolicy Exportable -Provider "Microsoft Enhanced RSA and AES Cryptographic Provider" -NotBefore $StartDate -NotAfter $EndDate
    $CertificateFileName = "$PSScriptRoot\$SubjectName.pfx"
    Write-Host "Exporting client certificate to file ""$CertificateFileName"""
    $SecurePfxPassword = Read-Host -AsSecureString -Prompt "Enter a password for the certificate PFX file:"
    $PfxFile = Export-PfxCertificate -Cert "Cert:\CurrentUser\My\$($Certificate.Thumbprint)" -FilePath $CertificateFileName -Password $SecurePfxPassword
}

function New-ClientSecret
{
    $Bytes = New-Object Byte[] 32
    $Random = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $Random.GetBytes($Bytes)
    $Random.Dispose()
    return [System.Convert]::ToBase64String($Bytes)
}