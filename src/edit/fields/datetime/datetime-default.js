﻿/* 
 * Field: DateTime - Default
 */

angular.module("eavFieldTemplates")
    .config(function (formlyConfigProvider, defaultFieldWrappers) {

        formlyConfigProvider.setType({
            name: "datetime-default",
            wrapper: defaultFieldWrappers,
            templateUrl: "fields/datetime/datetime-default.html",
            defaultOptions: {
                templateOptions: {
                    datepickerOptions: {}
                }
            },
            controller: ['$scope', '$locale', '$translate', function($scope, $locale, $translate) {
                $scope.format = $locale.DATETIME_FORMATS.mediumDate;
                $scope.datepickerPopup = {
                    clearText: $translate.instant("CalendarPopup.ClearButton"),
                    closeText: $translate.instant("CalendarPopup.CloseButton"),
                    currentText: $translate.instant("CalendarPopup.CurrentButton")
                };
            }],
            link: function (scope, el, attrs) {

                // Server delivers value as string, so convert it to UTC date
                scope.$watch('value', function (value) {
                    if (value && value.Value && !(value.Value instanceof Date)) {
                        scope.value.Value = convertDateToUTC(new Date(value.Value));
                    }
                });

                function convertDateToUTC(date) { return new Date(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate(), date.getUTCHours(), date.getUTCMinutes(), date.getUTCSeconds()); }
                //if (scope.value && scope.value.Value && typeof (scope.value.Value) === 'string')
                //    scope.value.Value = convertDateToUTC(new Date(scope.value.Value));
            }
        });

    });