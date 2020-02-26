// ------------------------------------------------------------------------------
//  <auto-generated>
//    Generated by Xsd2Code++. Version 5.0.0.47. www.xsd2code.com
//  </auto-generated>
// ------------------------------------------------------------------------------
#pragma warning disable
namespace Hybrasyl.Xml
{
using System;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Collections;
using System.Xml.Schema;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Xml;
using System.Collections.Generic;

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.3752.0")]
[Serializable]
[DebuggerStepThrough]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[XmlTypeAttribute(Namespace="http://www.hybrasyl.com/XML/Hybrasyl/2020-02")]
public partial class Reactor
{
    #region Private fields
    private string _description;
    private string _script;
    private byte _x;
    private byte _y;
    private bool _blocking;
    private bool _allowDead;
    private static XmlSerializer serializer;
    #endregion
    
    public Reactor()
    {
        _blocking = false;
        _allowDead = false;
    }
    
    [StringLengthAttribute(65534, MinimumLength=1)]
    public string Description
    {
        get
        {
            return _description;
        }
        set
        {
            ValidationContext validatorPropContext = new ValidationContext(this, null, null);
            validatorPropContext.MemberName = "Description";
            Validator.ValidateProperty(value, validatorPropContext);
            _description = value;
        }
    }
    
    [StringLengthAttribute(255, MinimumLength=1)]
    public string Script
    {
        get
        {
            return _script;
        }
        set
        {
            ValidationContext validatorPropContext = new ValidationContext(this, null, null);
            validatorPropContext.MemberName = "Script";
            Validator.ValidateProperty(value, validatorPropContext);
            _script = value;
        }
    }
    
    [XmlAttribute]
    public byte X
    {
        get
        {
            return _x;
        }
        set
        {
            _x = value;
        }
    }
    
    [XmlAttribute]
    public byte Y
    {
        get
        {
            return _y;
        }
        set
        {
            _y = value;
        }
    }
    
    [XmlAttribute]
    [DefaultValue(false)]
    public bool Blocking
    {
        get
        {
            return _blocking;
        }
        set
        {
            _blocking = value;
        }
    }
    
    [XmlAttribute]
    [DefaultValue(false)]
    public bool AllowDead
    {
        get
        {
            return _allowDead;
        }
        set
        {
            _allowDead = value;
        }
    }
    
    private static XmlSerializer Serializer
    {
        get
        {
            if ((serializer == null))
            {
                serializer = new XmlSerializerFactory().CreateSerializer(typeof(Reactor));
            }
            return serializer;
        }
    }
    
    #region Serialize/Deserialize
    /// <summary>
    /// Serializes current Reactor object into an XML string
    /// </summary>
    /// <returns>string XML value</returns>
    public virtual string Serialize()
    {
        StreamReader streamReader = null;
        MemoryStream memoryStream = null;
        try
        {
            memoryStream = new MemoryStream();
            System.Xml.XmlWriterSettings xmlWriterSettings = new System.Xml.XmlWriterSettings();
            xmlWriterSettings.Indent = true;
            xmlWriterSettings.IndentChars = "  ";
            System.Xml.XmlWriter xmlWriter = XmlWriter.Create(memoryStream, xmlWriterSettings);
            Serializer.Serialize(xmlWriter, this);
            memoryStream.Seek(0, SeekOrigin.Begin);
            streamReader = new StreamReader(memoryStream);
            return streamReader.ReadToEnd();
        }
        finally
        {
            if ((streamReader != null))
            {
                streamReader.Dispose();
            }
            if ((memoryStream != null))
            {
                memoryStream.Dispose();
            }
        }
    }
    
    /// <summary>
    /// Deserializes workflow markup into an Reactor object
    /// </summary>
    /// <param name="input">string workflow markup to deserialize</param>
    /// <param name="obj">Output Reactor object</param>
    /// <param name="exception">output Exception value if deserialize failed</param>
    /// <returns>true if this Serializer can deserialize the object; otherwise, false</returns>
    public static bool Deserialize(string input, out Reactor obj, out Exception exception)
    {
        exception = null;
        obj = default(Reactor);
        try
        {
            obj = Deserialize(input);
            return true;
        }
        catch (Exception ex)
        {
            exception = ex;
            return false;
        }
    }
    
    public static bool Deserialize(string input, out Reactor obj)
    {
        Exception exception = null;
        return Deserialize(input, out obj, out exception);
    }
    
    public static Reactor Deserialize(string input)
    {
        StringReader stringReader = null;
        try
        {
            stringReader = new StringReader(input);
            return ((Reactor)(Serializer.Deserialize(XmlReader.Create(stringReader))));
        }
        finally
        {
            if ((stringReader != null))
            {
                stringReader.Dispose();
            }
        }
    }
    
    public static Reactor Deserialize(Stream s)
    {
        return ((Reactor)(Serializer.Deserialize(s)));
    }
    #endregion
    
    /// <summary>
    /// Serializes current Reactor object into file
    /// </summary>
    /// <param name="fileName">full path of outupt xml file</param>
    /// <param name="exception">output Exception value if failed</param>
    /// <returns>true if can serialize and save into file; otherwise, false</returns>
    public virtual bool SaveToFile(string fileName, out Exception exception)
    {
        exception = null;
        try
        {
            SaveToFile(fileName);
            return true;
        }
        catch (Exception e)
        {
            exception = e;
            return false;
        }
    }
    
    public virtual void SaveToFile(string fileName)
    {
        StreamWriter streamWriter = null;
        try
        {
            string xmlString = Serialize();
            FileInfo xmlFile = new FileInfo(fileName);
            streamWriter = xmlFile.CreateText();
            streamWriter.WriteLine(xmlString);
            streamWriter.Close();
        }
        finally
        {
            if ((streamWriter != null))
            {
                streamWriter.Dispose();
            }
        }
    }
    
    /// <summary>
    /// Deserializes xml markup from file into an Reactor object
    /// </summary>
    /// <param name="fileName">string xml file to load and deserialize</param>
    /// <param name="obj">Output Reactor object</param>
    /// <param name="exception">output Exception value if deserialize failed</param>
    /// <returns>true if this Serializer can deserialize the object; otherwise, false</returns>
    public static bool LoadFromFile(string fileName, out Reactor obj, out Exception exception)
    {
        exception = null;
        obj = default(Reactor);
        try
        {
            obj = LoadFromFile(fileName);
            return true;
        }
        catch (Exception ex)
        {
            exception = ex;
            return false;
        }
    }
    
    public static bool LoadFromFile(string fileName, out Reactor obj)
    {
        Exception exception = null;
        return LoadFromFile(fileName, out obj, out exception);
    }
    
    public static Reactor LoadFromFile(string fileName)
    {
        FileStream file = null;
        StreamReader sr = null;
        try
        {
            file = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            sr = new StreamReader(file);
            string xmlString = sr.ReadToEnd();
            sr.Close();
            file.Close();
            return Deserialize(xmlString);
        }
        finally
        {
            if ((file != null))
            {
                file.Dispose();
            }
            if ((sr != null))
            {
                sr.Dispose();
            }
        }
    }
}
}
#pragma warning restore
