/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"KickStarter.cs"
 * 
 *	This script will make sure that PersistentEngine and the Player gameObjects are always created,
 *	regardless of which scene the game is begun from.  It will also check the key gameObjects for
 *	essential scripts and references.
 * 
 */

using System.Collections.Generic;
using UnityEngine;

namespace AC
{
	
	/**
	 * This component instantiates the PersistentEngine and Player prefabs when the game beings.
	 * It also provides static references to each of Adventure Creator's main components.
	 * It should be attached to the GameEngine prefab.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_kick_starter.html")]
	public class KickStarter : MonoBehaviour
	{
		
		private static Player playerPrefab = null;
		private static MainCamera mainCameraPrefab = null;
		private static Camera cameraMain = null;
		private static GameObject persistentEnginePrefab = null;
		private static GameObject gameEnginePrefab = null;
		
		// Managers
		private static SceneManager sceneManagerPrefab = null;
		private static SettingsManager settingsManagerPrefab = null;
		private static ActionsManager actionsManagerPrefab = null;
		private static VariablesManager variablesManagerPrefab = null;
		private static InventoryManager inventoryManagerPrefab = null;
		private static SpeechManager speechManagerPrefab = null;
		private static CursorManager cursorManagerPrefab = null;
		private static MenuManager menuManagerPrefab = null;
		
		// PersistentEngine components
		private static Options optionsComponent = null;
		private static RuntimeInventory runtimeInventoryComponent = null;
		private static RuntimeVariables runtimeVariablesComponent = null;
		private static PlayerMenus playerMenusComponent = null;
		private static StateHandler stateHandlerComponent = null;
		private static SceneChanger sceneChangerComponent = null;
		private static SaveSystem saveSystemComponent = null;
		private static LevelStorage levelStorageComponent = null;
		private static RuntimeLanguages runtimeLanguagesComponent = null;
		private static RuntimeDocuments runtimeDocumentsComponent = null;
		private static RuntimeObjectives runtimeObjectivesComponent = null;
		private static ActionListAssetManager actionListAssetManagerComponent = null;
		
		// GameEngine components
		private static MenuSystem menuSystemComponent = null;
		private static Dialog dialogComponent = null;
		private static PlayerInput playerInputComponent = null;
		private static PlayerInteraction playerInteractionComponent = null;
		private static PlayerMovement playerMovementComponent = null;
		private static PlayerCursor playerCursorComponent = null;
		private static PlayerQTE playerQTEComponent = null;
		private static SceneSettings sceneSettingsComponent = null;
		private static NavigationManager navigationManagerComponent = null;
		private static ActionListManager actionListManagerComponent = null;
		private static LocalVariables localVariablesComponent = null;
		private static MenuPreview menuPreviewComponent = null;
		private static EventManager eventManagerComponent = null;
		private static KickStarter kickStarterComponent = null;


		protected void Awake ()
		{
			if (GetComponent <MultiSceneChecker>() == null)
			{
				ACDebug.LogError ("A 'MultiSceneChecker' component must be attached to the GameEngine prefab - please re-import AC.", gameObject);
			}
		}


		public static void SetGameEngine (GameObject _gameEngine = null)
		{
			if (_gameEngine != null)
			{
				gameEnginePrefab = _gameEngine;

				menuSystemComponent = null;
				playerCursorComponent = null;
				playerInputComponent = null;
				playerInteractionComponent = null;
				playerMovementComponent = null;
				playerMenusComponent = null;
				playerQTEComponent = null;
				kickStarterComponent = null;
				sceneSettingsComponent = null;
				dialogComponent = null;
				menuPreviewComponent = null;
				navigationManagerComponent = null;
				actionListManagerComponent = null;
				localVariablesComponent = null;
				eventManagerComponent = null;

				return;
			}

			if (gameEnginePrefab == null)
			{
				SceneSettings sceneSettings = UnityVersionHandler.GetKickStarterComponent <SceneSettings>();
				if (sceneSettings != null)
				{
					gameEnginePrefab = sceneSettings.gameObject;
				}
			}
		}


		private static bool SetPersistentEngine ()
		{
			if (persistentEnginePrefab == null)
			{
				StateHandler stateHandler = UnityVersionHandler.GetKickStarterComponent <StateHandler>();
				
				if (stateHandler != null)
				{
					persistentEnginePrefab = stateHandler.gameObject;
				}
				else
				{
					GameObject newPersistentEngine = null;

					try
					{
						newPersistentEngine = (GameObject) Instantiate (Resources.Load (Resource.persistentEngine));
						newPersistentEngine.name = AdvGame.GetName (Resource.persistentEngine);
					}
					catch (System.Exception e)
		 			{
						ACDebug.LogWarning ("Could not create PersistentEngine - make sure " + Resource.persistentEngine + ", prefab is present in a Resources folder. Exception: " + e);
		 			}

		 			if (newPersistentEngine != null)
		 			{
						#if UNITY_EDITOR
						if (!TestPersistentEngine (newPersistentEngine))
						{
							return false;
						}
						#endif

						persistentEnginePrefab = newPersistentEngine;

						stateHandler = persistentEnginePrefab.GetComponent <StateHandler>();
						stateHandler.Initialise ();
						return true;
					}
				}
			}

			if (stateHandler != null)
			{
				stateHandler.RegisterInitialConstantIDs ();
			}
			return true;
		}



		protected void CheckRequiredManagerPackage (ManagerPackage requiredManagerPackage)
		{
			if (requiredManagerPackage == null)
			{
				return;
			}

			#if UNITY_EDITOR

			if ((requiredManagerPackage.sceneManager != null && requiredManagerPackage.sceneManager != KickStarter.sceneManager) ||
				(requiredManagerPackage.settingsManager != null && requiredManagerPackage.settingsManager != KickStarter.settingsManager) ||
				(requiredManagerPackage.actionsManager != null && requiredManagerPackage.actionsManager != KickStarter.actionsManager) ||
				(requiredManagerPackage.variablesManager != null && requiredManagerPackage.variablesManager != KickStarter.variablesManager) ||
				(requiredManagerPackage.inventoryManager != null && requiredManagerPackage.inventoryManager != KickStarter.inventoryManager) ||
				(requiredManagerPackage.speechManager != null && requiredManagerPackage.speechManager != KickStarter.speechManager) ||
				(requiredManagerPackage.cursorManager != null && requiredManagerPackage.cursorManager != KickStarter.cursorManager) ||
				(requiredManagerPackage.menuManager != null && requiredManagerPackage.menuManager != KickStarter.menuManager))
			{
				if (requiredManagerPackage.settingsManager != null)
				{
					if (requiredManagerPackage.settingsManager.name == "Demo_SettingsManager" && UnityVersionHandler.GetCurrentSceneName () == "Basement")
					{
						ACDebug.LogWarning ("The demo scene's required Manager asset files are not all loaded - please stop the game, and choose 'Adventure Creator -> Getting started -> Load 3D Demo managers from the top toolbar, and re-load the scene.", requiredManagerPackage);
						return;
					}
					else if (requiredManagerPackage.settingsManager.name == "Demo2D_SettingsManager" && UnityVersionHandler.GetCurrentSceneName () == "Park")
					{
						ACDebug.LogWarning ("The 2D demo scene's required Manager asset files are not all loaded - please stop the game, and choose 'Adventure Creator -> Getting started -> Load 2D Demo managers from the top toolbar, and re-load the scene.", requiredManagerPackage);
						return;
					}
				}

				ACDebug.LogWarning ("This scene's required Manager asset files are not all loaded - please find the asset file '" + requiredManagerPackage.name + "' and click 'Assign managers' in its Inspector.", requiredManagerPackage);
			}

			#endif

		}


		#if UNITY_EDITOR

		private static bool TestPersistentEngine (GameObject _persistentEngine)
		{
			bool testResult = true;

			if (_persistentEngine == null)
			{
				ACDebug.LogError ("No PersistentEngine found - please place one in the Resources directory");
				testResult = false;
			}
			else
			{
				if (_persistentEngine.GetComponent<Options> () == null)
				{
					ACDebug.LogError (persistentEnginePrefab.name + " has no Options component attached. It can be found in /Assets/AdventureCreator/Resources", _persistentEngine);
					testResult = false;
				}
				if (_persistentEngine.GetComponent<RuntimeInventory> () == null)
				{
					ACDebug.LogError (_persistentEngine.name + " has no RuntimeInventory component attached. It can be found in /Assets/AdventureCreator/Resources", _persistentEngine);
					testResult = false;
				}
				if (_persistentEngine.GetComponent<RuntimeVariables> () == null)
				{
					ACDebug.LogError (_persistentEngine.name + " has no RuntimeVariables component attached. It can be found in /Assets/AdventureCreator/Resources", _persistentEngine);
					testResult = false;
				}
				if (_persistentEngine.GetComponent<PlayerMenus> () == null)
				{
					ACDebug.LogError (_persistentEngine.name + " has no PlayerMenus component attached. It can be found in /Assets/AdventureCreator/Resources", _persistentEngine);
					testResult = false;
				}
				if (_persistentEngine.GetComponent<RuntimeObjectives> () == null)
				{
					ACDebug.LogError (_persistentEngine.name + " has no RuntimeObjectives component attached. It can be found in /Assets/AdventureCreator/Resources", _persistentEngine);
					testResult = false;
				}
				if (_persistentEngine.GetComponent<StateHandler> () == null)
				{
					ACDebug.LogError (_persistentEngine.name + " has no StateHandler component attached. It can be found in /Assets/AdventureCreator/Resources", _persistentEngine);
					testResult = false;
				}
				if (_persistentEngine.GetComponent<SceneChanger> () == null)
				{
					ACDebug.LogError (_persistentEngine.name + " has no SceneChanger component attached. It can be found in /Assets/AdventureCreator/Resources", _persistentEngine);
					testResult = false;
				}
				if (_persistentEngine.GetComponent<SaveSystem> () == null)
				{
					ACDebug.LogError (_persistentEngine.name + " has no SaveSystem component attached. It can be found in /Assets/AdventureCreator/Resources", _persistentEngine);
					testResult = false;
				}
				if (_persistentEngine.GetComponent<LevelStorage> () == null)
				{
					ACDebug.LogError (_persistentEngine.name + " has no LevelStorage component attached. It can be found in /Assets/AdventureCreator/Resources", _persistentEngine);
					testResult = false;
				}
				if (_persistentEngine.GetComponent<RuntimeLanguages> () == null)
				{
					ACDebug.LogError (_persistentEngine.name + " has no RuntimeLanguages component attached. It can be found in /Assets/AdventureCreator/Resources", _persistentEngine);
					testResult = false;
				}
				if (_persistentEngine.GetComponent<RuntimeDocuments> () == null)
				{
					ACDebug.LogError (_persistentEngine.name + " has no RuntimeDocuments component attached. It can be found in /Assets/AdventureCreator/Resources", _persistentEngine);
					testResult = false;
				}
				if (_persistentEngine.GetComponent<ActionListAssetManager> () == null)
				{
					ACDebug.LogError (_persistentEngine.name + " has no ActionListAssetManager component attached. It can be found in /Assets/AdventureCreator/Resources", _persistentEngine);
					testResult = false;
				}
			}

			return testResult;
		}

		#endif


		/**
		 * Clears the internal Manager references.  Call this when changing the assigned Managers, so that other Inspectors/Editors get updated to reflect this
		 */
		public static void ClearManagerCache ()
		{
			sceneManagerPrefab = null;
			settingsManagerPrefab = null;
			actionsManagerPrefab = null;
			variablesManagerPrefab = null;
			inventoryManagerPrefab = null;
			speechManagerPrefab = null;
			cursorManagerPrefab = null;
			menuManagerPrefab = null;
		}
		
		
		public static SceneManager sceneManager
		{
			get
			{
				if (sceneManagerPrefab != null) return sceneManagerPrefab;
				else if (AdvGame.GetReferences () && AdvGame.GetReferences ().sceneManager)
				{
					sceneManagerPrefab = AdvGame.GetReferences ().sceneManager;
					return sceneManagerPrefab;
				}
				return null;
			}
			set
			{
				sceneManagerPrefab = value;
			}
		}
		
		
		public static SettingsManager settingsManager
		{
			get
			{
				if (settingsManagerPrefab != null) return settingsManagerPrefab;
				else if (AdvGame.GetReferences () && AdvGame.GetReferences ().settingsManager)
				{
					settingsManagerPrefab = AdvGame.GetReferences ().settingsManager;
					return settingsManagerPrefab;
				}
				return null;
			}
			set
			{
				settingsManagerPrefab = value;
			}
		}
		
		
		public static ActionsManager actionsManager
		{
			get
			{
				if (actionsManagerPrefab != null) return actionsManagerPrefab;
				else if (AdvGame.GetReferences () && AdvGame.GetReferences ().actionsManager)
				{
					actionsManagerPrefab = AdvGame.GetReferences ().actionsManager;
					return actionsManagerPrefab;
				}
				return null;
			}
			set
			{
				actionsManagerPrefab = value;
			}
		}
		
		
		public static VariablesManager variablesManager
		{
			get
			{
				if (variablesManagerPrefab != null) return variablesManagerPrefab;
				else if (AdvGame.GetReferences () && AdvGame.GetReferences ().variablesManager)
				{
					variablesManagerPrefab = AdvGame.GetReferences ().variablesManager;
					return variablesManagerPrefab;
				}
				return null;
			}
			set
			{
				variablesManagerPrefab = value;
			}
		}
		
		
		public static InventoryManager inventoryManager
		{
			get
			{
				if (inventoryManagerPrefab != null) return inventoryManagerPrefab;
				else if (AdvGame.GetReferences () && AdvGame.GetReferences ().inventoryManager)
				{
					inventoryManagerPrefab = AdvGame.GetReferences ().inventoryManager;
					return inventoryManagerPrefab;
				}
				return null;
			}
			set
			{
				inventoryManagerPrefab = value;
			}
		}
		
		
		public static SpeechManager speechManager
		{
			get
			{
				if (speechManagerPrefab != null) return speechManagerPrefab;
				else if (AdvGame.GetReferences () && AdvGame.GetReferences ().speechManager)
				{
					speechManagerPrefab = AdvGame.GetReferences ().speechManager;
					return speechManagerPrefab;
				}
				return null;
			}
			set
			{
				speechManagerPrefab = value;
			}
		}
		
		
		public static CursorManager cursorManager
		{
			get
			{
				if (cursorManagerPrefab != null) return cursorManagerPrefab;
				else if (AdvGame.GetReferences () && AdvGame.GetReferences ().cursorManager)
				{
					cursorManagerPrefab = AdvGame.GetReferences ().cursorManager;
					return cursorManagerPrefab;
				}
				return null;
			}
			set
			{
				cursorManagerPrefab = value;
			}
		}
		
		
		public static MenuManager menuManager
		{
			get
			{
				if (menuManagerPrefab != null) return menuManagerPrefab;
				else if (AdvGame.GetReferences () && AdvGame.GetReferences ().menuManager)
				{
					menuManagerPrefab = AdvGame.GetReferences ().menuManager;
					return menuManagerPrefab;
				}
				return null;
			}
			set
			{
				menuManagerPrefab = value;
			}
		}
		
		
		public static Options options
		{
			get
			{
				if (optionsComponent != null) return optionsComponent;
				else if (persistentEnginePrefab)
				{
					optionsComponent = persistentEnginePrefab.GetComponent <Options>();
					return optionsComponent;
				}
				return null;
			}
		}
		
		
		public static RuntimeInventory runtimeInventory
		{
			get
			{
				if (runtimeInventoryComponent != null) return runtimeInventoryComponent;
				else if (persistentEnginePrefab)
				{
					runtimeInventoryComponent = persistentEnginePrefab.GetComponent <RuntimeInventory>();
					return runtimeInventoryComponent;
				}
				return null;
			}
		}
		
		
		public static RuntimeVariables runtimeVariables
		{
			get
			{
				if (runtimeVariablesComponent != null) return runtimeVariablesComponent;
				else if (persistentEnginePrefab)
				{
					runtimeVariablesComponent = persistentEnginePrefab.GetComponent <RuntimeVariables>();
					return runtimeVariablesComponent;
				}
				return null;
			}
		}
		
		
		public static PlayerMenus playerMenus
		{
			get
			{
				if (playerMenusComponent != null) return playerMenusComponent;
				else if (persistentEnginePrefab)
				{
					playerMenusComponent = persistentEnginePrefab.GetComponent <PlayerMenus>();
					return playerMenusComponent;
				}
				return null;
			}
		}
		
		
		public static StateHandler stateHandler
		{
			get
			{
				if (stateHandlerComponent != null) return stateHandlerComponent;
				else if (persistentEnginePrefab)
				{
					stateHandlerComponent = persistentEnginePrefab.GetComponent <StateHandler>();
					return stateHandlerComponent;
				}
				return null;
			}
		}
		
		
		public static SceneChanger sceneChanger
		{
			get
			{
				if (sceneChangerComponent != null) return sceneChangerComponent;
				else if (persistentEnginePrefab)
				{
					sceneChangerComponent = persistentEnginePrefab.GetComponent <SceneChanger>();
					return sceneChangerComponent;
				}
				return null;
			}
		}
		
		
		public static SaveSystem saveSystem
		{
			get
			{
				if (saveSystemComponent != null) return saveSystemComponent;
				else if (persistentEnginePrefab)
				{
					saveSystemComponent = persistentEnginePrefab.GetComponent <SaveSystem>();
					return saveSystemComponent;
				}
				return null;
			}
		}
		
		
		public static LevelStorage levelStorage
		{
			get
			{
				if (levelStorageComponent != null) return levelStorageComponent;
				else if (persistentEnginePrefab)
				{
					levelStorageComponent = persistentEnginePrefab.GetComponent <LevelStorage>();
					return levelStorageComponent;
				}
				return null;
			}
		}


