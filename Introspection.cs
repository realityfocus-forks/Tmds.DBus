// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Text;
using System.Reflection;

namespace NDesk.DBus
{
	//TODO: complete this class
	public class Introspector
	{
		const string NAMESPACE = "http://www.freedesktop.org/standards/dbus";
		const string PUBLIC_IDENTIFIER = "-//freedesktop//DTD D-BUS Object Introspection 1.0//EN";
		const string SYSTEM_IDENTIFIER = "http://www.freedesktop.org/standards/dbus/1.0/introspect.dtd";

		public StringBuilder sb;
		public string xml;
		public Type target_type;

		protected XmlWriter writer;

		public Introspector ()
		{
			XmlWriterSettings settings = new XmlWriterSettings ();
			settings.Indent = true;
			settings.IndentChars = ("  ");
			settings.OmitXmlDeclaration = true;

			sb = new StringBuilder ();

			writer = XmlWriter.Create (sb, settings);
		}

		public void HandleIntrospect ()
		{
			writer.WriteDocType ("node", PUBLIC_IDENTIFIER, SYSTEM_IDENTIFIER, null);

			//TODO: write version info in a comment, when we get an AssemblyInfo.cs

			writer.WriteComment (" Never rely on XML introspection data for dynamic binding. It is provided only for convenience and is subject to change at any time. ");

			//TODO: non-well-known introspection has paths as well, which we don't do yet. read the spec again

			writer.WriteStartElement ("node");

			//reflect our own interface manually
			//note that the name of the return value this way is 'ret' instead of 'data'
			WriteInterface (typeof (org.freedesktop.DBus.Introspectable));

			//reflect the target interface
			WriteInterface (target_type);

			//TODO: recurse interfaces, inheritance hierarchy?

			writer.WriteEndElement ();

			writer.Flush ();
			xml = sb.ToString ();
		}

		public void WriteArg (ParameterInfo pi)
		{
			Type piType = pi.IsOut ? pi.ParameterType.GetElementType () : pi.ParameterType;
			if (piType == typeof (void))
				return;

			writer.WriteStartElement ("arg");

			string piName;
			if (pi.IsRetval && String.IsNullOrEmpty (pi.Name))
				piName = "ret";
			else
				piName = String.IsNullOrEmpty (pi.Name) ? "arg" + pi.Position : pi.Name;

			writer.WriteAttributeString ("name", piName);

			//consider using some kind of inverse logic for event handler parameters
			//falling back to the default (no direction attr) will do for now though

			if (pi.IsOut || pi.IsRetval)
				writer.WriteAttributeString ("direction", "out");
			//else
			//	writer.WriteAttributeString ("direction", "in");

			writer.WriteAttributeString ("type", Signature.GetSig (piType).Value);

			writer.WriteEndElement ();
		}

		public void WriteMethod (MethodInfo mi)
		{
			writer.WriteStartElement ("method");
			writer.WriteAttributeString ("name", mi.Name);

			foreach (ParameterInfo pi in mi.GetParameters ())
				WriteArg (pi);

			WriteArg (mi.ReturnParameter);

			writer.WriteEndElement ();
		}

		public void WriteProperty (PropertyInfo pri)
		{
			//TODO: complete properties, possibly using its MethodInfos

			writer.WriteStartElement ("method");
			writer.WriteAttributeString ("name", "Get" + pri.Name);
			writer.WriteEndElement ();

			writer.WriteStartElement ("method");
			writer.WriteAttributeString ("name", "Set" + pri.Name);
			writer.WriteEndElement ();
		}

		public void WriteSignal (EventInfo ei)
		{
			writer.WriteStartElement ("signal");
			writer.WriteAttributeString ("name", ei.Name);

			foreach (ParameterInfo pi in ei.EventHandlerType.GetMethod ("Invoke").GetParameters ())
				WriteArg (pi);

			//no need to consider the delegate return value as dbus doesn't support it
			writer.WriteEndElement ();
		}

		public void WriteInterface (Type type)
		{
			//TODO: consider declaredonly, public
			//TODO: consider member order, maybe using GetMembers ()

			writer.WriteStartElement ("interface");

			//TODO: better fallback interface name when there's no attribute
			string interfaceName = type.Name;

			//TODO: no need for foreach
			foreach (InterfaceAttribute ia in type.GetCustomAttributes (typeof (InterfaceAttribute), false))
				interfaceName = ia.Name;

			writer.WriteAttributeString ("name", interfaceName);

			foreach (MethodInfo mi in type.GetMethods ())
				if (!mi.IsSpecialName)
					WriteMethod (mi);

			foreach (EventInfo ei in type.GetEvents ())
				WriteSignal (ei);

			foreach (PropertyInfo pri in type.GetProperties ())
				WriteProperty (pri);

			//TODO: indexers

			writer.WriteEndElement ();
		}
	}
}
