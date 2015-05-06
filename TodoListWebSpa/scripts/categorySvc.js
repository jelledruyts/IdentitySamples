'use strict';
angular.module('todoApp')
.factory('categorySvc', ['$http', 'config', function ($http, config) {
    return {
        getItems: function () {
            return $http.get(config.todoListWebApiRootUrl + 'api/category');
        },
        postItem: function (item) {
            return $http.post(config.todoListWebApiRootUrl + 'api/category', item);
        }
    };
}]);