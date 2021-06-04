using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using AC;

namespace PixelCrushers.DialogueSystem.AdventureCreatorSupport
{

    public enum UseDialogState { Never, IfPlayerInvolved, AfterStopIfPlayerInvolved, Always }

    /// <summary>
    /// This component synchronizes Adventure Creator data with Dialogue System data. 
    /// Add it to your Dialogue Manager object. It synchronizes AC's variables with
    /// the Dialogue System's Variable[] Lua table, and AC's inventory with the Dialogue
    /// System's Item[] Lua table, if the sync checkboxes are ticked. Otherwise you can
    /// use the acXXX() Lua functions that this script registers.
    /// 
    /// It also provides methods to save and load the Dialogue System's state to
    /// an AC global variable. You can call these methods when saving and loading games
    /// in AC.
    /// </summary>
    [AddComponentMenu("Pixel Crushers/Dialogue System/Third Party/Adventure Creator/Adventure Creator Bridge")]
    public class AdventureCreatorBridge : MonoBehaviour
    {

        /// <summary>
        /// The name of the AC global variable used to store Dialogue System state.
        /// </summary>
        public static string DialogueSystemGlobalVariableName = "DialogueSystemEnvironment";

        /// <summary>
        /// The AC GameState to use when in conversations.
        /// </summary>
        [Tooltip("The AC GameState to use when in conversations.")]
        public UseDialogState useDialogState = UseDialogState.IfPlayerInvolved;

        /// <summary>
        /// Specifies when conversations should take camera control.
        /// </summary>
        [Tooltip("Specifies when conversations should take camera control.")]
        public UseDialogState takeCameraControl = UseDialogState.IfPlayerInvolved;

        /// <summary>
        /// The max time to wait for the camera stop if takeCameraControl is AfterStopIfPlayerInvolved.
        /// </summary>
        [Tooltip("Max time to wait for camera stop if Take Camera Control is set to After Stop If Player Involved.")]
        public float maxTimeToWaitForCameraStop = 10f;

        [Serializable]
        public class SyncSettings
        {
            [PixelCrushers.HelpBox("Tick these checkboxes if you want to work with AC variables and items through Dialogue System Lua variables. Untick to use dedicated Lua functions.", HelpBoxMessageType.None)]
            [Header("To Dialogue System (On Conversation Start)")]
            [Tooltip("When conversation starts, copy AC variable values into Dialogue System's Lua variable table.")]
            public bool copyACVariablesToDialogueSystem = true;
            [Tooltip("When conversation starts, copy AC item counts into Dialogue System's Lua item table.")]
            public bool copyACItemCountsToDialogueSystem = true;

            [Header("Back To AC (On Conversation End)")]
            [Tooltip("When conversation ends, copy Dialogue System's Lua variable values into AC variables. (Will overwrite AC variable values.)")]
            public bool copyDialogueSystemToACVariables = true;
            [Tooltip("When conversation ends, copy Dialogue System's Lua item table into AC item counts. (Will overwrite AC item counts.)")]
            public bool copyDialogueSystemToACItemCounts = true;
        }

        public SyncSettings syncSettings = new SyncSettings();

        /// <summary>
        /// Set <c>true</c> to include dialogue entry status (offered and/or spoken) in save data.
        /// </summary>
        [Tooltip("Include dialogue entry status in save data (increases size).")]
        public bool includeSimStatus = false;

        /// <summary>
        /// Set <c>true</c> to prepend 'global_' in front of global AC variables in Dialogue System's Variable[] table.
        /// </summary>
        [Tooltip("Prepend 'global_' in front of global AC variables in Dialogue System's Variable[] table.")]
        public bool prependGlobalVariables = false;

        /// <summary>
        /// Set <c>true</c> to save the Lua environment to the AC global variable when
        /// conversations end.
        /// </summary>
        [Tooltip("Save the Lua environment to Adventure Creator when conversations end.")]
        public bool saveToGlobalVariableOnConversationEnd = false;

        /// <summary>
        /// Set <c>true</c> to resolve race conditions between AC() sequencer command and 
        /// Lua Script on subsequent dialogue entry.
        /// </summary>
        [Tooltip("Tick to resolve race conditions between AC() sequencer command and Lua Script on subsequent dialogue entry.")]
        [HideInInspector] // No longer used.
        public bool rerunScriptAfterCutscenes = false;

