using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Illusion.Component.Correct;
using UnityEngine;
using HarmonyLib;
using RootMotion.FinalIK;
using H;
using Manager;
using ActionGame;
using VRTK;
using BepInEx;
using BepInEx.Logging;
using UnityEngine.Networking;
using System.Collections;
using Valve.VR;
using System.IO;
using static SetParentKK.KK_SetParentVR;

namespace SetParentKK
{
	public partial class SetParent : MonoBehaviour
	{
		const int SmoothBuffer = 20;


		public void Init(HSprite _hsprite, List<MotionIK> _lstMotionIK)
		{
			hSprite = _hsprite;
			lstMotionIK = _lstMotionIK;
		}
		
		public void Start()
		{
			if (hSprite == null)
			{
				if (!(hSprite = GameObject.Find("VRTK/[VRTK_SDKManager]/SDKSetups/SteamVR/VRCameraBase/[CameraRig]/Controller (left)/Model/p_handL/HSceneMainCanvas/MainCanvas").GetComponent<HSprite>()))
				{
					//BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Error, "HSprite not found. SetParent will exit");
					Destroy(this);
				}
			}
			femaleExists = false;
			hideCanvas = MenuHideDefault.Value;

			hFlag = hSprite.flags;
			f_device = typeof(VRViveController).GetField("device", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			cameraEye = hSprite.managerVR.objCamera;
			controllers[Side.Left] = hSprite.managerVR.objMove.transform.Find("Controller (left)").gameObject;
			controllers[Side.Right] = hSprite.managerVR.objMove.transform.Find("Controller (right)").gameObject;		
			foreach (Side side in Enum.GetValues(typeof(Side)))
			{
				viveControllers[side] = controllers[side].GetComponent<VRViveController>();
				steamVRDevices[side] = f_device.GetValue(viveControllers[side]) as SteamVR_Controller.Device;
			}

			itemHands[0] = Traverse.Create(controllers[Side.Left].transform.Find("Model/p_handL").GetComponent<VRHandCtrl>()).Field("dicItem").GetValue<Dictionary<int, VRHandCtrl.AibuItem>>()[0].objBody.GetComponent<SkinnedMeshRenderer>();
			itemHands[1] = Traverse.Create(controllers[Side.Right].transform.Find("Model/p_handR").GetComponent<VRHandCtrl>()).Field("dicItem").GetValue<Dictionary<int, VRHandCtrl.AibuItem>>()[0].objBody.GetComponent<SkinnedMeshRenderer>();

			male = (ChaControl)Traverse.Create(hSprite).Field("male").GetValue();
			female = ((List<ChaControl>)Traverse.Create(hSprite).Field("females").GetValue())[0];
			obj_chaF_001 = female.objRoot;
			male_p_cf_bodybone = male.objAnim;
			female_p_cf_bodybone = female.objAnim;
			maleFBBIK = male_p_cf_bodybone.GetComponent<FullBodyBipedIK>();
			femaleFBBIK = female_p_cf_bodybone.GetComponent<FullBodyBipedIK>();
			femaleAim = femaleFBBIK.references.head.Find("cf_s_head/aim").gameObject;

			female_cf_j_root = femaleFBBIK.references.root.gameObject;
			female_cf_j_hips = femaleFBBIK.references.pelvis.gameObject;
			female_cf_n_height = femaleFBBIK.references.pelvis.parent.gameObject;
			female_cf_j_spine01 = femaleFBBIK.references.spine[0].gameObject;
			female_cf_j_spine02 = femaleFBBIK.references.spine[1].gameObject;
			female_cf_j_neck = femaleFBBIK.references.spine[2].gameObject;
			female_cf_j_head = femaleFBBIK.references.head.gameObject;
			female_cf_j_spine03 = femaleFBBIK.references.spine[2].parent.gameObject;

			switch (ParentPart.Value)
			{
				case BodyPart.Ass:
					femaleBase = female_cf_j_hips;
					break;
				case BodyPart.Torso:
					femaleBase = female_cf_j_spine02;
					break;
				case BodyPart.Head:
					femaleBase = female_cf_j_neck;
					break;
				default:
					femaleBase = female_cf_j_spine02;
					break;
			}
			femaleSpinePos = new GameObject("femaleSpinePos");

			Transform female_cf_pv_hand_R = female_cf_n_height.transform.Find("cf_pv_root/cf_pv_hand_R");
			Transform female_cf_pv_hand_L = female_cf_n_height.transform.Find("cf_pv_root/cf_pv_hand_L");
			Transform female_cf_pv_leg_R = female_cf_n_height.transform.Find("cf_pv_root/cf_pv_leg_R");
			Transform female_cf_pv_leg_L = female_cf_n_height.transform.Find("cf_pv_root/cf_pv_leg_L");
			BaseData female_hand_L_bd = femaleFBBIK.solver.leftHandEffector.target.GetComponent<BaseData>();
			BaseData female_hand_R_bd = femaleFBBIK.solver.rightHandEffector.target.GetComponent<BaseData>();
			BaseData female_leg_L_bd = femaleFBBIK.solver.leftFootEffector.target.GetComponent<BaseData>();
			BaseData female_leg_R_bd = femaleFBBIK.solver.rightFootEffector.target.GetComponent<BaseData>();

			Transform male_cf_n_height = maleFBBIK.references.pelvis.parent;
			Transform male_cf_pv_hand_R = male_cf_n_height.Find("cf_pv_root/cf_pv_hand_R");
			Transform male_cf_pv_hand_L = male_cf_n_height.Find("cf_pv_root/cf_pv_hand_L");
			Transform male_cf_pv_leg_R = male_cf_n_height.Find("cf_pv_root/cf_pv_leg_R");
			Transform male_cf_pv_leg_L = male_cf_n_height.Find("cf_pv_root/cf_pv_leg_L");

			male_cf_pv_hips = male_cf_n_height.Find("cf_pv_root/cf_pv_hips");
			male_hips_bd = maleFBBIK.solver.bodyEffector.target.GetComponent<BaseData>();

			BaseData male_hand_L_bd = maleFBBIK.solver.leftHandEffector.target.GetComponent<BaseData>();
			BaseData male_hand_R_bd = maleFBBIK.solver.rightHandEffector.target.GetComponent<BaseData>();
			BaseData male_leg_L_bd = maleFBBIK.solver.leftFootEffector.target.GetComponent<BaseData>();
			BaseData male_leg_R_bd = maleFBBIK.solver.rightFootEffector.target.GetComponent<BaseData>();

			limbs[(int)LimbName.FemaleLeftHand] = new Limb(
				limbpart: LimbName.FemaleLeftHand, 
				anchorObj: null, 
				animPos: female_cf_pv_hand_L, 
				effector: femaleFBBIK.solver.leftHandEffector, 
				origTarget: femaleFBBIK.solver.leftHandEffector.target, 
				targetBone: female_hand_L_bd, 
				chain: femaleFBBIK.solver.leftArmChain);

			limbs[(int)LimbName.FemaleRightHand] = new Limb(
				limbpart: LimbName.FemaleRightHand,
				anchorObj: null,
				animPos: female_cf_pv_hand_R,
				effector: femaleFBBIK.solver.rightHandEffector,
				origTarget: femaleFBBIK.solver.rightHandEffector.target,
				targetBone: female_hand_R_bd,
				chain: femaleFBBIK.solver.rightArmChain);

			limbs[(int)LimbName.FemaleLeftFoot] = new Limb(
				limbpart: LimbName.FemaleLeftFoot,
				anchorObj: null,
				animPos: female_cf_pv_leg_L,
				effector: femaleFBBIK.solver.leftFootEffector,
				origTarget: femaleFBBIK.solver.leftFootEffector.target,
				targetBone: female_leg_L_bd,
				chain: femaleFBBIK.solver.leftLegChain);

			limbs[(int)LimbName.FemaleRightFoot] = new Limb(
				limbpart: LimbName.FemaleRightFoot,
				anchorObj: null,
				animPos: female_cf_pv_leg_R,
				effector: femaleFBBIK.solver.rightFootEffector,
				origTarget: femaleFBBIK.solver.rightFootEffector.target,
				targetBone: female_leg_R_bd,
				chain: femaleFBBIK.solver.rightLegChain);

			limbs[(int)LimbName.MaleLeftHand] = new Limb(
				limbpart: LimbName.MaleLeftHand,
				anchorObj: null,
				animPos: male_cf_pv_hand_L,
				effector: maleFBBIK.solver.leftHandEffector,
				origTarget: maleFBBIK.solver.leftHandEffector.target,
				targetBone: male_hand_L_bd,
				chain: maleFBBIK.solver.leftArmChain,
				parentJointBone: maleFBBIK.solver.leftShoulderEffector.target.GetComponent<BaseData>(),
				parentJointEffector: maleFBBIK.solver.leftShoulderEffector,
				parentJointAnimPos: male_cf_n_height.Find("cf_pv_root/cf_pv_hips/cf_ik_hips/cf_kk_shoulder/cf_pv_shoulder_L"));

			limbs[(int)LimbName.MaleRightHand] = new Limb(
				limbpart: LimbName.MaleRightHand,
				anchorObj: null,
				animPos: male_cf_pv_hand_R,
				effector: maleFBBIK.solver.rightHandEffector,
				origTarget: maleFBBIK.solver.rightHandEffector.target,
				targetBone: male_hand_R_bd,
				chain: maleFBBIK.solver.rightArmChain,
				parentJointBone: maleFBBIK.solver.rightShoulderEffector.target.GetComponent<BaseData>(),
				parentJointEffector: maleFBBIK.solver.rightShoulderEffector,
				parentJointAnimPos: male_cf_n_height.Find("cf_pv_root/cf_pv_hips/cf_ik_hips/cf_kk_shoulder/cf_pv_shoulder_R"));

			limbs[(int)LimbName.MaleLeftFoot] = new Limb(
				limbpart: LimbName.MaleLeftFoot,
				anchorObj: null,
				animPos: male_cf_pv_leg_L,
				effector: maleFBBIK.solver.leftFootEffector,
				origTarget: maleFBBIK.solver.leftFootEffector.target,
				targetBone: male_leg_L_bd);

			limbs[(int)LimbName.MaleRightFoot] = new Limb(
				limbpart: LimbName.MaleRightFoot,
				anchorObj: null,
				animPos: male_cf_pv_leg_R,
				effector: maleFBBIK.solver.rightFootEffector,
				origTarget: maleFBBIK.solver.rightFootEffector.target,
				targetBone: male_leg_R_bd);


			SetShoulderColliders();

			foreach (Transform transform in GameObject.Find("Map").GetComponentsInChildren<Transform>())
				SetObjectColliders(transform);

			if (SetControllerCollider.Value)
			{
				foreach (KeyValuePair<Side, GameObject> pair in controllers)
					SetControllerColliders(pair.Value);
			}

			for (LimbName i = LimbName.FemaleLeftHand; i <= LimbName.FemaleRightFoot; i++)
			{
				SetLimbColliders(i);
			}

			if (SetMaleFeetCollider.Value)
			{
				for (LimbName i = LimbName.MaleLeftFoot; i <= LimbName.MaleRightFoot; i++)
					SetLimbColliders(i);
			}			
		}		

		public void LateUpdate()
		{
			if (!femaleExists)
			{
				if (obj_chaF_001 == null)
				{
					femaleExists = false;
					return;
				}
				femaleExists = true;
			}

			//Controllers may be inactive while game is running, the below code will attempt to find and assign left and right steamVR devices every frame if they are null
			foreach (Side side in (Side[])Enum.GetValues(typeof(Side)))
			{
				if (steamVRDevices[side] == null)
					steamVRDevices[side] = f_device.GetValue(viveControllers[side]) as SteamVR_Controller.Device;
			}

			//Initiate canvas if it's null
			if (objRightMenuCanvas == null)
				InitCanvas();
			else
			{
				//Hold button for 1 second to hide/unhide floating menu
				if (MenuPressing(Side.Left) || MenuPressing(Side.Right))
				{
					hideCount += Time.deltaTime;
					if (hideCount >= 1f)
					{
						//hideCanvas = !hideCanvas;
						hideCanvas = true;
						hideCount = 0f;

						if (MhamotoCounter == 0)
                        {
							OriginalHMDParent = cameraEye.transform.parent;
							OriginalLeftControllerParent = controllers[Side.Left].transform.parent;
							OriginalRightControllerParent = controllers[Side.Right].transform.parent;
						}

						MhamotoCounter++;
						MhamotoState = MhamotoCounter % 4;

						switch(MhamotoState)
                        {
							case 0:
                                {
									ResetState();
									break;
                                }
							case 1:
                                {
									ResetState();
									InitMhamotoSpeak();
									//InitMhamotoSync();
									break;
                                }
							case 2:
                                {
									ResetState();
									InitFPOV();
									//InitFPOV();		// Needs to be done twice to work correctly for some reason
									break;
								}
							case 3:
                                {
									ResetState();
									InitDakiMode();
									break;
                                }
							default:
                                {
									ResetState();
									break;
                                }
						}

					}
				}
				else
				{
					hideCount = 0f;
				}

				if (MhamotoStarted)		// Every frame, after Mhamoto is initialized by holding the B button for a second.
                {
					MhamotoSync();
				}

                if (FPOVStarted)
                {
                    FPOV();
                }

				if (DakiModeStarted)
                {
					DakiMode();
                }

				if (MhamotoSpeakStarted)
                {
					MhamotoSpeak();
                }

				//Make floating menu follow and rotate around female
				Vector3 point = femaleAim.transform.position - cameraEye.transform.position;
				point.y = 0f;
				point.Normalize();
				objRightMenuCanvas.transform.position = new Vector3(femaleAim.transform.position.x, cameraEye.transform.position.y, femaleAim.transform.position.z) + Quaternion.Euler(0f, 90f, 0f) * point * 0.4f;
				objRightMenuCanvas.transform.forward = (objRightMenuCanvas.transform.position - cameraEye.transform.position).normalized;
				objLeftMenuCanvas.transform.position = new Vector3(femaleAim.transform.position.x, cameraEye.transform.position.y, femaleAim.transform.position.z) + Quaternion.Euler(0f, -90f, 0f) * point * 0.4f;
				objLeftMenuCanvas.transform.forward = (objLeftMenuCanvas.transform.position - cameraEye.transform.position).normalized;
			
				//When SetParent is active, display the menu regardless of being hidden when user brings controller within set distance to the headset
				if (setFlag)
				{
					Vector3 vector = cameraEye.transform.position - ParentSideController(oppositeSide: true).transform.position; ;

					if (vector.magnitude <= MenuUpProximity.Value)
					{
						objRightMenuCanvas.SetActive(true);
						objLeftMenuCanvas.SetActive(hFlag.mode != HFlag.EMode.aibu);
					}
					else
					{
						objRightMenuCanvas.SetActive(!hideCanvas);
						objLeftMenuCanvas.SetActive(!hideCanvas && hFlag.mode != HFlag.EMode.aibu);
					}
				}
				else 
				{
					objRightMenuCanvas.SetActive(!hideCanvas);
					objLeftMenuCanvas.SetActive(!hideCanvas && hFlag.mode != HFlag.EMode.aibu);
				}
			}			
			
			if (SetParentToggleCondition(out bool isParentLeft))
			{
				if (!setFlag)
					SetP(isParentLeft);
				else
					UnsetP();
			}

			//If trigger is pressed, call function to interact with limbs. Otherwise increase timer since last trigger press
			foreach (Side side in Enum.GetValues(typeof(Side)))
			{
				if (TriggerRelease(side))
				{
					if (GripPressing(side))
						ControllerMaleFeetToggle(IsDoubleClick((TriggerState)side + 2, 0.3f));
					else
						ControllerLimbActions(controllers[side], IsDoubleClick((TriggerState)side, 0.25f));
				}
			}		

			//If keyboard shortcut for limb release is pressed, call function to interact with limbs with paramemters that will ensure the release of all limbs
			if (Input.GetKeyDown(LimbReleaseKey.Value.MainKey) && LimbReleaseKey.Value.Modifiers.All(x => Input.GetKey(x)))
				ControllerLimbActions(controllers[Side.Left], doubleClick: true, forceAll: true);
			
			if (Input.GetKeyDown(MaleFeetToggle.Value.MainKey) && MaleFeetToggle.Value.Modifiers.All(x => Input.GetKey(x)))
				ControllerMaleFeetToggle();			


			MaleIKs();

			FemaleIKs();


			if (setFlag)
			{
				//Reposition male rotation axis if motion changed
				if (nowAnimState != hFlag.nowAnimStateName)
				{
					if (SetParentMale.Value)
						InitMaleFollow();
					nowAnimState = hFlag.nowAnimStateName;
				}			
			
				ControllerCharacterAdjustment();


				if (male_p_cf_bodybone != null && SetParentMale.Value && currentCtrlstate != CtrlState.Following)
				{
					/////////////////////////
					// Make the male body rotate around the crotch to keep its head align with the HMD without moving the crotch by
					// -Create vector from male crotch to HMD position, then another vector from male crotch to male head to represent the spine
					// -Calculate the rotation from spine vector to HMD vector, then apply the rotation to the male body
					/////////////////////////
					Vector3 cameraVec = cameraEye.transform.position - maleCrotchPos.transform.position;
					Vector3 maleSpineVec = maleHeadPos.transform.position - maleCrotchPos.transform.position;
					Quaternion.FromToRotation(maleSpineVec, cameraVec).ToAngleAxis(out float lookRotAngle, out Vector3 lookRotAxis);
					male_p_cf_bodybone.transform.RotateAround(maleCrotchPos.transform.position, lookRotAxis, lookRotAngle);

					/////////////////////////
					// Update position of the spine vector, and using it as an axis to rotate the male body to "look" left or right by following the HMD's rotation
					//
					// - Since we're only interested in HMD's rotation along the spine axis, we take the HMD's right vector which will give us the rotation we need 
					//   and align it with the direction of the penis by rotating it to the left by 90 degress,  
					//   then calculate the rotation between the two vectors projected on the plane normal to the spine before applying it the male body
					/////////////////////////
					if (MaleYaw.Value)
					{
						maleSpineVec = maleHeadPos.transform.position - maleCrotchPos.transform.position;
						Vector3 malePenisProjected = Vector3.ProjectOnPlane(maleCrotchPos.transform.forward, maleSpineVec);
						Vector3 cameraForwardProjected = Quaternion.AngleAxis(-90, maleSpineVec) * Vector3.ProjectOnPlane(cameraEye.transform.right, maleSpineVec);
						Quaternion.FromToRotation(malePenisProjected, cameraForwardProjected).ToAngleAxis(out lookRotAngle, out lookRotAxis);
						male_p_cf_bodybone.transform.RotateAround(maleCrotchPos.transform.position, lookRotAxis, lookRotAngle);
					}
				}

				//Update player's shoulder collider's rotation to always be facing the girl
				shoulderCollider.transform.LookAt(femaleBase.transform, cameraEye.transform.up);
				//var myLogSource = BepInEx.Logging.Logger.CreateLogSource("MyLogSource");
				//myLogSource.LogInfo(hFlag.nowAnimationInfo.nameAnimation);
				//BepInEx.Logging.Logger.Sources.Remove(myLogSource);
			}


			/////////////////////////////////////////////////////////
			///Use arrays to store the position and rotation of the female pivot object during the last constant number of frames.
			///Fill the arrays with the current position and rotation if we want the female to strictly follow
			if (currentCtrlstate == CtrlState.Following)
			{
				for (int j = 0; j < SmoothBuffer; j++)
					quatSpineRot[j] = femaleSpinePos.transform.rotation;
				for (int i = 0; i < SmoothBuffer; i++)
					vecSpinePos[i] = femaleSpinePos.transform.position;
			}
			else
			{
				quatSpineRot[indexSpineRot] = femaleSpinePos.transform.rotation;
				vecSpinePos[indexSpinePos] = femaleSpinePos.transform.position;
			}
				
			if (indexSpineRot >= (SmoothBuffer -1))
				indexSpineRot = 0;
			else
				indexSpineRot++;

			if (indexSpinePos >= (SmoothBuffer - 1))
				indexSpinePos = 0;
			else
				indexSpinePos++;


			if ((setFlag && SetParentMode.Value < ParentMode.AnimationOnly) || currentCtrlstate == CtrlState.Following || currentCtrlstate == CtrlState.FemaleControl)
				FemalePositionUpdate(femaleSpinePos);	
		}

		/// <summary>
		/// Enable SetParent functionality
		/// </summary>
		/// <param name="_parentIsLeft">Whether the left controller is the parenting controller</param>
		private void SetP(bool _parentIsLeft)
		{
			if (obj_chaF_001 == null)
			{
				return;
			}
			if (male_p_cf_bodybone == null)
			{
				GameObject.Find("chaM_001/BodyTop/p_cf_body_bone");
			}
			parentIsLeft = _parentIsLeft;
			nowAnimState = hFlag.nowAnimStateName;		
			parentController = ParentSideController();

			if (femaleSpinePos == null)
			{
				femaleSpinePos = new GameObject("femaleSpinePos");
			}

			if (SetParentMode.Value == ParentMode.PositionOnly || SetParentMode.Value == ParentMode.PositionAndAnimation)
			{
				SetParentToController(femaleSpinePos, femaleBase, true);
			}	
			else
			{
				femaleSpinePos.transform.position = femaleBase.transform.position;
				femaleSpinePos.transform.rotation = femaleBase.transform.rotation;

				//Since we're in AnimationOnly mode, we don't need to hide the parent controller and disable its collider
				//unless the config HideParentConAlways is set to true, in which case we hide the parent controller, and only disable its collider if male hand is not sync'ed to it.
				if (HideParentConAlways.Value)
				{
					parentController.transform.Find("Model").gameObject.SetActive(false);

					if (SetControllerCollider.Value && !limbs[(int)ParentSideMaleHand()].AnchorObj)
						parentController.transform.Find("ControllerCollider").GetComponent<SphereCollider>().enabled = false;
				}
			}
			
			for (int i = 0; i < 20; i++)
			{
				vecSpinePos[i] = femaleSpinePos.transform.position;
			}
			indexSpinePos = 0;
			for (int j = 0; j < 20; j++)
			{
				quatSpineRot[j] = femaleSpinePos.transform.rotation;
			}
			indexSpineRot = 0;
			
			if (SetParentMale.Value && male_p_cf_bodybone != null && currentCtrlstate != CtrlState.Following)
			{
				InitMaleFollow();
			}
			if (SetParentMode.Value == ParentMode.PositionAndAnimation || SetParentMode.Value == ParentMode.AnimationOnly)
			{
				AddAnimSpeedController(obj_chaF_001);
			}

			if (SyncMaleHands.Value)
			{
				foreach (Side side in Enum.GetValues(typeof(Side)))
					SyncMaleHandsToggle(enable: true, side);
			}

			txtSetParentL.text = "親子付け Turn Off";
			txtSetParentR.text = "親子付け Turn Off";

			setFlag = true;
		}

		/// <summary>
		/// Diable SetParent functionality
		/// </summary>
		public void UnsetP()
		{
			UnityEngine.Object.Destroy(maleHeadPos);
			UnityEngine.Object.Destroy(maleCrotchPos);
			femaleSpinePos.transform.parent = null;

			PushLimbAutoAttachButton(true);

			foreach (KeyValuePair<Side, GameObject> pair in controllers)
				pair.Value.transform.Find("Model").gameObject.SetActive(true);

			if (SetControllerCollider.Value)
			{
				foreach (KeyValuePair<Side, GameObject> pair in controllers)
					pair.Value.transform.Find("ControllerCollider").GetComponent<SphereCollider>().enabled = true;
			}
			
			if (obj_chaF_001.GetComponent<AnimSpeedController>() != null)
			{
				UnityEngine.Object.Destroy(obj_chaF_001.GetComponent<AnimSpeedController>());
			}

			foreach (Side side in Enum.GetValues(typeof(Side)))
				SyncMaleHandsToggle(enable: false, side);

			for (LimbName i = LimbName.MaleLeftHand; i <= LimbName.MaleRightHand; i++)
			{
				limbs[(int)i].ParentJointBone.bone = null;
				limbs[(int)i].ParentJointEffector.positionWeight = 0f;
			}	

			male_hips_bd.bone = null;
			maleFBBIK.solver.bodyEffector.positionWeight = 0f;

			txtSetParentL.text = "左 親子付け Turn On";
			txtSetParentR.text = "右 親子付け Turn On";

			setFlag = false;
		}

		private void SetShoulderColliders()
		{
			shoulderCollider = new GameObject("SPCollider");
			shoulderCollider.transform.parent = cameraEye.transform;
			shoulderCollider.transform.localPosition = new Vector3(0f, -0.25f, -0.15f);
			shoulderCollider.transform.localRotation = Quaternion.identity;
			BoxCollider boxCollider2 = shoulderCollider.AddComponent<BoxCollider>();
			boxCollider2.isTrigger = true;
			boxCollider2.center = Vector3.zero;
			boxCollider2.size = new Vector3(0.4f, 0.2f, 0.25f);
			shoulderCollider.AddComponent<Rigidbody>().isKinematic = true;
		}

		private void SetControllerColliders(GameObject controller)
		{
			GameObject CtrlCollider = new GameObject("ControllerCollider");
			CtrlCollider.transform.parent = controller.transform;
			CtrlCollider.transform.localPosition = Vector3.zero;
			CtrlCollider.transform.localRotation = Quaternion.identity;
			SphereCollider sphereCollider = CtrlCollider.AddComponent<SphereCollider>();
			sphereCollider.isTrigger = true;
			sphereCollider.center = Vector3.zero;
			sphereCollider.radius = 0.05f;
			CtrlCollider.AddComponent<Rigidbody>().isKinematic = true;
		}

		private void SetLimbColliders(LimbName limb)
		{
			GameObject collider = new GameObject(limbs[(int)limb].LimbPart.ToString() + "Collider");
			collider.AddComponent<FixBodyParts>().Init(this, limbs[(int)limb].LimbPart);
			collider.transform.parent = limbs[(int)limb].Effector.bone;
			collider.transform.localPosition = Vector3.zero;
			
		}

		internal void SetObjectColliders(Transform transform)
		{
			MeshFilter meshFilter = transform.GetComponent<MeshFilter>();
			if (!(meshFilter == null) && transform.Find("SPCollider") == null)
			{
				GameObject mapObjCollider = new GameObject("SPCollider");
				mapObjCollider.transform.parent = transform.transform;
				mapObjCollider.transform.localPosition = Vector3.zero;
				mapObjCollider.transform.localRotation = Quaternion.identity;
				mapObjCollider.AddComponent<Rigidbody>().isKinematic = true;
				if (meshFilter.mesh.bounds.size.x < 0.03f || meshFilter.mesh.bounds.size.y < 0.03f || meshFilter.mesh.bounds.size.z < 0.03f)
				{
					BoxCollider boxCollider = mapObjCollider.AddComponent<BoxCollider>();
					boxCollider.isTrigger = true;
					boxCollider.center = meshFilter.mesh.bounds.center;
					boxCollider.size = meshFilter.mesh.bounds.size;
					if (boxCollider.size.x < 0.03f)
					{
						boxCollider.size += new Vector3(0.04f, 0f, 0f);
					}
					if (boxCollider.size.y < 0.03f)
					{
						boxCollider.size += new Vector3(0f, 0.04f, 0f);
					}
					if (boxCollider.size.z < 0.03f)
					{
						boxCollider.size += new Vector3(0f, 0f, 0.04f);
					}
				}
				else
				{
					MeshCollider meshCollider = mapObjCollider.AddComponent<MeshCollider>();
					meshCollider.convex = false;
					meshCollider.sharedMesh = meshFilter.mesh;
				}
			}
		}


		/// <summary>
		/// Initialize and position objects representing male's neck and crotch to be used for rotation calculation
		/// </summary>
		public void InitMaleFollow()
		{
			GameObject maleNeck = maleFBBIK.references.spine[2].gameObject;
			if (maleHeadPos == null)
				maleHeadPos = new GameObject("maleHeadPos");
			maleHeadPos.transform.position = maleNeck.transform.position;
			maleHeadPos.transform.rotation = maleNeck.transform.rotation;
			maleHeadPos.transform.parent = maleNeck.transform;
			maleHeadPos.transform.localPosition = new Vector3(0, 0, 0.08f);
			
			GameObject maleCrotch = maleFBBIK.references.leftThigh.parent.Find("cf_d_kokan/cm_J_dan_top").gameObject;
			if (maleCrotchPos == null)
				maleCrotchPos = new GameObject("maleCrotchPos");
			maleCrotchPos.transform.position = maleCrotch.transform.position;
			maleCrotchPos.transform.rotation = maleCrotch.transform.rotation;
			maleCrotchPos.transform.parent = male_p_cf_bodybone.transform;
		}

		/// <summary>
		/// Update female position and rotation
		/// </summary>
		/// <param name="target">The object to synchronize female body to</param>
		private void FemalePositionUpdate(GameObject target)
		{
			if (TrackingMode.Value && currentCtrlstate != CtrlState.Following)
			{
				Quaternion average = quatSpineRot[0];
				for (int i = 1; i < 20; i++)
				{
					average = Quaternion.Lerp(average, quatSpineRot[i], 1f / (i + 1));
				}
				switch (ParentPart.Value)
				{
					case BodyPart.Ass:
						female_p_cf_bodybone.transform.rotation = average * Quaternion.Inverse(female_cf_j_hips.transform.localRotation) * Quaternion.Inverse(female_cf_n_height.transform.localRotation) * Quaternion.Inverse(female_cf_j_root.transform.localRotation);
						break;
					case BodyPart.Torso:
						female_p_cf_bodybone.transform.rotation = average * Quaternion.Inverse(female_cf_j_spine02.transform.localRotation) * Quaternion.Inverse(female_cf_j_spine01.transform.localRotation) * Quaternion.Inverse(female_cf_j_hips.transform.localRotation) * Quaternion.Inverse(female_cf_n_height.transform.localRotation) * Quaternion.Inverse(female_cf_j_root.transform.localRotation);
						break;
					case BodyPart.Head:
						female_p_cf_bodybone.transform.rotation = average * Quaternion.Inverse(female_cf_j_neck.transform.localRotation) * Quaternion.Inverse(female_cf_j_spine03.transform.localRotation) * Quaternion.Inverse(female_cf_j_spine02.transform.localRotation) * Quaternion.Inverse(female_cf_j_spine01.transform.localRotation) * Quaternion.Inverse(female_cf_j_hips.transform.localRotation) * Quaternion.Inverse(female_cf_n_height.transform.localRotation) * Quaternion.Inverse(female_cf_j_root.transform.localRotation);
						break;
					default:
						female_p_cf_bodybone.transform.rotation = average * Quaternion.Inverse(female_cf_j_spine02.transform.localRotation) * Quaternion.Inverse(female_cf_j_spine01.transform.localRotation) * Quaternion.Inverse(female_cf_j_hips.transform.localRotation) * Quaternion.Inverse(female_cf_n_height.transform.localRotation) * Quaternion.Inverse(female_cf_j_root.transform.localRotation);
						break;
				}
			}
			else
			{
				switch (ParentPart.Value)
				{
					case BodyPart.Ass:
						female_p_cf_bodybone.transform.rotation = target.transform.rotation * Quaternion.Inverse(female_cf_j_hips.transform.localRotation) * Quaternion.Inverse(female_cf_n_height.transform.localRotation) * Quaternion.Inverse(female_cf_j_root.transform.localRotation);
						break;
					case BodyPart.Torso:
						female_p_cf_bodybone.transform.rotation = target.transform.rotation * Quaternion.Inverse(female_cf_j_spine02.transform.localRotation) * Quaternion.Inverse(female_cf_j_spine01.transform.localRotation) * Quaternion.Inverse(female_cf_j_hips.transform.localRotation) * Quaternion.Inverse(female_cf_n_height.transform.localRotation) * Quaternion.Inverse(female_cf_j_root.transform.localRotation);
						break;
					case BodyPart.Head:
						female_p_cf_bodybone.transform.rotation = target.transform.rotation * Quaternion.Inverse(female_cf_j_neck.transform.localRotation) * Quaternion.Inverse(female_cf_j_spine03.transform.localRotation) * Quaternion.Inverse(female_cf_j_spine02.transform.localRotation) * Quaternion.Inverse(female_cf_j_spine01.transform.localRotation) * Quaternion.Inverse(female_cf_j_hips.transform.localRotation) * Quaternion.Inverse(female_cf_n_height.transform.localRotation) * Quaternion.Inverse(female_cf_j_root.transform.localRotation);
						break;
					default:
						female_p_cf_bodybone.transform.rotation = target.transform.rotation * Quaternion.Inverse(female_cf_j_spine02.transform.localRotation) * Quaternion.Inverse(female_cf_j_spine01.transform.localRotation) * Quaternion.Inverse(female_cf_j_hips.transform.localRotation) * Quaternion.Inverse(female_cf_n_height.transform.localRotation) * Quaternion.Inverse(female_cf_j_root.transform.localRotation);
						break;
				}
			}


			if (TrackingMode.Value && currentCtrlstate != CtrlState.Following)
			{
				Vector3 sum = Vector3.zero;
				foreach (Vector3 pos in vecSpinePos)
				{
					sum += pos;
				}
				sum /= 20f;
				female_p_cf_bodybone.transform.position += sum - femaleBase.transform.position;
			}
			else
			{
				female_p_cf_bodybone.transform.position += target.transform.position - femaleBase.transform.position;
			}
		}

		private void AddAnimSpeedController(GameObject character)
		{
			if (character.GetComponent<AnimSpeedController>() != null)
			{
				return;
			}

			AnimSpeedController animSpeedController = character.AddComponent<AnimSpeedController>();

			animSpeedController.SetController(parentController, ParentSideController(oppositeSide: true), this);
		}


		private void SetParentToController (GameObject parentDummy, GameObject target, bool hideModel, bool notParentSide = false)
		{
			GameObject controller = ParentSideController(notParentSide);



			if (DakiModeStarted)
				parentDummy.transform.parent = ViveTracker.transform;
			else
				parentDummy.transform.parent = controller.transform;

			if (hideModel)
			{
				controller.transform.Find("Model").gameObject.SetActive(false);

				if (SetControllerCollider.Value && !limbs[(int)ParentSideMaleHand(notParentSide)].AnchorObj)
				{
					controller.transform.Find("ControllerCollider").GetComponent<SphereCollider>().enabled = false;
				}		
			}

			if (DakiModeStarted)
			{
				parentDummy.transform.position = ViveTracker.transform.position;
				parentDummy.transform.rotation = ViveTracker.transform.rotation;
			}
			else
			{
				// MhamotoVR: get rid of the offset between controller and girl.
				parentDummy.transform.position = controller.transform.position + BellyButtonOffset;
				//parentDummy.transform.position = target.transform.position;
				//parentDummy.transform.rotation = target.transform.rotation;
				// MhamotoVR: Match up the girl's rotation with the hip
				parentDummy.transform.rotation = Quaternion.LookRotation(HipAbstraction.transform.forward, HipAbstraction.transform.up);
			}

		}

		/// <summary>
		/// Change state of controller-to-characters relationship based on controller input
		/// </summary>
		private void ControllerCharacterAdjustment()
		{
			///////////////////
			//Based on controller input, set characters into one of these 4 states based on controller input:
			//	1. Remain still relative to the scene if only the trigger is held
			//	2. Both male and female following parent controller (controller specified when activating set parent)
			//	3. Male body parented to non parent controller
			//	4. Female body parented to non parent controller 
			//	5. If no matching controller input is present, return to default state of parenting
			///////////////////
			Side nonParentSide = ParentSideEnum(oppositeSide: true);

			if (GripPressing(nonParentSide))
			{
				if (currentCtrlstate != CtrlState.MaleControl && TrackPadDown(nonParentSide))
					currentCtrlstate = ChangeControlState(currentCtrlstate, CtrlState.MaleControl);

				else if (currentCtrlstate != CtrlState.FemaleControl && TrackPadUp(nonParentSide))
					currentCtrlstate = ChangeControlState(currentCtrlstate, CtrlState.FemaleControl);

				else if (currentCtrlstate != CtrlState.Following && TriggerPressing(nonParentSide))
					currentCtrlstate = ChangeControlState(currentCtrlstate, CtrlState.Following);
			}
			else if (currentCtrlstate != CtrlState.Stationary && TriggerPressing(nonParentSide) && hFlag.timeNoClickItem == 0)
			{
				currentCtrlstate = ChangeControlState(currentCtrlstate, CtrlState.Stationary);
			}
			else if (currentCtrlstate != CtrlState.None)
			{
				currentCtrlstate = ChangeControlState(currentCtrlstate, CtrlState.None);
			}			

			return;
		}

		/// <summary>
		/// Handles transition between controller-to-character parenting states
		/// </summary>
		/// <param name="fromState">initial state</param>
		/// <param name="toState">target state</param>
		/// <returns></returns>
		private CtrlState ChangeControlState (CtrlState fromState, CtrlState toState)
		{
			if (fromState == toState)
				return toState;
			
			// Undo effects of the current state
			switch (fromState)
			{
				case CtrlState.None:
					break;

				case CtrlState.MaleControl:
					male_p_cf_bodybone.transform.parent = male.objTop.transform;
					break;

				case CtrlState.FemaleControl:
					if (SetParentMode.Value == ParentMode.AnimationOnly)
						femaleSpinePos.transform.parent = null;
					else
						SetParentToController(femaleSpinePos, femaleBase, true);
					break;

				case CtrlState.Following:
					if (SetParentMode.Value != ParentMode.PositionOnly)
						AddAnimSpeedController(obj_chaF_001);
					if (SetParentMode.Value == ParentMode.AnimationOnly)
						femaleSpinePos.transform.parent = null;
					male_p_cf_bodybone.transform.parent = male.objTop.transform;
					break;

				case CtrlState.Stationary:
					if (SetParentMode.Value != ParentMode.AnimationOnly)
						SetParentToController(femaleSpinePos, femaleBase, true);
					if (SetParentMode.Value != ParentMode.PositionOnly)
						AddAnimSpeedController(obj_chaF_001);
					break;
			}
			
			//Apply effects of the target state and update current state to target state
			switch (toState)
			{
				case CtrlState.None:
					return CtrlState.None;

				case CtrlState.MaleControl:
					male_p_cf_bodybone.transform.parent = ParentSideController(oppositeSide: true).transform;
					return CtrlState.MaleControl;

				case CtrlState.FemaleControl:
					SetParentToController(femaleSpinePos, femaleBase, false, notParentSide: true);
					return CtrlState.FemaleControl;

				case CtrlState.Following:
					if (SetParentMode.Value == ParentMode.AnimationOnly)
						SetParentToController(femaleSpinePos, femaleBase, false);
					if (obj_chaF_001.GetComponent<AnimSpeedController>() != null)
					{
						UnityEngine.Object.Destroy(obj_chaF_001.GetComponent<AnimSpeedController>());
					}
					male_p_cf_bodybone.transform.parent = female_p_cf_bodybone.transform;
					return CtrlState.Following;

				case CtrlState.Stationary:
					if (SetParentMode.Value != ParentMode.AnimationOnly)
						femaleSpinePos.transform.parent = null;
					if (obj_chaF_001.GetComponent<AnimSpeedController>() != null)
					{
						UnityEngine.Object.Destroy(obj_chaF_001.GetComponent<AnimSpeedController>());
					}
					return CtrlState.Stationary;

				default:
					return CtrlState.None;
			}
		}

		/// <summary>
		/// Returns true if menu button and trigger are pressed at the same time, or if the keyboard shortcut for activating SetParent is pressed.
		/// </summary>
		/// <param name="isParentLeft">Returns true if the right controller is pressed. Otherwise returns false.</param>
		/// <returns></returns>
		private bool SetParentToggleCondition(out bool isParentLeft)
		{
			if (MenuPressing(Side.Right) && TriggerPressing(Side.Right) && (MenuPressDown(Side.Right) || TriggerPressDown(Side.Right)))
			{
				isParentLeft = true;
				return true;
			}		
			else if (MenuPressing(Side.Left) && TriggerPressing(Side.Left) && (MenuPressDown(Side.Left) || TriggerPressDown(Side.Left)))
			{
				isParentLeft = false;
				return true;
			}	
			else if (Input.GetKeyDown(SetParentToggle.Value.MainKey) && SetParentToggle.Value.Modifiers.All(x => Input.GetKey(x)))
			{
				isParentLeft = true;
				return true;
			}

			isParentLeft = false;
			return false;
		}

		private void InitMhamotoSync ()
        {

			//Controller that presses B
			InitController = controllers[Side.Right];
			// Set left controller initial reference transform
			ControllerMhamoto = controllers[Side.Left];
			ControllerInitState.transform.rotation = ControllerMhamoto.transform.rotation;
			HipAbstraction.transform.parent = ControllerMhamoto.transform;
			
			//Calculate the offset between parented controller and bellybutton
			BellyButtonOffset = InitController.transform.position - ControllerMhamoto.transform.position;
			InitialBellyButtonOffset = BellyButtonOffset;

			HipAbstraction.transform.position = InitController.transform.position;
			HipAbstraction.transform.rotation = ControllerMhamoto.transform.rotation;

			Vector3 CameraForwardNoUp = new Vector3(cameraEye.transform.forward.x, 0, cameraEye.transform.forward.z);
			HMDAbstraction.transform.rotation = Quaternion.LookRotation(CameraForwardNoUp, Vector3.up);
			// Hip is supposed to stand opposite of player on init.
			HipAbstraction.transform.rotation = Quaternion.LookRotation(-HMDAbstraction.transform.forward, Vector3.up);

			// Parent and set initial pose
			PushSetParentButton(true);                                                    // Parent girl to left controller
			var scene = FindObjectOfType<VRHScene>();

			// Find all the HPointDatas
			ActionMap map = Singleton<Scene>.Instance.commonSpace.GetComponentInChildren<ActionMap>();
			List<GameObject> objs = GlobalMethod.LoadAllFolder<GameObject>("h/common/", "HPoint_" + map.no, null, null);
			GameObject objPointFree = UnityEngine.Object.Instantiate<GameObject>(objs[objs.Count - 1]);
			HPointData[] datas = objPointFree.GetComponentsInChildren<HPointData>(true);

			// Find the H-point closest to us.
			CurrentHPoint = datas[0];
			foreach (HPointData ahpointdata in datas)
			{
				float distance = Vector3.Distance(ahpointdata.transform.position, cameraEye.transform.position);
				if (distance < ClosestHpointDistance)
				{
					ClosestHpointDistance = distance;
					CurrentHPoint = ahpointdata;
				}

			}
			ClosestHpointDistance = 9999999f;

			// Changes to Sonyu
			//scene.ChangeCategory(CurrentHPoint, 2);

			//HSceneProc.AnimationListInfo animlistinfo = hFlag.selectAnimationListInfo;						// Breaks stuff
			//animlistinfo.mode = HFlag.EMode.sonyu;															// Breaks stuff
			//animlistinfo.nameAnimation = "khs_f_n10";	// Change the selected animation to reverse cowgirl

			// Change the H mode to Sonyu
			//hFlag.mode = HFlag.EMode.sonyu;				// Doesn't break anything
			
						// Doesn't break anything
			//hFlag.click = HFlag.ClickKind.slow;
			//hFlag.click = HFlag.ClickKind.actionChange;


			//hSprite.SetSonyuStart();					// Doesn't break anything


			//hFlag.nowAnimationInfo.nameAnimation = "khs_f_02";
			//hFlag.nowAnimStateName = "WLoop";			// Breaks stuff
			//sonyu.SetPlay("khs_f_n04");
			//hFlag.nowAnimStateName = "InsertIdle";
			//hFlag.click = HFlag.ClickKind.modeChange;	// Breaks stuff


			//sonyu = Traverse.Create(scene).Field("lstProc").GetValue<List<HActionBase>>().OfType<HSonyu>().FirstOrDefault();		// Doesn't break anything
			//sonyu.Proc();                   // Doesn't break anything


			//hFlag.nowAnimationInfo.nameAnimation = "Doggystyle";
			//sonyu.SetPlay("SLoop", true);
			


			/* //This code doesn't break anything
            HSceneProc.AnimationListInfo MyAnimList = hFlag.nowAnimationInfo;
            MyAnimList.mode = HFlag.EMode.sonyu;
            MyAnimList.nameAnimation = "Doggystyle";
            scene.flags.selectAnimationListInfo = MyAnimList;

			hSprite.OnInsertNoVoiceClick();

			hFlag.nowAnimStateName = "WLoop";
			*/



			//public bool SonyuProc()


			StartCoroutine(ChangeMotion("h/anim/female/02_00_00.unity3d", "khs_f_n10"));        //Change girl to reverse cowgirl pose, somehow makes things work.
			LockedPose = PoseType.ReverseCowgirl;
			//PushSetParentButton(true);
			SetP(true);                                                                         // Necessary to stop the girl from drifting away from our controller

			  
			MhamotoStarted = true;
		}

		private void MhamotoSync ()
        {
			//Get controller relative rotation compared to start transform
			Quaternion rotationDelta =  ControllerMhamoto.transform.rotation * Quaternion.Inverse(ControllerInitState.transform.rotation);

			BellyButtonOffset = rotationDelta * InitialBellyButtonOffset;
			//HipAbstraction.transform.position = ControllerMhamoto.transform.position - BellyButtonOffset;		// For debugging laser pointer

			// Update HMD abstract reference transform
			Vector3 CameraForwardNoUp = new Vector3(cameraEye.transform.forward.x, 0, cameraEye.transform.forward.z);
			HMDAbstraction.transform.rotation = Quaternion.LookRotation(CameraForwardNoUp, Vector3.up);

			HMDAbstraction.transform.position = cameraEye.transform.position;       // For debugging laser pointer

			Vector3 UpBackRight = Vector3.Normalize(new Vector3(-1, 1, 1));
			Vector3 firstvec = HipAbstraction.transform.TransformDirection(UpBackRight);
			Vector3 secondvec = HMDAbstraction.transform.InverseTransformDirection(firstvec);

			// Debugging line renders
			//DrawLine(HipAbstraction.transform.position, HipAbstraction.transform.position + firstvec * 100, Color.red);
			//DrawLine(HMDAbstraction.transform.position, HMDAbstraction.transform.position + HMDAbstraction.transform.forward * 100, Color.white);


			// Get Dot products, for determining in which pose quadrant we are in.
			float PoseVectorDotForwardUp = Vector3.Dot(firstvec, HMDAbstraction.transform.up);
			float PoseVectorDotForwardRight = Vector3.Dot(firstvec, HMDAbstraction.transform.right);
			float PoseVectorDotForwardForward = Vector3.Dot(firstvec, HMDAbstraction.transform.forward);

			//float PoseVectorDotUpUp = Vector3.Dot(HipAbstraction.transform.up, HMDAbstraction.transform.up);
			//float PoseVectorDotUpRight = Vector3.Dot(HipAbstraction.transform.up, HMDAbstraction.transform.right);
			//float PoseVectorDotUpForward = Vector3.Dot(HipAbstraction.transform.up, HMDAbstraction.transform.forward);

			// Logging for debugging
			var myLogSource = BepInEx.Logging.Logger.CreateLogSource("MyLogSource");

			

			if ((PoseVectorDotForwardUp > 0) && (PoseVectorDotForwardRight > 0) && (PoseVectorDotForwardForward < 0))
            {
				if (LockedPose != PoseType.Carrying)
				{
					StartCoroutine(ChangeMotion("h/anim/female/02_00_00.unity3d", "khs_f_n04"));        //Change girl to cowgirl(strength) pose
					SetP(true);
					//sonyu.SetPlay("khs_f_n04");
					LockedPose = PoseType.Carrying;
					myLogSource.LogInfo("Entered Carrying");
					myLogSource.LogInfo("PoseVectorDot Forward Up, right, forward: ");
					myLogSource.LogInfo(PoseVectorDotForwardUp);
					myLogSource.LogInfo(PoseVectorDotForwardRight);
					myLogSource.LogInfo(PoseVectorDotForwardForward);
				}
			}

			if ((PoseVectorDotForwardUp < 0) && (PoseVectorDotForwardRight < 0) && (PoseVectorDotForwardForward > 0))
			{
				/*
				if (LockedPose != PoseType.Doggy)
				{
					StartCoroutine(ChangeMotion("h/anim/female/02_00_00.unity3d", "khs_f_02"));        //Change girl to kneeling doggystyle pose
					SetP(true);
					//sonyu.SetPlay("khs_f_02");
					LockedPose = PoseType.Doggy;
					myLogSource.LogInfo("Entered Doggy");
					myLogSource.LogInfo("PoseVectorDot Forward Up, right, forward: ");
					myLogSource.LogInfo(PoseVectorDotForwardUp);
					myLogSource.LogInfo(PoseVectorDotForwardRight);
					myLogSource.LogInfo(PoseVectorDotForwardForward);
				}
				*/
			}

			if ((PoseVectorDotForwardUp > 0) && (PoseVectorDotForwardRight > 0) && (PoseVectorDotForwardForward > 0))
			{
				/*
				 if (LockedPose != PoseType.Missionary)
				{
					StartCoroutine(ChangeMotion("h/anim/female/02_00_00.unity3d", "khs_f_n00"));        //Change girl to Mating press pose
					SetP(true);
					//sonyu.SetPlay("khs_f_00");
					LockedPose = PoseType.Missionary;
					myLogSource.LogInfo("Entered Missionary");
					myLogSource.LogInfo("PoseVectorDot Forward Up, right, forward: ");
					myLogSource.LogInfo(PoseVectorDotForwardUp);
					myLogSource.LogInfo(PoseVectorDotForwardRight);
					myLogSource.LogInfo(PoseVectorDotForwardForward);
				}
				*/
			}

			if ((PoseVectorDotForwardUp > 0) && (PoseVectorDotForwardRight < 0) && (PoseVectorDotForwardForward > 0))
			{
				if (LockedPose != PoseType.ReverseCowgirl)
				{
					StartCoroutine(ChangeMotion("h/anim/female/02_00_00.unity3d", "khs_f_02"));        //Change girl to kneeling doggystyle pose
					SetP(true);
					//sonyu.SetPlay("khs_f_n10");
					LockedPose = PoseType.ReverseCowgirl;
					myLogSource.LogInfo("Entered ReverseCowgirl");
					myLogSource.LogInfo("PoseVectorDot Forward Up, right, forward: ");
					myLogSource.LogInfo(PoseVectorDotForwardUp);
					myLogSource.LogInfo(PoseVectorDotForwardRight);
					myLogSource.LogInfo(PoseVectorDotForwardForward);
				}
			}

			BepInEx.Logging.Logger.Sources.Remove(myLogSource);

		}

        void InitFPOV()
        {
			if (FPOVHeadDummy.transform.parent == null)
			{
				FPOVHeadDummy.transform.position = female_cf_j_head.transform.position;
				FPOVHeadDummy.transform.rotation = female_cf_j_head.transform.rotation;

				FPOVHeadDummy.transform.parent = female_cf_j_head.transform;
			}

			FPOVHeadDummy.transform.localPosition = new Vector3(0, 0, 0);
			cameraEye.transform.parent = FPOVHeadDummy.transform;

			//FPOVHeadDummy.transform.localPosition = -cameraEye.transform.localPosition;

			controllers[Side.Right].transform.parent = female_cf_j_head.transform;
			controllers[Side.Left].transform.parent = female_cf_j_head.transform;

			FPOVStarted = true;
        }

        void FPOV()
        {
            return;
        }

		void InitDakiMode()
        {
			vrSystem = OpenVR.System;

			// FoundTrackedObjs = FindObjectsOfType<SteamVR_TrackedObject>();  // Doesn't detect the vive trackers

			Transform Mytransform = base.transform;
			var scene = FindObjectOfType<VRHScene>();
			//Mytransform.parent = scene.managerVR.scrCamera.origin;
			//Mytransform.parent = scene.managerVR.objBase.transform;		// This one doesn't make you move with hand + trigger
			//Mytransform.parent = scene.managerVR.objMove.transform;		// This one also moves with you when you move the scene by hand + trigger
			Mytransform.parent = cameraEye.transform.parent;			// This one moves with you when you move the scene by hand + trigger

			ViveTracker.transform.parent = Mytransform;
			ReadtrackerPos();

			DakiCameraDummy.transform.parent = cameraEye.transform.parent;
			cameraEye.transform.parent = DakiCameraDummy.transform;
			DakiCameraDummy.transform.position = new Vector3(0, cameraEye.transform.position.y, 0);

			controllers[Side.Right].transform.parent = DakiCameraDummy.transform;
			controllers[Side.Left].transform.parent = DakiCameraDummy.transform;

			//OriginalFemaleParent = female_cf_j_hips.transform.parent;

			//female_cf_j_hips.transform.parent = ViveTracker.transform;
			//female_cf_j_hips.transform.position = ViveTracker.transform.position;
			//female_cf_j_hips.transform.rotation = ViveTracker.transform.rotation;

			DakiModeStarted = true;
			SetP(false);

			return;
        }

		void DakiMode()
        {
			var myLogSource = BepInEx.Logging.Logger.CreateLogSource("MyLogSource");

			ReadtrackerPos();

			/*
			foreach (SteamVR_TrackedObject Myobj in FoundTrackedObjs)
			{
				myLogSource.LogInfo("TrackedObj index and pos: ");
				myLogSource.LogInfo(Myobj.index);
				myLogSource.LogInfo(Myobj.transform.position);
			}
			*/

			
			myLogSource.LogInfo("Left controller world pos: ");
			myLogSource.LogInfo(controllers[Side.Left].transform.position);
			myLogSource.LogInfo("Tracker world pos: ");
			myLogSource.LogInfo(ViveTracker.transform.position);
			myLogSource.LogInfo("Tracker SteamVR pos: ");
			myLogSource.LogInfo(RawTrackerInfo.transform.position);


			BepInEx.Logging.Logger.Sources.Remove(myLogSource);
			return;
        }

		private bool ReadtrackerPos()
		{
			vrSystem.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, 0, allPoses);

			var pose = allPoses[3];
			var poseHMD = allPoses[0];
			var poseController = allPoses[2];

			if (pose.bPoseIsValid)
			{
				var absTracking = pose.mDeviceToAbsoluteTracking;
				var mat = new SteamVR_Utils.RigidTransform(absTracking);

				var absTrackingHMD = poseHMD.mDeviceToAbsoluteTracking;
				var matHMD = new SteamVR_Utils.RigidTransform(absTrackingHMD);

				var absTrackingController = poseController.mDeviceToAbsoluteTracking;
				var matController = new SteamVR_Utils.RigidTransform(absTrackingController);

				Vector3 ParentForward;
				Vector3 ParentRight;
				Vector3 ParentUp;

				Vector3 HMDForward;
				Vector3 HMDRight;
				Vector3 HMDUp;

				Vector3 TrackerForward;
				Vector3 TrackerRight;
				Vector3 TrackerUp;

				ParentForward = cameraEye.transform.parent.InverseTransformDirection(Vector3.forward);
				ParentRight = cameraEye.transform.parent.InverseTransformDirection(Vector3.right);
				ParentUp = cameraEye.transform.parent.InverseTransformDirection(Vector3.up);

				HMDForward = matHMD.rot * Vector3.forward;
				HMDRight = matHMD.rot * Vector3.right;
				HMDUp = matHMD.rot * Vector3.up;

				TrackerForward = mat.rot * Vector3.forward;
				TrackerRight = mat.rot * Vector3.right;
				TrackerUp = mat.rot * Vector3.up;

				float TrackerPosForward = Vector3.Dot(mat.pos, TrackerForward);
				float TrackerPosRight = Vector3.Dot(mat.pos, TrackerRight);
				float TrackerPosUp = Vector3.Dot(mat.pos, TrackerUp);

				//ViveTracker.transform.localPosition = TrackerPosForward * ParentForward + TrackerPosRight * ParentRight + TrackerPosUp * ParentUp;		  // This one is same as mat.pos
				//ViveTracker.transform.position = TrackerPosForward * ParentForward + TrackerPosRight * ParentRight + TrackerPosUp * ParentUp;
				//ViveTracker.transform.localPosition = TrackerPosForward * Vector3.forward + TrackerPosRight * Vector3.right + TrackerPosUp * Vector3.up;    // this one sucks
				//ViveTracker.transform.localPosition = TrackerPosForward * HMDForward + TrackerPosRight * HMDRight + TrackerPosUp * HMDUp;


				//ViveTracker.transform.localPosition = mat.pos - matHMD.pos;		// Does not work well at all, rotates around the table, wich is probably 0,0,0
				//ViveTracker.transform.position = cameraEye.transform.TransformPoint(mat.pos - matHMD.pos);			// Somewhat works, but is bound to HMD transform
				//ViveTracker.transform.localPosition = cameraEye.transform.TransformPoint(mat.pos - matHMD.pos);			// Pretty much same results as below
				//ViveTracker.transform.rotation = mat.rot;                
				// Almost perfect, just need to invert 2 axes
				Vector3 v = mat.rot.eulerAngles;
				ViveTracker.transform.localRotation = Quaternion.Euler(v.x, v.y, v.z);							// rotation is perfect, though it's like steering her, definitely local.
				//ViveTracker.transform.localPosition = mat.pos;
				
				//ViveTracker.transform.localPosition = new Vector3(-mat.pos.z, mat.pos.y, mat.pos.x);			// Used to give good results, i think?
				ViveTracker.transform.localPosition = new Vector3(- mat.pos.x, mat.pos.y, - mat.pos.z);			// gives great results atm

				//ViveTracker.transform.localRotation = mat.rot;
				//ViveTracker.transform.localRotation = mat.rot;

				ViveTracker.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

				RawTrackerInfo.transform.position = mat.pos;
				RawTrackerInfo.transform.rotation = mat.rot;
				RawTrackerInfo.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

				return true;
			}
			return false;
		}

