using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;

namespace SFAPI
{
    internal struct Constants
    {
        internal static string API_VERSION = "0.3";
    }

    internal sealed class ParseException : System.Exception
    {
        internal ParseException(string msg = "") : base("Storyforge load error. " + msg)
        {
        }
    }

    /// <summary>
    /// Represents a localised string from the database.
    /// </summary>
    public sealed class LocalisedString
    {
        private string _text = "";
        private string _id = null;

        /// <summary>
        /// Returns the text of localised string in the current game language.
        /// </summary>
        public string Text
        {
            get { return _text; }
        }

        /// <summary>
        /// Returns the unique ID of this string. Useful for identifying attached audio.
        /// Can be null, if the field is default.
        /// </summary>
        public string ID
        {
            get { return _id; }
        }

        internal LocalisedString()  {}

        internal void ParseChild(XmlElement xml)
        {
            XmlElement xmlText = xml.SelectSingleNode("./Text") as XmlElement;
            _id = xmlText.GetAttribute("ID");
            _text = xmlText.InnerText;
        }

        internal void ParseFromElement(XmlElement xml)
        {
            _id = xml.GetAttribute("LocID");
            _text = xml.InnerText;
        }

        internal void SetText(string text)
        {
            _text = text;
            _id = null;
        }
    }

    public abstract class XmlLoader
    {
        protected string _projectFolder;

        protected XmlLoader(string projectFolder)
        {
            _projectFolder = projectFolder;
        }

        protected XmlDocument LoadXmlDocument(string fileName, bool optional=false)
        {
            string shortFileName = Path.GetFileNameWithoutExtension(fileName);
            string filePath = _projectFolder + "/" + shortFileName;
            TextAsset textFile = Resources.Load<TextAsset>(filePath);
            if (textFile == null)
            {
                if (optional)
                    return null;

                throw new System.Exception("Storyforge file '" + fileName + "' doesn't exist. " +
                    "Make sure ProjectName is correct on the Storyforge behaviour, and that the project files " +
                    "are stored in Assets/Resources/<ProjectName>");
            }

            XmlDocument xml = new XmlDocument();
            try
            {
                xml.LoadXml(textFile.text);
            }
            catch (System.Exception ex)
            {
                throw new ParseException("Malformed XML document '" + fileName + "' - " + ex.Message);
            }
            return xml;
        }
    }

    internal sealed class Locales : XmlLoader
    {
        internal Dictionary<string, string> locales;
        private string _currentLocale;
        private string _defaultLocale;
        internal Dictionary<string, string> strings;

        internal Locales(string projectFolder) : base(projectFolder)
        {
            locales = new Dictionary<string, string>();
            strings = new Dictionary<string, string>();
            _currentLocale = "";
            _defaultLocale = "";
        }

        internal void SetLocale(string code="")
        {
            if (!locales.ContainsKey(code))
                throw new System.Exception("Storyforge project doesn't contain information for locale code '" + code + "'");
            _currentLocale = code;
            LoadLocale();
        }

        internal string CurrentLocale
        {
            get { return _currentLocale; }
        }

        internal string GetString(string locID)
        {
            if (strings.ContainsKey(locID))
                return strings[locID];
            return null;
        }

        internal void Parse()
        {
            XmlDocument xml = LoadXmlDocument("locales.sfp", true);
            if (xml == null)
                return;

            XmlNodeList elemList = xml.GetElementsByTagName("Locale");
            foreach (XmlElement xmlLocale in elemList)
            {
                string code = xmlLocale.GetAttribute("code");
                string ID = xmlLocale.GetAttribute("ID");
                locales[code] = ID;
                if (ID == "default")
                {
                    _defaultLocale = code;
                    _currentLocale = code;
                }
            }
        }

        internal bool IsDefaultLocale()
        {
            return _currentLocale == _defaultLocale;
        }

        internal void LoadLocale()
        {
            strings.Clear();
            if (IsDefaultLocale())
                return;

            string ID = locales[_currentLocale];
            XmlDocument xml = LoadXmlDocument(ID+".sfs", true);
            if (xml == null)
                return;

            XmlNodeList elemList = xml.GetElementsByTagName("String");
            foreach (XmlElement xmlString in elemList)
            {
                string loc_id = xmlString.GetAttribute("loc_id");
                string text = xmlString.InnerText;
                strings.Add(loc_id, text);
            }
        }
    }

    internal sealed class FieldSpec
    {
        internal string gameID;
        internal string type;
        internal Field defaultValue;
        private Project _project;
        bool _needsTranslation = false;

        internal FieldSpec(Project project)
        {
            _project = project;
        }

        internal string GetLocText(LocalisedString locString)
        {
            if (_needsTranslation)
                return _project.GetLocText(locString);
            return locString.Text;
        }

        internal void Parse(XmlElement xml)
        {
            gameID = xml.GetAttribute("game_id");
            type = xml.GetAttribute("type");
            string defaultStr = xml.GetAttribute("default");
            defaultValue = CreateField();
            defaultValue.SetFromString(defaultStr);
        }

        internal Field CreateField()
        {
            Field field = null;
            switch (type)
            {
                case "bool":
                    field = new Field_Bool(this);
                    break;
                case "string":
                case "text":
                case "select":
                    field = new Field_String(this);
                    break;
                case "locstring":
                case "loctext":
                    _needsTranslation = true;
                    field = new Field_LocString(this);
                    break;
                case "voxstring":
                case "voxtext":
                    field = new Field_LocString(this);
                    break;
                case "int":
                    field = new Field_Int(this);
                    break;
                case "float":
                    field = new Field_Float(this);
                    break;
                default:
                    throw new ParseException("Unknown field type '" + type + "'");
            }
            return field;
        }
    }

    internal sealed class FieldsetSpec
    {
        internal string gameID;
        internal string ID;
        internal Dictionary<string, FieldSpec> fields;
        private Project _project;

        internal FieldsetSpec(Project project)
        {
            _project = project;
            fields = new Dictionary<string, FieldSpec>();
        }

        internal void Parse(XmlElement xml)
        {
            ID = xml.GetAttribute("ID");
            gameID = xml.GetAttribute("game_id");

            XmlNodeList elemList = xml.GetElementsByTagName("Field");
            foreach (XmlElement xmlFieldSpec in elemList)
            {
                FieldSpec field = new FieldSpec(_project);
                field.Parse(xmlFieldSpec);
                fields.Add(field.gameID, field);
            }
        }
    }

    internal sealed class FieldsetSpecs : XmlLoader
    {
        internal Dictionary<string, FieldsetSpec> specsByID;
        internal Dictionary<string, FieldsetSpec> specsByGameID;
        private Project _project;

        internal FieldsetSpecs(Project project) : base(project.ProjectFolder)
        {
            _project = project;
            specsByID = new Dictionary<string, FieldsetSpec>();
            specsByGameID = new Dictionary<string, FieldsetSpec>();
        }

