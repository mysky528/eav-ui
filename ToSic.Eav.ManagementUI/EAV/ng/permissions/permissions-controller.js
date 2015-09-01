(function () { // TN: this is a helper construct, research iife or read https://github.com/johnpapa/angularjs-styleguide#iife

    angular.module("PermissionsApp", ['PermissionsServices', 'eavGlobalConfigurationProvider', 'EavAdminUI'])
        .constant('createdBy', '2sic')          // just a demo how to use constant or value configs in AngularJS
        .constant('license', 'MIT')             // these wouldn't be necessary, just added for learning exprience
        .controller("PermissionList", PermissionListController)
        ;

    function PermissionListController(permissionsSvc, eavAdminDialogs, eavGlobalConfigurationProvider, appId, targetGuid, $modalInstance /* $location */) {
        var vm = this;
        var svc = permissionsSvc;
        permissionsSvc.PermissionTargetGuid = targetGuid; //$location.search().Target;

        vm.edit = function edit(itemId) {
            eavAdminDialogs.openItemEditWithEntityId(itemId, permissionsSvc.liveListReload);
        }

        vm.add = function add() {
            eavAdminDialogs.openMetadataNew('entity', svc.PermissionTargetGuid, svc.ctName, svc.liveListReload);
        }

        //vm.newUrl = function newUrl() {
        //    return eavGlobalConfigurationProvider.itemForm
        //        .getNewItemUrl(svc.ctId, svc.EntityAssignment, { keyGuid: svc.PermissionTargetGuid }, false);
        //}
        //vm.editUrl = function editUrl(id) {
        //    return eavGlobalConfigurationProvider.itemForm
        //        .getEditItemUrl(id, undefined, true);
        //}


        vm.items = permissionsSvc.liveList();
        
        vm.tryToDelete = function tryToDelete(item) {
            if (confirm("Delete '" + item.Title + "' (" + item.Id + ") ?"))
                permissionsSvc.delete(item.Id);
        };

        vm.refresh = svc.liveListReload;

        vm.close = function () {
            $modalInstance.dismiss('cancel');
        };
    };

} ());