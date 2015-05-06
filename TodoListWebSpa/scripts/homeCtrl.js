'use strict';
angular.module('todoApp')
.controller('homeCtrl', ['$scope', 'adalAuthenticationService', '$location', function ($scope, adalService, $location) {
    $scope.signin = function () {
        adalService.login(); // [NOTE] This explicitly triggers a sign-in.
    };
    $scope.signout = function () {
        adalService.logOut(); // [NOTE] This explicitly triggers a sign-out.
    };
    $scope.isActive = function (viewLocation) {
        return viewLocation === $location.path();
    };
}]);