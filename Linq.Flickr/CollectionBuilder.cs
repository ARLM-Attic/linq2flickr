using System;
using System.Collections.Generic;
using System.Xml;
using System.Reflection;
using Linq.Flickr.Attribute;
using Linq.Flickr.Repository;

namespace Linq.Flickr
{
    /// <summary>
    /// Used for creating IEnumerable<typeparamref name="T"/> result from REST response.
    /// </summary>
    /// <typeparam name="T">IDisposable</typeparam>
    public class CollectionBuilder<T> : HttpCallBase where T : IDisposable 
    {
        private readonly T _object;
        private readonly string _rootElement = string.Empty;
        readonly IDictionary<string, string> _propertyMap = new Dictionary<string, string>();

        public CollectionBuilder()
        {
            _object = Activator.CreateInstance<T>();
        }
        /// <summary>
        /// takes a root element , used if any attribute values need to be copied into all decendant nodes.
        /// </summary>
        /// <param name="rootElement"></param>
        public CollectionBuilder(string rootElement)
        {
            _object = Activator.CreateInstance<T>();
            _rootElement = rootElement;
        }

        public void FillProperty(T obj, string name, object value)
        {
            if (_propertyMap.ContainsKey(name))
            {
                ProcessMember(name, obj, value);
            }
        }

        private void ProcessMember(string name, object obj, object value)
        {
            Type info = obj.GetType();

            object[] xmlElements = info.GetCustomAttributes(typeof(XElementAttribute), true);
            object[] xmlAttributes = info.GetCustomAttributes(typeof(XAttributeAttribute), true);

            if (xmlElements.Length == 1 || xmlAttributes.Length == 1)
            {
                PropertyInfo pInfo = info.GetProperty(_propertyMap[name],
                                                      BindingFlags.NonPublic | BindingFlags.Instance |
                                                      BindingFlags.Public);

                if (pInfo.CanWrite)
                {
                    pInfo.SetValue(obj, GetValue(pInfo.PropertyType, value), null);
                }
            }

        }

        private object GetValue(Type type, object value)
        {
            string sValue = (string)value;
            object retValue = value;

            switch (type.FullName)
            {
                case "System.Boolean":
                    retValue = string.IsNullOrEmpty(sValue) ? false : ((sValue == "0" || sValue == "false") ? false : true);
                    break;
                case "System.String":
                    retValue = Convert.ToString(value);
                    break;
                case "System.Int32":
                    retValue = Convert.ToInt32(value);
                    break;
                case "System.DateTime":
                    retValue = Convert.ToDateTime(value);
                    break;
            }
            return retValue;
        }

        public delegate void ItemChangeHandler (T item);
      
        public IEnumerable<T> ToCollection(XmlElement element, ItemChangeHandler OnItemChange)
        {
            Type objectInfo = _object.GetType();

            CreatePropertyMap(objectInfo);

            IList<T> list = new List<T>();

            IList<XmlElement> elements = element.Descendants(GetRootElement(objectInfo));

            foreach (XmlElement e in elements)
            {
                T obj = Activator.CreateInstance<T>();
                // process any attribute from root element.
                if (!string.IsNullOrEmpty(_rootElement))
                {
                    XmlElement root = e.FindElement(_rootElement);
                    ProcessAttribute(obj, root);
                }
              
                ProcessNode(obj, e);
                
                // raise event so that any change might take place.
                if (OnItemChange != null)
                {
                    OnItemChange(obj);
                }
                // finally add to the list.
                list.Add(obj);
            }
            return list;
        }

        private void ProcessAttribute(T obj, XmlNode element)
        {
            if (element != null)
            {
                foreach (XmlAttribute attribute in element.Attributes)
                {
                    FillProperty(obj, attribute.LocalName, (attribute.Value ?? string.Empty));
                }
            }
        }

        private void ProcessNode(T obj, XmlElement rootElement)
        {

            if (rootElement.HasChildNodes)
            {
                // set the elements
                foreach (XmlElement item in rootElement.Descendants())
                {
                    FillProperty(obj, item.LocalName, (item.InnerXml ?? string.Empty));
                }
            }
            else
            {
                // single element.
                if (!string.IsNullOrEmpty(rootElement.InnerXml))
                {
                    FillProperty(obj, rootElement.LocalName, (rootElement.InnerXml ?? string.Empty));
                }
            }

            ProcessAttribute(obj, rootElement);
        }

        public IEnumerable<T> ToCollection(string requestUrl, ItemChangeHandler OnItemChange)
        {
            XmlElement element = base.GetElement(requestUrl);
            return ToCollection(element, OnItemChange);
        }

        private void CreatePropertyMap(Type objectInfo)
        {
            PropertyInfo[] infos = objectInfo.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

            foreach (PropertyInfo info in infos)
            {
                object[] attr = info.GetCustomAttributes(typeof(XNameAttribute), true);

                if (attr != null && attr.Length == 1)
                {
                    _propertyMap.Add((attr[0] as XNameAttribute).Name, info.Name);
                }
            }
        }

        private string GetRootElement(Type objectInfo)
        {
            string elementName = objectInfo.Name;
            object[] customAttr = objectInfo.GetCustomAttributes(typeof(XElementAttribute), true);

            if (customAttr != null && customAttr.Length == 1)
            {
                elementName = (customAttr[0] as XElementAttribute).Name;
            }
            return elementName;
        }
    }
}