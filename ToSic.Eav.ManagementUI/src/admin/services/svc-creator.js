/*  this file contains the svcCreator - a helper to quickly create services
 */

angular.module("EavServices")
    // This is a helper-factory to create services which manage one live list
    // check examples with the permissions-service or the content-type-service how we use it
    .factory("svcCreator", function() {
        var creator = {};

        // construct a object which has liveListCache, liveListReload(), liveListReset(),  
        creator.implementLiveList = function(getLiveList) {
            var t = {};

            t.liveListCache = [];                   // this is the cached list
            t.liveListCache.isLoaded = false;

            t.liveList = function getAllLive() {
                if (t.liveListCache.length === 0)
                    t.liveListReload();
                return t.liveListCache;
            };

            // use a promise-result to re-fill the live list of all items, return the promise again
            t._liveListUpdateWithResult = function updateLiveAll(result) {
                t.liveListCache.length = 0; // clear
                for (var i = 0; i < result.data.length; i++)
                    t.liveListCache.push(result.data[i]);
                t.liveListCache.isLoaded = true;
                return result;
            };

            t.liveListSourceRead = getLiveList;

            t.liveListReload = function getAll() {
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