		public static RuntimeLanguages runtimeLanguages
		{
			get
			{
				if (runtimeLanguagesComponent != null) return runtimeLanguagesComponent;
				else if (persistentEnginePrefab)
				{
					runtimeLanguagesComponent = persistentEnginePrefab.GetComponent <RuntimeLanguages>();
					return runtimeLanguagesComponent;
				}
				return null;
			}
		}


		public static RuntimeDocuments runtimeDocuments
		{
			get
			{
				if (runtimeDocumentsComponent != null) return runtimeDocumentsComponent;
				else if (persistentEnginePrefab)
				{
					runtimeDocumentsComponent = persistentEnginePrefab.GetComponent <RuntimeDocuments>();
					return runtimeDocumentsComponent;
				}
				return null;
			}
		}


		public static RuntimeObjectives runtimeObjectives
		{
			get
			{
				if (runtimeObjectivesComponent != null) return runtimeObjectivesComponent;
				else if (persistentEnginePrefab)
				{
					runtimeObjectivesComponent = persistentEnginePrefab.GetComponent <RuntimeObjectives>();
					return runtimeObjectivesComponent;
				}
				return null;
			}
		}


		public static ActionListAssetManager actionListAssetManager
		{
			get
			{
				if (actionListAssetManagerComponent != null) return actionListAssetManagerComponent;
				else if (persistentEnginePrefab)
				{
					actionListAssetManagerComponent = persistentEnginePrefab.GetComponent <ActionListAssetManager>();
					return actionListAssetManagerComponent;
				}
				return null;
			}
		}
		
		
		public static MenuSystem menuSystem
		{
			get
			{
				if (menuSystemComponent != null) return menuSystemComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					menuSystemComponent = gameEnginePrefab.GetComponent <MenuSystem>();
					return menuSystemComponent;
				}
				return null;
			}
		}
		
		
		public static Dialog dialog
		{
			get
			{
				if (dialogComponent != null) return dialogComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					dialogComponent = gameEnginePrefab.GetComponent <Dialog>();
					return dialogComponent;
				}
				return null;
			}
		}
		
		
		public static PlayerInput playerInput
		{
			get
			{
				if (playerInputComponent != null) return playerInputComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					playerInputComponent = gameEnginePrefab.GetComponent <PlayerInput>();
					return playerInputComponent;
				}
				return null;
			}
		}
		
		
		public static PlayerInteraction playerInteraction
		{
			get
			{
				if (playerInteractionComponent != null) return playerInteractionComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					playerInteractionComponent = gameEnginePrefab.GetComponent <PlayerInteraction>();
					return playerInteractionComponent;
				}
				return null;
			}
		}
		
		
		public static PlayerMovement playerMovement
		{
			get
			{
				if (playerMovementComponent != null) return playerMovementComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					playerMovementComponent = gameEnginePrefab.GetComponent <PlayerMovement>();
					return playerMovementComponent;
				}
				return null;
			}
		}
		
		
		public static PlayerCursor playerCursor
		{
			get
			{
				if (playerCursorComponent != null) return playerCursorComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					playerCursorComponent = gameEnginePrefab.GetComponent <PlayerCursor>();
					return playerCursorComponent;
				}
				return null;
			}
		}
		
		
		public static PlayerQTE playerQTE
		{
			get
			{
				if (playerQTEComponent != null) return playerQTEComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					playerQTEComponent = gameEnginePrefab.GetComponent <PlayerQTE>();
					return playerQTEComponent;
				}
				return null;
			}
		}
		
		
		public static SceneSettings sceneSettings
		{
			get
			{
				if (sceneSettingsComponent != null && Application.isPlaying) return sceneSettingsComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					sceneSettingsComponent = gameEnginePrefab.GetComponent <SceneSettings>();
					return sceneSettingsComponent;
				}
				return null;
			}
		}
		
		
		public static NavigationManager navigationManager
		{
			get
			{
				if (navigationManagerComponent != null) return navigationManagerComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					navigationManagerComponent = gameEnginePrefab.GetComponent <NavigationManager>();
					return navigationManagerComponent;
				}
				return null;
			}
		}
		
		
		public static ActionListManager actionListManager
		{
			get
			{
				if (actionListManagerComponent != null) 
				{
					return actionListManagerComponent;
				}
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					actionListManagerComponent = gameEnginePrefab.GetComponent <ActionListManager>();
					return actionListManagerComponent;
				}
				return null;
			}
		}
		
		
		public static LocalVariables localVariables
		{
			get
			{
				if (localVariablesComponent != null) return localVariablesComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					localVariablesComponent = gameEnginePrefab.GetComponent <LocalVariables>();
					return localVariablesComponent;
				}
				return null;
			}
		}
		
		
		public static MenuPreview menuPreview
		{
			get
			{
				if (menuPreviewComponent != null) return menuPreviewComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					menuPreviewComponent = gameEnginePrefab.GetComponent <MenuPreview>();
					return menuPreviewComponent;
				}
				return null;
			}
		}


		public static EventManager eventManager
		{
			get
			{
				if (eventManagerComponent != null) return eventManagerComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					eventManagerComponent = gameEnginePrefab.GetComponent <EventManager>();
					return eventManagerComponent;
				}
				return null;
			}
		}


		public static KickStarter kickStarter
		{
			get
			{
				if (kickStarterComponent != null) return kickStarterComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					kickStarterComponent = gameEnginePrefab.GetComponent <KickStarter>();
					return kickStarterComponent;
				}
				return null;
			}
		}


		public static Music music
		{
			get
			{
				if (KickStarter.stateHandler != null)
				{
					return KickStarter.stateHandler.GetMusicEngine ();
				}
				return null;
			}
		}
		
		
		public static Player player
		{
			get
			{
				return playerPrefab;
			}
			set
			{
				if (playerPrefab != value)
				{
					if (playerPrefab != null)
					{
						UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene (playerPrefab.gameObject, SceneChanger.CurrentScene);
					}

					playerPrefab = value;
					
					if (playerPrefab != null)
					{
						if (playerPrefab.IsLocalPlayer ())
						{
							// Remove others
							Player[] allPlayers = FindObjectsOfType<Player> ();
							foreach (Player allPlayer in allPlayers)
							{
								if (allPlayer != playerPrefab)
								{
									allPlayer.RemoveFromScene ();
								}
							}
							
							if (settingsManager.GetDefaultPlayer () != null)
							{
								ACDebug.Log ("Local player " + playerPrefab.GetName () + " found - this will override the default, " + settingsManager.GetDefaultPlayer ().GetName () + ", for the duration of scene " + playerPrefab.gameObject.scene.name, player);
							}
						}
						else if (settingsManager.playerSwitching == PlayerSwitching.Allow)
						{
							PlayerData playerData = saveSystem.GetPlayerData (playerPrefab.ID);

							if (!settingsManager.shareInventory)
							{
								runtimeInventory.SetNull ();
								runtimeInventory.RemoveRecipes ();
								runtimeObjectives.ClearUniqueToPlayer ();

								runtimeInventory.localItems.Clear ();
								runtimeDocuments.ClearCollection ();

								List<InvItem> newInventory = saveSystem.AssignInventory (playerData.inventoryData);
								runtimeInventory.AssignPlayerInventory (newInventory);
								runtimeDocuments.AssignPlayerDocuments (playerData);
								runtimeObjectives.AssignPlayerObjectives (playerData);

								// Menus
								foreach (AC.Menu menu in PlayerMenus.GetMenus ())
								{
									foreach (MenuElement element in menu.elements)
									{
										if (element is MenuInventoryBox)
										{
											MenuInventoryBox invBox = (MenuInventoryBox) element;
											invBox.ResetOffset ();
										}
									}
								}
							}

							mainCamera.LoadData (playerData, false);
							
							DontDestroyOnLoad (playerPrefab);
						}
						else
						{
							DontDestroyOnLoad (playerPrefab);
						}
						
						stateHandler.IgnoreNavMeshCollisions ();
						stateHandler.UpdateAllMaxVolumes ();
						foreach (_Camera camera in stateHandler.Cameras)
						{
							camera.ResetTarget ();
						}

						saveSystem.CurrentPlayerID = playerPrefab.ID;

						if (eventManager != null) eventManager.Call_OnSetPlayer (playerPrefab);
					}
				}
			}
		}
		
		
		public static MainCamera mainCamera
		{
			get
			{
				if (mainCameraPrefab != null)
				{
					return mainCameraPrefab;
				}
				else
				{
					MainCamera _mainCamera = (MainCamera) GameObject.FindObjectOfType (typeof (MainCamera));
					if (_mainCamera)
					{
						mainCameraPrefab = _mainCamera;
					}
					return mainCameraPrefab;
				}
			}
			set
			{
				if (value != null)
				{
					mainCameraPrefab = value;
				}
			}
		}


		/**
		 * A cache of Unity's own Camera.main
		 */
		public static Camera CameraMain
		{
			get
			{
				if (KickStarter.settingsManager.cacheCameraMain)
				{
					if (cameraMain == null)
					{
						cameraMain = Camera.main;
					}
					return cameraMain;
				}
				return Camera.main;
			}
			set
			{
				if (value != null)
				{
					cameraMain = value;
				}
			}
		}


		public void Initialise ()
		{
			if (settingsManager.IsInLoadingScene ())
			{
				ACDebug.Log ("Bypassing regular AC startup because the current scene is the 'Loading' scene.");
				return;
			}

			ClearVariables ();
			SetGameEngine (gameObject);

			bool havePersistentEngine = SetPersistentEngine ();
			if (!havePersistentEngine)
			{
				return;
			}

			CheckRequiredManagerPackage (sceneSettings.requiredManagerPackage);

			PreparePlayer ();

			if (mainCamera != null)
			{
				mainCamera.OnInitGameEngine ();
			}
			else
			{
				ACDebug.LogError ("No MainCamera found - please click 'Organise room objects' in the Scene Manager to create one.");
			}

			playerInput.OnInitGameEngine ();
			localVariables.OnInitGameEngine ();
			sceneSettings.OnInitGameEngine ();
		}


		public static void PreparePlayer ()
		{
			saveSystem.SpawnAllPlayers ();

			Player[] localPlayers = FindObjectsOfType<Player> ();

			if (settingsManager.playerSwitching == PlayerSwitching.Allow)
			{
				// Local players are ignored
				foreach (Player localPlayer in localPlayers)
				{
					if (localPlayer.ID <= -1)
					{
						ACDebug.LogWarning ("Local Player " + localPlayer.GetName () + " found in scene " + localPlayer.gameObject.scene.name + ". This is not allowed when Player Switching is enabled - in this mode, Players can only be spawned in.", localPlayer);
					}
				}
				
				PlayerPrefab playerPrefab = settingsManager.GetPlayerPrefab (saveSystem.CurrentPlayerID);
				if (playerPrefab != null) playerPrefab.SpawnInScene (true);
			}
			else
			{
				// Local players take priority
				foreach (Player localPlayer in localPlayers)
				{
					if (localPlayer.ID == -1)
					{
						localPlayer.ID = -2 - SceneChanger.CurrentSceneIndex; // Always unique to the scene
						player = localPlayer;
						return;
					}
				}

				foreach (Player localPlayer in localPlayers)
				{
					if (localPlayer.ID == 0)
					{
						player = localPlayer;
						return;
					}
				}

				if (settingsManager.GetDefaultPlayer () != null)
				{
					player = settingsManager.GetDefaultPlayer ().SpawnFromPrefab (0);
				}
			}

			if (player == null && settingsManager.movementMethod != MovementMethod.None)
			{
				ACDebug.LogWarning ("No Player found - this can be assigned in the Settings Manager.");
			}

			if (player != null)
			{
				player.EndPath ();
				player.Halt (false);
			}
		}


		/**
		 * Turns Adventure Creator off.
		 */
		public static void TurnOnAC ()
		{
			if (stateHandler != null)
			{
				stateHandler.SetACState (true);
				eventManager.Call_OnManuallySwitchAC (true);
				ACDebug.Log ("Adventure Creator has been turned on.");
			}
			else
			{
				ACDebug.LogWarning ("Cannot turn AC on because the PersistentEngine and GameEngine are not present!");
			}
		}
		
		
		/**
		 * Turns Adventure Creator on.
		 */
		public static void TurnOffAC ()
		{
			if (stateHandler != null)
			{
				eventManager.Call_OnManuallySwitchAC (false);
				stateHandler.SetACState (false);
				ACDebug.Log ("Adventure Creator has been turned off.");
			}
			else
			{
				ACDebug.LogWarning ("Cannot turn AC off because it is not on!");
			}
		}


		/**
		 * <summary>Unsets the values of all script variables, so that they can be re-assigned to the correct scene if multiple scenes are open.</summary>
		 */
		public void ClearVariables ()
		{
			playerPrefab = null;
			mainCameraPrefab = null;
			persistentEnginePrefab = null;
			gameEnginePrefab = null;

			// Managers
			sceneManagerPrefab = null;
			settingsManagerPrefab = null;
			actionsManagerPrefab = null;
			variablesManagerPrefab = null;
			inventoryManagerPrefab = null;
			speechManagerPrefab = null;
			cursorManagerPrefab = null;
			menuManagerPrefab = null;

			// PersistentEngine components
			optionsComponent = null;
			runtimeInventoryComponent = null;
			runtimeVariablesComponent = null;
			playerMenusComponent = null;
			stateHandlerComponent = null;
			sceneChangerComponent = null;
			saveSystemComponent = null;
			levelStorageComponent = null;
			runtimeLanguagesComponent = null;
			actionListAssetManagerComponent = null;

			// GameEngine components
			menuSystemComponent = null;
			dialogComponent = null;
			playerInputComponent = null;
			playerInteractionComponent = null;
			playerMovementComponent = null;
			playerCursorComponent = null;
			playerQTEComponent = null;
			sceneSettingsComponent = null;
			navigationManagerComponent = null;
			actionListManagerComponent = null;
			localVariablesComponent = null;
			menuPreviewComponent = null;
			eventManagerComponent = null;

			SetGameEngine ();
		}


		/**
		 * <summary>Restarts the game, resetting the game to its original state.  Save game files and options data will not be affected</summary>
		 * <param name = "resetMenus">If True, Menus will be rebuilt based on their original settings in the Menu Manager</param>
		 * <param name = "newSceneIndex">The build index number of the scene to switch to</param>
		 */
		public static void RestartGame (bool rebuildMenus, int newSceneIndex)
		{
			KickStarter.runtimeInventory.SetNull ();
			KickStarter.runtimeInventory.RemoveRecipes ();

			if (KickStarter.settingsManager.blackOutWhenInitialising)
			{
				KickStarter.mainCamera.ForceOverlayForFrames (6);
			}

			if (KickStarter.player)
			{
				DestroyImmediate (KickStarter.player.gameObject);
			}

			KickStarter.saveSystem.ClearAllData ();
			KickStarter.levelStorage.ClearAllLevelData ();

			KickStarter.stateHandler.Initialise (rebuildMenus);

			KickStarter.eventManager.Call_OnRestartGame ();

			KickStarter.stateHandler.CanGlobalOnStart ();

			KickStarter.sceneChanger.ChangeScene (newSceneIndex, false, true);
		}

	}
	
}