        /// <summary>
        /// Set this <c>true</c> to skip the next sync to Lua. The Conversation action sets
        /// this <c>true</c> because it manually syncs to Lua before the conversation starts.
        /// </summary>
        /// <value><c>true</c> to skip sync to lua; otherwise, <c>false</c>.</value>
        public bool skipSyncToLua { get; set; }

        /// <summary>
        /// Set the Dialogue System's localization according to Adventure Creator's current language.
        /// </summary>
        [Tooltip("Set the Dialogue System's localization according to Adventure Creator's current language.")]
        public bool useAdventureCreatorLanguage = true;

        /// <summary>
        /// Set the Dialogue System's localization according to Adventure Creator's current subtitle settings.
        /// </summary>
        [Tooltip("Set the Dialogue System's Show Subtitles checkboxes to Adventure Creator's Subtitles setting.")]
        public bool useAdventureCreatorSubtitlesSetting = false;

        [Tooltip("Log extra debug info.")]
        public bool debug = false;

        // Used to override the bridge's settings temporarily:
        [HideInInspector]
        public bool overrideBridge = false;
        [HideInInspector]
        public UseDialogState overrideUseDialogState = UseDialogState.IfPlayerInvolved;
        [HideInInspector]
        public UseDialogState overrideTakeCameraControl = UseDialogState.IfPlayerInvolved;
        public UseDialogState activeUseDialogState { get { return overrideBridge ? overrideUseDialogState : useDialogState; } }
        public UseDialogState activeTakeCameraControl { get { return overrideBridge ? overrideTakeCameraControl : takeCameraControl; } }

        private bool isPlayerInvolved = false;
        private DisplaySettings.SubtitleSettings originalSubtitleSettings = null;
        private string currentAdventureCreatorLanguage = string.Empty;
        private bool currentAdventureCreatorSubtitles = false;
        private AC.Conversation dummyConversation;

        private static bool areLuaFunctionsRegistered = false;

        private const float MovementThreshold = 0.1f; // Camera is "stopped" if it moves less than 0.1 units in 0.5 seconds.

        private void Awake()
        {
            if (!areLuaFunctionsRegistered)
            {
                areLuaFunctionsRegistered = true;
                Lua.RegisterFunction("SyncACToLua", this, SymbolExtensions.GetMethodInfo(() => SyncAdventureCreatorToLua()));
                Lua.RegisterFunction("SyncLuaToAC", this, SymbolExtensions.GetMethodInfo(() => SyncLuaToAdventureCreator()));
                Lua.RegisterFunction("acGetBoolean", null, SymbolExtensions.GetMethodInfo(() => acGetBoolean(string.Empty)));
                Lua.RegisterFunction("acGetInteger", null, SymbolExtensions.GetMethodInfo(() => acGetInteger(string.Empty)));
                Lua.RegisterFunction("acGetFloat", null, SymbolExtensions.GetMethodInfo(() => acGetFloat(string.Empty)));
                Lua.RegisterFunction("acGetText", null, SymbolExtensions.GetMethodInfo(() => acGetText(string.Empty)));
                Lua.RegisterFunction("acSetBoolean", null, SymbolExtensions.GetMethodInfo(() => acSetBoolean(string.Empty, false)));
                Lua.RegisterFunction("acSetInteger", null, SymbolExtensions.GetMethodInfo(() => acSetInteger(string.Empty, (double)0)));
                Lua.RegisterFunction("acSetFloat", null, SymbolExtensions.GetMethodInfo(() => acSetFloat(string.Empty, (double)0)));
                Lua.RegisterFunction("acSetText", null, SymbolExtensions.GetMethodInfo(() => acSetText(string.Empty, string.Empty)));
                Lua.RegisterFunction("acGetItemCount", null, SymbolExtensions.GetMethodInfo(() => acGetItemCount(string.Empty)));
                Lua.RegisterFunction("acSetItemCount", null, SymbolExtensions.GetMethodInfo(() => acSetItemCount(string.Empty, (double)0)));
                Lua.RegisterFunction("acIncItemCount", null, SymbolExtensions.GetMethodInfo(() => acIncItemCount(string.Empty, (double)0)));
            }
        }

