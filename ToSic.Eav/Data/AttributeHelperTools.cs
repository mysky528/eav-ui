using System;
using System.Collections.Generic;

namespace ToSic.Eav.Data
{
    /// <summary>
    /// Note: seems to be a helper class with tools 
    /// todo: probably refactor to fit into some "normal" object 
    /// </summary>
    internal class AttributeHelperTools
    {
        private static readonly Value<EntityRelationship> EntityRelationshipDefaultValue = new Value<EntityRelationship>(new EntityRelationship(null)) { Languages = new Dimension[0] };

        /// <summary>
        /// Convert a NameValueCollection-Like List to a Dictionary of IAttributes
        /// </summary>
        internal static Dictionary<string, IAttribute> GetTypedDictionaryForSingleLanguage(IDictionary<string, object> attributes, string titleAttributeName)
        {
            var result = new Dictionary<string, IAttribute>(StringComparer.OrdinalIgnoreCase);

            foreach (var attribute in attributes)
            {
                var attributeType = GetAttributeTypeName(attribute.Value);
                var baseModel = new AttributeBase(attribute.Key, attributeType, attribute.Key == titleAttributeName);
                var attributeModel = GetAttributeManagementModel(baseModel);
                var valuesModelList = new List<IValue>();
                if (attribute.Value != null)
                {
                    var valueModel = Value.GetValueModel(baseModel.Type, attribute.Value.ToString());
                    valuesModelList.Add(valueModel);
                }

                attributeModel.Values = valuesModelList;

                result[attribute.Key] = attributeModel;
            }

            return result;
        }

        /// <summary>
        /// Get EAV AttributeType for a value, like String, Number, DateTime or Boolean
        /// </summary>
        static string GetAttributeTypeName(object value)
        {
            if (value is DateTime)
                return "DateTime";
            if (value is decimal || value is int || value is double)
                return "Number";
            if (value is bool)
                return "Boolean";
            return "String";
        }

        /// <summary>
        /// Get Attribute for specified Typ
        /// </summary>
		/// <returns><see cref="Attribute{ValueType}"/></returns>
        internal static IAttributeManagement GetAttributeManagementModel(AttributeBase definition)
        {
            switch (definition.Type)
            {
                case "Boolean":
                    return new Attribute<bool?>(definition.Name, definition.Type, definition.IsTitle);
                case "DateTime":
                    return new Attribute<DateTime?>(definition.Name, definition.Type, definition.IsTitle);
                case "Number":
                    return new Attribute<decimal?>(definition.Name, definition.Type, definition.IsTitle);
                case "Entity":
                    return new Attribute<EntityRelationship>(definition.Name, definition.Type, definition.IsTitle) { Values = new IValue[] { EntityRelationshipDefaultValue } };
                default:
                    return new Attribute<string>(definition.Name, definition.Type, definition.IsTitle);
            }
        }
    }
}