﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Objects;
using System.Data.Objects.DataClasses;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Data;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;
using ToSic.Eav.Import;
using ToSic.Eav.ImportExport;

namespace ToSic.Eav
{
	public partial class EavContext
	{
		#region Static Fields
		/// <summary>
		/// AttributeSet StaticName must match this Regex. Accept Alphanumeric, except the first char must be alphabetic or underscore.
		/// </summary>
		public static string AttributeStaticNameRegEx = "^[_a-zA-Z]{1}[_a-zA-Z0-9]*";
		/// <summary>
		/// If AttributeSet StaticName doesn't match, users see this message.
		/// </summary>
		public static string AttributeStaticNameRegExNotes = "Only alphanumerics and underscore is allowed, first char must be alphabetic or underscore.";
		#endregion

		#region Constants
		/// <summary>
		/// Name of the Default App in all Zones
		/// </summary>
		public const string DefaultAppName = "Default";
		private const string CultureSystemKey = "Culture";
		/// <summary>
		/// DataTimeline Operation-Key for Entity-States (Entity-Versioning)
		/// </summary>
		private const string DataTimelineEntityStateOperation = "s";
		#endregion

		#region Private Fields
		private int _appId;
		private int _zoneId;
		private readonly Dictionary<int, Dictionary<int, IContentType>> _contentTypes = new Dictionary<int, Dictionary<int, IContentType>>();
		/// <summary>SaveChanges() assigns all Changes to this ChangeLog</summary>
		private int _mainChangeLogId;
		private bool _purgeCacheOnSave = true;
		private readonly List<EntityRelationshipQueueItem> _entityRelationshipsQueue = new List<EntityRelationshipQueueItem>();
		#endregion

		#region Properties
		/// <summary>
		/// AppId of this whole Context
		/// </summary>
		public int AppId
		{
			get { return _appId == 0 ? DataSource.MetaDataAppId : _appId; }
			set { _appId = value; }
		}

		/// <summary>
		/// ZoneId of this whole Context
		/// </summary>
		public int ZoneId
		{
			get { return _zoneId == 0 ? DataSource.DefaultZoneId : _zoneId; }
			set { _zoneId = value; }
		}

		/// <summary>
		/// Current UserName. Used for ChangeLog
		/// </summary>
		public string UserName { get; set; }

		/// <summary>
		/// Get or seth whether SaveChanges() should automatically purge cache.
		/// </summary>
		/// <remarks>Usefull if many changes are made in a batch and Cache should be purged after that batch</remarks>
		internal bool PurgeCacheOnSave
		{
			get { return _purgeCacheOnSave; }
			set { _purgeCacheOnSave = value; }
		}
		#endregion

		#region Constructor and Init
		/// <summary>
		/// Returns a new instace of the Eav Context. InitZoneApp should be called afterward. If not, default ZoneId and default AppId is used.
		/// </summary>
		public static EavContext Instance()
		{
			var connectionString = Configuration.GetConnectionString();
			return new EavContext(connectionString);
		}

		/// <summary>
		/// Returns a new instace of the Eav Context on specified ZoneId and/or AppId
		/// </summary>
		public static EavContext Instance(int? zoneId = null, int? appId = null)
		{
			var context = Instance();
			context.InitZoneApp(zoneId, appId);

			return context;
		}

		/// <summary>
		/// Set ZoneId and AppId on current context.
		/// </summary>
		public void InitZoneApp(int? zoneId = null, int? appId = null)
		{
			if (zoneId.HasValue)
				_zoneId = zoneId.Value;
			else
			{
				if (appId.HasValue)
					_zoneId = Apps.Where(a => a.AppID == appId.Value).Select(a => a.ZoneID).Single();
				else
					_zoneId = DataSource.DefaultZoneId;
			}

			if (appId.HasValue)
				// Set AppId and validate AppId exists with specified ZoneId
				_appId = Apps.Where(a => a.AppID == appId.Value && a.ZoneID == _zoneId).Select(a => a.AppID).Single();
			else
				_appId = Apps.Where(a => a.Name == DefaultAppName && a.ZoneID == _zoneId).Select(a => a.AppID).Single();

		}

		#endregion

		#region Add

		/// <summary>
		/// Import a new Entity
		/// </summary>
		internal Entity ImportEntity(int attributeSetId, Import.Entity entity, List<LogItem> importLog, bool isPublished = true)
		{
			return AddEntity(null, attributeSetId, entity.Values, null, entity.KeyNumber, null, null, entity.AssignmentObjectTypeId, 0, entity.EntityGuid, null, updateLog: importLog, isPublished: isPublished);
		}
		/// <summary>
		/// Add a new Entity
		/// </summary>
		public Entity AddEntity(AttributeSet attributeSet, IDictionary values, int? configurationSet, int? key, int assignmentObjectTypeId = 1, int sortOrder = 0, Guid? entityGuid = null, ICollection<int> dimensionIds = null, bool isPublished = true)
		{
			return AddEntity(attributeSet, 0, values, configurationSet, key, null, null, assignmentObjectTypeId, sortOrder, entityGuid, dimensionIds, isPublished: isPublished);
		}
		/// <summary>
		/// Add a new Entity
		/// </summary>
		public Entity AddEntity(int attributeSetId, IDictionary values, int? configurationSet, int? key, int assignmentObjectTypeId = 1, int sortOrder = 0, Guid? entityGuid = null, ICollection<int> dimensionIds = null, bool isPublished = true)
		{
			return AddEntity(null, attributeSetId, values, configurationSet, key, null, null, assignmentObjectTypeId, sortOrder, entityGuid, dimensionIds, isPublished: isPublished);
		}
		/// <summary>
		/// Add a new Entity
		/// </summary>
		public Entity AddEntity(int attributeSetId, IDictionary values, int? configurationSet, Guid key, int assignmentObjectTypeId = 1, int sortOrder = 0, Guid? entityGuid = null, ICollection<int> dimensionIds = null, bool isPublished = true)
		{
			return AddEntity(null, attributeSetId, values, configurationSet, null, key, null, assignmentObjectTypeId, sortOrder, entityGuid, dimensionIds, isPublished: isPublished);
		}
		/// <summary>
		/// Add a new Entity
		/// </summary>
		private Entity AddEntity(AttributeSet attributeSet, int attributeSetId, IDictionary values, int? configurationSet, int? keyNumber, Guid? keyGuid, string keyString, int assignmentObjectTypeId, int sortOrder, Guid? entityGuid, ICollection<int> dimensionIds, List<LogItem> updateLog = null, bool isPublished = true)
		{
			var changeId = GetChangeLogId();

			var newEntity = new Entity
			{
				ConfigurationSet = configurationSet,
				AssignmentObjectTypeID = assignmentObjectTypeId,
				KeyNumber = keyNumber,
				KeyGuid = keyGuid,
				KeyString = keyString,
				SortOrder = sortOrder,
				ChangeLogIDCreated = changeId,
				EntityGUID = (entityGuid.HasValue && entityGuid.Value != new Guid()) ? entityGuid.Value : Guid.NewGuid(),
				IsPublished = isPublished
			};

			if (attributeSet != null)
				newEntity.Set = attributeSet;
			else
				newEntity.AttributeSetID = attributeSetId;

			AddToEntities(newEntity);

			SaveChanges();

			UpdateEntity(newEntity.EntityID, values, masterRecord: true, dimensionIds: dimensionIds, autoSave: false, updateLog: updateLog);

			SaveChanges();

			return newEntity;
		}

