'use strict';
angular.module('todoApp')
.controller('todoListCtrl', ['$scope', '$location', 'todoListSvc', 'categorySvc', 'adalAuthenticationService', function ($scope, $location, todoListSvc, categorySvc, adalService) {
    $scope.loadingMessage = 'Loading...';
    $scope.error = '';
    $scope.todoList = null;
    $scope.title = '';
    $scope.categoryId = '';
    $scope.categories = null;

    $scope.populate = function () {
        // Load all categories.
        categorySvc.getItems().success(function (results) {
            $scope.error = '';
            $scope.loadingMessage = '';
            $scope.categories = results;
            if ($scope.categories.length > 0) {
                // Select the first category by default.
                $scope.categoryId = $scope.categories[0].Id;
            }

            // Refresh all todo items.
            $scope.refresh();
        }).error(function (err) {
            $scope.error = err;
            $scope.loadingMessage = '';
        })
    };

    $scope.refresh = function () {
        // Refresh all todo items.
        todoListSvc.getItems().success(function (results) {
            $scope.error = '';
            $scope.loadingMessage = '';
            // Assign the category for each todo item.
            for (var i = 0; i < results.length; i++) {
                for (var c = 0; c < $scope.categories.length; c++) {
                    if ($scope.categories[c].Id === results[i].CategoryId) {
                        results[i].Category = $scope.categories[c];
                    }
                }
            }
            $scope.todoList = results;
        }).error(function (err) {
            $scope.error = err;
            $scope.loadingMessage = '';
        })
    };

    $scope.add = function () {
        // Add a new todo item.
        todoListSvc.postItem({
            'Title': $scope.title,
            'CategoryId': $scope.categoryId
        }).success(function (results) {
            $scope.error = '';
            $scope.title = '';
            $scope.categoryId = $scope.categories[0].Id;
            $scope.refresh();
        }).error(function (err) {
            $scope.error = err;
        })
    };
}]);