
angular.module("EavServices")
    .factory("contentTypeFieldSvc", function($http, eavConfig, svcCreator, $filter) {
        return function createFieldsSvc(appId, contentType) {
            // start with a basic service which implement the live-list functionality
            var svc = {};
            svc.appId = appId;
            svc.contentType = contentType;

            svc.typeListRetrieve = function typeListRetrieve() {
                return $http.get("eav/contenttype/datatypes/", { params: { "appid": svc.appId } });
            };

            svc._inputTypesList = [];
            svc.getInputTypesList = function getInputTpes() {
                if (svc._inputTypesList.length > 0)
                    return svc._inputTypesList;
                $http.get("eav/contenttype/inputtypes/", { params: { "appid": svc.appId } })
                    .then(function(result) {
                        function addToList(value, key) {
                            var item = {
                                dataType: value.Type.substring(0, value.Type.indexOf("-")),
                                inputType: value.Type,
                                label: value.Label,
                                description: value.Description
                            };
                            svc._inputTypesList.push(item);
                        }

                        angular.forEach(result.data, addToList);

                        svc._inputTypesList = $filter("orderBy")(svc._inputTypesList, ["dataType", "inputType"]);
                    });
                return svc._inputTypesList;
            };

	        svc.getFields = function getFields() {
	            return $http.get("eav/contenttype/getfields", { params: { "appid": svc.appId, "staticName": svc.contentType.StaticName } })
	            .then(function(result) {
	                // merge the settings into one, with correct priority sequence
	                if (result.data ) {
	                    for (var i = 0; i < result.data.length; i++) {
	                        var fld = result.data[i];
	                        if(!fld.Metadata)
                                continue;
	                        var md = fld.Metadata;
	                        var allMd = md.All;
	                        var typeMd = md[fld.Type];
	                        var inputMd = md[fld.InputType];
	                        md.merged = angular.merge({}, allMd, typeMd, inputMd);
	                    }
	                }
	                    return result;
	                });
	        };

            svc = angular.extend(svc, svcCreator.implementLiveList(svc.getFields));

            svc.types = svcCreator.implementLiveList(svc.typeListRetrieve);


            svc.reOrder = function reOrder(idArray) {
                console.log(idArray);
                return $http.get("eav/contenttype/reorder", { params: { appid: svc.appId, contentTypeId: svc.contentType.Id, newSortOrder: JSON.stringify(idArray) } })
                    .then(svc.liveListReload);
            };

            svc.delete = function del(item) {
                return $http.get("eav/contenttype/delete", { params: { appid: svc.appId, contentTypeId: svc.contentType.Id, attributeId: item.Id } })
                    .then(svc.liveListReload);
            };

            svc.addMany = function add(items, count) {
                return $http.get("eav/contenttype/addfield/", { params: items[count] })
                    .then(function() {
                        if (items.length === ++count)
                            svc.liveListReload();
                        else
                            svc.addMany(items, count);
                    });
            };

            svc.add = function addOne(item) {
                return $http.get("eav/contenttype/addfield/", { params: item })
                    .then(svc.liveListReload);
            };

            svc.newItemCount = 0;
            svc.newItem = function newItem() {
                return {
                    AppId: svc.appId,
                    ContentTypeId: svc.contentType.Id,
                    Id: 0,
                    Type: "String",
                    InputType: "string-default",
                    StaticName: "",
                    IsTitle: svc.liveList().length === 0,
                    SortOrder: svc.liveList().length + svc.newItemCount++
                };
            };


            svc.delete = function del(item) {
                if (item.IsTitle)
                    throw "Can't delete Title";
                return $http.get("eav/contenttype/deletefield", { params: { appid: svc.appId, contentTypeId: svc.contentType.Id, attributeId: item.Id } })
                    .then(svc.liveListReload);
            };

            svc.updateInputType = function updateInputType(item) {
                return $http.get("eav/contenttype/updateinputtype", { params: { appid: svc.appId, attributeId: item.Id, field: item.StaticName, inputType: item.InputType } })
                    .then(svc.liveListReload);
            };


            svc.setTitle = function setTitle(item) {
                return $http.get("eav/contenttype/setTitle", { params: { appid: svc.appId, contentTypeId: svc.contentType.Id, attributeId: item.Id } })
                    .then(svc.liveListReload);
            };

            svc.rename = function rename(item, newName) {
                return $http.get("eav/contenttype/rename", { params: { appid: svc.appId, contentTypeId: svc.contentType.Id, attributeId: item.Id, newName: newName } })
                    .then(svc.liveListReload);
            };


            return svc;
        };
    });