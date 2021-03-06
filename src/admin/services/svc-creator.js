/*  this file contains the svcCreator - a helper to quickly create services
 */

angular.module("EavServices")
    // This is a helper-factory to create services which manage one live list
    // check examples with the permissions-service or the content-type-service how we use it
    .factory("svcCreator", function (toastr, $translate, $timeout) {
        var creator = {};

        // construct a object which has liveListCache, liveListReload(), liveListReset(),  
        creator.implementLiveList = function (getLiveList, disableToastr) {
            var t = {};
            t.disableToastr = !!disableToastr;
            t.liveListCache = [];                   // this is the cached list
            t.liveListCache.isLoaded = false;

            t.liveList = function getAllLive() {
                if (t.liveListCache.length === 0 && !t.liveListCache.isLoaded)
                    t.liveListReload();
                return t.liveListCache;
            };

            // use a promise-result to re-fill the live list of all items, return the promise again
            t._liveListUpdateWithResult = function updateLiveAll(result) {
                if (t.msg.isOpened)
                    toastr.clear(t.msg);
                else {
                    $timeout(300).then(function() {
                            toastr.clear(t.msg);
                        }
                    );
                }
                t.liveListCache.length = 0; // clear
                for (var i = 0; i < result.data.length; i++)
                    t.liveListCache.push(result.data[i]);
                t.liveListCache.isLoaded = true;
                return result;
            };

            t.liveListSourceRead = getLiveList;

            t.liveListReload = function getAll() {
                // show loading - must use the promise-mode because this may be used early before the language has arrived
                $translate("General.Messages.Loading").then(function(msg) {
                    t.msg = toastr.info(msg);
                });
                return t.liveListSourceRead()
                    .then(t._liveListUpdateWithResult);
            };

            t.liveListReset = function resetList() {
                t.liveListCache = [];
            };

            return t;
        };
        return creator;

    })

;