		void ResetState()
        {

			cameraEye.transform.parent = OriginalHMDParent;
			controllers[Side.Right].transform.parent = OriginalRightControllerParent;
			controllers[Side.Left].transform.parent = OriginalLeftControllerParent;


			if (MhamotoCounter > 0)
				UnsetP();

			//if (DakiModeStarted)
				//female_cf_j_hips.transform.parent = OriginalFemaleParent;

			MhamotoStarted = false;
			FPOVStarted = false;
			DakiModeStarted = false;

			return;
		}


		void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0.01f)
		{
			GameObject myLine = new GameObject();
			myLine.transform.position = start;
			myLine.AddComponent<LineRenderer>();
			LineRenderer lr = myLine.GetComponent<LineRenderer>();
			//lr.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
			lr.material = new Material(Shader.Find("Hidden/Internal-Colored")); 
			//lr.SetColors(color, color);
			lr.startColor = color;
			lr.endColor = color;
			//lr.SetWidth(0.1f, 0.1f);
			lr.startWidth = 0.1f;
			lr.endWidth = 0.1f;
			lr.SetPosition(0, start);
			lr.SetPosition(1, end);
			GameObject.Destroy(myLine, duration);
		}

		void InitMhamotoSpeak()
        {
			CreateFileWatcher(@"F:\SteamLibrary\steamapps\common\SkyrimVR\Data\Sound\fx\ConvAiFollower");

			MhamotoSpeakStarted = true;
        }