        internal void Parse()
        {
            XmlDocument xml = LoadXmlDocument("custom-fields.sfp", true);
            if (xml == null)
                return;
            XmlNodeList elemList = xml.GetElementsByTagName("FieldSet");
            foreach (XmlElement xmlFieldSet in elemList)
            {
                FieldsetSpec fieldSet = new FieldsetSpec(_project);
                fieldSet.Parse(xmlFieldSet);
                specsByGameID.Add(fieldSet.gameID, fieldSet);
                specsByID.Add(fieldSet.ID, fieldSet);
            }
        }
    }

    /// <summary>
    /// Base class of all field types.
    /// </summary>
    public abstract class Field
    {
        internal FieldSpec _spec;

        internal Field(FieldSpec spec)
        {
            _spec = spec;
        }

        internal virtual void Parse(XmlElement xml)
        {
        }

        internal virtual void SetFromString(string strValue)
        {

        }
    }

    /// <summary>
    /// Boolean field.
    /// </summary>
    public sealed class Field_Bool : Field
    {
        private bool _value = false;

        /// <summary>
        /// Field value.
        /// </summary>
        public bool Value
        {
            get { return _value; }
        }

        internal Field_Bool(FieldSpec spec) : base(spec)
        {

        }

        internal override void Parse(XmlElement xml)
        {
            string textVal = xml.GetAttribute("value");
            SetFromString(textVal);
        }

        internal override void SetFromString(string strValue)
        {
            _value = (strValue.ToLower() == "true");
        }
    }

    /// <summary>
    /// String field. May also be a LocalisedString.
    /// </summary>
    public class Field_String : Field
    {
        private string _text = "";

        /// <summary>
        /// Text value.
        /// </summary>
        public virtual string Text
        {
            get { return _text; }
        }

        internal Field_String(FieldSpec spec) : base(spec)
        {

        }

        internal override void Parse(XmlElement xml)
        {
            _text = xml.InnerText;
        }

        internal override void SetFromString(string strValue)
        {
            _text = strValue;
        }
    }

    /// <summary>
    /// Localised string. Will return both text for the current language and a unique ID.
    /// </summary>
    public sealed class Field_LocString : Field_String
    {
        private LocalisedString _locString;

        /// <summary>
        /// Text value for the current language.
        /// </summary>
        public override string Text
        {
            get { return _spec.GetLocText(_locString); }
        }

        /// <summary>
        /// Localisation ID. Useful for tying to audio files.
        /// </summary>
        public string LocID
        {
            get { return _locString.ID; }
        }

        internal Field_LocString(FieldSpec spec) : base(spec)
        {
            _locString = new LocalisedString();
        }

        internal override void Parse(XmlElement xml)
        {
            _locString.ParseChild(xml);
        }

        internal override void SetFromString(string strValue)
        {
            _locString.SetText(strValue);
        }
    }

    /// <summary>
    /// Integer field.
    /// </summary>
    public sealed class Field_Int : Field
    {
        private int _value = 0;

        /// <summary>
        /// Field value.
        /// </summary>
        public int Value
        {
            get { return _value; }
        }

        internal Field_Int(FieldSpec spec) : base(spec)
        {

        }

        internal override void Parse(XmlElement xml)
        {
            string textVal = xml.GetAttribute("value");
            SetFromString(textVal);
        }

        internal override void SetFromString(string strValue)
        {
            _value = int.Parse(strValue);
        }
    }

    /// <summary>
    /// Float field.
    /// </summary>
    public sealed class Field_Float : Field
    {
        private float _value = 0;

        /// <summary>
        ///  Field value.
        /// </summary>
        public float Value
        {
            get { return _value; }
        }

        internal Field_Float(FieldSpec spec) : base(spec)
        {

        }

        internal override void Parse(XmlElement xml)
        {
            string textVal = xml.GetAttribute("value");
            SetFromString(textVal);
        }

        internal override void SetFromString(string strValue)
        {
            _value = float.Parse(strValue);
        }
    }

    internal sealed class Fieldset
    {
        internal Dictionary<string, Field> fields;
        private FieldsetSpec _spec;

        internal Fieldset(FieldsetSpec spec)
        {
            fields = new Dictionary<string, Field>();
            _spec = spec;
        }

        internal void Parse(XmlElement xml)
        {
            XmlNodeList elemList = xml.GetElementsByTagName("Field");
            foreach (XmlElement xmlField in elemList)
            {
                string id = xmlField.GetAttribute("ID");
                FieldSpec spec = _spec.fields[id];
                Field field = spec.CreateField();
                field.Parse(xmlField);
                fields.Add(id, field);
            }
        }
    }

    internal sealed class Fieldsets
    {
        internal Dictionary<string, Fieldset> fieldsets;
        FieldsetSpecs _specs;

        internal Fieldsets(FieldsetSpecs specs)
        {
            fieldsets = new Dictionary<string, Fieldset>();
            _specs = specs;
        }

        internal void Parse(XmlElement xml)
        {
            XmlNodeList elemList = xml.GetElementsByTagName("Fieldset");
            foreach (XmlElement xmlFieldset in elemList)
            {
                string ext_id = xmlFieldset.GetAttribute("ID");
                string id = ext_id;
                //string context = "";
                if (ext_id.Contains("#"))
                {
                    string[] arr = ext_id.Split('#');
                    id = arr[0];
                    //context = arr[1];
                }
                FieldsetSpec spec = _specs.specsByID[id];
                Fieldset fieldset = new Fieldset(spec);
                fieldset.Parse(xmlFieldset);
                fieldsets.Add(ext_id, fieldset);
            }
        }

        internal Field GetField(string fieldsetGameID, string fieldID, bool throwError = true, string context = null)
        {
            if (!_specs.specsByGameID.ContainsKey(fieldsetGameID))
            {
                if (throwError)
                    throw new System.Exception("Storyforge: Can't find fieldset '" + fieldsetGameID + "'");
                return null;
            }

            FieldsetSpec setSpec = _specs.specsByGameID[fieldsetGameID];
            string ext_id = setSpec.ID;
            if (context != null)
                ext_id += "#" + context;
            if (fieldsets.ContainsKey(ext_id))
            {
                Fieldset fieldset = fieldsets[ext_id];
                if (fieldset.fields.ContainsKey(fieldID))
                    return fieldset.fields[fieldID];
            }

            if (!setSpec.fields.ContainsKey(fieldID))
            {
                if (throwError)
                    throw new System.Exception("Storyforge: Can't find field '" + fieldID + "' in fieldset '" + fieldsetGameID + "'");
                return null;
            }

            FieldSpec fieldSpec = setSpec.fields[fieldID];
            return fieldSpec.defaultValue;
        }
    }

