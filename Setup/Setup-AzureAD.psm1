function Get-GraphApiAccessToken ($Authority)
{
    # Load ADAL.
    Add-Type -Path "$PSScriptRoot\Microsoft.IdentityModel.Clients.ActiveDirectory.dll"
    Add-Type -Path "$PSScriptRoot\Microsoft.IdentityModel.Clients.ActiveDirectory.WindowsForms.dll"

    # Acquire a token to access the Graph API.
    $ClientId = "1950a258-227b-4e31-a9cf-717495945fc2" # Well-Known Client ID for Azure PowerShell.
    $RedirectUri = "urn:ietf:wg:oauth:2.0:oob"
    $ResourceAppIdURI = "https://graph.windows.net"
    $AuthContext = New-Object Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext($Authority)
    $AuthResult = $AuthContext.AcquireToken($ResourceAppIdURI, $ClientId, $RedirectUri, "Always")
    return $AuthResult.AccessToken
}

function Send-GraphApiRequest ($TenantName, $Headers, $Path, $Method, $Body)
{
    $Uri = "https://graph.windows.net/$($TenantName)/$($Path)?api-version=1.6"
    $Result = Invoke-RestMethod -Uri $Uri -Headers $Headers -Body $Body -Method $Method
	Start-Sleep -Seconds 5
    return $Result
}

function Send-GraphApiPostRequest ($TenantName, $Headers, $Path, $Body)
{
    $BodyJson = ConvertTo-Json -InputObject $Body -Depth 100
    $Result = Send-GraphApiRequest -TenantName $TenantName -Headers $Headers -Path $Path -Method "POST" -Body $BodyJson
    return $Result
}

function New-AzureADApplication ($TenantName, $Headers, $ApplicationDefinition)
{
    $Application = Send-GraphApiPostRequest -TenantName $TenantName -Headers $Headers -Path "applications" -Body $ApplicationDefinition
    return $Application
}

function New-AzureADServicePrincipal ($TenantName, $Headers, $AppId, $KeyCredentials)
{
    if (!$KeyCredentials)
    {
        $KeyCredentials = @()
    }
    $ApplicationServicePrincipalDefinition = @{
        "appId" = $AppId
        "keyCredentials" = $KeyCredentials
    }
    $ApplicationServicePrincipal = Send-GraphApiPostRequest -TenantName $TenantName -Headers $Headers -Path "servicePrincipals" -Body $ApplicationServicePrincipalDefinition
    return $ApplicationServicePrincipal
} 

function Get-AzureADApplications ($TenantName, $Headers)
{
    $Uri = "https://graph.windows.net/$($TenantName)/applications?api-version=1.6"
    $Result = Invoke-RestMethod -Uri $Uri -Headers $Headers -Method GET
    return $Result.Value
}

function Remove-AzureADApplication ($TenantName, $Headers, $ObjectId)
{
    $Uri = "https://graph.windows.net/$($TenantName)/applications/$($ObjectId)?api-version=1.6"
    $Result = Invoke-RestMethod -Uri $Uri -Headers $Headers -Method DELETE
}

function Add-AzureADRoleMember ($TenantName, $Headers, $RoleObjectId, $ServicePrincipalObjectId)
{
    $Path = "directoryRoles/$($RoleObjectId)/`$links/members"
    $Body = @{
        "url" = "https://graph.windows.net/$($TenantName)/servicePrincipals/$($ServicePrincipalObjectId)"
    }
    $Result = Send-GraphApiPostRequest -TenantName $TenantName -Headers $Headers -Path $Path -Body $Body
}

function Get-AzureADRole ($TenantName, $Headers, $RoleTemplateId)
{
    $DirectoryRoles = Send-GraphApiRequest -TenantName $TenantName -Headers $Headers -Path "directoryRoles" -Method GET
    $DirectoryRole = $DirectoryRoles.value | Where-Object { $_.roleTemplateId -eq $RoleTemplateId } | Select-Object -First 1
    if (!$DirectoryRole)
    {
        # If the role does not exist yet, activate it first based on the requested role template.
        $DirectoryRole = Send-GraphApiPostRequest -TenantName $TenantName -Headers $Headers -Path "directoryRoles" -Body @{ "roleTemplateId" = $RoleTemplateId }
    }
    return $DirectoryRole
}

function Grant-AzureADAdminConsentOnOAuth2Permission ($TenantName, $Headers, $ClientServicePrincipalObjectId, $ResourceServicePrincipalObjectId, $Scope)
{
    $oauth2PermissionGrant = @{
        "clientId" = $ClientServicePrincipalObjectId # The service principal Object ID of the application
        "consentType" = "AllPrincipals" # Grant admin consent for all principals
        "expiryTime" = (Get-Date).AddYears(10).ToString("u").Replace(" ", "T")
        "resourceId" = $ResourceServicePrincipalObjectId # The service principal Object ID representing the resource
        "scope" = $Scope # The required scope(s)
    }
    $Result = Send-GraphApiPostRequest -TenantName $TenantName -Headers $Headers -Path "oauth2PermissionGrants" -Body $oauth2PermissionGrant
}

