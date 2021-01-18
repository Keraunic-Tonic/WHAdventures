using SFAPI;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Storyforge sf = Storyforge.Get();

        // Test get item
        Script script1 = sf.GetSceneItem<Script>("fCV-HL2-SVAN");
        if (script1.BaseTypeID != "Script")
            throw new System.Exception("Base type is incorrect.");
        if (script1.ContentTypeID != "Script")
            throw new System.Exception("Content Type is incorrect.");

        // Test get by path
        Script script2 = sf.GetSceneItemByPath<Script>("Content.Folder 1.Scene 1.Script 1");
        if (script1 != script2)
            throw new System.Exception("Scripts aren't the same.");
        Script script3 = sf.GetSceneItemByPath<Script>("Rhubarb", false);
        if (script3 != null)
            throw new System.Exception("Script should be null.");
        script3 = sf.GetSceneItemByPath<Script>("", false);
        if (script3 != null)
            throw new System.Exception("Script should be null.");

        // Test Plain Text
        PlainText text1 = sf.GetSceneItem<PlainText>("Fsr-8A5-3Wzi");
        if (text1.Text != "Testing. Here is some more sensible content.")
            throw new System.Exception("Text content is incorrect.");
        text1 = sf.GetSceneItemByPath<PlainText>("Content.Folder 1.Scene 1.Plain Text 2");
        if (text1.Text != "Testing. Here is some more sensible content.")
            throw new System.Exception("Text content is incorrect.");

        // Test Script
        Script script = sf.GetSceneItem<Script>("fCV-HL2-SVAN");
        ScriptAction action = script.GetLine<ScriptAction>(0);
        if (action.Text != "Testing.")
            throw new System.Exception("Action text content is incorrect.");
        ScriptDialogue line = script.GetLine<ScriptDialogue>(1);
        if (line.CharacterName != "Jim")
            throw new System.Exception("Dialogue character is incorrect.");
        ScriptPhrase phrase = line.Phrases[0];
        if (phrase.Text != "Testing")
            throw new System.Exception("Phrase text is incorrect.");
        if (phrase.LocID != "4eT-yzk-LRSb")
            throw new System.Exception("Phrase locID is incorrect.");
        line = script.GetLine<ScriptDialogue>(2);
        if (line.CharacterName != "Fred")
            throw new System.Exception("Dialogue character is incorrect.");
        phrase = line.Phrases[0];
        if (phrase.Text != "Yep! This seems to work okay.")
            throw new System.Exception("Phrase text is incorrect.");
        if (phrase.LocID != "52H-29d-cb6F")
            throw new System.Exception("Phrase locID is incorrect.");

        // Fields
        PlainText text = sf.GetSceneItem<PlainText>("Fsr-8A5-3Wzi");
        string str = text.GetFieldText("example", "field0");
        if (str != "Interesting.")
            throw new System.Exception("Field string value is incorrect.");
        script = sf.GetSceneItem<Script>("fCV-HL2-SVAN");
        str = script.GetFieldText("example", "field0");
        if (str != "Testament")
            throw new System.Exception("Field string value is incorrect.");
        float num = text.GetFieldFloat("fieldset2", "field2");
        if (num != 0.0)
            throw new System.Exception("Field float value is incorrect.");

        // Line Fields
        script = sf.GetSceneItem<Script>("fCV-HL2-SVAN");

        str = script.GetFieldText("example", "field0");
        if (str != "Testament")
            throw new System.Exception("Field string value is incorrect.");
        action = script.GetLine<ScriptAction>(0);
        num = script.GetFieldFloat("scriptAction", "field0");
        if (num != 0.0)
            throw new System.Exception("Field float value is incorrect.");
        num = action.GetFieldFloat("scriptAction", "field0");
        if (num != 10.0)
            throw new System.Exception("Action float value is incorrect.");
        line = script.GetLine<ScriptDialogue>(1);
        bool boolValue = line.GetFieldBool("scriptLine", "field1");
        if (!boolValue)
            throw new System.Exception("Line bool value is incorrect.");
        phrase = line.GetPhrase(0);
        str = phrase.GetFieldText("scriptPhrase", "field0");
        if (str != "Phrase")
            throw new System.Exception("Phrase string value is incorrect.");

        // Locales
        if (sf.CurrentLocale != "en-gb")
            throw new System.Exception("Wrong locale!");
        script = sf.GetSceneItem<Script>("fCV-HL2-SVAN");
        line = script.GetLine<ScriptDialogue>(2);
        phrase = line.Phrases[0];
        if (phrase.Text != "Yep! This seems to work okay.")
            throw new System.Exception("Original text isn't right.");
        sf.SetLocale("en-us");
        if (sf.CurrentLocale != "en-us")
            throw new System.Exception("Wrong changed locale!");
        if (phrase.Text != "Yeehaw! This seems to work okay!")
            throw new System.Exception("Translated text isn't right.");
        phrase = line.Phrases[1];
        if (phrase.Text != "[Missing] Let's have another go.")
            throw new System.Exception("Untranslated text isn't right.");
        sf.SetLocale("en-gb");

        // Case insensitive path
        script = sf.GetSceneItemByPath<Script>("Content.Folder 1.sCENE 1.script 1");
        if (script == null)
            throw new System.Exception("Can't retrieve with case insensitive path.");
        Debug.Log("Test succeeded.");

        // Scene retrieval
        Scene scene = sf.GetSceneItemByPath<Scene>("Content.Folder 1.Scene 1");
        if (scene.GetChildren().Count != 3)
            throw new System.Exception("Wrong number of children in scene 1.");
        scene = sf.GetSceneItem<Scene>("nPd-Qck-DmbM");
        if (scene.GetChildren().Count != 3)
            throw new System.Exception("Wrong number of children in scene 2.");

        // Scene folder
        SceneFolder folder = sf.GetSceneItemByPath<SceneFolder>("Content.Folder 1");
        if (folder.GetChildren().Count != 2)
            throw new System.Exception("Wrong number of children in scene.");

        // Character
        Character character = sf.GetCharacterByGameID("dave");
        if (character.SubtitleName!="Dave")
            throw new System.Exception("Wrong character subtitle name.");

        // Strings
        Strings strings = sf.GetSceneItem<Strings>("LBc-nhJ-dZc6");
        StringEntry entry = strings.GetEntry(1);
        if (entry.Text!="This is the text of option B.")
            throw new System.Exception("Couldn't retrieve string.");
        entry = strings.GetEntryByGameID("option_b");
        sf.SetLocale("en-us");
        if (entry.Text != "Det har ar texten av option B.")
            throw new System.Exception("Couldn't retrieve translated string.");
    }
}