    /// <summary>
    /// SceneItem. Parent class of Scene, SceneFolder, and any ContentItem
    /// such as PlainText or Script. If Scene or SceneFolder, may contain Children.
    /// </summary>
    public abstract class SceneItem : XmlLoader
    {
        protected string _id;
        protected string _name;
        protected List<SceneItem> _children;
        protected Project _project;
        protected string _path;
        protected SceneItem _parent;

        public string ID
        {
            get { return _id; }
        }

        public string Path
        {
            get { return _path; }
        }

        protected SceneItem(Project project) : base(project.ProjectFolder)
        {
            _project = project;
            _children = new List<SceneItem>();
        }

        internal void SetParent(SceneItem parent)
        {
            _parent = parent;
        }

        internal void SetPath(string path)
        {
            _path = path;
        }

        internal Project Project
        {
            get { return _project; }
        }

        internal void ParseSceneTree(XmlElement xml)
        {
            _id = xml.GetAttribute("ID");
            _name = xml.GetAttribute("name");
            if (_parent != null)
                SetPath(_parent.Path + "." + _name);
            _project.RegisterSceneItem(this);

            XmlNodeList childList = xml.ChildNodes;
            foreach (XmlNode xmlNode in childList)
            {
                if (xmlNode.NodeType != XmlNodeType.Element)
                    continue;
                XmlElement xmlChild = xmlNode as XmlElement;
                SceneItem item = null;
                switch (xmlChild.Name)
                {
                    case "Folder":
                        {
                            item = new SceneFolder(_project);
                            break;
                        }
                    case "Scene":
                        {
                            item = new Scene(_project);
                            break;
                        }
                    case "Content":
                        {
                            string itemType = xmlChild.GetAttribute("type");
                            ContentType contentType = _project.GetContentType(itemType);
                            switch (contentType.BaseTypeID)
                            {
                                case "Script":
                                    item = new Script(_project, contentType);
                                    break;
                                case "PlainText":
                                    item = new PlainText(_project, contentType);
                                    break;
                                case "Strings":
                                    item = new Strings(_project, contentType);
                                    break;
                                case "Character":
                                    // Ignore
                                    break;
                                default:
                                    throw new ParseException();
                            }
                            break;
                        }
                    default:
                        {
                            throw new ParseException();
                        }
                }
                _children.Add(item);
                item.SetParent(this);
                item.ParseSceneTree(xmlChild);
                item.Parse();
            }
        }

        protected virtual void Parse()
        {
        }
    }

    /// <summary>
    /// Folder containing Scenes or other Folders
    /// </summary>
    public sealed class SceneFolder : SceneItem
    {
        internal SceneFolder(Project project) : base(project)
        {

        }

        /// <summary>
        /// Return a list of this scene's children
        /// </summary>
        /// <returns></returns>
        public IList<SceneItem> GetChildren()
        {
            return _children;
        }
    }

    /// <summary>
    /// Scene containing ContentItems such as Script or PlainText
    /// </summary>
    public sealed class Scene : SceneItem
    {
        internal Scene(Project project) : base(project)
        {

        }

        /// <summary>
        /// Return a list of this scene's children
        /// </summary>
        /// <returns></returns>
        public IList<SceneItem> GetChildren()
        {
            return _children;
        }
    }

    /// <summary>
    /// Base class for all content items in a scene.
    /// </summary>
    public abstract class ContentItem : SceneItem
    {
        private Fieldsets _fieldsets;
        private ContentType _contentType;
        private bool _needsTranslation = false;

        protected ContentItem(Project project, ContentType contentType) : base(project)
        {
            _fieldsets = new Fieldsets(project.FieldsetSpecs);
            _contentType = contentType;
            _needsTranslation = (contentType.GetOption("NeedsTranslation", "True") == "True");
        }

        protected override void Parse()
        {
            XmlDocument xml = LoadXmlDocument(ID + ".sfc");
            XmlElement xmlContent = xml.SelectSingleNode("/Content/*[2]") as XmlElement;
            ParseContent(xmlContent);
            XmlElement xmlFieldsets = xml.SelectSingleNode("/Content/Fieldsets") as XmlElement;
            if (xmlFieldsets != null)
                ParseFieldsets(xmlFieldsets);
        }

        protected virtual void ParseContent(XmlElement xmlContent)
        {
        }

        private void ParseFieldsets(XmlElement xml)
        {
            _fieldsets.Parse(xml);
        }

        internal bool NeedsTranslation
        {
            get { return _needsTranslation; }
        }

        /// <summary>
        /// Return the UUID of the content type. Note that user-defined types will
        /// be a hashed number.
        /// </summary>
        public string ContentTypeID
        {
            get { return _contentType.ID; }
        }

        /// <summary>
        /// Return the base type.
        /// </summary>
        public string BaseTypeID
        {
            get { return _contentType.BaseTypeID; }
        }

        /// <summary>
        /// Generic method of retrieving a field.
        /// </summary>
        /// <typeparam name="T">Specify expected field type, for example Field_String.</typeparam>
        /// <param name="fieldsetGameID">The gameID of the fieldset (see under the top of a fieldset pane in Storyforge)</param>
        /// <param name="fieldID">The gameID of the specific field.</param>
        /// <param name="throwErrors">If true, throws an error if the field doesn't exist. If false, returns null instead. Defaults to true.</param>
        /// <returns>Field object or subclass, or null if it doesn't exist.</returns>
        public T GetField<T>(string fieldsetGameID, string fieldID, bool throwErrors = true) where T : Field
        {
            Field field = _fieldsets.GetField(fieldsetGameID, fieldID, throwErrors);
            if (field == null)
                return null;
            if (field is T)
                return field as T;
            if (throwErrors)
                throw new System.Exception("Field '" + fieldID + "' of fieldset '" + fieldsetGameID + "' is not of the expected type.");
            return null;
        }

        internal T GetField<T>(string fieldsetGameID, string fieldID, string context, bool throwErrors = true) where T : Field
        {
            Field field = _fieldsets.GetField(fieldsetGameID, fieldID, throwErrors, context);
            if (field == null)
                return null;
            if (field is T)
                return field as T;
            if (throwErrors)
                throw new System.Exception("Field '" + fieldID + "' of fieldset '" + fieldsetGameID + "' is not of the expected type.");
            return null;
        }

        /// <summary>
        /// Retrieves the localisation ID of a field, if it has one.
        /// </summary>
        /// <param name="fieldsetGameID">The gameID of the fieldset (see under the top of a fieldset pane in Storyforge)</param>
        /// <param name="fieldID">The gameID of the specific field.</param>
        /// <returns>The localisation ID, or null if it doesn't have one.</returns>
        public string GetFieldLocID(string fieldsetGameID, string fieldID)
        {
            Field_LocString locString = GetField<Field_LocString>(fieldsetGameID, fieldID, false);
            if (locString != null)
                return locString.LocID;
            return null;
        }