        public virtual void Start()
        {
            PersistentDataManager.includeSimStatus = includeSimStatus;
            skipSyncToLua = false;
            dummyConversation = gameObject.AddComponent<AC.Conversation>();
            SaveOriginalSettings();
            UpdateSettingsFromAC();
        }

        /// <summary>
        /// Prepares to run a conversation by freezing AC and syncing data to Lua.
        /// </summary>
        /// <param name="actor">The other actor.</param>
        public virtual void OnConversationStart(Transform actor)
        {
            CheckACSettings();
            CheckIfPlayerIsInvolved(actor);
            if (!skipSyncToLua) SyncAdventureCreatorToLua();
            skipSyncToLua = false;
            SetConversationGameState();
        }

        /// <summary>
        /// At the end of a conversation, unfreezes AC and syncs Lua back to AC.
        /// </summary>
        /// <param name="actor">Actor.</param>
        public virtual void OnConversationEnd(Transform actor)
        {
            UnsetConversationGameState();
            SyncLuaToAdventureCreator();
            if (saveToGlobalVariableOnConversationEnd) SaveDialogueSystemToGlobalVariable();
        }

        private void CheckIfPlayerIsInvolved(Transform actor)
        {
            if (actor == null)
            {
                isPlayerInvolved = false;
            }
            else if (actor.GetComponentInChildren<Player>() != null)
            {
                isPlayerInvolved = true;
            }
            else
            {
                Actor dbActor = DialogueManager.MasterDatabase.GetActor(OverrideActorName.GetActorName(actor));
                isPlayerInvolved = (dbActor != null) && dbActor.IsPlayer;
            }
        }

        /// <summary>
        /// Sets GameState to DialogOptions if specified in the properties.
        /// </summary>
        public virtual void SetConversationGameState()
        {
            switch (activeUseDialogState)
            {
                case UseDialogState.Never:
                    break;
                case UseDialogState.IfPlayerInvolved:
                case UseDialogState.AfterStopIfPlayerInvolved:
                    if (isPlayerInvolved) SetGameStateToCutscene();
                    break;
                case UseDialogState.Always:
                    SetGameStateToCutscene();
                    break;
            }
            switch (activeTakeCameraControl)
            {
                case UseDialogState.Never:
                    break;
                case UseDialogState.IfPlayerInvolved:
                    if (isPlayerInvolved) DisableACCameraControl();
                    break;
                case UseDialogState.AfterStopIfPlayerInvolved:
                    if (isPlayerInvolved) IdleACCameraControl();
                    break;
                case UseDialogState.Always:
                    DisableACCameraControl();
                    break;
            }
        }

        /// <summary>
        /// Restores the previous GameState if necessary.
        /// </summary>
        public virtual void UnsetConversationGameState()
        {
            switch (activeUseDialogState)
            {
                case UseDialogState.Never:
                    break;
                case UseDialogState.IfPlayerInvolved:
                    if (isPlayerInvolved) RestorePreviousGameState();
                    break;
                case UseDialogState.Always:
                    RestorePreviousGameState();
                    break;
            }
            switch (activeTakeCameraControl)
            {
                case UseDialogState.Never:
                    break;
                case UseDialogState.IfPlayerInvolved:
                    if (isPlayerInvolved) EnableACCameraControl();
                    break;
                case UseDialogState.Always:
                    EnableACCameraControl();
                    break;
            }
        }

        /// <summary>
        /// Sets AC's GameState to DialogOptions.
        /// </summary>
        public void SetGameStateToCutscene()
        {
            if (KickStarter.stateHandler == null) return;
            //previousGameState = (KickStarter.stateHandler.gameState == GameState.DialogOptions) ? GameState.Normal : KickStarter.stateHandler.gameState;
            KickStarter.playerInput.PendingOptionConversation = dummyConversation;
            if (DialogueManager.IsConversationActive) SetConversationCursor();

        }

        public void RestorePreviousGameState()
        {
            if (KickStarter.stateHandler == null) return;
            KickStarter.playerInput.PendingOptionConversation = null;
            RestorePreviousCursor();
        }

        public void DisableACCameraControl()
        {
            if (KickStarter.stateHandler == null) return;
            KickStarter.stateHandler.SetCameraSystem(false);
        }

