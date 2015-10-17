﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using Microsoft.Practices.ObjectBuilder2;
using ToSic.Eav.BLL;
using ToSic.Eav.Data;
using ToSic.Eav.DataSources.Caches;
using ToSic.Eav.Persistence;
using ToSic.Eav.Serializers;

namespace ToSic.Eav.WebApi
{
	/// <summary>
	/// Web API Controller for ContentTypes
	/// </summary>
	public class ContentTypeController : Eav3WebApiBase
    {
        #region Content-Type Get, Delete, Save
        [HttpGet]
	    public IEnumerable<dynamic> Get(int appId, string scope = null, bool withStatistics = false)
        {
            // scope can be null (eav) or alternatives would be "System", "2SexyContent-System", "2SexyContent-App", "2SexyContent"
            var cache = DataSource.GetCache(null, appId) as BaseCache;
            var allTypes = cache.GetContentTypes().Select(t => t.Value);

            var filteredType = allTypes.Where(t => t.Scope == scope).OrderBy(t => t.Name).Select(t => new {
                Id = t.AttributeSetId,
                t.Name,
                t.StaticName,
                t.Scope,
                t.Description,
                DefinitionSet = t.UsesConfigurationOfAttributeSet,
                Ghost = t.UsesConfigurationOfAttributeSet == null,
                Items = cache.LightList.Count(i => i.Type == t),
                Fields = (t as ContentType).AttributeDefinitions.Count 
            });

            return filteredType;
	    }

        [HttpGet]
	    public IContentType GetSingle(int appId, string contentTypeStaticName, string scope = null)
	    {
            SetAppIdAndUser(appId);
            // var source = InitialDS;
            var cache = DataSource.GetCache(null, appId);
            return cache.GetContentType(contentTypeStaticName);
        }

	    [HttpDelete]
	    public bool Delete(int appId, string staticName)
	    {
            SetAppIdAndUser(appId);
            CurrentContext.ContentType.Delete(staticName);
	        return true;
	    }

	    [HttpPost]
	    public bool Save(int appId, Dictionary<string, string> item)
	    {
            SetAppIdAndUser(appId);
	        var changeStaticName = false;
            bool.TryParse(item["ChangeStaticName"], out changeStaticName);
            CurrentContext.ContentType.AddOrUpdate(item["StaticName"], item["Scope"], item["Name"], 
                item["InputType"],
                item["Description"],
                null, false, 
                changeStaticName, 
                changeStaticName ? item["NewStaticName"] : item["StaticName"]);
	        return true;
	    }
        #endregion

        #region Fields - Get, Reorder, Data-Types (for dropdown), etc.
        /// <summary>
        /// Returns the configuration for a content type
        /// </summary>
        [HttpGet]
        public IEnumerable<dynamic> GetFields(int appId, string staticName)
        {
            SetAppIdAndUser(appId);

            var fields =
                CurrentContext.ContentType.GetContentTypeConfiguration(staticName)
                    .OrderBy(ct => (ct.Item1 as AttributeBase).SortOrder);

            return fields.Select(a =>
		        new
		        {
			        Id = a.Item1.AttributeId,
			        (a.Item1 as AttributeBase).SortOrder,
					a.Item1.Type,
                    InputType = findInputType(a.Item2),
					StaticName = a.Item1.Name,
					a.Item1.IsTitle,
					a.Item1.AttributeId,
					Metadata = a.Item2.ToDictionary(e => e.Key, e => new Serializer().Prepare(e.Value))
		        });
        }

	    private string findInputType(Dictionary<string, IEntity> definitions)
	    {
	        if (!definitions.ContainsKey("All"))
	            return "default";

	        var inputType = definitions["All"]?.GetBestValue("InputType");

	        if (inputType == null)
	            return "default";
	        return inputType.ToString();


	    }

        [HttpGet]
        public bool Reorder(int appId, int contentTypeId, int attributeId, string direction)
        {
            SetAppIdAndUser(appId);
            CurrentContext.ContentType.Reorder(contentTypeId, attributeId, direction);
            return true;
        }

	    [HttpGet]
	    public string[] DataTypes(int appId)
	    {
            SetAppIdAndUser(appId);
	        return CurrentContext.SqlDb.AttributeTypes.OrderBy(a => a.Type).Select(a => a.Type).ToArray();
	    }

	    [HttpGet]
	    public IEnumerable<Dictionary<string, object>> InputTypes(int appId)
	    {
	        var entC = new ToSic.Eav.WebApi.EntitiesController();
	        var coreInputTypes = entC.GetAllOfTypeForAdmin(Constants.MetaDataAppId, "ContentType-InputType");

            // now add app-specific items if possible
	        if (Constants.MetaDataAppId != appId)
	        {
	            var coreTypesList = coreInputTypes.First();
	            var appSpecificInputType = entC.GetAllOfTypeForAdmin(appId, "ContentType-InputType").FirstOrDefault();
	            if (appSpecificInputType != null)
                    coreTypesList.ForEach(i => coreTypesList.Add(i.Key, i.Value));
	        }
	        return coreInputTypes;

	    }

            //[HttpGet]
	    //public Dictionary<string, string> InputTypes(int appId)
	    //{
	    //    var types = new Dictionary<string, string>
	    //    {
     //           {"boolean-default", "default" },

     //           {"custom-gps", "GPS Picker" },

     //           {"datetime-default", "default (date / time picker)" },

     //           {"empty-title", "default (title/field group)" },

     //           {"entity-default", "default (entity picker)" },

     //           {"number-default", "default (number input)" },


	    //        {"string-default", "default (single or more lines)"},
	    //        {"string-dropdown", "drop-down"},
	    //        {"string-wysiwyg", "light WYSIWYG editor (recommended)"},
     //           {"string-wysiwyg-adv", "WYSIWYG full (not recommended)" }
	    //    };
	    //    return types;
	    //}
            
        [HttpGet]
	    public int AddField(int appId, int contentTypeId, string staticName, string type, string inputType, int sortOrder)
	    {
            SetAppIdAndUser(appId);
	        return CurrentContext.Attributes.AddAttribute(contentTypeId, staticName, type, inputType, sortOrder, 1, false, true).AttributeID;
	        throw new HttpUnhandledException();
	    }

        [HttpDelete]
	    public bool DeleteField(int appId, int contentTypeId, int attributeId)
	    {
            SetAppIdAndUser(appId);
            // todo: add security check if it really is in this app and content-type
            return CurrentContext.Attributes.RemoveAttribute(attributeId);
	    }

        [HttpGet]
	    public void SetTitle(int appId, int contentTypeId, int attributeId)
	    {
            SetAppIdAndUser(appId);
            CurrentContext.Attributes.SetTitleAttribute(attributeId, contentTypeId);
	    }

        #endregion

    }
}