        /// <summary>
        /// Returns the text value of a String or LocalisedString field. Throws an error if it
        /// doesn't exist or is the wrong type.
        /// </summary>
        /// <param name="fieldsetGameID">The gameID of the fieldset (see under the top of a fieldset pane in Storyforge)</param>
        /// <param name="fieldID">The gameID of the specific field.</param>
        /// <returns>The value.</returns>
        public string GetFieldText(string fieldsetGameID, string fieldID)
        {
            return GetField<Field_String>(fieldsetGameID, fieldID).Text;
        }

        /// <summary>
        /// Returns the numeric value of the field. Throws an error if it
        /// doesn't exist or is the wrong type.
        /// </summary>
        /// <param name="fieldsetGameID">The gameID of the fieldset (see under the top of a fieldset pane in Storyforge)</param>
        /// <param name="fieldID">The gameID of the specific field.</param>
        /// <returns>The value.</returns>
        public float GetFieldFloat(string fieldsetGameID, string fieldID)
        {
            return GetField<Field_Float>(fieldsetGameID, fieldID).Value;
        }

        /// <summary>
        /// Returns the numeric value of the field. Throws an error if it
        /// doesn't exist or is the wrong type.
        /// </summary>
        /// <param name="fieldsetGameID">The gameID of the fieldset (see under the top of a fieldset pane in Storyforge)</param>
        /// <param name="fieldID">The gameID of the specific field.</param>
        /// <returns>The value.</returns>
        public int GetFieldInt(string fieldsetGameID, string fieldID)
        {
            return GetField<Field_Int>(fieldsetGameID, fieldID).Value;
        }

        /// <summary>
        /// Returns the boolean value of the field. Throws an error if it
        /// doesn't exist or is the wrong type.
        /// </summary>
        /// <param name="fieldsetGameID">The gameID of the fieldset (see under the top of a fieldset pane in Storyforge)</param>
        /// <param name="fieldID">The gameID of the specific field.</param>
        /// <returns>The value.</returns>
        public bool GetFieldBool(string fieldsetGameID, string fieldID)
        {
            return GetField<Field_Bool>(fieldsetGameID, fieldID).Value;
        }
    }

    /// <summary>
    /// A line in a script or set of barks. To identify whether it is a dialogue line or an
    /// action line, you can test Type==DIALOGUE or Type==ACTION.
    /// For sets of barks, it will never be Type==ACTION, but only Type==DIALOGUE.
    /// </summary>
    public abstract class ScriptItem
    {
        /// <summary>
        /// Identifier for an action line. You can cast an object that has this Type to ScriptAction.
        /// </summary>
        public const int ACTION = 0;
        /// <summary>
        /// Identifier for a dialogue line. You can cast an object that has this Type to ScriptDialogue
        /// </summary>
        public const int DIALOGUE = 1;
        internal const int PHRASE = 2;

        protected string _id;
        protected int _type;
        protected Script _script;

        /// <summary>
        /// Unique ID of this item. This is not the localisation ID, for that check the LocID
        /// of an individual line.
        /// </summary>
        public string ID
        {
            get { return _id; }
        }

        /// <summary>
        /// The Type of the item - action or dialogue? See members ACTION or DIALOGUE.
        /// </summary>
        public int Type
        {
            get { return _type; }
        }

        internal ScriptItem(Script script, int type)
        {
            _script = script;
            _type = type;
        }

        internal virtual void Parse(XmlElement xml)
        {
            _id = xml.GetAttribute("ID");
        }

        /// <summary>
        /// Generic method of retrieving a field.
        /// </summary>
        /// <typeparam name="T">Specify expected field type, for example Field_String.</typeparam>
        /// <param name="fieldsetGameID">The gameID of the fieldset (see under the top of a fieldset pane in Storyforge)</param>
        /// <param name="fieldID">The gameID of the specific field.</param>
        /// <param name="throwErrors">If true, throws an error if the field doesn't exist. If false, returns null instead. Defaults to true.</param>
        /// <returns>Field object or subclass, or null if it doesn't exist.</returns>
        public T GetField<T>(string fieldsetGameID, string fieldID, bool throwErrors = true) where T : Field
        {
            Field field = _script.GetField<Field>(fieldsetGameID, fieldID, ID, throwErrors);
            if (field == null)
                return null;
            if (field is T)
                return field as T;
            if (throwErrors)
                throw new System.Exception("Field '" + fieldID + "' of fieldset '" + fieldsetGameID + "' is not of the expected type.");
            return null;
        }

        /// <summary>
        /// Retrieves the localisation ID of a field, if it has one.
        /// </summary>
        /// <param name="fieldsetGameID">The gameID of the fieldset (see under the top of a fieldset pane in Storyforge)</param>
        /// <param name="fieldID">The gameID of the specific field.</param>
        /// <returns>The localisation ID, or null if it doesn't have one.</returns>
        public string GetFieldLocID(string fieldsetGameID, string fieldID)
        {
            Field_LocString locString = GetField<Field_LocString>(fieldsetGameID, fieldID, false);
            if (locString != null)
                return locString.LocID;
            return null;
        }

        /// <summary>
        /// Returns the text value of a String or LocalisedString field. Throws an error if it
        /// doesn't exist or is the wrong type.
        /// </summary>
        /// <param name="fieldsetGameID">The gameID of the fieldset (see under the top of a fieldset pane in Storyforge)</param>
        /// <param name="fieldID">The gameID of the specific field.</param>
        /// <returns>The value.</returns>
        public string GetFieldText(string fieldsetGameID, string fieldID)
        {
            return GetField<Field_String>(fieldsetGameID, fieldID).Text;
        }

        /// <summary>
        /// Returns the numeric value of the field. Throws an error if it
        /// doesn't exist or is the wrong type.
        /// </summary>
        /// <param name="fieldsetGameID">The gameID of the fieldset (see under the top of a fieldset pane in Storyforge)</param>
        /// <param name="fieldID">The gameID of the specific field.</param>
        /// <returns>The value.</returns>
        public float GetFieldFloat(string fieldsetGameID, string fieldID)
        {
            return GetField<Field_Float>(fieldsetGameID, fieldID).Value;
        }

        /// <summary>
        /// Returns the numeric value of the field. Throws an error if it
        /// doesn't exist or is the wrong type.
        /// </summary>
        /// <param name="fieldsetGameID">The gameID of the fieldset (see under the top of a fieldset pane in Storyforge)</param>
        /// <param name="fieldID">The gameID of the specific field.</param>
        /// <returns>The value.</returns>
        public int GetFieldInt(string fieldsetGameID, string fieldID)
        {
            return GetField<Field_Int>(fieldsetGameID, fieldID).Value;
        }

        /// <summary>
        /// Returns the boolean value of the field. Throws an error if it
        /// doesn't exist or is the wrong type.
        /// </summary>
        /// <param name="fieldsetGameID">The gameID of the fieldset (see under the top of a fieldset pane in Storyforge)</param>
        /// <param name="fieldID">The gameID of the specific field.</param>
        /// <returns>The value.</returns>
        public bool GetFieldBool(string fieldsetGameID, string fieldID)
        {
            return GetField<Field_Bool>(fieldsetGameID, fieldID).Value;
        }
    }