		void MhamotoSpeak()
        {

        }

		public static AudioClip LoadAudioClip(string path, AudioType type)
		{
			AudioClip result;
			using (WWW www = new WWW(BepInEx4.Utility.ConvertToWWWFormat(path)))
			{
				AudioClip audioClipCompressed = www.GetAudioClipCompressed(false, type);
				while (audioClipCompressed.loadState != AudioDataLoadState.Loaded)
				{
				}
				result = audioClipCompressed;
			}
			return result;
		}

		public void CreateFileWatcher(string path)
		{
			// Create a new FileSystemWatcher and set its properties.
			FileSystemWatcher watcher = new FileSystemWatcher();
			watcher.Path = path;
			/* Watch for changes in LastAccess and LastWrite times, and 
			   the renaming of files or directories. */
			watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
			   | NotifyFilters.FileName | NotifyFilters.DirectoryName;
			// Only watch text files.
			watcher.Filter = "*.wav";

			// Add event handlers.
			watcher.Changed += new FileSystemEventHandler(OnChanged);
			watcher.Created += new FileSystemEventHandler(OnChanged);
			watcher.Deleted += new FileSystemEventHandler(OnChanged);

			// Begin watching.
			watcher.EnableRaisingEvents = true;
		}

		// Define the event handlers.
		void OnChanged(object source, FileSystemEventArgs e)
		{
			audioSource = gameObject.AddComponent<AudioSource>();
			audioSource.clip = LoadAudioClip("F:/SteamLibrary/steamapps/common/SkyrimVR/Data/Sound/fx/ConvAiFollower/output.wav", AudioType.WAV);

			audioSource.Play(0);
		}

