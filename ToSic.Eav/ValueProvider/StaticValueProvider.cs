﻿using System.Collections.Generic;

namespace ToSic.Eav.ValueProvider
{
	/// <summary>
	/// Property Accessor to test a Pipeline with Static Values
	/// </summary>
	public class StaticValueProvider : BaseValueProvider// IValueProvider
	{
		/// <summary>
		/// List with static properties and Test-Values
		/// </summary>
		public Dictionary<string, string> Properties { get; private set; }

		/// <summary>
		/// The class constructor
		/// </summary>
		public StaticValueProvider(string name)
		{
			Properties = new Dictionary<string, string>();
			Name = name;
		}

		//public string Name { get; private set; }

		public override string Get(string property, string format, ref bool propertyNotFound)
		{
			try
			{
				return Properties[property];
			}
			catch (KeyNotFoundException)
			{
				propertyNotFound = true;
				return null;
			}
		}

        public override bool Has(string property)
        {
            return Properties.ContainsKey(property);
        }
	}
}