using UnityEngine;
using SFAPI;
using System;

/// <summary>
/// Place this behaviour on an object in the scene, set the project name,
/// place the project folder inside your Resources folder, and then you can use
/// Storyforge.Get() to access the storyforget database.
/// e.g.
/// Storyforge sf = Storyforge.Get();
/// PlainText item = sf.GetContentItem<PlainText>("fCV-HL2-SVAN");
/// </summary>
public class Storyforge : MonoBehaviour
{
    // Project to load
    public string ProjectName = "";

    /// <summary>
    /// Return the shared Storyforge instance. For example, to get an item from the database, use
    /// Storyforge sf = Storyforge.Get();
    /// PlainText item = sf.GetSceneItem<PlainText>("fCV-HL2-SVAN");
    /// </summary>
    /// <returns>The shared Storyforge instance.</returns>
    public static Storyforge Get()
    {
        if (_instance == null)
        {
            _instance = UnityEngine.Object.FindObjectOfType(typeof(Storyforge)) as Storyforge;
            if (_instance == null)
                throw new System.Exception("Can't find Storyforge instance - the main Storyforge behaviour " +
                    "should be on a GameObject in your scene.");
        }
        return _instance;
    }

    /// <summary>
    /// Get a content item from the Storyforge database.
    /// </summary>
    /// <typeparam name="T">The SceneItem type you expect - for example, Scene, Script, Strings, or PlainText.</typeparam>
    /// <param name="itemID">The unique ID of the item.</param>
    /// <param name="throwError">If true, throws an error if the item doesn't exist or is the wrong type. If false, returns null instead.</param>
    /// <returns>The item, or null.</returns>
    public T GetSceneItem<T>(string itemID, bool throwError = true) where T : SceneItem
    {
        return _project.GetSceneItem<T>(itemID, throwError);
    }

    /// <summary>
    /// Get a content item from the Storyforge database.
    /// </summary>
    /// <typeparam name="T">The SceneItem type you expect - for example, Scene, Script, Strings, or PlainText.</typeparam>
    /// <param name="itemPath">The pathname for the item in the databse. e.g. "Content.Folder1.MyPlainText". This is case insensitive.</param>
    /// <param name="throwError">If true, throws an error if the item doesn't exist or is the wrong type. If false, returns null instead.</param>
    /// <returns>The item, or null.</returns>
    public T GetSceneItemByPath<T>(string itemPath, bool throwError = true) where T : SceneItem
    {
        return _project.GetSceneItemByPath<T>(itemPath, throwError);
    }

    /// <summary>
    /// Set the current locale. Will throw an exception if the
    /// Storyforge project doesn't contain language files
    /// for this locale code. 
    /// </summary>
    /// <param name="code">e.g. en-gb</param>
    public void SetLocale(string localeCode)
    {
        _project.SetLocale(localeCode);
    }

    /// <summary>
    /// Get the current locale code.
    /// </summary>
    public string CurrentLocale
    {
        get { return _project.CurrentLocale; }
    }

    /// <summary>
    /// Get a Character entry from the database.
    /// </summary>
    /// <param name="gameID">The gameID specified on the Character page in Storyforge</param>
    /// <returns></returns>
    public Character GetCharacterByGameID(string gameID)
    {
        return _project.GetCharacterByGameID(gameID);
    }

    // -------------------------------------------
    // Deprecated v0.2
    // -------------------------------------------
    [@ObsoleteAttribute("This is deprecated - use GetSceneItem<T>() instead")]
    public T GetContentItem<T>(string itemID, bool throwError = true) where T : ContentItem
    {
        return _project.GetSceneItem<T>(itemID, throwError);
    }

    [@ObsoleteAttribute("This is deprecated - use GetSceneItemByPath<T>() instead")]
    public T GetContentItemByPath<T>(string itemPath, bool throwError = true) where T : ContentItem
    {
        return _project.GetSceneItemByPath<T>(itemPath, throwError);
    }

    [@ObsoleteAttribute("This is deprecated - use GetSceneItem<Scene>() instead")]
    public Scene GetScene(string sceneID, bool throwError = true)
    {
        return _project.GetSceneItem<Scene>(sceneID, throwError);
    }

    [@ObsoleteAttribute("This is deprecated - use GetSceneItemByPath<Scene>() instead")]
    public Scene GetSceneByPath(string scenePath, bool throwError = true)
    {
        return _project.GetSceneItemByPath<Scene>(scenePath, throwError);
    }

    // -------------------------------------------
    // Internal
    // -------------------------------------------

    private static Storyforge _instance = null;
    private string _projectFolder;
    private Project _project;

    private void Awake()
    {
        LoadProject();
    }

    private void LoadProject()
    {
        _projectFolder = ProjectName;
        _project = new Project(_projectFolder);
        _project.Parse();
    }

    // -------------------------------------------
}