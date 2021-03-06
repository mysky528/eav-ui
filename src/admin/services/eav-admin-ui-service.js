/*  this file contains a service to handle 
 * How it works
 * This service tries to open a modal dialog if it can, otherwise a new window returning a promise to allow
 * ...refresh when the window close. 
 * 
 * In most cases there is a nice command to open something, like openItemEditWithEntityId(id, callback)
 * ...and there is also a more advanced version where you could specify more closely what you wanted
 * ...usually ending with an X, so like openItemEditWithEntityIdX(resolve, callbacks)
 * 
 * the simple callback is 1 function (usually to refresh the main list), the complex callbacks have the following structure
 * 1. .success (optional)
 * 2. .error (optional) 
 * 3. .notify (optional)
 * 4. .close (optional) --> this one is attached to all events if no primary handler is defined 
 * 
 * How to use
 * 1. you must already include all js files in your main app - so the controllers you'll need must be preloaded
 * 2. Your main app must also declare the other apps as dependencies, so angular.module('yourname', ['dialog 1', 'diolag 2'])
 * 3. your main app must also need this ['EavAdminUI']
 * 4. your controller must require eavAdminDialogs
 * 5. Then you can call such a dialog
 */

// Todo
// 1. Import / Export
// 2. Pipeline Designer

