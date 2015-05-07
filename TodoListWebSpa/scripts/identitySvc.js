'use strict';
angular.module('todoApp')
.factory('identitySvc', ['$http', 'config', function ($http, config) {
    return {
        getItem: function () {
            return $http.get(config.todoListWebApiRootUrl + 'api/identity');
        },
    };
}]);