function Grant-AzureADAdminConsentOnAppRole ($TenantName, $Headers, $ClientServicePrincipalObjectId, $ResourceServicePrincipalObjectId, $AppRoleId)
{
    $appRoleAssignment = @{
       "id" = $AppRoleId # The ID of the App Role being granted
       "principalId" = $ClientServicePrincipalObjectId # The service principal Object ID of the application
       "resourceId" = $ResourceServicePrincipalObjectId # The service principal Object ID representing the resource
     }
    $Result = Send-GraphApiPostRequest -TenantName $TenantName -Headers $Headers -Path "servicePrincipals/$ClientServicePrincipalObjectId/appRoleAssignments" -Body $appRoleAssignment
}

function Get-AzureADOAuth2PermissionId ($AzureADApplication, $PermissionValue)
{
    $Permission = $AzureADApplication.oauth2Permissions | Where { $_.value -eq $PermissionValue }
    if (!$Permission)
    {
        throw "The OAuth 2.0 permission ""$PermissionValue"" was not found in the application object"
    }
    return $Permission.id
}

function Get-AzureADAppRoleId ($AzureADApplication, $RoleValue)
{
    $AppRole = $AzureADApplication.appRoles | Where { $_.value -eq $RoleValue }
    if (!$AppRole)
    {
        throw "The App Role ""$RoleValue"" was not found in the application object"
    }
    return $AppRole.id
}

