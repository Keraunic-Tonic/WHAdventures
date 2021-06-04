using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.AdventureCreatorSupport;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/// <summary>
	/// This custom action does a checks the result of a Lua expression in the Dialogue System.
	/// Contributed by Chad Kilgore.
	/// </summary>
    [System.Serializable]
    public class ActionDialogueSystemCheckConversationActive : ActionCheck
    {

		public override ActionCategory Category { get { return ActionCategory.ThirdParty; } }
		public override string Title { get { return "Dialogue System Check Is Conversation Active"; } }
		public override string Description { get { return "Checks if a Dialogue System conversation is active."; } }

		public ActionDialogueSystemCheckConversationActive()
        {
            this.isDisplayed = true;
            category = ActionCategory.ThirdParty;
            title = "Dialogue System Check Is Conversation Active";
            description = "Checks if a Dialogue System conversation is active.";
        }
        
		public override int GetNextOutputIndex()
		{
			return DialogueManager.isConversationActive ? 0 : 1;
		}

	}
}