    /// <summary>
    /// A phrase in a script or set of dialogue barks.
    /// </summary>
    public sealed class ScriptPhrase : ScriptItem
    {
        private LocalisedString _locString;

        /// <summary>
        /// The localised text of the line.
        /// </summary>
        public string Text
        {
            get
            {
                if (_script.NeedsTranslation)
                    return _script.Project.GetLocText(_locString);
                else
                    return _locString.Text;
            }
        }

        /// <summary>
        /// The localisation ID of the line. Useful for tying to audio.
        /// </summary>
        public string LocID
        {
            get { return _locString.ID; }
        }

        internal ScriptPhrase(Script script) : base(script, PHRASE)
        {
            _locString = new LocalisedString();
        }

        internal override void Parse(XmlElement xml)
        {
            _id = xml.GetAttribute("ID");
            _locString.ParseChild(xml);
        }
    }

    /// <summary>
    /// A dialogue item in a script or set of dialogue barks. Contains a character ID
    /// and a set of phrases.
    /// </summary>
    public sealed class ScriptDialogue : ScriptItem
    {
        private string _characterName = "";
        private string _character_game_id = "";
        private List<ScriptPhrase> _phrases;

        /// <summary>
        /// The name of the character. This is not localised - use CharacterSubtitleName for that.
        /// </summary>
        public string CharacterName
        {
            get { return _characterName; }
        }

        /// <summary>
        /// The Character tied to this line. May not be set if no
        /// character in your Characters folder matches the character name
        /// </summary>
        public Character Character
        {
            get {
                if (_character_game_id!=null)
                    return _script.Project.GetCharacterByGameID(_character_game_id);
                return null;
            }
        }

        /// <summary>
        /// The localised name of the character, if one exists. If not, the base character name.
        /// </summary>
        public string CharacterSubtitleName
        {
            get
            {
                Character character = Character;
                if (character != null)
                    return character.SubtitleName;
                return CharacterName;
            }
        }


        /// <summary>
        /// A list of the phrases contained in the dialogue.
        /// You can use this to access the phrases directly,
        /// or use GetPhrase(index)
        /// </summary>
        public IList<ScriptPhrase> Phrases
        {
            get { return _phrases; }
        }

        /// <summary>
        /// The number of phrases contained in the dialogue.
        /// </summary>
        public int PhraseCount
        {
            get { return _phrases.Count; }
        }

        internal ScriptDialogue(Script script) : base(script, DIALOGUE)
        {
            _phrases = new List<ScriptPhrase>();
        }

        internal override void Parse(XmlElement xml)
        {
            base.Parse(xml);
            _characterName = xml.GetAttribute("character");
            _character_game_id = xml.GetAttribute("character_game_id");

            XmlNodeList elemList = xml.GetElementsByTagName("Phrase");
            foreach (XmlElement xmlPhrase in elemList)
            {
                ScriptPhrase phrase = new ScriptPhrase(_script);
                _phrases.Add(phrase);
                phrase.Parse(xmlPhrase);
            }
        }

        /// <summary>
        /// Retrieve a specific phrase from the dialogue.
        /// </summary>
        /// <param name="index">Index of the phrase, from 0...PhraseCount</param>
        /// <returns>A ScriptPhrase object.</returns>
        public ScriptPhrase GetPhrase(int index)
        {
            if (index >= _phrases.Count)
                throw new System.Exception("Storyforge - index '" + index + "' of phrase higher than number of phrases!");
            return _phrases[index];
        }
    }

    /// <summary>
    /// An item from a script.
    /// </summary>
    public sealed class ScriptAction : ScriptItem
    {
        private string _text = "";

        /// <summary>
        /// The text of the action. This is not localised.
        /// </summary>
        public string Text
        {
            get { return _text; }
        }

        internal ScriptAction(Script script) : base(script, ACTION)
        {

        }

        internal override void Parse(XmlElement xml)
        {
            base.Parse(xml);
            _text = xml.InnerText;
        }
    }

    /// <summary>
    /// A script content item, representing a cut-scene script or a set of bark lines.
    /// </summary>
    public sealed class Script : ContentItem
    {
        private List<ScriptItem> _lines;

        /// <summary>
        /// A list of the lines contained in the script.
        /// You can use this to access the lines directly,
        /// or use GetLine(index)
        /// </summary>
        public IList<ScriptItem> Lines
        {
            get { return _lines; }
        }

        /// <summary>
        /// The number of lines contained in this script.
        /// </summary>
        public int LineCount
        {
            get { return _lines.Count; }
        }

        /// <summary>
        /// Get a specific line from the script. This will always be a ScriptItem, but
        /// if you know the line type you can request it cast to ScriptDialogue or ScriptAction.
        /// In a set of barks this will always be ScriptDialogue - in a script, it might vary.
        /// </summary>
        /// <typeparam name="T">ScriptAction, or ScriptDialogue</typeparam>
        /// <param name="index">Index from 0...LineCount</param>
        /// <returns></returns>
        public T GetLine<T>(int index) where T : ScriptItem
        {
            if (index >= _lines.Count)
                throw new System.Exception("Index out of range trying to fetch script line:" + index);
            ScriptItem line = _lines[index];
            if (line is T)
                return line as T;
            throw new System.Exception("Trying to fetch script line:" + index + " as wrong type.");
        }

        /// <summary>
        /// Get a specific line from the script.
        /// </summary>
        /// <param name="index">Index from 0...LineCount</param>
        /// <returns>The line.</returns>
        public ScriptItem GetLine(int index)
        {
            if (index >= _lines.Count)
                throw new System.Exception("Index out of range trying to fetch script line:" + index);
            return _lines[index];
        }

        internal Script(Project project, ContentType contentType) : base(project, contentType)
        {
            _lines = new List<ScriptItem>();
        }

        protected override void ParseContent(XmlElement xml)
        {
            XmlNodeList childList = xml.ChildNodes;
            foreach (XmlNode xmlNode in childList)
            {
                if (xmlNode.NodeType != XmlNodeType.Element)
                    continue;
                XmlElement xmlChild = xmlNode as XmlElement;
                ScriptItem line = null;
                switch (xmlChild.Name)
                {
                    case "Action":
                        line = new ScriptAction(this);
                        break;
                    case "Dialogue":
                        line = new ScriptDialogue(this);
                        break;
                }
                _lines.Add(line);
                line.Parse(xmlChild);
            }
        }
    }

    /// <summary>
    /// Plain text content item.
    /// </summary>
    public sealed class PlainText : ContentItem
    {
        private LocalisedString _locString;

