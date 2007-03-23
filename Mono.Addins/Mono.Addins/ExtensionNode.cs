
using System;
using System.Collections;
using System.Xml;
using System.Reflection;
using Mono.Addins.Description;

namespace Mono.Addins
{
	public class ExtensionNode
	{
		bool childrenLoaded;
		TreeNode treeNode;
		ExtensionNodeList childNodes;
		RuntimeAddin addin;
		string addinId;
		ExtensionNodeType nodeType;
		event ExtensionNodeEventHandler extensionNodeChanged;
		
		public string Id {
			get { return treeNode.Id; }
		}
		
		public string Path {
			get { return treeNode.GetPath (); }
		}
		
		public bool HasId {
			get { return !Id.StartsWith (ExtensionTree.AutoIdPrefix); }
		}
		
		internal void SetTreeNode (TreeNode node)
		{
			treeNode = node;
		}
		
		internal void SetData (string plugid, ExtensionNodeType nodeType)
		{
			this.addinId = plugid;
			this.nodeType = nodeType;
		}
		
		internal string AddinId {
			get { return addinId; }
		}
		
		internal TreeNode TreeNode {
			get { return treeNode; }
		}
		
		public RuntimeAddin Addin {
			get {
				if (addinId == null)
					return null;
				if (addin == null) {
					if (!AddinManager.SessionService.IsAddinLoaded (addinId))
						AddinManager.SessionService.LoadAddin (null, addinId, true);
					addin = AddinManager.SessionService.GetAddin (addinId);
				}
				return addin; 
			}
		}
		
		public event ExtensionNodeEventHandler ExtensionNodeChanged {
			add {
				extensionNodeChanged += value;
				foreach (ExtensionNode node in ChildNodes)
					extensionNodeChanged (this, new ExtensionNodeEventArgs (ExtensionChange.Add, node));
			}
			remove {
				extensionNodeChanged -= value;
			}
		}
		
		public ExtensionNodeList ChildNodes {
			get {
				if (childrenLoaded)
					return childNodes;
				
				childrenLoaded = true;
				
				try {
					if (treeNode.Children.Count == 0) {
						childNodes = ExtensionNodeList.Empty;
						return childNodes;
					}
				}
				catch (Exception ex) {
					AddinManager.ReportError (null, null, ex, false);
					childNodes = ExtensionNodeList.Empty;
					return childNodes;
				}

				ArrayList list = new ArrayList ();
				foreach (TreeNode cn in treeNode.Children) {
					
					// For each node check if it is visible for the current context.
					// If something fails while evaluating the condition, just ignore the node.
					
					try {
						if (cn.ExtensionNode != null && cn.IsEnabled)
							list.Add (cn.ExtensionNode);
					} catch (Exception ex) {
						AddinManager.ReportError (null, null, ex, false);
					}
				}
				if (list.Count > 0)
					childNodes = new ExtensionNodeList (list);
				else
					childNodes = ExtensionNodeList.Empty;
			
				return childNodes;
			}
		}
		
		public object[] GetChildObjects ()
		{
			return GetChildObjects (typeof(object), true);
		}
		
		public object[] GetChildObjects (bool reuseCachedInstance)
		{
			return GetChildObjects (typeof(object), reuseCachedInstance);
		}
		
		public object[] GetChildObjects (Type arrayElementType)
		{
			return GetChildObjects (arrayElementType, true);
		}
		
		public object[] GetChildObjects (Type arrayElementType, bool reuseCachedInstance)
		{
			ArrayList list = new ArrayList (ChildNodes.Count);
			
			for (int n=0; n<ChildNodes.Count; n++) {
				TypeExtensionNode node = ChildNodes [n] as TypeExtensionNode;
				if (node == null) {
					AddinManager.ReportError ("Error while getting object for node in path '" + Path + "'. Extension node is not a subclass of TypeExtensionNode.", null, null, false);
					continue;
				}
				
				try {
					if (reuseCachedInstance)
						list.Add (node.GetInstance (arrayElementType));
					else
						list.Add (node.CreateInstance (arrayElementType));
				}
				catch (Exception ex) {
					AddinManager.ReportError ("Error while getting object for node in path '" + Path + "'.", null, ex, false);
				}
			}
			return (object[]) list.ToArray (arrayElementType);
		}
		
		internal protected virtual void Read (NodeElement elem)
		{
			if (nodeType == null || nodeType.Fields == null)
				return;

			NodeAttribute[] attributes = elem.Attributes;
			string[] required = nodeType.RequiredFields != null ? (string[]) nodeType.RequiredFields.Clone () : null;
			int nreq = required != null ? required.Length : 0;
			
			foreach (NodeAttribute at in attributes) {
				
				FieldInfo f = (FieldInfo) nodeType.Fields [at.name];
				if (f == null)
					continue;
					
				if (required != null) {
					int i = Array.IndexOf (required, at.name);
					if (i != -1) {
						required [i] = null;
						nreq--;
					}
				}
					
				object val;

				if (f.FieldType == typeof(string)) {
					val = at.value;
				}
				else if (f.FieldType == typeof(string[])) {
					string[] ss = at.value.Split (',');
					if (ss.Length == 0 && ss[0].Length == 0)
						val = new string [0];
					else {
						for (int n=0; n<ss.Length; n++)
							ss [n] = ss[n].Trim ();
						val = ss;
					}
				}
				else if (f.FieldType.IsEnum) {
					val = Enum.Parse (f.FieldType, at.value);
				}
				else {
					try {
						val = Convert.ChangeType (at.Value, f.FieldType);
					} catch (InvalidCastException) {
						throw new InvalidOperationException ("Property type not supported by [NodeAttribute]: " + f.DeclaringType + "." + f.Name);
					}
				}
					
				f.SetValue (this, val);
			}
			if (nreq > 0) {
				foreach (string s in required)
					if (s != null)
						throw new InvalidOperationException ("Required attribute '" + s + "' not found.");
			}
		}
		
		internal bool NotifyChildChanged ()
		{
			if (!childrenLoaded)
				return false;

			ExtensionNodeList oldList = childNodes;
			childrenLoaded = false;
			
			bool changed = false;
			
			foreach (ExtensionNode nod in oldList) {
				if (ChildNodes [nod.Id] == null) {
					changed = true;
					OnChildNodeRemoved (nod);
				}
			}
			foreach (ExtensionNode nod in ChildNodes) {
				if (oldList [nod.Id] == null) {
					changed = true;
					OnChildNodeAdded (nod);
				}
			}
			if (changed)
				OnChildrenChanged ();
			return changed;
		}
		
		// Called when the add-in that defined this extension node is actually
		// loaded in memory.
		internal protected virtual void OnAddinLoaded ()
		{
		}
		
		// Called when the add-in that defined this extension node is being
		// unloaded from memory.
		internal protected virtual void OnAddinUnloaded ()
		{
		}
		
		// Called when the children list of this node has changed. It may be due to add-ins
		// being loaded/unloaded, or to conditions being changed.
		protected virtual void OnChildrenChanged ()
		{
		}
		
		protected virtual void OnChildNodeAdded (ExtensionNode node)
		{
			if (extensionNodeChanged != null)
				extensionNodeChanged (this, new ExtensionNodeEventArgs (ExtensionChange.Add, node));
		}
		
		protected virtual void OnChildNodeRemoved (ExtensionNode node)
		{
			if (extensionNodeChanged != null)
				extensionNodeChanged (this, new ExtensionNodeEventArgs (ExtensionChange.Remove, node));
		}
	}
}
