﻿/* global angular */
(function () {
	"use strict";

	var app = angular.module("eavEditEntity");

	// The controller for the main form directive
  app.controller("EditEntityWrapperCtrl", function editEntityCtrl(
    $q,
    $http,
    $scope,
    items,
    $uibModalInstance,
    $window,
    $translate,
    toastr,
    partOfPage,
    publishing,
    featuresSvc) {

    // testing featuresSvc
    //featuresSvc.enabled('f6b8d6da-4744-453b-9543-0de499aa2352').then(
    //  function(enabled) {
    //    console.log('async: ' + enabled);
    //  });



		var vm = this;
		
    vm.partOfPage = partOfPage;
	  vm.publishing = publishing;
		vm.itemList = items;

	    // this is the callback after saving - needed to close everything
	    vm.afterSave = function(result) {
	        if (result.status === 200)
	            vm.close(result);
	        else {
	            alert($translate.instant("Errors.UnclearError"));
	        }
	    };
		
	    vm.state = {
	        isDirty: function() {
	            throw $translate.instant("Errors.InnerControlMustOverride");
	        }
	    };

	    vm.close = function (result) {
		    $uibModalInstance.close(result);
		};

	    $window.addEventListener('beforeunload', function (e) {
	        var unsavedChangesText = $translate.instant("Errors.UnsavedChanges");
	        if (vm.state.isDirty()) {
	            (e || window.event).returnValue = unsavedChangesText; //Gecko + IE
	            return unsavedChangesText; //Gecko + Webkit, Safari, Chrome etc.
	        }
	        return null;
	    });
	});
})();