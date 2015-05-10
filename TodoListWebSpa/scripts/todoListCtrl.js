'use strict';
angular.module('todoApp')
.controller('todoListCtrl', ['$scope', '$location', 'todoListSvc', 'categorySvc', 'adalAuthenticationService', function ($scope, $location, todoListSvc, categorySvc, adalService) {
    $scope.loadingMessage = 'Loading...';
    $scope.error = '';
    $scope.todoList = null;
    $scope.title = '';
    $scope.categoryId = '';
    $scope.categories = null;
    $scope.newCategoryName = '';
    $scope.newCategoryIsPrivate = false;

    $scope.populate = function () {
        // Load all categories.
        categorySvc.getItems().success(function (results) {
            $scope.categories = results;

            // Select the first category by default.
            if ($scope.categories.length > 0) {
                $scope.categoryId = $scope.categories[0].Id;
            }

            // Load all todo items.
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
        }).error(function (err) {
            $scope.error = err;
            $scope.loadingMessage = '';
        });
    };

    $scope.add = function () {
        // Add a new todo item.
        todoListSvc.postItem({
            'Title': $scope.title,
            'CategoryId': $scope.categoryId,
            'NewCategoryName': $scope.newCategoryName,
            'NewCategoryIsPrivate': $scope.newCategoryIsPrivate
        }).success(function (results) {
            $scope.error = '';
            $scope.title = '';
            $scope.categoryId = $scope.categories[0].Id;
            $scope.newCategoryName = '';
            $scope.newCategoryIsPrivate = false;
            $scope.populate();
        }).error(function (err) {
            $scope.error = err;
        })
    };
}]);