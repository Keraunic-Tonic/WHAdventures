using UnityEngine;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.AdventureCreatorSupport;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{
	/// <summary>
	/// This custom Adventure Creator action calls AdventureCreatorBridge.SyncAdventureCreatorToLua
	/// or SyncLuaToAdventureCreator.
	/// </summary>
	[System.Serializable]
	public class ActionDialogueSystemSyncLua : Action
	{
		public enum Mode { ACToLua, LuaToAC }

		public Mode mode;

		public override ActionCategory Category { get { return ActionCategory.ThirdParty; } }
		public override string Title { get { return "Dialogue System Sync Lua"; } }
		public override string Description { get { return "Syncs AC data to Dialogue System Lua or Lua to AC."; } }

		public ActionDialogueSystemSyncLua()
		{
			this.isDisplayed = true;
			category = ActionCategory.ThirdParty;
			title = "Dialogue System Sync Lua";
			description = "Syncs AC data to Dialogue System Lua or Lua to AC.";
		}


		override public float Run()
		{
			var bridge = DialogueManager.instance.GetComponent<AdventureCreatorBridge>();
			if (bridge != null)
			{
				switch (mode)
				{
					case Mode.ACToLua:
						bridge.SyncAdventureCreatorToLua();
						break;
					case Mode.LuaToAC:
						bridge.SyncLuaToAdventureCreator();
						break;
				}
            }
			return 0;
		}


#if UNITY_EDITOR

		override public void ShowGUI ()
		{
			// Action-specific Inspector GUI code here
			mode = (Mode)EditorGUILayout.EnumPopup(new GUIContent("Direction:", "Sync Adventure Creator data to Lua or Lua back to Adventure Creator?"), mode);

			AfterRunningOption ();
		}		
		
		public override string SetLabel ()
		{
			return " (" + mode + ")";
		}

#endif

	}

}
