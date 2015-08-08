﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using ToSic.Eav.ImportExport;

namespace ToSic.Eav.Persistence
{
    public class DbVersioning
    {
        public EavContext Context { get; internal set; }

        public DbVersioning(EavContext cntx)
        {
            Context = cntx;
        }

        /// <summary>
        /// Creates a ChangeLog immediately
        /// </summary>
        /// <remarks>Also opens the SQL Connection to ensure this ChangeLog is used for Auditing on this SQL Connection</remarks>
        public int GetChangeLogId(string userName)
        {
            if (Context.MainChangeLogId == 0)
            {
                if (Context.Connection.State != ConnectionState.Open)
                    Context.Connection.Open();	// make sure same connection is used later
                Context.MainChangeLogId = Context.AddChangeLog(userName).Single().ChangeID;
            }

            return Context.MainChangeLogId;
        }

        /// <summary>
        /// Creates a ChangeLog immediately
        /// </summary>
        internal int GetChangeLogId()
        {
            return GetChangeLogId(Context.UserName);
        }

        /// <summary>
        /// Set ChangeLog ID on current Context and connection
        /// </summary>
        /// <param name="changeLogId"></param>
        public void SetChangeLogId(int changeLogId)
        {
            if (Context.MainChangeLogId != 0)
                throw new Exception("ChangeLogID was already set");


            Context.Connection.Open();	// make sure same connection is used later
            Context.SetChangeLogIdInternal(changeLogId);
            Context.MainChangeLogId = changeLogId;
        }




        /// <summary>
        /// Persist modified Entity to DataTimeline
        /// </summary>
        internal void SaveEntityToDataTimeline(Entity currentEntity)
        {
            var export = new XmlExport(Context);
            var entityModelSerialized = export.GetEntityXElement(currentEntity.EntityID);
            var timelineItem = new DataTimelineItem
            {
                SourceTable = "ToSIC_EAV_Entities",
                Operation = Constants.DataTimelineEntityStateOperation,
                NewData = entityModelSerialized.ToString(),
                SourceGuid = currentEntity.EntityGUID,
                SourceID = currentEntity.EntityID,
                SysLogID = GetChangeLogId(),
                SysCreatedDate = DateTime.Now
            };
            Context.AddToDataTimeline(timelineItem);

            Context.SaveChanges();
        }

        /// <summary>
        /// Get an Entity in the specified Version from DataTimeline using XmlImport
        /// </summary>
        /// <param name="entityId">EntityId</param>
        /// <param name="changeId">ChangeId to retrieve</param>
        /// <param name="defaultCultureDimension">Default Language</param>
        public Import.ImportEntity GetEntityVersion(int entityId, int changeId, int? defaultCultureDimension)
        {
            // Get Timeline Item
            string timelineItem;
            try
            {
                timelineItem = Context.DataTimeline.Where(d => d.Operation == Constants.DataTimelineEntityStateOperation && d.SourceID == entityId && d.SysLogID == changeId).Select(d => d.NewData).SingleOrDefault();
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(string.Format("Error getting EntityId {0} with ChangeId {1} from DataTimeline. {2}", entityId, changeId, ex.Message));
            }
            if (timelineItem == null)
                throw new InvalidOperationException(string.Format("EntityId {0} with ChangeId {1} not found in DataTimeline.", entityId, changeId));

            // Parse XML
            var xEntity = XElement.Parse(timelineItem);
            var assignmentObjectTypeName = xEntity.Attribute("AssignmentObjectType").Value;
            var assignmentObjectTypeId = new DbShortcuts(Context).GetAssignmentObjectType(assignmentObjectTypeName).AssignmentObjectTypeID;

            // Prepare source and target-Languages
            if (!defaultCultureDimension.HasValue)
                throw new NotSupportedException("GetEntityVersion without defaultCultureDimension is not yet supported.");
            var dim = new DbDimensions(Context);
            var defaultLanguage = dim.GetDimension(defaultCultureDimension.Value).ExternalKey;
            var targetDimensions = dim.GetLanguages();
            var allSourceDimensionIds = ((IEnumerable<object>)xEntity.XPathEvaluate("/Value/Dimension/@DimensionID")).Select(d => int.Parse(((XAttribute)d).Value)).ToArray();
            var allSourceDimensionIdsDistinct = allSourceDimensionIds.Distinct().ToArray();
            var sourceDimensions = dim.GetDimensions(allSourceDimensionIdsDistinct).ToList();
            int sourceDefaultDimensionId;
            if (allSourceDimensionIdsDistinct.Contains(defaultCultureDimension.Value))	// if default culture exists in the Entity, sourceDefaultDimensionId is still the same
                sourceDefaultDimensionId = defaultCultureDimension.Value;
            else
            {
                var sourceDimensionsIdsGrouped = (from n in allSourceDimensionIds group n by n into g select new { DimensionId = g.Key, Qty = g.Count() }).ToArray();
                sourceDefaultDimensionId = sourceDimensionsIdsGrouped.Any() ? sourceDimensionsIdsGrouped.OrderByDescending(g => g.Qty).First().DimensionId : defaultCultureDimension.Value;
            }

            // Load Entity from Xml unsing XmlImport
            return XmlImport.GetImportEntity(xEntity, assignmentObjectTypeId, targetDimensions, sourceDimensions, sourceDefaultDimensionId, defaultLanguage);
        }

