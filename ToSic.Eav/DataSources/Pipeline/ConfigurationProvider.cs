﻿using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls.Expressions;
using ToSic.Eav.PropertyAccess;
using ToSic.Eav.Tokens;

namespace ToSic.Eav.DataSources
{
	/// <summary>
	/// Provides Configuration for a configurable DataSource
	/// </summary>
	public class ConfigurationProvider : IConfigurationProvider
	{
		//public string DataSourceType { get; internal set; }
		public Dictionary<string, IPropertyAccess> Sources { get; private set; }
		/// <summary>
		/// List of all Configurations for this DataSource
		/// </summary>
		//public IDictionary<string, string> configList { get; internal set; }
		private readonly TokenReplace _reusableTokenReplace;

		/// <summary>
		/// Constructs a new Configuration Provider
		/// </summary>
		public ConfigurationProvider()
		{
			Sources = new Dictionary<string, IPropertyAccess>();
			_reusableTokenReplace = new TokenReplace(Sources);
		}

        /// <summary>
        /// This will go through a dictionary of strings (usually configuration values) and replace all tokens in that string
        /// with whatever the token-resolver delivers. It's usually needed to initialize a DataSource. 
        /// </summary>
        /// <param name="configList">Dictionary of configuration strings</param>
        /// <param name="instanceSpecificPropertyAccesses">Instance specific additional value-dictionaries</param>
		public void LoadConfiguration(IDictionary<string, string> configList, Dictionary<string, IPropertyAccess> instanceSpecificPropertyAccesses = null)
		{
            #region if there are instance-specific additional Property-Access objects, add them to the sources-list
            // note: it's important to create a one-time use list of sources if instance-specific sources are needed, to never modify the "global" list.
            var useAdditionalPA = (instanceSpecificPropertyAccesses != null); // not null, so it has instance specific stuff
		    if (useAdditionalPA)
		        foreach (var pa in Sources)
		            if (!instanceSpecificPropertyAccesses.ContainsKey(pa.Key))
		                instanceSpecificPropertyAccesses.Add(pa.Key, pa.Value);
		    var instanceTokenReplace = useAdditionalPA ? new TokenReplace(instanceSpecificPropertyAccesses) : _reusableTokenReplace;
            #endregion

            #region Loop through all config-items and token-replace them
            foreach (var o in configList.ToList())
			{
                // check if the string contains a token or not
                if (!instanceTokenReplace.ContainsTokens(o.Value))
					continue;

                var newValue = instanceTokenReplace.ReplaceTokens(o.Value);

				// do recursion 3 times, so if result had another token, replace that too
				for (var i = 0; i < 3; i++)
				{
                    if (instanceTokenReplace.ContainsTokens(newValue))
                        newValue = instanceTokenReplace.ReplaceTokens(newValue);
					else
						break;
				}

				configList[o.Key] = newValue;
            }
            #endregion
        }
	}
}