        /// <summary>
        /// The text, localised to the current language.
        /// </summary>
        public string Text
        {
            get
            {
                if (NeedsTranslation)
                    return _project.GetLocText(_locString);
                return _locString.Text;
            }
        }

        /// <summary>
        /// The localisation ID.
        /// </summary>
        public string LocID
        {
            get { return _locString.ID; }
        }

        internal PlainText(Project project, ContentType contentType) : base(project, contentType)
        {
            _locString = new LocalisedString();
        }

        protected override void ParseContent(XmlElement xml)
        {
            _locString.ParseChild(xml);
        }
    }

    /// <summary>
    /// Entry in a String content type.
    /// </summary>
    public sealed class StringEntry
    {
        private LocalisedString _locString;
        private string _gameID;
        private Strings _strings;

        /// <summary>
        /// The text, localised to the current language.
        /// </summary>
        public string Text
        {
            get
            {
                if (_strings.NeedsTranslation)
                {
                    return _strings.Project.GetLocText(_locString);
                }
                else
                {
                    return _locString.Text;
                }
            }
        }

        /// <summary>
        /// The localisation ID.
        /// </summary>
        public string LocID
        {
            get { return _locString.ID; }
        }

        /// <summary>
        /// GameID assigned in the editor, if any.
        /// </summary>
        public string GameID
        {
            get { return _gameID; }
        }

        internal StringEntry(Strings strings)
        {
            _strings = strings;
            _locString = new LocalisedString();
            _gameID = "";
        }

        internal void Parse(XmlElement xml)
        {
            _locString.ParseFromElement(xml);
            _gameID = xml.GetAttribute("game_id");
        }
    }

    /// <summary>
    /// Set of strings.
    /// </summary>
    public sealed class Strings : ContentItem
    {
        private List<StringEntry> _strings;
        private Dictionary<string, StringEntry> _stringsByGameID;

        /// <summary>
        /// A list of the lines contained in the script.
        /// You can use this to access the lines directly,
        /// or use GetEntry(index)
        /// </summary>
        public IList<StringEntry> Entries
        {
            get { return _strings; }
        }

        /// <summary>
        /// The number of lines contained in this script.
        /// </summary>
        public int EntryCount
        {
            get { return _strings.Count; }
        }

        /// <summary>
        /// Return entry by index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public StringEntry GetEntry(int index)
        {
            return _strings[index];
        }

        /// <summary>
        /// Return entry by game ID
        /// </summary>
        /// <param name="gameID"></param>
        /// <returns></returns>
        public StringEntry GetEntryByGameID(string gameID)
        {
            if (_stringsByGameID.ContainsKey(gameID))
                return _stringsByGameID[gameID];
            return null;
        }

        internal Strings(Project project, ContentType contentType) : base(project, contentType)
        {
            _strings = new List<StringEntry>();
            _stringsByGameID = new Dictionary<string, StringEntry>();
        }

        protected override void ParseContent(XmlElement xml)
        {
            XmlNodeList childList = xml.ChildNodes;
            foreach (XmlNode xmlNode in childList)
            {
                if (xmlNode.NodeType != XmlNodeType.Element)
                    continue;
                XmlElement xmlChild = xmlNode as XmlElement;
                StringEntry entry = new StringEntry(this);
                _strings.Add(entry);
                entry.Parse(xmlChild);
                if (entry.GameID!="")
                {
                    _stringsByGameID.Add(entry.GameID, entry);
                }
            }
        }
    }

    /// <summary>
    /// Character defined in the Characters section of Storyforge
    /// </summary>
    public sealed class Character
    {
        private string _game_id = "";
        private string _ID = "";
        private string _displayName = "";
        private LocalisedString _subtitleName = null;
        private Fieldsets _fieldsets;
        private Project _project;

        /// <summary>
        /// Unique ID of this item. This is not the localisation ID, for that check the LocID
        /// of an individual line.
        /// </summary>
        public string ID
        {
            get { return _ID; }
        }

        /// <summary>
        /// GameID
        /// </summary>
        public string GameID
        {
            get { return _game_id; }
        }

        /// <summary>
        /// Localised subtitle name of the character.
        /// </summary>
        public string SubtitleName
        {
            get
            {
                if ((_subtitleName == null) || (_subtitleName.ID==null))
                    return _displayName;
                return _project.GetLocText(_subtitleName);
            }
        }

        internal Character(Project project)
        {
            _project = project;
            _subtitleName = new LocalisedString();
            _fieldsets = new Fieldsets(project.FieldsetSpecs);
        }

        internal void Parse(XmlElement xml)
        {
            _game_id = xml.GetAttribute("game_id");
            _ID = xml.GetAttribute("ID");
            _displayName = xml.GetAttribute("display_name");
            XmlElement xmlSubtitleName = xml.SelectSingleNode("/SubtitleName") as XmlElement;
            if (xmlSubtitleName!=null)
               _subtitleName.ParseChild(xmlSubtitleName);
            XmlElement xmlFieldsets = xml.SelectSingleNode("/Fieldsets") as XmlElement;
            if (xmlFieldsets != null)
                _fieldsets.Parse(xmlFieldsets);
        }

        /// <summary>
        /// Generic method of retrieving a field.
        /// </summary>
        /// <typeparam name="T">Specify expected field type, for example Field_String.</typeparam>
        /// <param name="fieldsetGameID">The gameID of the fieldset (see under the top of a fieldset pane in Storyforge)</param>
        /// <param name="fieldID">The gameID of the specific field.</param>
        /// <param name="throwErrors">If true, throws an error if the field doesn't exist. If false, returns null instead. Defaults to true.</param>
        /// <returns>Field object or subclass, or null if it doesn't exist.</returns>
        public T GetField<T>(string fieldsetGameID, string fieldID, bool throwErrors = true) where T : Field
        {
            Field field = _fieldsets.GetField(fieldsetGameID, fieldID, throwErrors);
            if (field == null)
                return null;
            if (field is T)
                return field as T;
            if (throwErrors)
                throw new System.Exception("Field '" + fieldID + "' of fieldset '" + fieldsetGameID + "' is not of the expected type.");
            return null;
        }

        internal T GetField<T>(string fieldsetGameID, string fieldID, string context, bool throwErrors = true) where T : Field
        {
            Field field = _fieldsets.GetField(fieldsetGameID, fieldID, throwErrors, context);
            if (field == null)
                return null;
            if (field is T)
                return field as T;
            if (throwErrors)
                throw new System.Exception("Field '" + fieldID + "' of fieldset '" + fieldsetGameID + "' is not of the expected type.");
            return null;
        }

        /// <summary>
        /// Retrieves the localisation ID of a field, if it has one.
        /// </summary>
        /// <param name="fieldsetGameID">The gameID of the fieldset (see under the top of a fieldset pane in Storyforge)</param>
        /// <param name="fieldID">The gameID of the specific field.</param>
        /// <returns>The localisation ID, or null if it doesn't have one.</returns>
        public string GetFieldLocID(string fieldsetGameID, string fieldID)
        {
            Field_LocString locString = GetField<Field_LocString>(fieldsetGameID, fieldID, false);
            if (locString != null)
                return locString.LocID;
            return null;
        }

