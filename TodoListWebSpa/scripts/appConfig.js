'use strict';
angular.module('configuration', [])
.constant('config',
    {
        StsRootUrl: 'https://login.microsoftonline.com/',
        // The tenant name in Azure AD, or 'adfs' for AD FS (see https://technet.microsoft.com/en-us/windows-server-docs/identity/ad-fs/development/single-page-application-with-ad-fs).
        StsPath: 'tenant.onmicrosoft.com',
        // AD FS does not have a sign-out endpoint, Azure AD does.
        StsSupportsLogOut: true,
        TodoListWebSpaClientId: '',
        TodoListWebApiRootUrl: 'https://localhost:44307/',
        TodoListWebApiResourceId: 'http://identitysamples/todolistwebapi',
        ApplicationName: 'Todo List (SPA)'
    }
);