        public void EnableACCameraControl()
        {
            if (KickStarter.stateHandler == null) return;
            KickStarter.stateHandler.SetCameraSystem(true);
        }

        public void IdleACCameraControl()
        {
            StartCoroutine(WaitForCameraToStop());
        }

        private IEnumerator WaitForCameraToStop()
        {
            var cam = Camera.main;
            if (cam == null) yield break;
            var maxTime = Time.time + maxTimeToWaitForCameraStop;
            var lastPosition = cam.transform.position;
            while ((Vector3.Distance(cam.transform.position, lastPosition) < MovementThreshold) && (Time.time < maxTime))
            {
                lastPosition = cam.transform.position;
                yield return new WaitForSeconds(0.5f);

            }
            DisableACCameraControl();
        }

        /// <summary>
        /// Sets the conversation cursor.
        /// </summary>
        public void SetConversationCursor()
        {
            if (!isEnforcingCursor) StartCoroutine(EnforceCursor());
        }

        /// <summary>
        /// Restores the previous cursor.
        /// </summary>
        public void RestorePreviousCursor()
        {
            stopEnforcingCursor = true;
        }

        private bool isEnforcingCursor = false;
        private bool stopEnforcingCursor = false;
        private Texture previousWaitIcon;
        private float previousWaitIconSize;
        private Vector2 previousWaitIconClickOffset;

        private IEnumerator EnforceCursor()
        {
            if (isEnforcingCursor || KickStarter.cursorManager == null) yield break;
            if (!(isPlayerInvolved || activeUseDialogState == UseDialogState.Always)) yield break;
            isEnforcingCursor = true;
            stopEnforcingCursor = false;
            previousWaitIcon = KickStarter.cursorManager.waitIcon.texture;
            previousWaitIconSize = KickStarter.cursorManager.waitIcon.size;
            previousWaitIconClickOffset = KickStarter.cursorManager.waitIcon.clickOffset;
            KickStarter.cursorManager.waitIcon.texture = KickStarter.cursorManager.pointerIcon.texture;
            KickStarter.cursorManager.waitIcon.size = KickStarter.cursorManager.pointerIcon.size;
            KickStarter.cursorManager.waitIcon.clickOffset = KickStarter.cursorManager.pointerIcon.clickOffset;
            var previousCursorDisplay = KickStarter.cursorManager.cursorDisplay;
            var endOfFrame = new WaitForEndOfFrame();
            while (!stopEnforcingCursor)
            {
                KickStarter.cursorManager.cursorDisplay = CursorDisplay.Always;
                KickStarter.playerInput.SetInGameCursorState(false);
                yield return endOfFrame;
            }
            KickStarter.cursorManager.cursorDisplay = previousCursorDisplay;
            KickStarter.cursorManager.waitIcon.texture = previousWaitIcon;
            KickStarter.cursorManager.waitIcon.size = previousWaitIconSize;
            KickStarter.cursorManager.waitIcon.clickOffset = previousWaitIconClickOffset;
            isEnforcingCursor = false;
        }

        /// <summary>
        /// Syncs the AC data to Lua.
        /// </summary>
        public virtual void SyncAdventureCreatorToLua()
        {
            if (syncSettings.copyACVariablesToDialogueSystem) SyncVariablesToLua();
            if (syncSettings.copyACItemCountsToDialogueSystem) SyncInventoryToLua();
        }

        /// <summary>
        /// Syncs Lua back to AC data.
        /// </summary>
        public virtual void SyncLuaToAdventureCreator()
        {
            if (syncSettings.copyDialogueSystemToACVariables) SyncLuaToVariables();
            if (syncSettings.copyDialogueSystemToACItemCounts) SyncLuaToInventory();
        }

        public void SyncVariablesToLua()
        {
            if (AC.KickStarter.runtimeVariables != null) SyncVarListToLua(AC.KickStarter.runtimeVariables.globalVars, true);
            if (AC.KickStarter.localVariables != null) SyncVarListToLua(AC.KickStarter.localVariables.localVars, false);
        }