		/// <summary>
		/// Clone an Entity with all Values
		/// </summary>
		private Entity CloneEntity(Entity sourceEntity)
		{
			var clone = sourceEntity.CopyEntity(this);

			AddToEntities(clone);

			// Add all Values with Dimensions
			foreach (var eavValue in sourceEntity.Values.ToList())
			{
				var value = eavValue.CopyEntity(this);
				// copy Dimensions
				foreach (var valuesDimension in eavValue.ValuesDimensions)
					value.ValuesDimensions.Add(new ValueDimension { DimensionID = valuesDimension.DimensionID, ReadOnly = valuesDimension.ReadOnly });

				clone.Values.Add(value);
			}

			// Add all Related Entities
			foreach (var entityParentRelationship in sourceEntity.EntityParentRelationships)
				clone.EntityParentRelationships.Add(new EntityRelationship { AttributeID = entityParentRelationship.AttributeID, ChildEntityID = entityParentRelationship.ChildEntityID });

			return clone;
		}

		/// <summary>
		/// Add a new Value
		/// </summary>
		private EavValue AddValue(Entity entity, int attributeId, string value, bool autoSave = true)
		{
			var changeId = GetChangeLogId();

			var newValue = new EavValue
			{
				AttributeID = attributeId,
				Entity = entity,
				Value = value,
				ChangeLogIDCreated = changeId
			};

			AddToValues(newValue);
			if (autoSave)
				SaveChanges();
			return newValue;
		}

		/// <summary>
		/// Append a new Attribute to an AttributeSet
		/// </summary>
		public Attribute AppendAttribute(AttributeSet attributeSet, string staticName, string type, bool isTitle = false, bool autoSave = true)
		{
			return AppendAttribute(attributeSet, 0, staticName, type, isTitle, autoSave);
		}
		/// <summary>
		/// Append a new Attribute to an AttributeSet
		/// </summary>
		public Attribute AppendAttribute(int attributeSetId, string staticName, string type, bool isTitle = false)
		{
			return AppendAttribute(null, attributeSetId, staticName, type, isTitle, true);
		}
		/// <summary>
		/// Append a new Attribute to an AttributeSet
		/// </summary>
		private Attribute AppendAttribute(AttributeSet attributeSet, int attributeSetId, string staticName, string type, bool isTitle, bool autoSave)
		{
			var sortOrder = attributeSet != null ? attributeSet.AttributesInSets.Max(s => (int?)s.SortOrder) : AttributesInSets.Where(a => a.AttributeSetID == attributeSetId).Max(s => (int?)s.SortOrder);
			if (!sortOrder.HasValue)
				sortOrder = 0;
			else
				sortOrder++;

			return AddAttribute(attributeSet, attributeSetId, staticName, type, sortOrder.Value, 1, isTitle, autoSave);
		}

		/// <summary>
		/// Append a new Attribute to an AttributeSet
		/// </summary>
		public Attribute AddAttribute(int attributeSetId, string staticName, string type, int sortOrder = 0, int attributeGroupId = 1, bool isTitle = false, bool autoSave = true)
		{
			return AddAttribute(null, attributeSetId, staticName, type, sortOrder, attributeGroupId, isTitle, autoSave);
		}

