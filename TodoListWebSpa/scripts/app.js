'use strict';
angular.module('todoApp', ['ngRoute', 'AdalAngular', 'configuration'])
.config(['$routeProvider', '$httpProvider', 'adalAuthenticationServiceProvider', 'config', function ($routeProvider, $httpProvider, adalProvider, config) {
    // Configure the routes.
    $routeProvider.when("/Home", {
        controller: "homeCtrl",
        templateUrl: "views/Home.html"
    }).when("/TodoList", {
        controller: "todoListCtrl",
        templateUrl: "views/TodoList.html",
        requireADLogin: true // [NOTE] This implicitly triggers a sign-in.
    }).when("/Account", {
        controller: "accountCtrl",
        templateUrl: "views/Account.html",
        requireADLogin: true // [NOTE] This implicitly triggers a sign-in.
    }).otherwise({ redirectTo: "/Home" });

    // Allow Cross-Origin Resource Sharing (CORS) to the Web API domain.
    $httpProvider.defaults.useXDomain = true;
    delete $httpProvider.defaults.headers.common['X-Requested-With'];

    // Initialize ADAL.
    adalProvider.init(
        {
            instance: 'https://login.microsoftonline.com/',
            tenant: config.aadTenant,
            clientId: config.todoListWebSpaClientId,
            endpoints: config.webApiEndpoints,
            extraQueryParameter: 'nux=1', // Triggers the "New UX" logon experience in Azure AD.
        },
        $httpProvider
    );
}]);