function Initialize-AzureAD ($ConfigurationValues, $AzureADInstance, $TenantName)
{
    # Authenticate.
    $AzureADAuthority = "$AzureADInstance$TenantName"
    Write-Warning "Authorizing access to Azure Active Directory. Please log in with an admin account of the directory itself (not an external account)!"
    $GraphApiToken = Get-GraphApiAccessToken -Authority $AzureADAuthority
    $Headers = @{
        "Content-Type" = "application/json"
        "Authorization" = "Bearer $GraphApiToken"
    }

    # Determine the STS configuration values.
    $ConfigurationValues["StsRootUrl"] = $AzureADInstance
    $ConfigurationValues["StsPath"] = $TenantName
    $ConfigurationValues["StsAccessTokenIssuer"] = $null
    $ConfigurationValues["StsIdTokenIssuer"] = $null
    $ConfigurationValues["StsSupportsLogOut"] = "true"
    $ConfigurationValues["CanValidateAuthority"] = "true"
    $ConfigurationValues["AccountProviderAuthority"] = $AzureADAuthority

    # Set up some variables.
    $CredentialStartDate = (Get-Date).AddDays(-1).ToString("u").Replace(" ", "T")
    $CredentialEndDate = (Get-Date).AddYears(10).ToString("u").Replace(" ", "T")
    $TaxonomyApiDisplayName = "Taxonomy API"
    $TodoListApiDisplayName = "TodoList API"
    $WebSpaClientDisplayName = "TodoList Web SPA Client"
    $WebAppClientDisplayName = "TodoList Web App Client"
    $WebCoreClientDisplayName = "TodoList Web Core Client"
    $WebFormsClientDisplayName = "TodoList WebForms Client"
    $WpfClientDisplayName = "TodoList WPF Client"
    $ConsoleClientDisplayName = "TodoList Console Client"
    $Windows10ClientDisplayName = "TodoList Windows 10 Client"
    $DaemonClientDisplayName = "TodoList Daemon Client"
    $ApplicationDisplayNames = @($TaxonomyApiDisplayName, $TodoListApiDisplayName, $WebSpaClientDisplayName, $WebAppClientDisplayName, $WebCoreClientDisplayName, $WebFormsClientDisplayName, $WpfClientDisplayName, $ConsoleClientDisplayName, $Windows10ClientDisplayName, $DaemonClientDisplayName)

    # Ensure we start from scratch.
    $ExistingApplications = Get-AzureADApplications -TenantName $TenantName -Headers $Headers
    $ExistingApplications | Where-Object { $ApplicationDisplayNames.Contains($_.displayName) } | ForEach-Object {
        Write-Host "Deleting Azure AD application ""$($_.displayName)"""
        Remove-AzureADApplication -TenantName $TenantName -Headers $Headers -ObjectId $_.objectId
    }

    # Activate and retrieve the "Directory Readers" and "Directory Writers" roles in the directory.
    $DirectoryReaderRole = Get-AzureADRole -TenantName $TenantName -Headers $Headers -RoleTemplateId "88d8e3e3-8f55-4a1e-953a-9b9898b8876b"
    $DirectoryWriterRole = Get-AzureADRole -TenantName $TenantName -Headers $Headers -RoleTemplateId "9360feb5-f418-4baa-8175-e2a00bac4301"

	# Wait a bit after deleting the applications before registering them again.
	Start-Sleep -Seconds 5

    # Register the Server application for the Taxonomy API.
    Write-Host "Creating ""$TaxonomyApiDisplayName"" in Azure AD"
    $ConfigurationValues["TaxonomyWebApiClientSecret"] = New-ClientSecret
    $TaxonomyApiDefinition = @{
        "displayName" = $TaxonomyApiDisplayName
        "groupMembershipClaims" = "SecurityGroup" # Emit (security) group membership claims
        "replyUrls" = @($ConfigurationValues["TaxonomyWebApiRootUrl"])
        "identifierUris" = @($ConfigurationValues["TaxonomyWebApiResourceId"])
        "requiredResourceAccess" = @( # Define access to other applications
            @{
                "resourceAppId" = "00000002-0000-0000-c000-000000000000" # Azure Active Directory
                "resourceAccess" = @(
                    @{
                        "id" = "311a71cc-e848-46a1-bdf8-97ff7156d8e6" # "User.Read": Sign in and read user profile
                        "type" = "Scope" # Delegated (User) Permission
                    }
                )
            }
        )
        "passwordCredentials" = @( # Add a client secret
            @{
                "keyId" = New-Guid
                "startDate" = $CredentialStartDate
                "endDate" = $CredentialEndDate
                "value" = $ConfigurationValues["TaxonomyWebApiClientSecret"]
            }
        )
    }
    $TaxonomyApi = New-AzureADApplication -TenantName $TenantName -Headers $Headers -ApplicationDefinition $TaxonomyApiDefinition
    # Create the service principal representing the application instance in the directory.
    $TaxonomyApiServicePrincipal = New-AzureADServicePrincipal -TenantName $TenantName -Headers $Headers -AppId $TaxonomyApi.appId
    # Add the service principal to the "Directory Readers" role (which is done automatically when an admin consents to the application when it needs directory read permissions).
    Add-AzureADRoleMember -TenantName $TenantName -Headers $Headers -RoleObjectId $DirectoryReaderRole.objectId -ServicePrincipalObjectId $TaxonomyApiServicePrincipal.objectId
    $ConfigurationValues["TaxonomyWebApiClientId"] = $TaxonomyApi.appId
    # Note that the "user_impersonation" permission is automatically added.
    $TaxonomyApiUserImpersonationPermissionId = Get-AzureADOAuth2PermissionId -AzureADApplication $TaxonomyApi -PermissionValue "user_impersonation"

    # Register the Server application for the TodoList API.
    Write-Host "Creating ""$TodoListApiDisplayName"" in Azure AD"
    $ConfigurationValues["TodoListWebApiClientSecret"] = New-ClientSecret
    $TodoListApiDefinition = @{
        "displayName" = $TodoListApiDisplayName
        "groupMembershipClaims" = "SecurityGroup" # Emit (security) group membership claims
        "replyUrls" = @($ConfigurationValues["TodoListWebApiRootUrl"])
        "identifierUris" = @($ConfigurationValues["TodoListWebApiResourceId"])
        "requiredResourceAccess" = @( # Define access to other applications
            @{
                "resourceAppId" = "00000002-0000-0000-c000-000000000000" # Declare access to Azure Active Directory
                "resourceAccess" = @(
                    @{
                        "id" = "311a71cc-e848-46a1-bdf8-97ff7156d8e6" # "User.Read": Sign in and read user profile
                        "type" = "Scope" # Delegated (User) Permission
                    }
                )
            },
            @{
                "resourceAppId" = $TaxonomyApi.appId # Declare access to the Taxonomy API
                "resourceAccess" = @(
                    @{
                        "id" = $TaxonomyApiUserImpersonationPermissionId # The ID for the "user_impersonation" access permission
                        "type" = "Scope" # Delegated (User) Permission
                    }
                )
            }
        )
        "oauth2Permissions" = @( # Define app-specific delegated (user) permissions
            @{
                "id" = New-Guid
                "value" = "Todo.Read"
                "type" = "User" # User consent is allowed (otherwise use "Admin" for admin-only consent)
                "adminConsentDescription" = "Allow the application to read todo's on behalf of the signed-in user."
                "adminConsentDisplayName" = "Read todo's"
                "userConsentDescription" = "Allow the application to read todo's on your behalf."
                "userConsentDisplayName" = "Read todo's"
            },
            @{
                "id" = New-Guid
                "value" = "Todo.ReadWrite"
                "type" = "User" # User consent is allowed (otherwise use "Admin" for admin-only consent)
                "adminConsentDescription" = "Allow the application to write todo's on behalf of the signed-in user."
                "adminConsentDisplayName" = "Write todo's"
                "userConsentDescription" = "Allow the application to write todo's on your behalf."
                "userConsentDisplayName" = "Write todo's"
            },
            @{
                "id" = New-Guid
                "value" = "Todo.Read.All"
                "type" = "Admin" # Admin consent is required to have read permissions on todo's of all users
                "adminConsentDescription" = "Allow the application to read todo's of all users."
                "adminConsentDisplayName" = "Read all todo's"
                "userConsentDescription" = "Allow the application to write todo's of all users."
                "userConsentDisplayName" = "Read all todo's"
            }
        )
        "appRoles" =  @( # Define app-specific roles
            @{
                "id" = New-Guid
                "value" = "administrator"
                "displayName" = "Administrator"
                "description" = "Administrators can manage the application"
                "allowedMemberTypes" = @("User") # Only users can get this role
            },
            @{
                "id" = New-Guid
                "value" = "contributor"
                "displayName" = "Contributor"
                "description" = "Contributors can manage their own todo lists"
                "allowedMemberTypes" = @("User") # Only users can get this role
            },
            @{
                "id" = New-Guid
                "value" = "Todo.ReadWrite.All"
                "displayName" = "Read and write all todo's"
                "description" = "Application is allowed to read and write todo's for all users"
                "allowedMemberTypes" = @("Application") # Only applications can get this role
            }
        )
        "passwordCredentials" = @( # Add a client secret
            @{
                "keyId" = New-Guid
                "startDate" = $CredentialStartDate
                "endDate" = $CredentialEndDate
                "value" = $ConfigurationValues["TodoListWebApiClientSecret"]
            }
        )
    }
    $TodoListApi = New-AzureADApplication -TenantName $TenantName -Headers $Headers -ApplicationDefinition $TodoListApiDefinition
    # Create the service principal representing the application instance in the directory.
    $TodoListApiServicePrincipal = New-AzureADServicePrincipal -TenantName $TenantName -Headers $Headers -AppId $TodoListApi.appId
    # Add the service principal to the "Directory Writers" role (which is done automatically when an admin consents to the application when it needs directory write permissions).
    Add-AzureADRoleMember -TenantName $TenantName -Headers $Headers -RoleObjectId $DirectoryWriterRole.objectId -ServicePrincipalObjectId $TodoListApiServicePrincipal.objectId
    $ConfigurationValues["TodoListWebApiClientId"] = $TodoListApi.appId
    # Note that the "user_impersonation" permission is automatically added.
    $TodoListApiTodoReadPermissionId = Get-AzureADOAuth2PermissionId -AzureADApplication $TodoListApi -PermissionValue "Todo.Read"
    $TodoListApiTodoReadWritePermissionId = Get-AzureADOAuth2PermissionId -AzureADApplication $TodoListApi -PermissionValue "Todo.ReadWrite"
    $TodoListApiTodoReadAllPermissionId = Get-AzureADOAuth2PermissionId -AzureADApplication $TodoListApi -PermissionValue "Todo.Read.All"
	$TodoListApiTodoReadWriteAllAppRoleId = Get-AzureADAppRoleId -AzureADApplication $TodoListApi -RoleValue "Todo.ReadWrite.All"
    # Grant admin consent for the TodoList API to access the Taxonomy API.
    Grant-AzureADAdminConsentOnOAuth2Permission -TenantName $TenantName -Headers $Headers -ClientServicePrincipalObjectId $TodoListApiServicePrincipal.objectId -ResourceServicePrincipalObjectId $TaxonomyApiServicePrincipal.objectId -Scope "user_impersonation"
    
    # Register the Server application for the Daemon app.
    Write-Host "Creating ""$DaemonClientDisplayName"" in Azure AD"
    $ConfigurationValues["TodoListDaemonClientSecret"] = New-ClientSecret
    $DaemonClientDefinition = @{
        "displayName" = $DaemonClientDisplayName
        "groupMembershipClaims" = "SecurityGroup" # Emit (security) group membership claims
        "identifierUris" = @($ConfigurationValues["TodoListDaemonResourceId"])
        "requiredResourceAccess" = @( # Define access to other applications
            @{
                "resourceAppId" = "00000002-0000-0000-c000-000000000000" # Declare access to Azure Active Directory
                "resourceAccess" = @(
                    @{
                        "id" = "311a71cc-e848-46a1-bdf8-97ff7156d8e6" # "User.Read": Sign in and read user profile
                        "type" = "Scope" # Delegated (User) Permission
                    }
                )
            },
            @{
                "resourceAppId" = $TodoListApi.appId # Declare access to the TodoList API
                "resourceAccess" = @(
                    @{
                        "id" = $TodoListApiTodoReadPermissionId # The ID for the "Todo.Read" access permission
                        "type" = "Scope" # Delegated (User) Permission
                    },
                    @{
                        "id" = $TodoListApiTodoReadWritePermissionId # The ID for the "Todo.ReadWrite" access permission
                        "type" = "Scope" # Delegated (User) Permission
                    },
                    @{
                        "id" = $TodoListApiTodoReadWriteAllAppRoleId # The ID for the "Todo.ReadWrite.All" app role
                        "type" = "Role" # Application Permission
                    }
                )
            }
        )
        "passwordCredentials" = @( # Add a client secret
            @{
                "keyId" = New-Guid
                "startDate" = $CredentialStartDate
                "endDate" = $CredentialEndDate
                "value" = $ConfigurationValues["TodoListDaemonClientSecret"]
            }
        )
    }
    $DaemonClient = New-AzureADApplication -TenantName $TenantName -Headers $Headers -ApplicationDefinition $DaemonClientDefinition
    $DaemonClientCertificateSubjectName = $ConfigurationValues["TodoListDaemonCertificateName"]
    $DaemonClientCertificateFileName = "$PSScriptRoot\$DaemonClientCertificateSubjectName.pfx"
    if (Test-Path $DaemonClientCertificateFileName)
    {
        $DaemonClientCertificate = Get-ClientCertificate -CertificateFileName $DaemonClientCertificateFileName
        $DaemonClientCertificateData = [System.Convert]::ToBase64String($DaemonClientCertificate.GetRawCertData())
        $DaemonClientKeyCredentials = @(
            @{
                "type" = "AsymmetricX509Cert" # X509 Certificate
                "startDate" = $CredentialStartDate
                "endDate" = $DaemonClientCertificate.NotAfter.AddDays(-1).ToString("u").Replace(" ", "T") # The credential end date must be before the certificate expiration date
                "usage" = "Verify"
                "value" = $DaemonClientCertificateData # Base64 encoded certificate bytes

            }
        )
        $DaemonClientServicePrincipal = New-AzureADServicePrincipal -TenantName $TenantName -Headers $Headers -AppId $DaemonClient.appId -KeyCredentials $DaemonClientKeyCredentials
    }
    else
    {
        Write-Warning "The client certificate file ""$DaemonClientCertificateFileName"" was not found, the Daemon Client application will be registered without it"
        $DaemonClientServicePrincipal = New-AzureADServicePrincipal -TenantName $TenantName -Headers $Headers -AppId $DaemonClient.appId
    }
	# Grant admin consent on the Daemon Client to access the TodoList API through the "Todo.ReadWrite.All" App Role
    Grant-AzureADAdminConsentOnAppRole -TenantName $TenantName -Headers $Headers -ClientServicePrincipalObjectId $DaemonClientServicePrincipal.objectId -ResourceServicePrincipalObjectId $TodoListApiServicePrincipal.objectId -AppRoleId $TodoListApiTodoReadWriteAllAppRoleId
    $ConfigurationValues["TodoListDaemonClientId"] = $DaemonClient.appId

    # Register the Server application for the TodoList Web App.
    Write-Host "Creating ""$WebAppClientDisplayName"" in Azure AD"
    $ConfigurationValues["TodoListWebAppClientSecret"] = New-ClientSecret
    $WebAppClientDefinition = @{
        "displayName" = $WebAppClientDisplayName
        "groupMembershipClaims" = "SecurityGroup" # Emit (security) group membership claims
        "replyUrls" = @($ConfigurationValues["TodoListWebAppRootUrl"])
        "identifierUris" = @($ConfigurationValues["TodoListWebAppResourceId"])
        "requiredResourceAccess" = @( # Define access to other applications
            @{
                "resourceAppId" = "00000002-0000-0000-c000-000000000000" # Declare access to Azure Active Directory
                "resourceAccess" = @(
                    @{
                        "id" = "311a71cc-e848-46a1-bdf8-97ff7156d8e6" # "User.Read": Sign in and read user profile
                        "type" = "Scope" # Delegated (User) Permission
                    }
                )
            },
            @{
                "resourceAppId" = $TodoListApi.appId # Declare access to the TodoList API
                "resourceAccess" = @(
                    @{
                        "id" = $TodoListApiTodoReadPermissionId # The ID for the "Todo.Read" access permission
                        "type" = "Scope" # Delegated (User) Permission
                    },
                    @{
                        "id" = $TodoListApiTodoReadWritePermissionId # The ID for the "Todo.ReadWrite" access permission
                        "type" = "Scope" # Delegated (User) Permission
                    }
                )
            }
        )
        "passwordCredentials" = @( # Add a client secret
            @{
                "keyId" = New-Guid
                "startDate" = $CredentialStartDate
                "endDate" = $CredentialEndDate
                "value" = $ConfigurationValues["TodoListWebAppClientSecret"]
            }
        )
    }
    $WebAppClient = New-AzureADApplication -TenantName $TenantName -Headers $Headers -ApplicationDefinition $WebAppClientDefinition
    # Create the service principal representing the application instance in the directory.
    $WebAppClientServicePrincipal = New-AzureADServicePrincipal -TenantName $TenantName -Headers $Headers -AppId $WebAppClient.appId
    # Add the service principal to the "Directory Readers" role (which is done automatically when an admin consents to the application when it needs directory read permissions).
    Add-AzureADRoleMember -TenantName $TenantName -Headers $Headers -RoleObjectId $DirectoryReaderRole.objectId -ServicePrincipalObjectId $WebAppClientServicePrincipal.objectId
    $ConfigurationValues["TodoListWebAppClientId"] = $WebAppClient.appId
    
    # Register the Server application for the TodoList Web Core.
    Write-Host "Creating ""$WebCoreClientDisplayName"" in Azure AD"
    $ConfigurationValues["TodoListWebCoreClientSecret"] = New-ClientSecret
	# The ASP.NET Core middleware listens for sign ins on the "/signin-oidc" endpoint.
    $WebCoreClientRedirectUri = $ConfigurationValues["TodoListWebCoreRootUrl"]
    $WebCoreClientRedirectUri = $WebCoreClientRedirectUri.TrimEnd('/')
    $WebCoreClientRedirectUri = "$WebCoreClientRedirectUri/signin-oidc"
    $WebCoreClientDefinition = @{
        "displayName" = $WebCoreClientDisplayName
        "groupMembershipClaims" = "SecurityGroup" # Emit (security) group membership claims
        "replyUrls" = @($WebCoreClientRedirectUri)
        "identifierUris" = @($ConfigurationValues["TodoListWebCoreResourceId"])
        "requiredResourceAccess" = @( # Define access to other applications
            @{
                "resourceAppId" = "00000002-0000-0000-c000-000000000000" # Declare access to Azure Active Directory
                "resourceAccess" = @(
                    @{
                        "id" = "311a71cc-e848-46a1-bdf8-97ff7156d8e6" # "User.Read": Sign in and read user profile
                        "type" = "Scope" # Delegated (User) Permission
                    }
                )
            },
            @{
                "resourceAppId" = $TodoListApi.appId # Declare access to the TodoList API
                "resourceAccess" = @(
                    @{
                        "id" = $TodoListApiTodoReadPermissionId # The ID for the "Todo.Read" access permission
                        "type" = "Scope" # Delegated (User) Permission
                    },
                    @{
                        "id" = $TodoListApiTodoReadWritePermissionId # The ID for the "Todo.ReadWrite" access permission
                        "type" = "Scope" # Delegated (User) Permission
                    }
                )
            }
        )
        "passwordCredentials" = @( # Add a client secret
            @{
                "keyId" = New-Guid
                "startDate" = $CredentialStartDate
                "endDate" = $CredentialEndDate
                "value" = $ConfigurationValues["TodoListWebCoreClientSecret"]
            }
        )
    }
    $WebCoreClient = New-AzureADApplication -TenantName $TenantName -Headers $Headers -ApplicationDefinition $WebCoreClientDefinition
    # Create the service principal representing the application instance in the directory.
    $WebCoreClientServicePrincipal = New-AzureADServicePrincipal -TenantName $TenantName -Headers $Headers -AppId $WebCoreClient.appId
    # Add the service principal to the "Directory Readers" role (which is done automatically when an admin consents to the application when it needs directory read permissions).
    Add-AzureADRoleMember -TenantName $TenantName -Headers $Headers -RoleObjectId $DirectoryReaderRole.objectId -ServicePrincipalObjectId $WebCoreClientServicePrincipal.objectId
    $ConfigurationValues["TodoListWebCoreClientId"] = $WebCoreClient.appId

    # Register the Server application for the TodoList WebForms app.
    Write-Host "Creating ""$WebFormsClientDisplayName"" in Azure AD"
    $ConfigurationValues["TodoListWebFormsClientSecret"] = New-ClientSecret
    $WebFormsClientDefinition = @{
        "displayName" = $WebFormsClientDisplayName
        "groupMembershipClaims" = "SecurityGroup" # Emit (security) group membership claims
        "replyUrls" = @($ConfigurationValues["TodoListWebFormsRootUrl"])
        "identifierUris" = @($ConfigurationValues["TodoListWebFormsResourceId"])
        "requiredResourceAccess" = @( # Define access to other applications
            @{
                "resourceAppId" = "00000002-0000-0000-c000-000000000000" # Declare access to Azure Active Directory
                "resourceAccess" = @(
                    @{
                        "id" = "311a71cc-e848-46a1-bdf8-97ff7156d8e6" # "User.Read": Sign in and read user profile
                        "type" = "Scope" # Delegated (User) Permission
                    }
                )
            },
            @{
                "resourceAppId" = $TodoListApi.appId # Declare access to the TodoList API
                "resourceAccess" = @(
                    @{
                        "id" = $TodoListApiTodoReadPermissionId # The ID for the "Todo.Read" access permission
                        "type" = "Scope" # Delegated (User) Permission
                    },
                    @{
                        "id" = $TodoListApiTodoReadWritePermissionId # The ID for the "Todo.ReadWrite" access permission
                        "type" = "Scope" # Delegated (User) Permission
                    }
                )
            }
        )
        "passwordCredentials" = @( # Add a client secret
            @{
                "keyId" = New-Guid
                "startDate" = $CredentialStartDate
                "endDate" = $CredentialEndDate
                "value" = $ConfigurationValues["TodoListWebFormsClientSecret"]
            }
        )
    }
    $WebFormsClient = New-AzureADApplication -TenantName $TenantName -Headers $Headers -ApplicationDefinition $WebFormsClientDefinition
    # Create the service principal representing the application instance in the directory.
    $WebFormsClientServicePrincipal = New-AzureADServicePrincipal -TenantName $TenantName -Headers $Headers -AppId $WebFormsClient.appId
    # Add the service principal to the "Directory Readers" role (which is done automatically when an admin consents to the application when it needs directory read permissions).
    Add-AzureADRoleMember -TenantName $TenantName -Headers $Headers -RoleObjectId $DirectoryReaderRole.objectId -ServicePrincipalObjectId $WebFormsClientServicePrincipal.objectId
    $ConfigurationValues["TodoListWebFormsClientId"] = $WebFormsClient.appId

    # Register the Native application for the Web SPA app.
    Write-Host "Creating ""$WebSpaClientDisplayName"" in Azure AD"
    $WebSpaClientDefinition = @{
        "displayName" = $WebSpaClientDisplayName
        "groupMembershipClaims" = "SecurityGroup" # Emit (security) group membership claims
        "replyUrls" = @($ConfigurationValues["TodoListWebSpaRootUrl"])
        "publicClient" = $true # This is a public client
        "oauth2AllowImplicitFlow" = $true # Allow the OAuth 2.0 Implicit Flow for the SPA web application
        "requiredResourceAccess" = @( # Define access to other applications
            @{
                "resourceAppId" = "00000002-0000-0000-c000-000000000000" # Declare access to Azure Active Directory
                "resourceAccess" = @(
                    @{
                        "id" = "311a71cc-e848-46a1-bdf8-97ff7156d8e6" # "User.Read": Sign in and read user profile
                        "type" = "Scope" # Delegated (User) Permission
                    }
                )
            },
            @{
                "resourceAppId" = $TodoListApi.appId # Declare access to the TodoList API
                "resourceAccess" = @(
                    @{
                        "id" = $TodoListApiTodoReadPermissionId # The ID for the "Todo.Read" access permission
                        "type" = "Scope" # Delegated (User) Permission
                    },
                    @{
                        "id" = $TodoListApiTodoReadWritePermissionId # The ID for the "Todo.ReadWrite" access permission
                        "type" = "Scope" # Delegated (User) Permission
                    }
                )
            }
        )
    }
    $WebSpaClient = New-AzureADApplication -TenantName $TenantName -Headers $Headers -ApplicationDefinition $WebSpaClientDefinition
    $WebSpaClientServicePrincipal = New-AzureADServicePrincipal -TenantName $TenantName -Headers $Headers -AppId $WebSpaClient.appId
    $ConfigurationValues["TodoListWebSpaClientId"] = $WebSpaClient.appId
    # Grant admin consent for the TodoList Web SPA application to access the TodoList API (this cannot be done during an OAuth 2.0 Implicit Grant).
    Grant-AzureADAdminConsentOnOAuth2Permission -TenantName $TenantName -Headers $Headers -ClientServicePrincipalObjectId $WebSpaClientServicePrincipal.objectId -ResourceServicePrincipalObjectId $TodoListApiServicePrincipal.objectId -Scope "user_impersonation"

    # Register the Native application for the WPF app.
    Write-Host "Creating ""$WpfClientDisplayName"" in Azure AD"
    $WpfClientDefinition = @{
        "displayName" = $WpfClientDisplayName
        "groupMembershipClaims" = "SecurityGroup" # Emit (security) group membership claims
        "replyUrls" = @($ConfigurationValues["TodoListWpfRedirectUrl"])
        "publicClient" = $true # This is a public client
        "requiredResourceAccess" = @( # Define access to other applications
            @{
                "resourceAppId" = "00000002-0000-0000-c000-000000000000" # Declare access to Azure Active Directory
                "resourceAccess" = @(
                    @{
                        "id" = "311a71cc-e848-46a1-bdf8-97ff7156d8e6" # "User.Read": Sign in and read user profile
                        "type" = "Scope" # Delegated (User) Permission
                    }
                )
            },
            @{
                "resourceAppId" = $TodoListApi.appId # Declare access to the TodoList API
                "resourceAccess" = @(
                    @{
                        "id" = $TodoListApiTodoReadPermissionId # The ID for the "Todo.Read" access permission
                        "type" = "Scope" # Delegated (User) Permission
                    },
                    @{
                        "id" = $TodoListApiTodoReadWritePermissionId # The ID for the "Todo.ReadWrite" access permission
                        "type" = "Scope" # Delegated (User) Permission
                    }
                )
            }
        )
    }
    $WpfClient = New-AzureADApplication -TenantName $TenantName -Headers $Headers -ApplicationDefinition $WpfClientDefinition
    $WpfClientServicePrincipal = New-AzureADServicePrincipal -TenantName $TenantName -Headers $Headers -AppId $WpfClient.appId
    $ConfigurationValues["TodoListWpfClientId"] = $WpfClient.appId

    # Register the Native application for the Console app.
    Write-Host "Creating ""$ConsoleClientDisplayName"" in Azure AD"
    $ConsoleClientDefinition = @{
        "displayName" = $ConsoleClientDisplayName
        "groupMembershipClaims" = "SecurityGroup" # Emit (security) group membership claims
        "replyUrls" = @($ConfigurationValues["TodoListConsoleRedirectUrl"])
        "publicClient" = $true # This is a public client
        "requiredResourceAccess" = @( # Define access to other applications
            @{
                "resourceAppId" = "00000002-0000-0000-c000-000000000000" # Declare access to Azure Active Directory
                "resourceAccess" = @(
                    @{
                        "id" = "311a71cc-e848-46a1-bdf8-97ff7156d8e6" # "User.Read": Sign in and read user profile
                        "type" = "Scope" # Delegated (User) Permission
                    }
                )
            },
            @{
                "resourceAppId" = $TodoListApi.appId # Declare access to the TodoList API
                "resourceAccess" = @(
                    @{
                        "id" = $TodoListApiTodoReadPermissionId # The ID for the "Todo.Read" access permission
                        "type" = "Scope" # Delegated (User) Permission
                    },
                    @{
                        "id" = $TodoListApiTodoReadWritePermissionId # The ID for the "Todo.ReadWrite" access permission
                        "type" = "Scope" # Delegated (User) Permission
                    }
                )
            }
        )
    }
    $ConsoleClient = New-AzureADApplication -TenantName $TenantName -Headers $Headers -ApplicationDefinition $ConsoleClientDefinition
    $ConsoleClientServicePrincipal = New-AzureADServicePrincipal -TenantName $TenantName -Headers $Headers -AppId $ConsoleClient.appId
    $ConfigurationValues["TodoListConsoleClientId"] = $ConsoleClient.appId
    
    # Register the Native application for the Windows 10 app.
    Write-Host "Creating ""$Windows10ClientDisplayName"" in Azure AD"
    $Windows10ClientDefinition = @{
        "displayName" = $Windows10ClientDisplayName
        "groupMembershipClaims" = "SecurityGroup" # Emit (security) group membership claims
        "replyUrls" = @($ConfigurationValues["TodoListWindows10RedirectUrl"])
        "publicClient" = $true # This is a public client
        "requiredResourceAccess" = @( # Define access to other applications
            @{
                "resourceAppId" = "00000002-0000-0000-c000-000000000000" # Declare access to Azure Active Directory
                "resourceAccess" = @(
                    @{
                        "id" = "311a71cc-e848-46a1-bdf8-97ff7156d8e6" # "User.Read": Sign in and read user profile
                        "type" = "Scope" # Delegated (User) Permission
                    }
                )
            },
            @{
                "resourceAppId" = $TodoListApi.appId # Declare access to the TodoList API
                "resourceAccess" = @(
                    @{
                        "id" = $TodoListApiTodoReadPermissionId # The ID for the "Todo.Read" access permission
                        "type" = "Scope" # Delegated (User) Permission
                    },
                    @{
                        "id" = $TodoListApiTodoReadWritePermissionId # The ID for the "Todo.ReadWrite" access permission
                        "type" = "Scope" # Delegated (User) Permission
                    }
                )
            }
        )
    }
    $Windows10Client = New-AzureADApplication -TenantName $TenantName -Headers $Headers -ApplicationDefinition $Windows10ClientDefinition
    $Windows10ClientServicePrincipal = New-AzureADServicePrincipal -TenantName $TenantName -Headers $Headers -AppId $Windows10Client.appId
    $ConfigurationValues["TodoListWindows10ClientId"] = $Windows10Client.appId
}