		/// <summary>
		/// Returns the controller that's acting as parent.
		/// </summary>
		/// <param name="oppositeSide">Whether to return the opposite side of the parent</param>
		/// <returns></returns>
		internal GameObject ParentSideController(bool oppositeSide = false) => (parentIsLeft ^ oppositeSide) ? controllers[Side.Left] : controllers[Side.Right];

		/// <summary>
		/// Returns the male hand that's on the same side as the parenting controller.
		/// </summary>
		/// <param name="oppositeSide">Whether to return the opposite side of the parent</param>
		/// <returns></returns>
		internal LimbName ParentSideMaleHand(bool oppositeSide = false) => (parentIsLeft ^ oppositeSide) ? LimbName.MaleLeftHand : LimbName.MaleRightHand;

		/// <summary>
		/// Returns the side that the parent controller is on
		/// </summary>
		/// <param name="oppositeSide">Whether to return the opposite side of the parent</param>
		/// <returns></returns>
		internal Side ParentSideEnum(bool oppositeSide = false) => (parentIsLeft ^ oppositeSide) ? Side.Left : Side.Right;


		/// <summary>
		/// Describes the parenting relationship between the controller and the female/male character
		/// </summary>
		internal enum CtrlState
		{
			None,
			Stationary,
			Following,
			MaleControl,
			FemaleControl
		}