        /// <summary>
        /// Returns the text value of a String or LocalisedString field. Throws an error if it
        /// doesn't exist or is the wrong type.
        /// </summary>
        /// <param name="fieldsetGameID">The gameID of the fieldset (see under the top of a fieldset pane in Storyforge)</param>
        /// <param name="fieldID">The gameID of the specific field.</param>
        /// <returns>The value.</returns>
        public string GetFieldText(string fieldsetGameID, string fieldID)
        {
            return GetField<Field_String>(fieldsetGameID, fieldID).Text;
        }

        /// <summary>
        /// Returns the numeric value of the field. Throws an error if it
        /// doesn't exist or is the wrong type.
        /// </summary>
        /// <param name="fieldsetGameID">The gameID of the fieldset (see under the top of a fieldset pane in Storyforge)</param>
        /// <param name="fieldID">The gameID of the specific field.</param>
        /// <returns>The value.</returns>
        public float GetFieldFloat(string fieldsetGameID, string fieldID)
        {
            return GetField<Field_Float>(fieldsetGameID, fieldID).Value;
        }

        /// <summary>
        /// Returns the numeric value of the field. Throws an error if it
        /// doesn't exist or is the wrong type.
        /// </summary>
        /// <param name="fieldsetGameID">The gameID of the fieldset (see under the top of a fieldset pane in Storyforge)</param>
        /// <param name="fieldID">The gameID of the specific field.</param>
        /// <returns>The value.</returns>
        public int GetFieldInt(string fieldsetGameID, string fieldID)
        {
            return GetField<Field_Int>(fieldsetGameID, fieldID).Value;
        }

        /// <summary>
        /// Returns the boolean value of the field. Throws an error if it
        /// doesn't exist or is the wrong type.
        /// </summary>
        /// <param name="fieldsetGameID">The gameID of the fieldset (see under the top of a fieldset pane in Storyforge)</param>
        /// <param name="fieldID">The gameID of the specific field.</param>
        /// <returns>The value.</returns>
        public bool GetFieldBool(string fieldsetGameID, string fieldID)
        {
            return GetField<Field_Bool>(fieldsetGameID, fieldID).Value;
        }
    }

    internal sealed class Characters : XmlLoader
    {
        private Dictionary<string, Character> _characters;
        private Dictionary<string, Character> _charactersByGameID;
        private Project _project;

        internal Characters(Project project) : base(project.ProjectFolder)
        {
            _project = project;
            _characters = new Dictionary<string, Character>();
            _charactersByGameID = new Dictionary<string, Character>();
        }

        internal Character GetByGameID(string gameID)
        {
            if (_charactersByGameID.ContainsKey(gameID))
                return _charactersByGameID[gameID];
            return null;
        }

        internal void Parse()
        {
            XmlDocument xml = LoadXmlDocument("characters.sfp", true);
            if (xml == null)
                return;

            XmlNodeList elemList = xml.GetElementsByTagName("Character");
            foreach (XmlElement xmlCharacter in elemList)
            {

                Character character = new Character(_project);
                character.Parse(xmlCharacter);
                _characters.Add(character.ID, character);
                _charactersByGameID.Add(character.GameID, character);
            }
        }
    }

    public sealed class ContentType
    {
        private string _ID;
        private string _displayName;
        private string _baseTypeID;
        private Dictionary<string, string> _config;

        internal string ID
        {
            get { return _ID; }
        }

        internal string DisplayName
        {
            get { return _displayName; }
        }

        internal string BaseTypeID
        {
            get { return _baseTypeID; }
        }

        internal string GetOption(string option, string def="")
        {
            if (_config.ContainsKey(option))
                return _config[option];
            return def;
        }

        internal ContentType()
        {
            _config = new Dictionary<string, string>();
        }

        internal void Parse(XmlElement xml)
        {
            _ID = xml.GetAttribute("ID");
            _displayName = xml.GetAttribute("display_name");
            XmlElement xmlBaseType = xml.SelectSingleNode("./BaseType") as XmlElement;
            string baseType = xmlBaseType.GetAttribute("ID");
            _baseTypeID = baseType;

            XmlNodeList elemList = xmlBaseType.GetElementsByTagName("Config");
            foreach (XmlElement xmlConfig in elemList)
            {
                _config[xmlConfig.GetAttribute("option")] = xmlConfig.GetAttribute("value");
            }

        }
    }

    internal sealed class ContentTypes : XmlLoader
    {
        private Dictionary<string, ContentType> _types;
        private Project _project;

        internal ContentTypes(Project project) : base(project.ProjectFolder)
        {
            _project = project;
            _types = new Dictionary<string, ContentType>();
        }

        internal ContentType GetType(string typeID)
        {
            if (_types.ContainsKey(typeID))
                return _types[typeID];
            return null;
        }

        internal void Parse()
        {
            XmlDocument xml = LoadXmlDocument("content-types.sfp", true);
            if (xml == null)
                return;

            XmlNodeList elemList = xml.GetElementsByTagName("ContentType");
            foreach (XmlElement xmlContentType in elemList)
            {

                ContentType contentType = new ContentType();
                contentType.Parse(xmlContentType);
                _types.Add(contentType.ID, contentType);
            }
        }
    }

    /// <summary>
    /// The Storyforge project. Don't access the game through this,
    /// use Storyforge.Get() instead.
    /// </summary>
    public sealed class Project : XmlLoader
    {
        private SceneFolder _sceneRoot;
        private Dictionary<string, SceneItem> _idMap;
        private Dictionary<string, SceneItem> _pathMap;
        private ContentTypes _contentTypes;
        private FieldsetSpecs _fieldsetSpecs;
        private Locales _locales;
        private Characters _characters;

        internal Project(string projectFolder) : base(projectFolder)
        {
            _idMap = new Dictionary<string, SceneItem>();
            _pathMap = new Dictionary<string, SceneItem>();
            _fieldsetSpecs = new FieldsetSpecs(this);
            _locales = new Locales(projectFolder);
            _characters = new Characters(this);
            _contentTypes = new ContentTypes(this);
        }

        internal string ProjectFolder
        {
            get { return _projectFolder; }
        }

        /// <summary>
        /// Set the current locale. Will throw an exception if the
        /// Storyforge project doesn't contain language files
        /// for this locale code.
        /// </summary>
        /// <param name="code">e.g. en-gb</param>
        public void SetLocale(string code)
        {
            _locales.SetLocale(code);
        }

