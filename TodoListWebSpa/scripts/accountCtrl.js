'use strict';
angular.module('todoApp')
.controller('accountCtrl', ['$scope', 'config', 'identitySvc', 'adalAuthenticationService', function ($scope, config, identitySvc, adalService) {
    $scope.loadingMessage = 'Loading...';
    $scope.error = '';
    $scope.infoMessage = '';
    $scope.identityUpdate = { displayName: '' };
    $scope.identityInfo = null;

    $scope.update = function () {
        // Update the identity information.
        identitySvc.postItem($scope.identityUpdate).success(function (results) {
            $scope.error = '';
            $scope.infoMessage = 'Your changes were saved.';
        }).error(function (err) {
            $scope.error = err;
        })
    };

    $scope.getHashCode = function (value) {
        var hash = 0;
        if (!value || value.length == 0) return hash;
        for (var i = 0; i < value.length; i++) {
            var char = value.charCodeAt(i);
            hash = ((hash << 5) - hash) + char;
            hash = hash & hash;
        }
        return hash;
    };

    $scope.populate = function () {
        identitySvc.getItem().success(function (results) {
            $scope.error = '';
            $scope.loadingMessage = '';

            // [NOTE] Get the current user's claims from the user info profile, which is
            // automatically populated by ADAL.JS from the ID token.
            var userClaims = [];
            for (var claimType in $scope.userInfo.profile) {
                if ($scope.userInfo.profile.hasOwnProperty(claimType)) {
                    userClaims.push({
                        issuer: '', type: claimType, value: $scope.userInfo.profile[claimType], remark: ''
                    });
                }
            }

            // Create a root identity info object for the current application, and embed the
            // retrieved Web API identity info into it.
            $scope.identityInfo = {
                source: 'ID Token',
                application: config.ApplicationName,
                isAuthenticated: $scope.userInfo.isAuthenticated,
                authenticationType: 'JWT',
                name: $scope.userInfo.userName,
                claims: userClaims,
                relatedApplicationIdentities: [results]
            };
        }).error(function (err) {
            $scope.error = err;
            $scope.loadingMessage = '';
        })
    };
}]);