        private void SyncVarListToLua(List<GVar> varList, bool global)
        {
            foreach (var variable in varList)
            {
                if (!string.Equals(variable.label, DialogueSystemGlobalVariableName))
                {
                    string luaName = DialogueLua.StringToTableIndex(variable.label);
                    if (global && prependGlobalVariables) luaName = "global_" + luaName;
                    switch (variable.type)
                    {
                        case VariableType.Boolean:
                            bool boolValue = variable.BooleanValue;
                            DialogueLua.SetVariable(luaName, boolValue);
                            break;
                        case VariableType.Integer:
                        case VariableType.PopUp:
                            DialogueLua.SetVariable(luaName, variable.IntegerValue);
                            break;
                        case VariableType.Float:
                            DialogueLua.SetVariable(luaName, variable.FloatValue);
                            break;
                        case VariableType.String:
                            DialogueLua.SetVariable(luaName, variable.TextValue);
                            break;
                        default:
                            if (DialogueDebug.LogWarnings) Debug.LogWarning("Dialogue System: AdventureCreatorBridge doesn't know how to sync variable type " + variable.type, this);
                            break;
                    }
                }
            }
        }

        public void SyncLuaToVariables()
        {
            SyncLuaToVarList(AC.KickStarter.runtimeVariables.globalVars, true);
            SyncLuaToVarList(AC.KickStarter.localVariables.localVars, false);
        }

        private void SyncLuaToVarList(List<GVar> varList, bool global)
        {
            foreach (var variable in varList)
            {
                string luaName = DialogueLua.StringToTableIndex(variable.label);
                if (global && prependGlobalVariables) luaName = "global_" + luaName;
                var luaValue = DialogueLua.GetVariable(luaName);
                switch (variable.type)
                {
                    case VariableType.Boolean:
                        variable.BooleanValue = luaValue.asBool;
                        break;
                    case VariableType.Integer:
                    case VariableType.PopUp:
                        variable.IntegerValue = luaValue.asInt;
                        break;
                    case VariableType.Float:
                        variable.FloatValue = luaValue.asFloat;
                        break;
                    case VariableType.String:
                        variable.TextValue = luaValue.asString;
                        break;
                    default:
                        if (DialogueDebug.LogWarnings) Debug.LogWarning("Dialogue System: AdventureCreatorBridge doesn't know how to sync variable type " + variable.type, this);
                        break;
                }
            }
        }

        /// <summary>
        /// Syncs AC's inventory to Lua.
        /// </summary>
        public void SyncInventoryToLua()
        {
            var inventoryManager = KickStarter.inventoryManager;
            var runtimeInventory = KickStarter.runtimeInventory;
            if (inventoryManager == null || runtimeInventory == null) return;
            foreach (var item in inventoryManager.items)
            {
                string luaName = DialogueLua.StringToTableIndex(item.label);
                var runtimeItemInstance = runtimeInventory.GetInstance(item.id);
                int runtimeCount = (runtimeItemInstance != null) ? runtimeItemInstance.Count : 0;
                Lua.Run(string.Format("Item[\"{0}\"] = {{ Name=\"{1}\", Description=\"\", Is_Item=true, AC_ID={2}, Count={3} }}",
                                      luaName, item.label, item.id, runtimeCount), debug || DialogueDebug.LogInfo);
            }
        }

