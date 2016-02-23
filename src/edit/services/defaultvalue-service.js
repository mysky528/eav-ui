﻿/* global angular */
(function () {
	"use strict";

	angular.module("eavEditEntity")
        .service("eavDefaultValueService", function () {
		// Returns a typed default value from the string representation
		return function parseDefaultValue(fieldConfig) {
			var e = fieldConfig;
			var d = e.templateOptions.settings.All.DefaultValue;

		    if (e.templateOptions.header.Prefill && e.templateOptions.header.Prefill[e.key]) {
			    d = e.templateOptions.header.Prefill[e.key];
			}

			switch (e.type.split("-")[0]) {
				case "boolean":
					return d !== undefined && d !== null ? d.toLowerCase() === "true" : false;
				case "datetime":
					return d !== undefined && d !== null && d !== "" ? new Date(d) : null;
				case "entity":
				    return d !== undefined && d !== null && d !== "" ? d : []; 
				case "number":
				    return d !== undefined && d !== null && d !== "" ? Number(d) : "";
                default:
					return d ? d : "";
			}
		};
	});

})();