angular.module('EavAdminUi', ['ng',
    'ui.bootstrap',         // for the $uibModal etc.
    'EavServices',
    'eavTemplates',         // Provides all cached templates
    'PermissionsApp',       // Permissions dialogs to manage permissions
    'ContentItemsAppAgnostic', 
    'PipelineManagement',   // Manage pipelines
    'ContentImportApp',
    'ContentExportApp',
    'HistoryApp',            // the item-history app

    // big todo: currently removed dependency to eavEditentity (much faster) but it actually does...
    // ...need it to initialize this class, so ATM this only works in a system where the other dependency
    // is defined. very not clean :( 
    // but much faster for now
    // the correct clean up would be to create an edit-dialogs class or something (todo)
    // "eavEditEntity"			// the edit-app
])
    .factory('eavAdminDialogs', function ($uibModal, eavConfig, $window,
        // these are needed just for simple access to some dialogs
        entitiesSvc,    // warning: this only works ATM when called in 2sxc, because it needs the eavEditEntity dependency
        contentTypeSvc,
        appId) {
        /*jshint laxbreak:true */

        var svc = {};

        //#region List of Content Items dialogs
        svc.openContentItems = function oci(appId, staticName, itemId, closeCallback) {
            var resolve = svc.CreateResolve({ appId: appId, contentType: staticName, contentTypeId: itemId });
            return svc.OpenModal('content-items/content-items-agnostic.html', 'ContentItemsList as vm', 'fullscreen', resolve, closeCallback);
        };
        //#endregion

        //#region content import export
        svc.openContentImport = function ocimp(appId, staticName, closeCallback) {
            var resolve = svc.CreateResolve({ appId: appId, contentType: staticName });
            return svc.OpenModal('content-import-export/content-import.html', 'ContentImport as vm', 'lg', resolve, closeCallback);
        };

        svc.openContentExport = function ocexp(appId, staticName, closeCallback, optionalIds) {
            var resolve = svc.CreateResolve({ appId: appId, contentType: staticName, itemIds: optionalIds });
            return svc.OpenModal('content-import-export/content-export.html', 'ContentExport as vm', 'lg', resolve, closeCallback);
        };

        //#endregion

        //#region ContentType dialogs
        svc.openContentTypeEdit = function octe(item, closeCallback) {
            var resolve = svc.CreateResolve({ item: item });
            return svc.OpenModal('content-types/content-types-edit.html', 'Edit as vm', '', resolve, closeCallback);
        };

        svc.openContentTypeFields = function octf(item, closeCallback) {
            var resolve = svc.CreateResolve({ contentType: item });
            return svc.OpenModal('content-types/content-types-fields.html', 'FieldList as vm', 'xlg', resolve, closeCallback);
        };

        // this one assumes we have a content-item, but must first retrieve content-type-infos
        svc.openContentTypeFieldsOfItems = function octf(item, closeCallback) {
            return entitiesSvc.getManyForEditing(appId, item)
                .then(function (result) {
                    var ctName = result.data[0].Header.ContentTypeName;
                    var svcForThis = contentTypeSvc(appId); // note: won't specify scope to fallback
                    return svcForThis.getDetails(ctName)
                        .then(function (result2) {
                            return svc.openContentTypeFields(result2.data, closeCallback);
                        });
                });
        };

        //#endregion
        //#region Item - new, edit
        svc.openItemNew = function oin(contentTypeName, closeCallback) {
            return svc.openEditItems([{ ContentTypeName: contentTypeName }], closeCallback, { partOfPage: false });
        };

        svc.openItemEditWithEntityId = function oie(entityId, closeCallback) {
            return svc.openEditItems([{ EntityId: entityId }], closeCallback, { partOfPage: false });
        };

        svc.openEditItems = function oel(items, closeCallback, moreResolves) {
            var merged = angular.extend({ items: items }, moreResolves || {});
            merged.partOfPage = Boolean(merged.partOfPage);
            merged.publishing = merged.publishing || null;
            console.log('openEditItems: partOfPage: ' + merged.partOfPage, ' publishing: ' + merged.publishing);
            var resolve = svc.CreateResolve(merged);
            return svc.OpenModal('form/main-form.html', 'EditEntityWrapperCtrl as vm', 'ent-edit', resolve, closeCallback);
        };

        svc.openItemHistory = function ioh(entityId, closeCallback) {
            return svc.OpenModal('content-items/history.html', 'History as vm', 'lg',
                svc.CreateResolve({ entityId: entityId }),
                closeCallback);
        };
        //#endregion

      //#region Permissions Dialog
      function openPermissions(params, closeCallback) {
        return svc.OpenModal('permissions/permissions.html',
          'PermissionList as vm',
          'xlg',
          svc.CreateResolve(params),
          closeCallback);
      }

      svc.openPermissionsForGuid = function opfg(appId, targetKey, closeCallback) {
        return openPermissions({ appId: appId, targetKey: targetKey, targetType: 4, keyType: 'guid' }, closeCallback);
      };

      svc.openPermissions = function opfg(appId, targetType, keyType, targetKey, closeCallback) {
        return openPermissions({ appId: appId, targetKey: targetKey, targetType: targetType, keyType: keyType },
          closeCallback);
      };
      //#endregion

      //#region Pipeline Designer
      svc.editPipeline = function ep(appId, pipelineId, closeCallback) {
          var url = svc.derivedUrl({
              dialog: 'pipeline-designer',
              pipelineId: pipelineId
          });
          $window.open(url);
          return;
      };
      //#endregion

        //#region GenerateUrlBasedOnCurrent
        svc.derivedUrl = function derivedUrl(varsToReplace) {
            var url = window.location.href;
            for (var prop in varsToReplace)
                if (varsToReplace.hasOwnProperty(prop))
                    url = svc.replaceOrAddOneParam(url, prop, varsToReplace[prop]);

            return url;
        };

        svc.replaceOrAddOneParam = function replaceOneParam(original, param, value) {
            var rule = new RegExp('(' + param + '=).*?(&)', 'i');
            var newText = rule.test(original)
                ? original.replace(rule, '$1' + value + '$2')
                : original + '&' + param + '=' + value;
            return newText;
        };
        //#endregion

        //#region Internal helpers
        svc._attachCallbacks = function attachCallbacks(promise, callbacks) {
            if (typeof (callbacks) === 'undefined')
                return null;
            if (typeof (callbacks) === 'function') // if it's only one callback, use it for all close-cases
                callbacks = { close: callbacks };
            return promise.result.then(callbacks.success || callbacks.close, callbacks.error || callbacks.close, callbacks.notify || callbacks.close);
        };

        // Will open a modal window. Has various specials, like
        // 1. If the templateUrl begins with "~/" - this will be re-mapped to the ng-app root. Only use this for not-inline stuff
        // 2. The controller can be written as "something as vm" and this will be split and configured corectly
        svc.openModalComponent = function (componentName, size, values, callbacks) {
            var modalInstance = $uibModal.open({
                component: componentName,
                resolve: svc.CreateResolve(values),
                size: size,
            });
            return svc._attachCallbacks(modalInstance, callbacks);
        };

        svc.OpenModal = function openModal(templateUrl, controller, size, resolveValues, callbacks) {
            var foundAs = controller.indexOf(' as ');
            var contAs = foundAs > 0 ?
                controller.substring(foundAs + 4)
                : null;

            if (foundAs > 0) controller = controller.substring(0, foundAs);
            var modalInstance = $uibModal.open({
                animation: true,
                templateUrl: templateUrl,
                controller: controller,
                controllerAs: contAs,
                size: size,
                resolve: resolveValues
            });

            return svc._attachCallbacks(modalInstance, callbacks);
        };

        /// This will create a resolve-object containing return function()... for each property in the array
        svc.CreateResolve = function createResolve() {
            var fns = {}, list = arguments[0];
            for (var prop in list)
                if (list.hasOwnProperty(prop))
                    fns[prop] = svc._create1Resolve(list[prop]);
            return fns;
        };

        svc._create1Resolve = function (value) {
            return function () { return value; };
        };
        //#endregion
        return svc;
    })

    ;