		internal enum Side
		{
			Left,
			Right
		}


		internal CtrlState currentCtrlstate;
		
		internal bool setFlag;

		private bool femaleExists;

		private HFlag hFlag;

		private ChaControl male;

		private ChaControl female;

		private FullBodyBipedIK maleFBBIK;

		private FullBodyBipedIK femaleFBBIK;

		private List<MotionIK> lstMotionIK;

		private string nowAnimState = "";
		
		internal GameObject parentController;

		private GameObject cameraEye;

		private GameObject shoulderCollider;

		private GameObject femaleAim;

		private float hideCount;

		internal HSprite hSprite;

		private GameObject obj_chaF_001;

		internal GameObject female_p_cf_bodybone;

		private GameObject female_cf_j_root;

		private GameObject female_cf_n_height;

		private GameObject female_cf_j_hips;

		private GameObject female_cf_j_spine01;

		private GameObject female_cf_j_spine02;

		private GameObject female_cf_j_spine03;

		private GameObject female_cf_j_neck;

		private GameObject femaleBase;

		private GameObject femaleSpinePos;

		internal GameObject male_p_cf_bodybone;

		private GameObject maleHeadPos;

		private GameObject maleCrotchPos;

		private Transform male_cf_pv_hips;

		private BaseData male_hips_bd;

