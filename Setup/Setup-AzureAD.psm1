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

function Grant-AzureADAdminConsent ($TenantName, $Headers, $ClientServicePrincipalObjectId, $ResourceServicePrincipalObjectId, $Scope)
{
    $iOAuth2PermissionGrant = @{
        "clientId" = $ClientServicePrincipalObjectId # The service principal Object ID of the application
        "consentType" = "AllPrincipals" # Grant admin consent for all principals
        "expiryTime" = (Get-Date).AddYears(10).ToString("u").Replace(" ", "T")
        "resourceId" = $ResourceServicePrincipalObjectId # The service principal Object ID representing the resource
        "scope" = $Scope # The required scope(s)
    }
    $Result = Send-GraphApiPostRequest -TenantName $TenantName -Headers $Headers -Path "oauth2PermissionGrants" -Body $iOAuth2PermissionGrant
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
    $WpfClientDisplayName = "TodoList WPF Client"
    $ConsoleClientDisplayName = "TodoList Console Client"
    $Windows10ClientDisplayName = "TodoList Windows 10 Client"
    $DaemonClientDisplayName = "TodoList Daemon Client"
    $ApplicationDisplayNames = @($TaxonomyApiDisplayName, $TodoListApiDisplayName, $WebSpaClientDisplayName, $WebAppClientDisplayName, $WebCoreClientDisplayName, $WpfClientDisplayName, $ConsoleClientDisplayName, $Windows10ClientDisplayName, $DaemonClientDisplayName)

    # Ensure we start from scratch.
    $ExistingApplications = Get-AzureADApplications -TenantName $TenantName -Headers $Headers
    $ExistingApplications | Where-Object { $ApplicationDisplayNames.Contains($_.displayName) } | ForEach-Object {
        Write-Host "Deleting Azure AD application ""$($_.displayName)"""
        Remove-AzureADApplication -TenantName $TenantName -Headers $Headers -ObjectId $_.objectId
    }

    # Activate and retrieve the "Directory Readers" and "Directory Writers" roles in the directory.
    $DirectoryReaderRole = Get-AzureADRole -TenantName $TenantName -Headers $Headers -RoleTemplateId "88d8e3e3-8f55-4a1e-953a-9b9898b8876b"
    $DirectoryWriterRole = Get-AzureADRole -TenantName $TenantName -Headers $Headers -RoleTemplateId "9360feb5-f418-4baa-8175-e2a00bac4301"

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
                        "id" = "5778995a-e1bf-45b8-affa-663a9f3f4d04" # "Directory.Read": Read directory data
                        "type" = "Role" # Application Permission
                    },
                    @{
                        "id" = "311a71cc-e848-46a1-bdf8-97ff7156d8e6" # "UserProfile.Read": Sign in and read user profile
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
                        "id" = "5778995a-e1bf-45b8-affa-663a9f3f4d04" # "Directory.Read": Read directory data
                        "type" = "Role" # Application Permission
                    },
                    @{
                        "id" = "78c8a3c8-a07e-4b9e-af1b-b5ccab50a175" # "Directory.Write": Read and write directory data
                        "type" = "Role" # Application Permission
                    },
                    @{
                        "id" = "311a71cc-e848-46a1-bdf8-97ff7156d8e6" # "UserProfile.Read": Sign in and read user profile
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
        "oauth2Permissions" = @( # Define app-specific permissions
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
                "value" = "Todo.Write"
                "type" = "User" # User consent is allowed (otherwise use "Admin" for admin-only consent)
                "adminConsentDescription" = "Allow the application to write todo's on behalf of the signed-in user."
                "adminConsentDisplayName" = "Write todo's"
                "userConsentDescription" = "Allow the application to write todo's on your behalf."
                "userConsentDisplayName" = "Write todo's"
            }
        )
        "appRoles" =  @( # Define app-specific roles
            @{
                "id" = New-Guid
                "value" = "administrator"
                "displayName" = "Administrator"
                "description" = "Administrators can manage the application"
                "allowedMemberTypes" = @("User")
            },
            @{
                "id" = New-Guid
                "value" = "contributor"
                "displayName" = "Contributor"
                "description" = "Contributors can manage their own todo lists"
                "allowedMemberTypes" = @("User")
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
    $TodoListApiTodoWritePermissionId = Get-AzureADOAuth2PermissionId -AzureADApplication $TodoListApi -PermissionValue "Todo.Write"
    # Grant admin consent for the TodoList API to access the Taxonomy API.
    Grant-AzureADAdminConsent -TenantName $TenantName -Headers $Headers -ClientServicePrincipalObjectId $TodoListApiServicePrincipal.objectId -ResourceServicePrincipalObjectId $TaxonomyApiServicePrincipal.objectId -Scope "user_impersonation"
    
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
                        "id" = "311a71cc-e848-46a1-bdf8-97ff7156d8e6" # "UserProfile.Read": Sign in and read user profile
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
                        "id" = $TodoListApiTodoWritePermissionId # The ID for the "Todo.Write" access permission
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
                        "id" = "5778995a-e1bf-45b8-affa-663a9f3f4d04" # "Directory.Read": Read directory data
                        "type" = "Role" # Application Permission
                    },
                    @{
                        "id" = "311a71cc-e848-46a1-bdf8-97ff7156d8e6" # "UserProfile.Read": Sign in and read user profile
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
                        "id" = $TodoListApiTodoWritePermissionId # The ID for the "Todo.Write" access permission
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
                        "id" = "5778995a-e1bf-45b8-affa-663a9f3f4d04" # "Directory.Read": Read directory data
                        "type" = "Role" # Application Permission
                    },
                    @{
                        "id" = "311a71cc-e848-46a1-bdf8-97ff7156d8e6" # "UserProfile.Read": Sign in and read user profile
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
                        "id" = $TodoListApiTodoWritePermissionId # The ID for the "Todo.Write" access permission
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
                        "id" = "311a71cc-e848-46a1-bdf8-97ff7156d8e6" # "UserProfile.Read": Sign in and read user profile
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
                        "id" = $TodoListApiTodoWritePermissionId # The ID for the "Todo.Write" access permission
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
    Grant-AzureADAdminConsent -TenantName $TenantName -Headers $Headers -ClientServicePrincipalObjectId $WebSpaClientServicePrincipal.objectId -ResourceServicePrincipalObjectId $TodoListApiServicePrincipal.objectId -Scope "user_impersonation"

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
                        "id" = "311a71cc-e848-46a1-bdf8-97ff7156d8e6" # "UserProfile.Read": Sign in and read user profile
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
                        "id" = $TodoListApiTodoWritePermissionId # The ID for the "Todo.Write" access permission
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
                        "id" = "311a71cc-e848-46a1-bdf8-97ff7156d8e6" # "UserProfile.Read": Sign in and read user profile
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
                        "id" = $TodoListApiTodoWritePermissionId # The ID for the "Todo.Write" access permission
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
                        "id" = "311a71cc-e848-46a1-bdf8-97ff7156d8e6" # "UserProfile.Read": Sign in and read user profile
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
                        "id" = $TodoListApiTodoWritePermissionId # The ID for the "Todo.Write" access permission
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
