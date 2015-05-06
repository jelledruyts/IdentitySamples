'use strict';
angular.module('todoApp')
.factory('todoListSvc', ['$http', 'config', function ($http, config) {
    return {
        getItems: function () {
            return $http.get(config.todoListWebApiRootUrl + 'api/todolist');
        },
        postItem: function (item) {
            return $http.post(config.todoListWebApiRootUrl + 'api/todolist', item);
        }
    };
}]);