		private Vector3[] vecSpinePos = new Vector3[SmoothBuffer];

		private int indexSpinePos;

		private Quaternion[] quatSpineRot = new Quaternion[SmoothBuffer];

		private int indexSpineRot;

		private bool hideCanvas;

		internal bool limbAutoAttach = true;

		internal bool parentIsLeft;

		private float[] lastTriggerRelease = new float[4] { 0, 0 ,0 ,0};

		private bool MhamotoStarted = false;

		Vector3 BellyButtonOffset;
		Vector3 InitialBellyButtonOffset;

		internal GameObject ControllerInitState = new GameObject("ControllerInitState");

		internal GameObject ControllerMhamoto = new GameObject();

		internal GameObject InitController = new GameObject();

		internal GameObject HipAbstraction = new GameObject("HipAbstraction");

		internal GameObject HipAbstractionInitState = new GameObject("HipAbstractionInitState");

		internal GameObject HMDAbstraction = new GameObject("HMDAbstraction");

		internal HSonyu sonyu;

		float ClosestHpointDistance = 9999999f;

		internal HPointData CurrentHPoint;

        bool FPOVStarted = false;

		internal GameObject FPOVHeadDummy = new GameObject();

		private GameObject female_cf_j_head;

		int MhamotoCounter = 0;

		int MhamotoState = 0;

		bool DakiModeStarted = false;

		Transform OriginalHMDParent = null;
		Transform OriginalLeftControllerParent = null;
		Transform OriginalRightControllerParent = null;
		Transform OriginalFemaleParent = null;

		CVRSystem vrSystem = OpenVR.System;
		TrackedDevicePose_t[] allPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

		internal GameObject ViveTracker = new GameObject("ViveTracker");
		SteamVR_TrackedObject[] FoundTrackedObjs;

		internal GameObject DakiCameraDummy = new GameObject();
		internal GameObject RawTrackerInfo = new GameObject("RawTrackerInfo");

		bool MhamotoSpeakStarted = false;

		AudioSource audioSource;


		internal enum PoseType
		{
			Carrying,
			Doggy,
			Missionary,
			Cowgirl,
			ReverseCowgirl,
			Spooning
		}
		PoseType LockedPose;


	}
}