		/// <summary>
		/// Append a new Attribute to an AttributeSet
		/// </summary>
		private Attribute AddAttribute(AttributeSet attributeSet, int attributeSetId, string staticName, string type, int sortOrder, int attributeGroupId, bool isTitle, bool autoSave)
		{
			if (attributeSet == null)
				attributeSet = AttributeSets.Single(a => a.AttributeSetID == attributeSetId);
			else if (attributeSetId != 0)
				throw new Exception("Can only set attributeSet or attributeSetId");

			if (!System.Text.RegularExpressions.Regex.IsMatch(staticName, AttributeStaticNameRegEx, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
				throw new Exception("Attribute static name \"" + staticName + "\" is invalid. " + AttributeStaticNameRegExNotes);

			// Prevent Duplicate Name
			if (AttributesInSets.Any(s => s.Attribute.StaticName == staticName && !s.Attribute.ChangeLogIDDeleted.HasValue && s.AttributeSetID == attributeSet.AttributeSetID && s.Set.AppID == _appId))
				throw new ArgumentException("An Attribute with static name " + staticName + " already exists", "staticName");

			var newAttribute = new Attribute
			{
				Type = type,
				StaticName = staticName,
				ChangeLogIDCreated = GetChangeLogId()
			};
			var setAssignment = new AttributeInSet
			{
				Attribute = newAttribute,
				Set = attributeSet,
				SortOrder = sortOrder,
				AttributeGroupID = attributeGroupId,
				IsTitle = isTitle
			};
			AddToAttributes(newAttribute);
			AddToAttributesInSets(setAssignment);

			// Set Attribute as Title if there's no title field in this set
			if (!attributeSet.AttributesInSets.Any(a => a.IsTitle))
				setAssignment.IsTitle = true;

			if (isTitle)
			{
				// unset old Title Fields
				var oldTitleFields = attributeSet.AttributesInSets.Where(a => a.IsTitle && a.Attribute.StaticName != staticName).ToList();
				foreach (var titleField in oldTitleFields)
					titleField.IsTitle = false;
			}

			if (autoSave)
				SaveChanges();
			return newAttribute;
		}

		/// <summary>
		/// Add a new AttributeSet
		/// </summary>
		public AttributeSet AddAttributeSet(string name, string description, string staticName, string scope, bool autoSave = true)
		{
			return AddAttributeSet(name, description, staticName, scope, autoSave, null);
		}

		private AttributeSet AddAttributeSet(string name, string description, string staticName, string scope, bool autoSave, int? appId)
		{
			if (string.IsNullOrEmpty(staticName))
				staticName = Guid.NewGuid().ToString();

			var targetAppId = appId.HasValue ? appId.Value : _appId;

			// ensure AttributeSet with StaticName doesn't exist on App
			if (AttributeSetExists(staticName, targetAppId))
				throw new Exception("An AttributeSet with StaticName \"" + staticName + "\" already exists.");

			var newSet = new AttributeSet
			{
				Name = name,
				StaticName = staticName,
				Description = description,
				Scope = scope,
				ChangeLogIDCreated = GetChangeLogId(),
				AppID = targetAppId
			};

			AddToAttributeSets(newSet);

			if (autoSave)
				SaveChanges();

			return newSet;
		}

		#endregion

		#region Update

		/// <summary>
		/// Update an Entity
		/// </summary>
		/// <param name="entityGuid">EntityGUID</param>
		/// <param name="newValues">new Values of this Entity</param>
		/// <param name="autoSave">auto save Changes to DB</param>
		/// <param name="dimensionIds">DimensionIds for all Values</param>
		/// <param name="masterRecord">Is this the Master Record/Language</param>
		/// <param name="updateLog">Update/Import Log List</param>
		/// <param name="preserveUndefinedValues">Preserve Values if Attribute is not specifeied in NewValues</param>
		/// <returns>the updated Entity</returns>
		public Entity UpdateEntity(Guid entityGuid, IDictionary newValues, bool autoSave = true, ICollection<int> dimensionIds = null, bool masterRecord = true, List<LogItem> updateLog = null, bool preserveUndefinedValues = true)
		{
			var entity = GetEntity(entityGuid);
			return UpdateEntity(entity.EntityID, newValues, autoSave, dimensionIds, masterRecord, updateLog, preserveUndefinedValues);
		}

		/// <summary>
		/// Update an Entity
		/// </summary>
		/// <param name="entityId">EntityID</param>
		/// <param name="newValues">new Values of this Entity</param>
		/// <param name="autoSave">auto save Changes to DB</param>
		/// <param name="dimensionIds">DimensionIds for all Values</param>
		/// <param name="masterRecord">Is this the Master Record/Language</param>
		/// <param name="updateLog">Update/Import Log List</param>
		/// <param name="preserveUndefinedValues">Preserve Values if Attribute is not specifeied in NewValues</param>
		/// <param name="isPublished">Is this Entity Published or a draft</param>
		/// <returns>the updated Entity</returns>
		public Entity UpdateEntity(int entityId, IDictionary newValues, bool autoSave = true, ICollection<int> dimensionIds = null, bool masterRecord = true, List<LogItem> updateLog = null, bool preserveUndefinedValues = true, bool isPublished = true)
		{
			var entity = Entities.Single(e => e.EntityID == entityId);

			#region Unpublished Save (Draft-Saves)
			// Current Entity is published but Update as a draft
			if (entity.IsPublished && !isPublished)
			{
				// Prevent duplicate Draft
				var existingDraft = Entities.Where(e => e.PublishedEntityId == entityId && !e.ChangeLogIDDeleted.HasValue).Select(e => e.EntityID).FirstOrDefault();
				if (existingDraft != 0)
					throw new InvalidOperationException(string.Format("Published EntityId {0} has already a draft with EntityId {1}", entityId, existingDraft));

				// create a new Draft-Entity
				entity = CloneEntity(entity);
				entity.IsPublished = false;
				entity.PublishedEntityId = entityId;
			}
			// Update as Published but Current Entity is a Draft-Entity
			else if (!entity.IsPublished && isPublished)
			{
				entity = PublishEntity(entityId, false);
			}
			#endregion

			if (dimensionIds == null)
				dimensionIds = new List<int>(0);

			// Load all Attributes and current Values - .ToList() to prevent (slow) lazy loading
			var attributes = GetAttributes(entity.AttributeSetID).ToList();
			var currentValues = entity.EntityID != 0 ? Values.Include("Attribute").Include("ValuesDimensions").Where(v => v.EntityID == entity.EntityID).ToList() : entity.Values.ToList();

			// Update Values from Import Model
			var newValuesImport = newValues as Dictionary<string, List<IValueImportModel>>;
			if (newValuesImport != null)
				UpdateEntityFromImportModel(entity, newValuesImport, updateLog, attributes, currentValues, preserveUndefinedValues);
			// Update Values from ValueViewModel
			else
				UpdateEntityDefault(entity, newValues, dimensionIds, masterRecord, attributes, currentValues);

			SaveChanges();	// must save now to generate EntityModel afterward

			SaveEntityToDataTimeline(entity);

			return entity;
		}

		/// <summary>
		/// Update an Entity when not using the Import
		/// </summary>
		private void UpdateEntityDefault(Entity entity, IDictionary newValues, ICollection<int> dimensionIds, bool masterRecord, List<Attribute> attributes, List<EavValue> currentValues)
		{
			// Get all entities to make updates faster?
			var allEntities = DataSource.GetInitialDataSource(ZoneId, AppId)[DataSource.DefaultStreamName].List;
			var newValuesTyped = DictionaryToValuesViewModel(newValues);
			foreach (var newValue in newValuesTyped)
			{
				var attribute = attributes.Single(a => a.StaticName == newValue.Key);
				UpdateValue(entity, attribute, masterRecord, currentValues, allEntities, newValue.Value, dimensionIds);
			}

			#region if Dimensions are specified, purge/remove specified dimensions for Values that are not in newValues
			if (dimensionIds.Count > 0)
			{
				var keys = newValuesTyped.Keys.ToArray();
				// Get all Values that are not present in newValues
				var valuesToPurge = entity.Values.Where(v => !v.ChangeLogIDDeleted.HasValue && !keys.Contains(v.Attribute.StaticName) && v.ValuesDimensions.Any(d => dimensionIds.Contains(d.DimensionID)));
				foreach (var valueToPurge in valuesToPurge)
				{
					// Check if the Value is only used in this supplied dimension (carefull, dont' know what to do if we have multiple dimensions!, must define later)
					// if yes, delete/invalidate the value
					if (valueToPurge.ValuesDimensions.Count == 1)
						valueToPurge.ChangeLogIDDeleted = GetChangeLogId();
					// if now, remove dimension from Value
					else
					{
						foreach (var valueDimension in valueToPurge.ValuesDimensions.Where(d => dimensionIds.Contains(d.DimensionID)).ToList())
							valueToPurge.ValuesDimensions.Remove(valueDimension);
					}
				}
			}
			#endregion
		}

		/// <summary>
		/// Update an Entity when using the Import
		/// </summary>
		private void UpdateEntityFromImportModel(Entity currentEntity, Dictionary<string, List<IValueImportModel>> newValuesImport, List<LogItem> updateLog, List<Attribute> attributes, List<EavValue> currentValues, bool preserveUndefinedValues)
		{
			if (updateLog == null)
				throw new ArgumentNullException("updateLog", "When Calling UpdateEntity() with newValues of Type IValueImportModel updateLog must be set.");

			// track updated values to remove values that were not updated automatically
			var updatedValueIds = new List<int>();
			var updatedAttributeIds = new List<int>();
			foreach (var newValue in newValuesImport)
			{
				var attribute = attributes.SingleOrDefault(a => a.StaticName == newValue.Key);
				if (attribute == null) // Attribute not found
				{
					// Log Warning for all Values
					updateLog.AddRange(newValue.Value.Select(v => new LogItem(EventLogEntryType.Warning, "Attribute not found for Value")
								{
									Attribute = new Import.Attribute { StaticName = newValue.Key },
									Value = v,
									Entity = v.Entity
								}));
					continue;
				}

				updatedAttributeIds.Add(attribute.AttributeID);

				foreach (var newSingleValue in newValue.Value)
				{
					try
					{
						var updatedValue = UpdateValueByImport(currentEntity, attribute, currentValues, newSingleValue);

						var updatedEavValue = updatedValue as EavValue;
						if (updatedEavValue != null)
							updatedValueIds.Add(updatedEavValue.ValueID);
					}
					catch (Exception ex)
					{
						updateLog.Add(new LogItem(EventLogEntryType.Error, "Update Entity-Value failed")
						{
							Attribute = new Import.Attribute { StaticName = newValue.Key },
							Value = newSingleValue,
							Entity = newSingleValue.Entity,
							Exception = ex
						});
					}
				}
			}

			// remove all existing values that were not updated
			var valuesToDelete = currentEntity.Values.Where(v => !updatedValueIds.Contains(v.ValueID) && v.ChangeLogIDDeleted == null && (preserveUndefinedValues == false || updatedAttributeIds.Contains(v.AttributeID))).ToList();
			valuesToDelete.ForEach(v => v.ChangeLogIDDeleted = GetChangeLogId());
		}

		/// <summary>
		/// Publish a Draft Entity
		/// </summary>
		/// <param name="entityId">ID of the Draft-Entity</param>
		public void PublishEntity(int entityId)
		{
			PublishEntity(entityId, true);
		}

		/// <summary>
		/// Publish a Draft-Entity
		/// </summary>
		/// <param name="entityId">ID of the Draft-Entity</param>
		/// <param name="copyEntityValues">Should all values of the Draft-Entity be copied to the published Entity</param>
		/// <returns>The published Entity</returns>
		private Entity PublishEntity(int entityId, bool copyEntityValues)
		{
			var unpublishedEntity = GetEntity(entityId);
			if (unpublishedEntity.IsPublished)
				throw new InvalidDataException(string.Format("EntityId {0} is already published"));

			Entity publishedEntity;

			// Publish Draft-Entity
			if (!unpublishedEntity.PublishedEntityId.HasValue)
			{
				unpublishedEntity.IsPublished = true;
				publishedEntity = unpublishedEntity;
			}
			// Replace currently published Entity with Unpublished Entity
			else
			{
				publishedEntity = GetEntity(unpublishedEntity.PublishedEntityId.Value);
				if (copyEntityValues)
					throw new NotImplementedException();
				//UpdateEntity(publishedEntity.EntityID,)

				// delete the Draft Entity
				DeleteEntity(entityId);
			}

			SaveChanges();

			return publishedEntity;
		}

		#region Update Values
		/// <summary>
		/// Update a Value when using IValueImportModel. Returns the Updated Value (for simple Values) or null (for Entity-Values)
		/// </summary>
		private object UpdateValueByImport(Entity currentEntity, Attribute attribute, List<EavValue> currentValues, IValueImportModel newValue)
		{
			switch (attribute.Type)
			{
				// Handle Entity Relationships - they're stored in own tables
				case "Entity":
					if (newValue is ValueImportModel<List<Guid>> && attribute.Type == "Entity")
						UpdateEntityRelationships(attribute.AttributeID, ((ValueImportModel<List<Guid>>)newValue).Value, currentEntity.EntityGUID);
					else
						throw new NotSupportedException("UpdateValue() for Attribute " + attribute.StaticName + " with newValue of type" + newValue.GetType() + " not supported. Expected List<Guid>");

					return null;
				// Handle simple values in Values-Table
				default:
					// masterRecord can be true or false, it's not used when valueDimensions is specified
					return UpdateSimpleValue(attribute, currentEntity, null, true, GetTypedValue(newValue, attribute.Type, attribute.StaticName), null, false, currentValues, null, newValue.ValueDimensions);
			}
		}

		/// <summary>
		/// Get typed value from ValueImportModel
		/// </summary>
		private static object GetTypedValue(IValueImportModel newValue, string attributeType = null, string attributeStaticName = null)
		{
			object newValueTyped;
			if (newValue is ValueImportModel<bool?> && (attributeType == null || attributeType == "Boolean"))
				newValueTyped = ((ValueImportModel<bool?>)newValue).Value;
			else if (newValue is ValueImportModel<DateTime?> && (attributeType == null || attributeType == "DateTime"))
				newValueTyped = ((ValueImportModel<DateTime?>)newValue).Value;
			else if (newValue is ValueImportModel<decimal?> && (attributeType == null || attributeType == "Number"))
				newValueTyped = ((ValueImportModel<decimal?>)newValue).Value;
			else if (newValue is ValueImportModel<string> &&
					 (attributeType == null || attributeType == "String" || attributeType == "Hyperlink"))
				newValueTyped = ((ValueImportModel<string>)newValue).Value;
			else
				throw new NotSupportedException(string.Format("GetTypedValue() for Attribute {0} (Type: {1}) with newValue of type {2} not supported.", attributeStaticName, attributeType, newValue.GetType()));
			return newValueTyped;
		}

		/// <summary>
		/// Update a Value when using ValueViewModel
		/// </summary>
		private void UpdateValue(Entity currentEntity, Attribute attribute, bool masterRecord, List<EavValue> currentValues, IDictionary<int, IEntity> allEntities, ValueViewModel newValue, ICollection<int> dimensionIds)
		{
			switch (attribute.Type)
			{
				// Handle Entity Relationships - they're stored in own tables
				case "Entity":
					UpdateEntityRelationships(attribute.AttributeID, (int[])newValue.Value, currentEntity);
					break;
				// Handle simple values in Values-Table
				default:
					UpdateSimpleValue(attribute, currentEntity, dimensionIds, masterRecord, newValue.Value, newValue.ValueId, newValue.ReadOnly, currentValues, allEntities);
					break;
			}
		}

		/// <summary>
		/// Update a Value in the Values-Table
		/// </summary>
		private EavValue UpdateSimpleValue(Attribute attribute, Entity entity, ICollection<int> dimensionIds, bool masterRecord, object newValue, int? valueId, bool readOnly, List<EavValue> currentValues, IDictionary<int, IEntity> allEntities, IEnumerable<Import.ValueDimension> valueDimensions = null)
		{
			var newValueSerialized = SerializeValue(newValue);
			var changeId = GetChangeLogId();

			// Get Value or create new one
			var value = GetOrCreateValue(attribute, entity, masterRecord, valueId, readOnly, currentValues, allEntities, newValueSerialized, changeId, valueDimensions);

			#region Update DimensionIds on this and other values

			// Update Dimensions as specified by Import
			if (valueDimensions != null)
			{
				var valueDimensionsToDelete = value.ValuesDimensions.ToList();
				// loop all specified Dimensions, add or update it for this value
				foreach (var valueDimension in valueDimensions)
				{
					// ToDo: 2bg Log Error but continue
					var dimensionId = GetDimensionId(null, valueDimension.DimensionExternalKey);
					if (dimensionId == 0)
						throw new Exception("Dimension " + valueDimension.DimensionExternalKey + " not found. EntityId: " + entity.EntityID + " Attribute-StaticName: " + attribute.StaticName);

					var existingValueDimension = value.ValuesDimensions.SingleOrDefault(v => v.DimensionID == dimensionId);
					if (existingValueDimension == null)
						value.ValuesDimensions.Add(new ValueDimension { DimensionID = dimensionId, ReadOnly = valueDimension.ReadOnly });
					else
					{
						valueDimensionsToDelete.Remove(valueDimensionsToDelete.Single(vd => vd.DimensionID == dimensionId));
						existingValueDimension.ReadOnly = valueDimension.ReadOnly;
					}
				}

				// remove old dimensions
				valueDimensionsToDelete.ForEach(DeleteObject);
			}
			// Update Dimensions as specified on the whole Entity
			else if (dimensionIds != null)
			{
				#region Ensure specified Dimensions are updated/added (whether Value has changed or not)
				// Update existing ValuesDimensions
				foreach (var valueDimension in value.ValuesDimensions.Where(vd => dimensionIds.Contains(vd.DimensionID)))
				{
					// ReSharper disable RedundantCheckBeforeAssignment
					// Check to prevent unneeded DB queries
					if (valueDimension.ReadOnly != readOnly)
						// ReSharper restore RedundantCheckBeforeAssignment
						valueDimension.ReadOnly = readOnly;
				}

				// Add new ValuesDimensions
				foreach (var dimensionId in dimensionIds.Where(i => value.ValuesDimensions.All(d => d.DimensionID != i)))
					value.ValuesDimensions.Add(new ValueDimension { DimensionID = dimensionId, ReadOnly = readOnly });

				#endregion

				// Remove current Dimension(s) from other Values
				if (!masterRecord)
				{
					// Get other Values for current Attribute having all Current Dimensions assigned
					var otherValuesWithCurrentDimensions = currentValues.Where(v => v.AttributeID == attribute.AttributeID && v.ValueID != value.ValueID && dimensionIds.All(d => v.ValuesDimensions.Select(vd => vd.DimensionID).Contains(d)));
					foreach (var otherValue in otherValuesWithCurrentDimensions)
					{
						foreach (var valueDimension in otherValue.ValuesDimensions.Where(vd => dimensionIds.Contains(vd.DimensionID)).ToList())
						{
							// if only one Dimension assigned, mark this value as deleted
							if (otherValue.ValuesDimensions.Count == 1)
								otherValue.ChangeLogIDDeleted = changeId;

							otherValue.ValuesDimensions.Remove(valueDimension);
						}
					}
				}
			}
			#endregion

			return value;
		}

		/// <summary>
		/// Get an EavValue for specified EntityId etc. or create a new one. Uses different mechanism when running an Import or ValueId is specified.
		/// </summary>
		private EavValue GetOrCreateValue(Attribute attribute, Entity entity, bool masterRecord, int? valueId, bool readOnly, List<EavValue> currentValues, IDictionary<int, IEntity> allEntities, string newValueSerialized, int changeId, IEnumerable<Import.ValueDimension> valueDimensions)
		{
			EavValue value = null;
			// if Import-Dimension(s) are Specified
			if (valueDimensions != null)
			{
				// Get first value having first Dimension or add new value
				value = currentValues.FirstOrDefault(v => v.ChangeLogIDDeleted == null && v.Attribute.StaticName == attribute.StaticName && v.ValuesDimensions.Any(d => d.Dimension.ExternalKey.Equals(valueDimensions.First().DimensionExternalKey, StringComparison.InvariantCultureIgnoreCase))) ??
						AddValue(entity, attribute.AttributeID, newValueSerialized, autoSave: false);
			}
			// if ValueID & EntityId is specified, use this Value
			else if (valueId.HasValue && entity.EntityID != 0)
			{
				value = currentValues.Single(v => v.ValueID == valueId.Value && v.Attribute.StaticName == attribute.StaticName);
				// If Master, ensure ValueID is from Master!
				var entityModel = allEntities[entity.EntityID];
				var attributeModel = (IAttributeManagement)entityModel.Attributes.SingleOrDefault(a => a.Key == attribute.StaticName).Value;
				if (masterRecord && value.ValueID != attributeModel.DefaultValue.ValueId)
					throw new Exception("Master Record cannot use a ValueID rather ValueID from Master. Attribute-StaticName: " + attribute.StaticName);
			}
			// Find Value (if not specified) or create new one
			else
			{
				if (masterRecord) // if true, don't create new Value (except no one exists)
					value = currentValues.Where(v => v.AttributeID == attribute.AttributeID).OrderBy(a => a.ChangeLogIDCreated).FirstOrDefault();

				// if no Value found, create new one
				if (value == null)
				{
					if (!masterRecord && currentValues.All(v => v.AttributeID != attribute.AttributeID))
						// if updating Additional-Entity but Default-Entity doesn't have any atom
						throw new Exception("Update of a \"" + attribute.StaticName +
											"\" is not allowed. You must first updated this Value for the Default-Entity.");

					value = AddValue(entity, attribute.AttributeID, newValueSerialized, autoSave: false);
				}
			}

			// Update old/existing Value
			if (value.ValueID != 0 || entity.EntityID == 0)
			{
				if (!readOnly)
					UpdateValue(value, newValueSerialized, changeId, false);
			}
			return value;
		}

		/// <summary>
		/// Serialize Value to a String for SQL Server or XML Export
		/// </summary>
		public static string SerializeValue(object newValue)
		{
			string newValueSerialized;
			if (newValue is DateTime)
				newValueSerialized = ((DateTime)newValue).ToString("s");
			else if (newValue is double)
				newValueSerialized = ((double)newValue).ToString(CultureInfo.InvariantCulture);
			else if (newValue is decimal)
				newValueSerialized = ((decimal)newValue).ToString(CultureInfo.InvariantCulture);
			else if (newValue == null)
				newValueSerialized = string.Empty;
			else
				newValueSerialized = newValue.ToString();
			return newValueSerialized;
		}

		/// <summary>
		/// Serialize Value to a String for SQL Server or XML Export
		/// </summary>
		internal string SerializeValue(IValue value)
		{
			var stringValue = value as ValueModel<string>;
			if (stringValue != null)
				return stringValue.TypedContents;

			var relationshipValue = value as ValueModel<EntityRelationshipModel>;
			if (relationshipValue != null)
			{
				var entityGuids = relationshipValue.TypedContents.EntityIds.Select(entityId => GetEntity(entityId).EntityGUID);

				return string.Join(",", entityGuids);
			}

			var boolValue = value as ValueModel<bool?>;
			if (boolValue != null)
				return boolValue.TypedContents.ToString();

			var dateTimeValue = value as ValueModel<DateTime?>;
			if (dateTimeValue != null)
				return dateTimeValue.TypedContents.HasValue ? dateTimeValue.TypedContents.Value.ToString("s") : "";

			var decimalValue = value as ValueModel<decimal?>;
			if (decimalValue != null)
				return decimalValue.TypedContents.HasValue ? decimalValue.TypedContents.Value.ToString(CultureInfo.InvariantCulture) : "";

			throw new NotSupportedException("Can't serialize Value");
		}

		/// <summary>
		/// Update Relationships of an Entity
		/// </summary>
		private void UpdateEntityRelationships(int attributeId, IEnumerable<int> newValue, Entity currentEntity)
		{
			// remove existing Relationships that are not in new list
			var newEntityIds = newValue.Distinct().ToList();
			// remove duplicates and build List to use FindIndex() later
			var relationsToDelete = currentEntity.EntityParentRelationships.Where(e => e.AttributeID == attributeId && !newEntityIds.Contains(e.ChildEntityID));
			foreach (var relationToDelete in relationsToDelete.ToList())
				EntityRelationships.DeleteObject(relationToDelete);


			// add new Relationships that don't exist yet
			var relationsToAdd = newEntityIds.Where(e => !currentEntity.EntityParentRelationships.Any(r => r.AttributeID == attributeId && r.ChildEntityID == e));
			foreach (var childEntityId in relationsToAdd)
				currentEntity.EntityParentRelationships.Add(new EntityRelationship { AttributeID = attributeId, ChildEntityID = childEntityId });

			// ensure sortorder of all
			foreach (var relationship in currentEntity.EntityParentRelationships.Where(r => r.AttributeID == attributeId))
			{
				var newSortOrder = newEntityIds.FindIndex(i => i == relationship.ChildEntityID);
				relationship.SortOrder = newSortOrder;
			}
		}

		/// <summary>
		/// Update Relationships of an Entity. Update isn't done until ImportEntityRelationshipsQueue() is called!
		/// </summary>
		private void UpdateEntityRelationships(int attributeId, List<Guid> newValue, Guid entityGuid)
		{
			_entityRelationshipsQueue.Add(new EntityRelationshipQueueItem { AttributeId = attributeId, ChildEntityGuids = newValue, ParentEntityGuid = entityGuid });
		}

		/// <summary>
		/// Import Entity Relationships Queue (Populated by UpdateEntityRelationships) and Clear Queue afterward.
		/// </summary>
		internal void ImportEntityRelationshipsQueue()
		{
			foreach (var relationship in _entityRelationshipsQueue)
			{
				var entity = GetEntity(relationship.ParentEntityGuid);
				var childEntityIds = new List<int>();
				foreach (var childGuid in relationship.ChildEntityGuids)
				{
					try
					{
						childEntityIds.Add(GetEntity(childGuid).EntityID);
					}
					catch (InvalidOperationException) { }	// may occur if the child entity wasn't created successfully
				}

				UpdateEntityRelationships(relationship.AttributeId, childEntityIds, entity);
			}

			_entityRelationshipsQueue.Clear();
		}

		/// <summary>
		/// Update a Value
		/// </summary>
		private void UpdateValue(EavValue currentValue, string value, int changeId, bool autoSave = true)
		{
			// only if value has changed
			if (currentValue.Value.Equals(value))
				return;

			currentValue.Value = value;
			currentValue.ChangeLogIDModified = changeId;
			currentValue.ChangeLogIDDeleted = null;

			if (autoSave)
				SaveChanges();
		}
		#endregion

		/// <summary>
		/// Persists all updates to the data source and optionally resets change tracking in the object context.
		/// Also Creates an initial ChangeLog (used by SQL Server for Auditing).
		/// If items were modified, Cache is purged on current Zone/App
		/// </summary>
		public override int SaveChanges(System.Data.Objects.SaveOptions options)
		{
			// enure changelog exists and is set to SQL CONTEXT_INFO variable
			if (_mainChangeLogId == 0)
				GetChangeLogId(UserName);

			var modifiedItems = base.SaveChanges(options);

			if (modifiedItems != 0 && _purgeCacheOnSave)
				DataSource.GetCache(ZoneId, AppId).PurgeCache(ZoneId, AppId);

			return modifiedItems;
		}

		/// <summary>
		/// Change the sort order of an attribute - move up or down
		/// </summary>
		/// <remarks>Does an interchange with the Sort Order below/above the current attribute</remarks>
		public void ChangeAttributeOrder(int attributeId, int setId, AttributeMoveDirection direction)
		{
			var attributeToMove = AttributesInSets.Single(a => a.AttributeID == attributeId && a.AttributeSetID == setId);
			var attributeToInterchange = direction == AttributeMoveDirection.Up ?
				AttributesInSets.OrderByDescending(a => a.SortOrder).First(a => a.AttributeSetID == setId && a.SortOrder < attributeToMove.SortOrder) :
				AttributesInSets.OrderBy(a => a.SortOrder).First(a => a.AttributeSetID == setId && a.SortOrder > attributeToMove.SortOrder);

			var newSortOrder = attributeToInterchange.SortOrder;
			attributeToInterchange.SortOrder = attributeToMove.SortOrder;
			attributeToMove.SortOrder = newSortOrder;
			SaveChanges();
		}

		/// <summary>
		/// Set an Attribute as Title on an AttributeSet
		/// </summary>
		public void SetTitleAttribute(int attributeId, int attributeSetId)
		{
			AttributesInSets.Single(a => a.AttributeID == attributeId && a.AttributeSetID == attributeSetId).IsTitle = true;

			// unset other Attributes with isTitle=true
			var oldTitleAttributes = AttributesInSets.Where(s => s.AttributeSetID == attributeSetId && s.IsTitle);
			foreach (var oldTitleAttribute in oldTitleAttributes)
				oldTitleAttribute.IsTitle = false;

			SaveChanges();
		}

		/// <summary>
		/// Update an Attribute
		/// </summary>
		public Attribute UpdateAttribute(int attributeId, string staticName)
		{
			return UpdateAttribute(attributeId, staticName, null);
		}
		/// <summary>
		/// Update an Attribute
		/// </summary>
		public Attribute UpdateAttribute(int attributeId, string staticName, int? attributeSetId = null, bool isTitle = false)
		{
			var attribute = Attributes.Single(a => a.AttributeID == attributeId);
			SaveChanges();

			if (isTitle)
				SetTitleAttribute(attributeId, attributeSetId.Value);

			return attribute;
		}

		/// <summary>
		/// Update AdditionalProperties of a Field
		/// </summary>
		public Entity UpdateFieldAdditionalProperties(int attributeId, bool isAllProperty, IDictionary fieldProperties)
		{
			var fieldPropertyEntity = Entities.FirstOrDefault(e => e.AssignmentObjectTypeID == DataSource.AssignmentObjectTypeIdFieldProperties && e.KeyNumber == attributeId);
			if (fieldPropertyEntity != null)
				return UpdateEntity(fieldPropertyEntity.EntityID, fieldProperties);

			var metaDataSetName = isAllProperty ? "@All" : "@" + Attributes.Single(a => a.AttributeID == attributeId).Type;
			var systemScope = AttributeScope.System.ToString();
			var attributeSetId = AttributeSets.First(s => s.StaticName == metaDataSetName && s.Scope == systemScope && s.AppID == _appId).AttributeSetID;

			return AddEntity(attributeSetId, fieldProperties, null, attributeId, DataSource.AssignmentObjectTypeIdFieldProperties);
		}

		#endregion

		#region Delete

		/// <summary>
		/// Remove an Attribute from an AttributeSet and delete values
		/// </summary>
		public void RemoveAttributeInSet(int attributeId, int attributeSetId)
		{
			// Delete the AttributeInSet
			DeleteObject(AttributesInSets.Single(a => a.AttributeID == attributeId && a.AttributeSetID == attributeSetId));

			// Delete all Values an their ValueDimensions
			var valuesToDelete = Values.Where(v => v.AttributeID == attributeId && v.Entity.AttributeSetID == attributeSetId).ToList();
			foreach (var valueToDelete in valuesToDelete)
			{
				valueToDelete.ValuesDimensions.ToList().ForEach(DeleteObject);
				DeleteObject(valueToDelete);
			}

			// Delete all Entity-Relationships
			var relationshipsToDelete = EntityRelationships.Where(r => r.AttributeID == attributeId).ToList(); // No Filter by AttributeSetID is needed here at the moment because attribute can't be in multiple sets currently
			relationshipsToDelete.ForEach(DeleteObject);

			SaveChanges();
		}

		/// <summary>
		/// Test whehter Entity can be deleted safe if it has no relationships
		/// </summary>
		/// <returns>Item1: Indicates whether Entity can be deleted. Item2: Messages why Entity can't be deleted safe.</returns>
		public Tuple<bool, string> CanDeleteEntity(int entityId)
		{
			var messages = new List<string>();

			var entityAsParent = EntityRelationships.Where(r => r.ParentEntityID == entityId).Select(r => r.ChildEntityID).ToList();
			if (entityAsParent.Any())
				messages.Add(string.Format("Entity has {0} Parent-Relationships to Entities: {1}.", entityAsParent.Count, string.Join(", ", entityAsParent)));

			var entityChild = EntityRelationships.Where(r => r.ChildEntityID == entityId).Select(r => r.ParentEntityID).ToList();
			if (entityChild.Any())
				messages.Add(string.Format("Entity has {0} Child-Relationships to Entities: {1}.", entityChild.Count, string.Join(", ", entityChild)));

			var assignedEntitiesFieldProperties = GetEntitiesInternal(DataSource.AssignmentObjectTypeIdFieldProperties, entityId).Select(e => e.EntityID).ToList();
			if (assignedEntitiesFieldProperties.Any())
				messages.Add(string.Format("Entity has {0} assigned Field-Property-Entities: {1}.", assignedEntitiesFieldProperties.Count, string.Join(", ", assignedEntitiesFieldProperties)));

			var assignedEntitiesDataPipeline = GetEntitiesInternal(DataSource.AssignmentObjectTypeIdDataPipeline, entityId).Select(e => e.EntityID).ToList();
			if (assignedEntitiesDataPipeline.Any())
				messages.Add(string.Format("Entity has {0} assigned Data-Pipeline Entities: {1}.", assignedEntitiesDataPipeline.Count, string.Join(", ", assignedEntitiesDataPipeline)));

			return Tuple.Create(!messages.Any(), string.Join(" ", messages));
		}

		/// <summary>
		/// Delete an Entity
		/// </summary>
		public bool DeleteEntity(int entityId)
		{
			return DeleteEntity(Entities.SingleOrDefault(e => e.EntityID == entityId));
		}

		/// <summary>
		/// Delete an Entity
		/// </summary>
		public bool DeleteEntity(Guid entityGuid)
		{
			return DeleteEntity(Entities.SingleOrDefault(e => e.EntityGUID == entityGuid));
		}

		/// <summary>
		/// Delete an Entity
		/// </summary>
		private bool DeleteEntity(Entity entity)
		{
			if (entity == null)
				return false;

			entity.ChangeLogIDDeleted = GetChangeLogId();

			SaveChanges();

			return true;
		}

		#endregion

		#region Zones

		/// <summary>
		/// Get all Zones
		/// </summary>
		/// <returns>Dictionary with ZoneId as Key and ZoneModel</returns>
		public Dictionary<int, ZoneModel> GetAllZones()
		{
			var zones = (from z in Zones
						 select
							 new
							 {
								 ZoneId = z.ZoneID,
								 DefaultAppId = z.Apps.FirstOrDefault(a => a.Name == DefaultAppName).AppID,
								 Apps = from a in z.Apps select new { a.AppID, a.Name }
							 }).ToDictionary(z => z.ZoneId,
												 z =>
												 new ZoneModel
												 {
													 ZoneId = z.ZoneId,
													 Apps = z.Apps.ToDictionary(a => a.AppID, a => a.Name),
													 DefaultAppId = z.DefaultAppId
												 });

			return zones;
		}

		/// <summary>
		/// Get all Zones
		/// </summary>
		/// <returns></returns>
		public List<Zone> GetZones()
		{
			return Zones.ToList();
		}

		/// <summary>
		/// Get a single Zone
		/// </summary>
		/// <returns>Zone or null</returns>
		public Zone GetZone(int zoneId)
		{
			return Zones.SingleOrDefault(z => z.ZoneID == zoneId);
		}

		/// <summary>
		/// Creates a new Zone with a default App and Culture-Root-Dimension
		/// </summary>
		public Tuple<Zone, App> AddZone(string name)
		{
			var newZone = new Zone { Name = name };
			AddToZones(newZone);

			AddDimension(CultureSystemKey, "Culture Root", newZone);

			var newApp = AddApp(newZone);

			SaveChanges();

			return Tuple.Create(newZone, newApp);
		}

		/// <summary>
		/// Update a Zone
		/// </summary>
		public void UpdateZone(int zoneId, string name)
		{
			var zone = Zones.Single(z => z.ZoneID == zoneId);
			zone.Name = name;

			SaveChanges();
		}

		#endregion

		#region Apps

		/// <summary>
		/// Add a new App
		/// </summary>
		private App AddApp(Zone zone, string name = DefaultAppName)
		{
			var newApp = new App
			{
				Name = name,
				Zone = zone
			};
			AddToApps(newApp);

			SaveChanges();	// required to ensure AppId is created - required in EnsureSharedAttributeSets();

			EnsureSharedAttributeSets(newApp);

			DataSource.GetCache(ZoneId, AppId).PurgeGlobalCache();

			return newApp;
		}

		/// <summary>
		/// Add a new App to the current Zone
		/// </summary>
		/// <param name="name">The name of the new App</param>
		/// <returns></returns>
		public App AddApp(string name)
		{
			return AddApp(GetZone(ZoneId), name);
		}

		/// <summary>
		/// Delete an existing App with any Values and Attributes
		/// </summary>
		/// <param name="appId">AppId to delete</param>
		public void DeleteApp(int appId)
		{
			// enure changelog exists and is set to SQL CONTEXT_INFO variable
			if (_mainChangeLogId == 0)
				GetChangeLogId(UserName);

			// Delete app using StoredProcedure
			DeleteAppInternal(appId);

			// Remove App from Global Cache
			DataSource.GetCache(ZoneId, AppId).PurgeGlobalCache();
		}

		/// <summary>
		/// Get all Apps in the current Zone
		/// </summary>
		/// <returns></returns>
		public List<App> GetApps()
		{
			return Apps.Where(a => a.ZoneID == ZoneId).ToList();
		}

		/// <summary>
		/// Ensure all AttributeSets with AlwaysShareConfiguration=true exist on specified App. App must be saved and have an AppId
		/// </summary>
		private void EnsureSharedAttributeSets(App app)
		{
			if (app.AppID == 0)
				throw new Exception("App must have a valid AppID");

			var sharedAttributeSets = GetAttributeSets(DataSource.MetaDataAppId, null).Where(a => a.AlwaysShareConfiguration);
			foreach (var sharedSet in sharedAttributeSets)
			{
				// Skip if attributeSet with StaticName already exists
				if (app.AttributeSets.Any(a => a.StaticName == sharedSet.StaticName))
					continue;

				// create new AttributeSet
				var newAttributeSet = AddAttributeSet(sharedSet.Name, sharedSet.Description, sharedSet.StaticName, sharedSet.Scope, false, app.AppID);
				newAttributeSet.UsesConfigurationOfAttributeSet = sharedSet.AttributeSetID;
			}

			// Ensure new AttributeSets are created and cache is refreshed
			SaveChanges();
		}

		#endregion

		/// <summary>
		/// Convert IOrderedDictionary to <see cref="Dictionary{String, ValueViewModel}" /> (for backward capability)
		/// </summary>
		private static Dictionary<string, ValueViewModel> DictionaryToValuesViewModel(IDictionary newValues)
		{
			if (newValues is Dictionary<string, ValueViewModel>)
				return (Dictionary<string, ValueViewModel>)newValues;

			return newValues.Keys.Cast<object>().ToDictionary(key => key.ToString(), key => new ValueViewModel { ReadOnly = false, Value = newValues[key] });
		}

		#region Internal Helper Classes
		private class EntityRelationshipQueueItem
		{
			public int AttributeId { get; set; }
			public Guid ParentEntityGuid { get; set; }
			public List<Guid> ChildEntityGuids { get; set; }
		}
		#endregion

		#region Versioning

		/// <summary>
		/// Creates a ChangeLog immediately
		/// </summary>
		/// <remarks>Also opens the SQL Connection to ensure this ChangeLog is used for Auditing on this SQL Connection</remarks>
		public int GetChangeLogId(string userName)
		{
			if (_mainChangeLogId == 0)
			{
				if (Connection.State != ConnectionState.Open)
					Connection.Open();	// make sure same connection is used later
				_mainChangeLogId = AddChangeLog(userName).Single().ChangeID;
			}

			return _mainChangeLogId;
		}

		/// <summary>
		/// Creates a ChangeLog immediately
		/// </summary>
		private int GetChangeLogId()
		{
			return GetChangeLogId(UserName);
		}

		/// <summary>
		/// Set ChangeLog ID on current Context and connection
		/// </summary>
		/// <param name="changeLogId"></param>
		public void SetChangeLogId(int changeLogId)
		{
			if (_mainChangeLogId != 0)
				throw new Exception("ChangeLogID was already set");


			Connection.Open();	// make sure same connection is used later
			SetChangeLogIdInternal(changeLogId);
			_mainChangeLogId = changeLogId;
		}

		/// <summary>
		/// Persist modified Entity to DataTimeline
		/// </summary>
		private void SaveEntityToDataTimeline(Entity currentEntity)
		{
			var export = new XmlExport(this);
			var entityModelSerialized = export.GetEntityXElement(currentEntity.EntityID);
			var timelineItem = new DataTimelineItem
			{
				SourceTable = "ToSIC_EAV_Entities",
				Operation = DataTimelineEntityStateOperation,
				NewData = entityModelSerialized.ToString(),
				SourceGuid = currentEntity.EntityGUID,
				SourceID = currentEntity.EntityID,
				SysLogID = GetChangeLogId(),
				SysCreatedDate = DateTime.Now
			};
			AddToDataTimeline(timelineItem);

			SaveChanges();
		}

		/// <summary>
		/// Get all Versions of specified EntityId
		/// </summary>
		public DataTable GetEntityVersions(int entityId)
		{
			// get Versions from DataTimeline
			var entityVersions = (from d in DataTimeline
								  join c in ChangeLogs on d.SysLogID equals c.ChangeID
								  where d.Operation == DataTimelineEntityStateOperation && d.SourceID == entityId
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
		/// Get an Entity in the specified Version from DataTimeline using XmlImport
		/// </summary>
		/// <param name="entityId">EntityId</param>
		/// <param name="changeId">ChangeId to retrieve</param>
		/// <param name="defaultCultureDimension">Default Language</param>
		public Import.Entity GetEntityVersion(int entityId, int changeId, int? defaultCultureDimension)
		{
			// Get Timeline Item
			var timelineItem = DataTimeline.SingleOrDefault(d => d.Operation == DataTimelineEntityStateOperation && d.SourceID == entityId && d.SysLogID == changeId);
			if (timelineItem == null)
				throw new InvalidOperationException(string.Format("EntityId {0} with ChangeId {1} not found in DataTimeline.", entityId, changeId));

			// Parse XML
			var xEntity = XElement.Parse(timelineItem.NewData);
			var assignmentObjectTypeName = xEntity.Attribute("AssignmentObjectType").Value;
			var assignmentObjectTypeId = GetAssignmentObjectType(assignmentObjectTypeName).AssignmentObjectTypeID;

			// Prepare source and target-Languages
			if (!defaultCultureDimension.HasValue)
				throw new NotSupportedException("GetEntityVersion without defaultCultureDimension is not yet supported.");
			var defaultLanguage = GetDimension(defaultCultureDimension.Value).ExternalKey;
			var targetDimensions = GetLanguages();
			var allSourceDimensionIds = ((IEnumerable<object>)xEntity.XPathEvaluate("/Value/Dimension/@DimensionID")).Select(d => int.Parse(((XAttribute)d).Value)).ToArray();
			var allSourceDimensionIdsDistinct = allSourceDimensionIds.Distinct().ToArray();
			var sourceDimensions = GetDimensions(allSourceDimensionIdsDistinct).ToList();
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
		/// Get the Values of an Entity in the specified Version
		/// </summary>
		public DataTable GetEntityVersionValues(int entityId, int changeId, int? defaultCultureDimension)
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
					result.Rows.Add(attribute.Key, firstLanguage, GetTypedValue(valueModel));	// Add Main-Language

					foreach (var valueDimension in valueModel.ValueDimensions.Skip(1))	// Add additional Languages
					{
						result.Rows.Add(attribute.Key, valueDimension.DimensionExternalKey, GetTypedValue(valueModel), firstLanguage + (valueDimension.ReadOnly ? " (read)" : " (write)"));
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Restore an Entity to the specified Version by creating a new Version using Import
		/// </summary>
		public void RestoreEntityVersion(int entityId, int changeId, int? defaultCultureDimension)
		{
			var newVersion = GetEntityVersion(entityId, changeId, defaultCultureDimension);

			var import = new Import.Import(_zoneId, _appId, UserName, true, false);
			import.RunImport(null, new List<Import.Entity> { newVersion });
		}

		#endregion
	}

	#region Ensure Static name of new AttributeSets
	public partial class AttributeSet
	{
		public AttributeSet()
		{
			_StaticName = Guid.NewGuid().ToString();
			_AppID = AppID;
		}
	}
	#endregion
}