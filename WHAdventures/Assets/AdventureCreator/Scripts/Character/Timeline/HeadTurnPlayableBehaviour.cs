﻿/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"HeadTurnPlayableBehaviour.cs"
 * 
 *	A PlayableBehaviour used by HeadTurnMixer.
 * 
 */

using UnityEngine;
using UnityEngine.Playables;
#if !ACIgnoreTimeline
using UnityEngine.Timeline;

namespace AC
{

	/**
	 * A PlayableBehaviour used by HeadTurnMixer.
	 */
	internal sealed class HeadTurnPlayableBehaviour : PlayableBehaviour
	{

		#region Variables

		public Transform headTurnTarget;

		#endregion


		#region GetSet

		public bool IsValid
		{
			get
			{
				return headTurnTarget != null;
			}
		}

		#endregion

	}

}
#endif