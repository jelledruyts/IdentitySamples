'use strict';
angular.module('configuration', [])
.constant('config',
    {
        ApplicationName: 'Todo List (SPA)',
        StsRootUrl: 'https://login.microsoftonline.com/',
        // The tenant name in Azure AD, or 'adfs' for AD FS (see https://technet.microsoft.com/en-us/windows-server-docs/identity/ad-fs/development/single-page-application-with-ad-fs).
        StsPath: 'identitysamples.onmicrosoft.com',
        // AD FS does not have a sign-out endpoint, Azure AD does.
        StsSupportsLogOut: true,
        TodoListWebSpaClientId: '31580c09-e971-431a-8ab7-86a7b244720e',
        TodoListWebApiRootUrl: 'https://localhost:44307/',
        WebApiEndpoints: {
            // [NOTE] This is used by ADAL.JS to automatically attach tokens to these endpoints.
            // Format: '<Endpoint URL>': '<Resource ID>'
            'https://localhost:44307/api': 'http://identitysamples/todolistwebapi'
        }
    }
);
