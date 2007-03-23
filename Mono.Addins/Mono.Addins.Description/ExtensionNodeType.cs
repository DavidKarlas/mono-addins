
using System;
using System.Xml;
using System.Collections;
using System.Collections.Specialized;
using Mono.Addins.Serialization;

namespace Mono.Addins.Description
{
	public class ExtensionNodeType: ExtensionNodeSet
	{
		string typeName;
		string objectTypeName;
		string description;
		string addinId;
		NodeTypeAttributeCollection attributes;
		
		// Cached clr type
		[NonSerialized]
		internal Type Type;
		
		// Cached serializable fields
		[NonSerialized]
		internal Hashtable Fields;
		internal string[] RequiredFields;
		
		// Addin where this extension type is implemented
		internal string AddinId {
			get { return addinId; }
			set { addinId = value; }
		}
		
		// Type of the extension node
		public string TypeName {
			get { return typeName != null ? typeName : string.Empty; }
			set { typeName = value; }
		}
		
		public string NodeName {
			get { return Id; }
			set { Id = value; }
		}
		
		// Type of the object that the extension creates (only valid for TypeNodeExtension).
		public string ObjectTypeName {
			get { return objectTypeName != null ? objectTypeName : string.Empty; }
			set { objectTypeName = value; }
		}
		
		// The description
		public string Description {
			get { return description != null ? description : string.Empty; }
			set { description = value; }
		}
		
		public NodeTypeAttributeCollection Attributes {
			get {
				if (attributes == null) {
					attributes = new NodeTypeAttributeCollection ();
					if (Element != null) {
						XmlElement atts = Element ["Attributes"];
						if (atts != null) {
							foreach (XmlNode node in atts.ChildNodes) {
								XmlElement e = node as XmlElement;
								if (e != null)
									attributes.Add (new NodeTypeAttribute (e));
							}
						}
					}
				}
				return attributes;
			}
		}

		internal ExtensionNodeType (XmlElement element): base (element)
		{
			typeName = element.GetAttribute ("type");
			XmlElement de = element ["Description"];
			if (de != null)
				description = de.InnerText;
		}
		
		internal ExtensionNodeType ()
		{
		}
			
		internal override string IdAttribute {
			get { return "name"; }
		}
		
		internal override void Verify (string location, StringCollection errors)
		{
			base.Verify (location, errors);
		}
		
		internal override void SaveXml (XmlElement parent, string nodeName)
		{
			base.SaveXml (parent, "ExtensionNode");
			
			XmlElement atts = Element ["Attributes"];
			if (Attributes.Count > 0) {
				if (atts == null) {
					atts = parent.OwnerDocument.CreateElement ("Attributes");
					Element.AppendChild (atts);
				}
				Attributes.SaveXml (atts);
			} else {
				if (atts != null)
					Element.RemoveChild (atts);
			}
			
			if (TypeName.Length > 0)
				Element.SetAttribute ("type", TypeName);
			else
				Element.RemoveAttribute ("type");
			
			if (ObjectTypeName.Length > 0)
				Element.SetAttribute ("objectType", ObjectTypeName);
			else
				Element.RemoveAttribute ("objectType");

			SaveXmlDescription (Description);
		}
		
		internal override void Write (BinaryXmlWriter writer)
		{
			base.Write (writer);
			if (Id.Length == 0)
				Id = "Type";
			if (TypeName.Length == 0)
				typeName = "Mono.Addins.TypeExtensionNode";
			writer.WriteValue ("typeName", typeName);
			writer.WriteValue ("objectTypeName", objectTypeName);
			writer.WriteValue ("description", description);
			writer.WriteValue ("addinId", addinId);
			writer.WriteValue ("Attributes", attributes);
		}
		
		internal override void Read (BinaryXmlReader reader)
		{
			base.Read (reader);
			typeName = reader.ReadStringValue ("typeName");
			objectTypeName = reader.ReadStringValue ("objectTypeName");
			description = reader.ReadStringValue ("description");
			addinId = reader.ReadStringValue ("addinId");
			attributes = (NodeTypeAttributeCollection) reader.ReadValue ("Attributes", new NodeTypeAttributeCollection ());
		}
	}
}
