﻿/* 
 * Field: Entity - Query
 * 
 */

angular.module("eavFieldTemplates")
    .config(function (formlyConfigProvider, defaultFieldWrappers) {

        var wrappers = defaultFieldWrappers.slice(0); // copy the array
        wrappers.splice(defaultFieldWrappers.indexOf("eavLocalization"), 1); // remove the localization...

        formlyConfigProvider.setType({
            name: "entity-query",
            templateUrl: "fields/entity/entity-query.html",
            wrapper: wrappers,
            controller: "FieldTemplate-EntityQueryCtrl"
        });
    })
    .controller("FieldTemplate-EntityQueryCtrl", function ($controller, $scope, $http, $filter, $translate, $uibModal, appId, eavAdminDialogs, eavDefaultValueService, fieldMask, $q, $timeout, entitiesSvc, debugState, query) {

        // use "inherited" controller just like described in http://stackoverflow.com/questions/18461263/can-an-angularjs-controller-inherit-from-another-controller-in-the-same-module
        $controller("FieldTemplate-EntityCtrl", { $scope: $scope });

        var paramsMask, lastParamsMask;

        function activate() {
            // Initialize url parameters mask
            paramsMask = fieldMask($scope.to.settings.merged.UrlParameters || null, $scope, $scope.maybeReload, null); // this will contain the auto-resolve url parameters
        }
        
        // ajax call to get the entities
        $scope.getAvailableEntities = function () {
            var params = paramsMask.resolve(); // always get the latest definition
            return query("Test?includeGuid=true" + (params ? '&' + params : '')).get().then(function (data) {
                $scope.availableEntities = data.data[$scope.to.settings.merged.StreamName].map(function (e) {
                    return { Value: e.Guid, Text: e.Title, Id: e.Id };
                });
            });
        };

        $scope.maybeReload = function (force) {
            var newMask = paramsMask.resolve();
            if (lastParamsMask !== newMask || force) {
                lastParamsMask = newMask;
                return $scope.getAvailableEntities();
            }
            return $q.when();
        };

        activate();
    });