        /// <summary>
        /// Get all Versions of specified EntityId
        /// </summary>
        public DataTable GetEntityVersions(int entityId)
        {
            // get Versions from DataTimeline
            var entityVersions = (from d in Context.DataTimeline
                                  join c in Context.ChangeLogs on d.SysLogID equals c.ChangeID
                                  where d.Operation == Constants.DataTimelineEntityStateOperation && d.SourceID == entityId
                                  orderby c.Timestamp descending
                                  select new { d.SysCreatedDate, c.User, c.ChangeID }).ToList();

            // Generate DataTable with Version-Numbers
            var versionNumber = entityVersions.Count;	// add version number decrement to prevent additional sorting
            var result = new DataTable();
            result.Columns.Add("Timestamp", typeof(DateTime));
            result.Columns.Add("User", typeof(string));
            result.Columns.Add("ChangeId", typeof(int));
            result.Columns.Add("VersionNumber", typeof(int));
            foreach (var version in entityVersions)
                result.Rows.Add(version.SysCreatedDate, version.User, version.ChangeID, versionNumber--);	// decrement versionnumber

            return result;
        }


        /// <summary>
        /// Get the Values of an Entity in the specified Version
        /// </summary>
        public DataTable GetEntityVersionValues(int entityId, int changeId, int? defaultCultureDimension, string multiValuesSeparator = null)
        {
            var entityVersion = GetEntityVersion(entityId, changeId, defaultCultureDimension);

            var result = new DataTable();
            result.Columns.Add("Field");
            result.Columns.Add("Language");
            result.Columns.Add("Value");
            result.Columns.Add("SharedWith");

            foreach (var attribute in entityVersion.Values)
            {
                foreach (var valueModel in attribute.Value)
                {
                    var firstLanguage = valueModel.ValueDimensions.First().DimensionExternalKey;
                    result.Rows.Add(attribute.Key, firstLanguage, Context.GetTypedValue(valueModel, multiValuesSeparator: multiValuesSeparator));	// Add Main-Language

                    foreach (var valueDimension in valueModel.ValueDimensions.Skip(1))	// Add additional Languages
                    {
                        result.Rows.Add(attribute.Key, valueDimension.DimensionExternalKey, Context.GetTypedValue(valueModel, multiValuesSeparator: multiValuesSeparator), firstLanguage + (valueDimension.ReadOnly ? " (read)" : " (write)"));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Restore an Entity to the specified Version by creating a new Version using the Import
        /// </summary>
        public void RestoreEntityVersion(int entityId, int changeId, int? defaultCultureDimension)
        {
            // Get Entity in specified Version/ChangeId
            var newVersion = GetEntityVersion(entityId, changeId, defaultCultureDimension);

            // Restore Entity
            var import = new Import.Import(Context.ZoneId /* _zoneId*/,Context.AppId /* _appId*/, Context.UserName, false, false);
            import.RunImport(null, new List<Import.ImportEntity> { newVersion });

            // Delete Draft (if any)
            var entityDraft = new DbLoadAsEav(Context).GetEavEntity(entityId).GetDraft();
            if (entityDraft != null)
                Context.EntCommands.DeleteEntity(entityDraft.RepositoryId);
        }

    }
}