        /// <summary>
        /// Syncs Lua to AC's inventory.
        /// </summary>
        public void SyncLuaToInventory()
        {
            LuaTableWrapper luaItemTable = Lua.Run("return Item").AsTable;
            if (luaItemTable == null) return;
            foreach (var luaItem in luaItemTable.Values)
            {
                LuaTableWrapper fields = luaItem as LuaTableWrapper;
                if (fields != null)
                {
                    foreach (var fieldNameObject in fields.Keys)
                    {
                        string fieldName = fieldNameObject as string;
                        if (string.Equals(fieldName, "AC_ID"))
                        {
                            try
                            {
                                if (debug) Debug.Log("Dialogue System: AdventureCreatorBridge.SyncLuaToInventory");

                                // Get Name:
                                object o = fields["Name"];
                                bool valid = (o != null) && (o.GetType() == typeof(string));
                                string itemName = valid ? (string)fields["Name"] : string.Empty;
                                if (debug) Debug.Log("Dialogue System: AdventureCreatorBridge.SyncLuaToInventory: Name=" + itemName);

                                // Get AC_ID:
                                o = fields["AC_ID"];
                                valid = valid && (o != null) && (o.GetType() == typeof(double) || o.GetType() == typeof(float));
                                double value = (o.GetType() == typeof(double)) ? (double)fields["AC_ID"] : (float)fields["AC_ID"];
                                int itemID = valid ? ((int)value) : 0;
                                if (debug) Debug.Log("Dialogue System: AdventureCreatorBridge.SyncLuaToInventory: AC ID=" + itemID);

                                // Get Count:
                                o = fields["Count"];
                                valid = valid && (o != null) && (o.GetType() == typeof(double) || o.GetType() == typeof(float));
                                value = (o.GetType() == typeof(double)) ? (double)fields["Count"] : (float)fields["Count"];
                                int newCount = valid ? ((int)value) : 0;
                                if (debug) Debug.Log("Dialogue System: AdventureCreatorBridge.SyncLuaToInventory: Count=" + newCount);

                                if (valid) UpdateAdventureCreatorItem(itemName, itemID, newCount);
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogError(e.Message);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates the count of an item in AC's inventory.
        /// </summary>
        /// <param name="itemName">Item name.</param>
        /// <param name="itemID">Item ID.</param>
        /// <param name="newCount">New count.</param>
        protected void UpdateAdventureCreatorItem(string itemName, int itemID, int newCount)
        {
            if (debug) Debug.Log("Dialogue System: AdventureCreatorBridge.UpdateAdventureCreatorItem: Name=" + itemName + ", ID=" + itemID + ", Count=" + newCount);
            var runtimeInventory = KickStarter.runtimeInventory;
            if (runtimeInventory == null)
            {
                if (debug) Debug.Log("Dialogue System: AdventureCreatorBridge.UpdateAdventureCreatorItem: runtimeInventory is null");
                return;
            }
            var playerID = (KickStarter.player != null) ? KickStarter.player.ID : -1;
            var itemInstance = runtimeInventory.GetInstance(itemID);
            if (itemInstance == null)
            {
                if (newCount > 0)
                {
                    if (debug || DialogueDebug.LogInfo) Debug.Log(string.Format("{0}: Adding new {1} {2} to inventory", DialogueDebug.Prefix, newCount, itemName));
                    runtimeInventory.Add(itemID, newCount, false, playerID);
                }
            }
            else if (newCount > itemInstance.Count)
            {
                int amountToAdd = newCount - itemInstance.Count;
                if (debug || DialogueDebug.LogInfo) Debug.Log(string.Format("{0}: Adding {1} {2} to inventory", DialogueDebug.Prefix, amountToAdd, itemName));
                runtimeInventory.Add(itemInstance.ItemID, amountToAdd, false, playerID);
            }
            else if (newCount < itemInstance.Count)
            {
                int amountToRemove = itemInstance.Count - newCount;
                if (debug || DialogueDebug.LogInfo) Debug.Log(string.Format("{0}: Removing {1} {2} from inventory", DialogueDebug.Prefix, amountToRemove, itemName));
                runtimeInventory.Remove(itemInstance.ItemID, amountToRemove);
            }
        }

        /// <summary>
        /// Saves the Dialogue System state to a dedicated AC global variable. This method
        /// will create the global variable if it doesn't already exist.
        /// </summary>
        public static void SaveDialogueSystemToGlobalVariable()
        {
            string data = (FindObjectOfType<PixelCrushers.SaveSystem>() != null)
                ? PixelCrushers.SaveSystem.Serialize(PixelCrushers.SaveSystem.RecordSavedGameData())
                : PersistentDataManager.GetSaveData();
            GlobalVariables.SetStringValue(GetDialogueSystemVarID(), data);
        }

        /// <summary>
        /// Loads the Dialogue System state from a dedicated AC global variable.
        /// </summary>
        public static void LoadDialogueSystemFromGlobalVariable()
        {
            var data = GlobalVariables.GetStringValue(GetDialogueSystemVarID());
            if (FindObjectOfType<PixelCrushers.SaveSystem>() != null)
            {
                PixelCrushers.SaveSystem.ApplySavedGameData(PixelCrushers.SaveSystem.Deserialize<SavedGameData>(data));
            }
            else
            {
                PersistentDataManager.ApplySaveData(data);
            }
        }

        /// <summary>
        /// Gets the ID of the DialogueSystemEnvironment AC variable. If the variable hasn't been defined
        /// in AC yet, this method also creates the variable.
        /// </summary>
        /// <returns>The DialogueSystemEnvironment variable ID.</returns>
        private static int GetDialogueSystemVarID()
        {
            var variablesManager = KickStarter.variablesManager;
            if (variablesManager == null) return 0;
            List<GVar> globalVarList = GlobalVariables.GetAllVars();
            foreach (GVar var in globalVarList)
            {
                if (string.Equals(var.label, DialogueSystemGlobalVariableName)) return var.id;
            }
            GVar newVar = new GVar(GetVarIDArray(variablesManager));
            newVar.label = DialogueSystemGlobalVariableName;
            newVar.type = VariableType.String;
            variablesManager.vars.Add(newVar);
            globalVarList.Add(newVar);
            return newVar.id;
        }

        /// <summary>
        /// Gets the variable ID array. To add a new variable, AC needs a reference to the 
        /// current IDs. This generates the list of current IDs.
        /// </summary>
        /// <returns>The variable ID array.</returns>
        /// <param name="variablesManager">Variables manager.</param>
        private static int[] GetVarIDArray(VariablesManager variablesManager)
        {
            List<int> idArray = new List<int>();
            foreach (GVar var in GlobalVariables.GetAllVars())
            {
                idArray.Add(var.id);
            }
            idArray.Sort();
            return idArray.ToArray();
        }

        private static string GetACLanguageName()
        {
            return (AC.Options.GetLanguage() > 0) ? AC.Options.GetLanguageName() : string.Empty;
        }

        public static void UpdateSettingsFromAC()
        {
            var bridge = FindObjectOfType<AdventureCreatorBridge>();
            if (bridge == null) return;
            bridge.StartCoroutine(bridge.DelayedUpdateSettingsFromAC());
        }

        /// <summary>
        /// Waits one frame, then updates the Dialogue System's settings from AC as specified
        /// by the useAdventureCreatorXXX bools. We need to wait one frame because AC calls the 
        /// save/load options hooks before setting the language.
        /// </summary>
        private IEnumerator DelayedUpdateSettingsFromAC()
        {
            yield return null;
            UpdateSettingsFromACNow();
        }

        private void UpdateSettingsFromACNow()
        {
            if (useAdventureCreatorLanguage)
            {
                DialogueManager.SetLanguage(GetACLanguageName());
            }
            if (useAdventureCreatorSubtitlesSetting && originalSubtitleSettings != null)
            {
                var acSubtitles = AC.Options.optionsData.showSubtitles;
                var subtitleSettings = DialogueManager.DisplaySettings.subtitleSettings;
                subtitleSettings.showNPCSubtitlesDuringLine = acSubtitles && originalSubtitleSettings.showNPCSubtitlesDuringLine;
                subtitleSettings.showNPCSubtitlesWithResponses = acSubtitles && originalSubtitleSettings.showNPCSubtitlesWithResponses;
                subtitleSettings.showPCSubtitlesDuringLine = acSubtitles && originalSubtitleSettings.showPCSubtitlesDuringLine;
            }
        }

        private void SaveOriginalSettings()
        {
            currentAdventureCreatorLanguage = GetACLanguageName();
            currentAdventureCreatorSubtitles = AC.Options.optionsData.showSubtitles;
            var subtitleSettings = DialogueManager.DisplaySettings.subtitleSettings;
            originalSubtitleSettings = new DisplaySettings.SubtitleSettings();
            originalSubtitleSettings.showNPCSubtitlesDuringLine = subtitleSettings.showNPCSubtitlesDuringLine;
            originalSubtitleSettings.showNPCSubtitlesWithResponses = subtitleSettings.showNPCSubtitlesWithResponses;
            originalSubtitleSettings.showPCSubtitlesDuringLine = subtitleSettings.showPCSubtitlesDuringLine;
        }

        private void CheckACSettings()
        {
            if (useAdventureCreatorLanguage || useAdventureCreatorSubtitlesSetting)
            {
                var acLanguageName = GetACLanguageName();
                if (!(string.Equals(currentAdventureCreatorLanguage, acLanguageName) && (currentAdventureCreatorSubtitles == AC.Options.optionsData.showSubtitles)))
                {
                    UpdateSettingsFromACNow();
                    currentAdventureCreatorLanguage = acLanguageName;
                    currentAdventureCreatorSubtitles = AC.Options.optionsData.showSubtitles;
                }
            }
        }

        #region Lua Functions

        protected static GVar GetACVariable(string variableName)
        {
            if (KickStarter.localVariables == null || KickStarter.runtimeVariables == null) return null;
            return GetACVariableFromList(AC.KickStarter.localVariables.localVars, variableName) ??
                GetACVariableFromList(AC.KickStarter.runtimeVariables.globalVars, variableName);
        }

        protected static GVar GetACVariableFromList(List<GVar> varList, string variableName)
        {
            if (varList == null) return null;
            return varList.Find(x => x.label == variableName);
        }

        protected static bool acGetBoolean(string variableName)
        {
            var gvar = GetACVariable(variableName);
            return (gvar != null) ? gvar.BooleanValue : false;
        }

        protected static double acGetInteger(string variableName)
        {
            var gvar = GetACVariable(variableName);
            return (gvar != null) ? gvar.IntegerValue : 0;
        }

        protected static double acGetFloat(string variableName)
        {
            var gvar = GetACVariable(variableName);
            return (gvar != null) ? gvar.FloatValue : 0;
        }

        protected static string acGetText(string variableName)
        {
            var gvar = GetACVariable(variableName);
            return (gvar != null) ? gvar.TextValue : string.Empty;
        }

        protected static void  acSetBoolean(string variableName, bool value)
        {
            var gvar = GetACVariable(variableName);
            if (gvar != null) gvar.BooleanValue = value;
        }

        protected static void acSetInteger(string variableName, double value)
        {
            var gvar = GetACVariable(variableName);
            if (gvar != null) gvar.IntegerValue = (int)value;
        }

        protected static void acSetFloat(string variableName, double value)
        {
            var gvar = GetACVariable(variableName);
            if (gvar != null) gvar.FloatValue = (float)value;
        }

        protected static void acSetText(string variableName, string value)
        {
            var gvar = GetACVariable(variableName);
            if (gvar != null) gvar.TextValue = value;
        }

        protected static double acGetItemCount(string itemName)
        {
            var inventoryManager = KickStarter.inventoryManager;
            var runtimeInventory = KickStarter.runtimeInventory;
            if (inventoryManager == null || runtimeInventory == null) return 0;
            foreach (var item in inventoryManager.items)
            {
                if (item.label == itemName)
                {
                    var runtimeItemInstance = runtimeInventory.GetInstance(item.id);
                    return (runtimeItemInstance != null) ? runtimeItemInstance.Count : 0;
                }
            }
            return 0;
        }

        protected static void acSetItemCount(string itemName, double value)
        {
            var inventoryManager = KickStarter.inventoryManager;
            var runtimeInventory = KickStarter.runtimeInventory;
            if (inventoryManager == null || runtimeInventory == null) return;
            var newCount = (int)value;
            foreach (var item in inventoryManager.items)
            {
                if (item.label == itemName)
                {
                    var itemID = item.id;
                    var playerID = (KickStarter.player != null) ? KickStarter.player.ID : -1;
                    var itemInstance = runtimeInventory.GetInstance(itemID);
                    if (itemInstance == null)
                    {
                        if (newCount > 0)
                        {
                            runtimeInventory.Add(itemID, newCount, false, playerID);
                        }
                    }
                    else if (newCount > itemInstance.Count)
                    {
                        int amountToAdd = newCount - itemInstance.Count;
                        runtimeInventory.Add(itemInstance.ItemID, amountToAdd, false, playerID);
                    }
                    else if (newCount < itemInstance.Count)
                    {
                        int amountToRemove = itemInstance.Count - newCount;
                        runtimeInventory.Remove(itemInstance.ItemID, amountToRemove);
                    }
                }
            }
        }

        protected static void acIncItemCount(string itemName, double value)
        {
            acSetItemCount(itemName, acGetItemCount(itemName) + value);
        }

        #endregion

    }
}
