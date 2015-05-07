'use strict';
angular.module('configuration', [])
.constant('config',
    {
        applicationName: 'Todo List (SPA)',
        aadTenant: 'identitysamples.onmicrosoft.com',
        todoListWebSpaClientId: '31580c09-e971-431a-8ab7-86a7b244720e',
        todoListWebApiRootUrl: 'https://localhost:44307/',
        webApiEndpoints: {
            'https://localhost:44307/api': 'http://identitysamples/todolistwebapi'
        }
    }
);