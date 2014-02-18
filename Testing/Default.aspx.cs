﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using ToSic.Eav.DataSources;


namespace ToSic.Eav
{
	public partial class Default : Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			Configuration.SetConnectionString("SiteSqlServer");

			//Tests2dm();

			//Chain5();
			//InitialDataSource();
			//var typeFiltered = EntityTypeFilter();
			//AttributeFilter(typeFiltered);
			//Chain6();
			//EntityIdFilter();
			//DataPipelineFactory();

			//EntityTypeFilter("Type 11:26");
			AddEntity();
			EntityTypeFilter("Person ML");

			//var source = DataSource.GetInitialDataSource(1, 1);
			//var entities = source.Out["Default"].List;
			//ShowEntity(entities[3378]);
		}

		public void AddEntity()
		{
			var context = EavContext.Instance(1, 1);
			var userName = "Testing 2bg 17:53";
			context.UserName = userName;
			var newValues = new Dictionary<string, ValueViewModel>
				{
					{"FirstName", new ValueViewModel {Value = "Benjamin 17:51"}},
					{"LastName", new ValueViewModel {Value = "Gemperle 17:51"}},
					{"Address", new ValueViewModel {Value = "Churerstrasse 35 17:51"}},
					{"ZIP", new ValueViewModel {Value = "9470 17:51"}},
					{"City", new ValueViewModel {Value = "Buchs 17:51"}}
				};

			context.AddEntity(37, newValues, null, null);
		}


		private void DataPipelineFactory()
		{
			Trace.Write("DataPipelineFactory", "Before Init");
			var source = DataSources.DataPipelineFactory.GetDataSource(1, 1, 347, null);
			Trace.Write("DataPipelineFactory", "End Init");

			ShowDataSource(source, "DataPipelineFactory", true);
		}

		//private EntityIdFilter EntityIdFilter()
		//{
		//	var source = DataSource.GetInitialDataSource("SiteSqlServer", 2, 2);
		//	//var filterPipeline = (EntityIdFilter)DataSource.GetDataSource("ToSic.Eav.DataSources.EntityIdFilter", source);

		//	var dataSourceId = 1;
		//	var pipelineId = 1;

		//	var configList = new Dictionary<string, object> { { "EntityIds", "[Settings:EntityIds]" } };
		//	var filterPipeline = new EntityIdFilter(dataSourceId, pipelineId, source);

		//	//filterPipeline.Configuration["EntityIds"] = "329, 330";
		//	//filterPipeline.Configuration["EntityIds"] = Request.QueryString["entities"];

		//	var settingsPropertyProvider = new SimplePropertyProvider();
		//	settingsPropertyProvider.Values.Add("EntityIds", new[] { 329, 330 });
		//	filterPipeline.ConfigurationProvider.Sources.Add("Settings", settingsPropertyProvider);

		//	ShowDataSource(filterPipeline, "EntityTypeFilter", true);

		//	return filterPipeline;
		//}

		//private DataSource DemoFactory()
		//{
		//	var DataSources = new[] { "230203, EntityTypeFilter", "230244, ICache", "306010, EntityTypeFilter" };
		//	var Connections = new[]
		//		{
		//			"230244:Default>230203:Default",
		//			"230244:Default>306010:Default"
		//		};
		//	var x = new ConfigurationProvider();


		//}

		//private void Chain6()
		//{
		//	string[] chain = { "ToSic.Eav.DataSources.Testing.TestStoreWithManyEntities", "ToSic.Eav.DataSources.Caches.ICache" };
		//	var source = DataSource.AssembleDataSource(chain);
		//	((TestStoreWithManyEntities)source).TotalItems = 100000;
		//	ShowDataSource(source, "Chain6");
		//}

		//private void Tests2dm()
		//{
		//	Response.Write("<br>" + DateTime.Now.ToString() + "<br>");
		//	var emptySource = new Empty();
		//	Response.Write(emptySource.Ready + "<br>");

		//	// Assemble chain
		//	string[] chain1 =
		//		{
		//			"ToSic.Eav.DataSources.Empty", "ToSic.Eav.DataSources.PassThrough", "ToSic.Eav.DataSources.PassThrough"
		//		};

		//	var source = DataSource.AssembleDataSource(chain1);
		//	//Response.Write("<br><br>length:" + source.DistanceFromSource);
		//	Response.Write("<br>count:" + source.Out["Default"].List.Count + " ; ready: " + source.Ready);

		//	// Assemble chain with type filtering
		//	string[] chain2 =
		//		{
		//			"ToSic.Eav.DataSources.Testing.TestStoreWithManyEntities","ToSic.Eav.DataSources.PassThrough","ToSic.Eav.DataSources.EntityTypeFilter"
		//		};
		//	source = DataSource.AssembleDataSource(chain2);
		//	var filterSource = (EntityTypeFilter)source;
		//	//filterSource.TypeName = "Demo";
		//	Response.Write("<h1>type filter</h1>");
		//	//Response.Write("length:" + source.DistanceFromSource);
		//	Response.Write("<br>count:" + source.Out["Default"].List.Count + " ; ready: " + source.Ready);


		//	// Assemble chain with dependency
		//	string[] chain3 =
		//		{
		//			"ToSic.Eav.DataSources.Testing.TestStoreWithManyEntities",
		//			"ToSic.Eav.DataSources.PassThrough",
		//			"ToSic.Eav.DataSources.PassThrough",
		//			"ToSic.Eav.DataSources.FilterAndSort"
		//		};
		//	source = DataSource.AssembleDataSource(chain3);
		//	ShowDataSource(source, "Dependency");

		//	// Assemble chain with 10'000 entities
		//	string[] chain4 =
		//		{
		//			"ToSic.Eav.DataSources.Testing.TestStoreWithManyEntities",
		//			"ToSic.Eav.DataSources.Caches.ICache", 
		//	"ToSic.Eav.DataSources.PassThrough",
		//			"ToSic.Eav.DataSources.PassThrough"
		//		};
		//	source = DataSource.AssembleDataSource(chain4);
		//	//var generator = ((IDataPipeline)source).UpstreamSource.UpstreamSource.UpstreamSource;
		//	//generator.TotalItems = 1000000;
		//	//ShowDataSource(source, "1'000'000 items, no cache");


		//	//// Response.Write("<h2>with a multiple ID filter</h2>");
		//	//var source3 = DataSource.GetDataSource("ToSic.Eav.DataSources.FilterAndSort", source);
		//	////((IDataSourceInternals)source3).UpstreamSource = source;
		//	//var filterAndSort = (FilterAndSort)source3;
		//	//filterAndSort.EntityIdFilterUrlParameterName = "ID2";
		//	//ShowDataSource(source3, "1'000'000 items, multipleId-filter");


		//	//Response.Write("<h2>with a type filter</h2>");
		//	var source2 = DataSource.GetDataSource("ToSic.Eav.DataSources.EntityTypeFilter", source);
		//	//((IDataSourceInternals)source2).UpstreamSource = source;
		//	filterSource = (EntityTypeFilter)source2;
		//	//filterSource.TypeName = "Kitchenware";
		//	ShowDataSource(source2, "With Type Filter");
		//	//Trace.Write("Filtering type", "Start");
		//	//Response.Write("<br>count:" + source2.Entities.Count + "; Length:" + source2.DistanceFromSource + " ; ready: " + source2.Ready);
		//	//Trace.Write("Filtering type", "Done");


		//	//Response.Write("<h2>with a extensions assignments filter</h2>");
		//	var source4 = DataSource.GetDataSource("ToSic.Eav.DataSources.Testing.TestStoreWithManyExtensions");

		//	//((IDataSourceInternals)source4).UpstreamSource = source.UpstreamSource.UpstreamSource.UpstreamSource;
		//	//((IDataSourceInternals)source.UpstreamSource.UpstreamSource).UpstreamSource = source4;
		//	//ShowDataSource(source, "Extension Assignments, Unused");

		//	// doesn't work yet...
		//	// Response.Write(((IMetaDataSource)source).IndexForExternalInt[17, 20].ToString());
		//}

		private void InitialDataSource()
		{
			var source = DataSource.GetInitialDataSource();

			ShowDataSource(source, "Initial DataSource");
		}

		private EntityTypeFilter EntityTypeFilter(string typeName)
		{
			var source = DataSource.GetInitialDataSource();

			var filterPipeline = (EntityTypeFilter)DataSource.GetDataSource("ToSic.Eav.DataSources.EntityTypeFilter", 1, 1, source);
			filterPipeline.Configuration["TypeName"] = typeName;
			ShowDataSource(filterPipeline, "EntityTypeFilter", true);

			return filterPipeline;
		}

		private void AttributeFilter(DataSources.IDataSource source)
		{
			var filterPipeline = (AttributeFilter)DataSource.GetDataSource("ToSic.Eav.DataSources.AttributeFilter", 1, 1, source);
			filterPipeline.Configuration["AttributeNames"] = "LastName,FirstName";
			ShowDataSource(filterPipeline, "AttributeFilter", true);
		}

		//private void Chain5()
		//{
		//	string[] chain = { "ToSic.Eav.DataSources.Caches.DNNFarmCache, ToSic.Eav.Professional", "ToSic.Eav.DataSources.SqlSources.EavSqlStore" };
		//	var source = DataSource.AssembleDataSource(chain);

		//	var eavSqlStore = source;
		//	while (!(eavSqlStore is EavSqlStore))
		//		eavSqlStore = ((IDataTarget)eavSqlStore).UpstreamSource;
		//	((EavSqlStore)eavSqlStore).Init("SiteSqlServer");

		//	ShowDataSource(source, "EavSqlStore");
		//}


		public void ShowDataSource(DataSources.IDataSource source, string title, bool fullEntities = false)
		{
			Response.Write("<h2>" + title + " (Name: " + source.Name + ")</h2>");
			Trace.Write("Filtering" + title, "Start");
			//Response.Write("Ready: " + source.Ready);//; Chain: " + source.NameChain + "; Length: " + source.DistanceFromSource);
			//Response.Write("<br>count:" + source.Out["Default"].List.Count);

			foreach (var dataStream in source.Out)
			{
				Response.Write("<h3>" + dataStream.Key + " Count:" + dataStream.Value.List.Count + "</h3>");

				if (fullEntities)
				{
					Response.Write("<h4>Entities Details</h4><hr/>");
					foreach (var entity in dataStream.Value.List.Select(e => e.Value))
						ShowEntity(entity);
				}

			}

			Trace.Write("Filtering" + title, "Done");
		}

		public void ShowEntity(IEntity entity)
		{
			Response.Write(entity.EntityId + "<br/>");
			foreach (var attribute in entity.Attributes)
			{
				Response.Write(attribute.Key + ": " + attribute.Value[0] + "<br/>");

				var relationship = attribute.Value as AttributeModel<EntityRelationshipModel>;
				if (relationship != null)
				{
					Response.Write("Entities count: " + relationship.TypedContents.Count() + "<br/>");
					Response.Write("Entity Titles: " + string.Join(", ", relationship.TypedContents.Select(e => e.Title[0])) + "<br/>");
				}
			}

			Response.Write("Children[\"People\"]: " + entity.Relationships.Children["People"].Count() + "<br/>");
			Response.Write("AllChildren: " + entity.Relationships.AllChildren.Count() + "<br/>");
			Response.Write("AllParents: " + entity.Relationships.AllParents.Count() + "<br/>");

			Response.Write("<hr/>");
		}
	}
}