'use strict';
angular.module('todoApp')
.controller('accountCtrl', ['$scope', 'config', 'identitySvc', 'adalAuthenticationService', function ($scope, config, identitySvc, adalService) {
    $scope.loadingMessage = 'Loading...';
    $scope.error = '';
    $scope.identityInfo = null;

    $scope.getHashCode = function (value) {
        var hash = 0;
        if (!value || value.length == 0) return hash;
        for (var i = 0; i < value.length; i++) {
            var char = value.charCodeAt(i);
            hash = ((hash << 5) - hash) + char;
            hash = hash & hash; // Convert to 32bit integer 
        }
        return hash;
    };

    $scope.populate = function () {
        identitySvc.getItem().success(function (results) {
            $scope.error = '';
            $scope.loadingMessage = '';

            // Get the current user's claims from the user info profile.
            var userClaims = [];
            for (var claimType in $scope.userInfo.profile) {
                if ($scope.userInfo.profile.hasOwnProperty(claimType)) {
                    userClaims.push({
                        Issuer: '', Type: claimType, Value: $scope.userInfo.profile[claimType], Remark: ''
                    });
                }
            }

            // Create a root identity info object for the current application, and embed the
            // retrieved Web API identity info into it.
            $scope.identityInfo = {
                Application: config.applicationName,
                IsAuthenticated: $scope.userInfo.isAuthenticated,
                AuthenticationType: 'JWT',
                Name: $scope.userInfo.userName,
                Claims: userClaims,
                RelatedApplicationIdentities: [results]
            };
        }).error(function (err) {
            $scope.error = err;
            $scope.loadingMessage = '';
        })
    };
}]);