        /// <summary>
        /// Choose how the API should respond if you are in a localised
        /// language and try to fetch a string which hasn't been localised.
        /// Default is ReturnDefaultWithPrefix, with a prefix of '[Missing]'
        /// </summary>
        public enum MissingLocaleStringPolicy
        {
            /// <summary>
            /// Throw an exception.
            /// </summary>
            ThrowException,
            /// <summary>
            /// Return an empty string.
            /// </summary>
            ReturnBlank,
            /// <summary>
            /// Return the string in the default language.
            /// </summary>
            ReturnDefault,
            /// <summary>
            /// Return the string in the default language with a prefix.
            /// </summary>
            ReturnDefaultWithPrefix
        }
        private MissingLocaleStringPolicy _missingLocaleStringPolicy = MissingLocaleStringPolicy.ReturnDefaultWithPrefix;
        private string _missingLocaleStringPrefix = "[Missing] ";

        /// <summary>
        /// Choose how the API should respond if you are in a localised
        /// language and try to fetch a string which hasn't been localised.
        /// Default is MissingLocaleStringPolicy.ReturnDefaultWithPrefix, with a prefix of '[Missing]'
        /// </summary>
        /// <param name="policy"></param>
        /// <param name="missingPrefix">The prefix to use for MissingLocaleStringPolicy.ReturnDefaultWithPrefix</param>
        public void SetMissingLocaleStringPolicy(MissingLocaleStringPolicy policy, string missingPrefix="")
        {
            _missingLocaleStringPolicy = policy;
            _missingLocaleStringPrefix = missingPrefix;
        }

        /// <summary>
        /// Get the current locale code.
        /// </summary>
        public string CurrentLocale
        {
            get { return _locales.CurrentLocale; }
        }

        internal FieldsetSpecs FieldsetSpecs
        {
            get { return _fieldsetSpecs; }
        }

        internal void Parse()
        {
            _locales.Parse();
            _characters.Parse();
            _fieldsetSpecs.Parse();
            _contentTypes.Parse();
            ParseProject();
        }

        private void ParseProject()
        {
            XmlDocument xml = LoadXmlDocument("project.sfp", true);
            if (xml == null)
                throw new ParseException("Can't load Storyforge project '" + this._projectFolder + "' - wrong project name? Project folder should be stored under Assets/Resources");
            string api_version = xml.DocumentElement.GetAttribute("api_version");
            if (api_version == "")
                api_version = "0.0";
            int version_diff = CompareAPIVersions(Constants.API_VERSION, api_version);
            if (version_diff > 0)
                throw new ParseException("Can't load Storyforge project. Project is created with a higher API version than this build. Project is version: " + api_version + ", this build is: " + Constants.API_VERSION);


            XmlElement xmlScenes = xml.SelectSingleNode("/Project/Scenes") as XmlElement;

            _sceneRoot = new SceneFolder(this);
            _sceneRoot.SetPath("Content");
            _sceneRoot.ParseSceneTree(xmlScenes);
        }

        internal void RegisterSceneItem(SceneItem item)
        {
            _idMap.Add(item.ID, item);
            _pathMap.Add(item.Path.ToLower(), item);
        }

        internal ContentType GetContentType(string typeID)
        {
            ContentType contentType = _contentTypes.GetType(typeID);
            if (contentType==null)
                throw new ParseException("Unknown content type '" + typeID + "'");
            return contentType;
        }

        /// <summary>
        /// Get a content item from the Storyforge database.
        /// </summary>
        /// <typeparam name="T">The SceneItem type you expect - for example, Script or PlainText.</typeparam>
        /// <param name="itemID">The unique ID of the item.</param>
        /// <param name="throwError">If true, throws an error if the item doesn't exist or is the wrong type. If false, returns null instead.</param>
        /// <returns>The item, or null.</returns>
        public T GetSceneItem<T>(string itemID, bool throwError = true) where T : SceneItem
        {
            if (_idMap.ContainsKey(itemID))
            {
                SceneItem item = _idMap[itemID];
                if (item is T)
                    return item as T;
                throw new System.Exception("Storyforge scene item is not expected type: " + itemID);
            }

            if (throwError)
                throw new System.Exception("Can't find Storyforge scene item: " + itemID);
            return null;
        }

        /// <summary>
        /// Get a content item from the Storyforge database.
        /// </summary>
        /// <typeparam name="T">The SceneItem type you expect - for example, Script or PlainText.</typeparam>
        /// <param name="itemPath">The pathname for the item in the database. e.g. "Content.Folder1.MyScene.MyPlainText". This is case-insensitive.</param>
        /// <param name="throwError">If true, throws an error if the item doesn't exist or is the wrong type. If false, returns null instead.</param>
        /// <returns>The item, or null.</returns>
        public T GetSceneItemByPath<T>(string itemPath, bool throwError = true) where T : SceneItem
        {
            string pathLower = itemPath.ToLower();

            // TODO: Remove once people have stopped using this!
            if (pathLower.StartsWith("scenes."))
                pathLower = "content."+ pathLower.Substring(7);

            if (_pathMap.ContainsKey(pathLower))
            {
                SceneItem item = _pathMap[pathLower];
                if (item is T)
                    return item as T;
                throw new System.Exception("Storyforge scene item is not expected type: " + itemPath);
            }

            if (throwError)
                throw new System.Exception("Can't find Storyforge scene item: " + itemPath);
            return null;
        }

        /// <summary>
        /// Returns a character, or null if it isn't defined in the game.
        /// </summary>
        /// <param name="gameID"></param>
        /// <returns></returns>
        public Character GetCharacterByGameID(string gameID)
        {
            return _characters.GetByGameID(gameID);
        }

        private int CompareAPIVersions(string versionA, string versionB)
        {
            string[] vA = versionA.Split('.');
            string[] vB = versionB.Split('.');

            if (vA.Length != vB.Length)
                throw new System.Exception("Comparing different length versions?");
            for (int i = 0; i < vA.Length; i++)
            {
                int a = int.Parse(vA[i]);
                int b = int.Parse(vB[i]);
                if (b > a)
                    return 1;
                if (b < a)
                    return -1;
            }
            return 0;
        }

        internal string GetLocText(LocalisedString locString)
        {
            if (locString.ID == null)
                return "";
            if (!_locales.IsDefaultLocale())
            {
                string text = _locales.GetString(locString.ID);
                if (text != null)
                    return text;
                switch (_missingLocaleStringPolicy)
                {
                    case MissingLocaleStringPolicy.ThrowException:
                        throw new System.Exception("String ID '" + locString.ID + "' not localised in '" +
                            _locales.CurrentLocale + "'. Default text:'" + locString.Text + "'");
                    case MissingLocaleStringPolicy.ReturnBlank:
                        return "";
                    case MissingLocaleStringPolicy.ReturnDefault:
                        return locString.Text;
                    case MissingLocaleStringPolicy.ReturnDefaultWithPrefix:
                        return _missingLocaleStringPrefix + locString.Text;
                }
            }
            return locString.Text;
        }
    }
}