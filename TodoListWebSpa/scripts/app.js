'use strict';
angular.module('todoApp', ['ngRoute', 'AdalAngular', 'configuration'])
.config(['$routeProvider', '$httpProvider', 'adalAuthenticationServiceProvider', 'config', function ($routeProvider, $httpProvider, adalProvider, config) {
    // [SCENARIO] OAuth 2.0 Implicit Grant
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

    // [NOTE] Allow Cross-Origin Resource Sharing (CORS) to the Web API domain.
    $httpProvider.defaults.useXDomain = true;
    delete $httpProvider.defaults.headers.common['X-Requested-With'];

    // [NOTE] Initialize ADAL.JS.
    adalProvider.init(
        {
            instance: config.StsRootUrl,
            tenant: config.StsPath,
            clientId: config.TodoListWebSpaClientId,
            endpoints: config.WebApiEndpoints, // [NOTE] Instruct ADAL.JS to automatically attach tokens to these endpoints
            extraQueryParameter: 'nux=1', // Triggers the "New UX" logon experience in Azure AD.
        },
        $httpProvider
    );
}]);