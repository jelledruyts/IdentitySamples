'use strict';
angular.module('todoApp')
.factory('todoListSvc', ['$http', 'config', function ($http, config) {
    return {
        getItems: function () {
            return $http.get(config.TodoListWebApiRootUrl + 'api/todolist');
        },
        postItem: function (item) {
            return $http.post(config.TodoListWebApiRootUrl + 'api/todolist', item);
        }
    };
}]);