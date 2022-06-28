﻿using System;
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
using static SetParentKK.KK_MhamotoVR;

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
						MhamotoState = MhamotoCounter % 3;

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
									InitFBTCalibration();
									break;
                                }
							case 2:
                                {
									InitFPOV();
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
				if (FBTCalibrationStarted)
                {
					FBTCalibration();
                }
                if (FPOVStarted)
                {
                    FPOV();
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


		private void SetParentToController(GameObject parentDummy, GameObject target, bool hideModel, bool notParentSide = false)
		{
			GameObject controller = ParentSideController(notParentSide);



			if (FPOVStarted)
			{
				parentDummy.transform.parent = FindNearestTracker(target.transform.position).Tracker.transform;
			}
			else
			{ 
				parentDummy.transform.parent = controller.transform;
			}

			if (hideModel)
			{
				controller.transform.Find("Model").gameObject.SetActive(false);

				if (SetControllerCollider.Value && !limbs[(int)ParentSideMaleHand(notParentSide)].AnchorObj)
				{
					controller.transform.Find("ControllerCollider").GetComponent<SphereCollider>().enabled = false;
				}		
			}

			if (FPOVStarted)
			{
				//parentDummy.transform.position = MyTracker.transform.position;
				//parentDummy.transform.rotation = MyTracker.transform.rotation;
				parentDummy.transform.position = target.transform.position;
				parentDummy.transform.rotation = target.transform.rotation;
			}
			else
			{
				parentDummy.transform.position = target.transform.position;
				parentDummy.transform.rotation = target.transform.rotation;
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

		void InitFBTCalibration()
        {
			//Teleport to girl's location
			if (FPOVHeadDummy.transform.parent == null)
			{
				FPOVHeadDummy.transform.position = female_cf_j_head.transform.position;
				FPOVHeadDummy.transform.rotation = female_cf_j_head.transform.rotation;
			}

			cameraEye.transform.parent = FPOVHeadDummy.transform;

			controllers[Side.Right].transform.parent = FPOVHeadDummy.transform;
			controllers[Side.Left].transform.parent = FPOVHeadDummy.transform;

			TrackersManager = FindObjectOfType<SteamVR_ControllerManager>();

			// Adjust the size of the tracked objects array
			if (TrackersManager.objects.Length < 16)
            {
				GameObject[] DummyArray = new GameObject[TrackersManager.objects.Length];
				TrackersManager.objects.CopyTo(DummyArray, 0);
				Array.Resize<GameObject>(ref TrackersManager.objects, 16);
				DummyArray.CopyTo(TrackersManager.objects, 0);
			}
			
			//Spawn trackers with cubes
			for (int i = 0; i < NumTrackers;  i++)
            {
				ViveTracker NewTracker = new ViveTracker();
				NewTracker.Init(TrackersManager, this);
				MyTrackers.Add(NewTracker);
            }
			
			/*
			Cube1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
			Cube2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
			Cube3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
			Cube4 = GameObject.CreatePrimitive(PrimitiveType.Cube);
			*/

			FBTCalibrationStarted = true;
		}

		void FBTCalibration()
        {
			foreach (ViveTracker UpdateTracker in MyTrackers)
            {
				UpdateTracker.LateUpdate();
            }
			
			/*
			Cube1.transform.position = TrackersManager.objects[0].transform.position;
			Cube1.transform.rotation = TrackersManager.objects[0].transform.rotation;
			Cube1.transform.localScale = new Vector3(0.07f, 0.07f, 0.07f);

			Cube2.transform.position = TrackersManager.objects[1].transform.position;
			Cube2.transform.rotation = TrackersManager.objects[1].transform.rotation;
			Cube2.transform.localScale = new Vector3(0.07f, 0.07f, 0.07f);

			Cube3.transform.position = TrackersManager.objects[2].transform.position;
			Cube3.transform.rotation = TrackersManager.objects[2].transform.rotation;
			Cube3.transform.localScale = new Vector3(0.07f, 0.07f, 0.07f);

			Cube4.transform.position = TrackersManager.objects[3].transform.position;
			Cube4.transform.rotation = TrackersManager.objects[3].transform.rotation;
			Cube4.transform.localScale = new Vector3(0.07f, 0.07f, 0.07f);
			*/
		}


		void InitFPOV()
        {
			// Find tracker closest to bone
			Limb GirlLeftFoot = setParentObj.limbs[(int)LimbName.FemaleLeftFoot];
			LeftFootTracker = FindNearestTracker(GirlLeftFoot.AnimPos.position);

			Limb GirlRightFoot = setParentObj.limbs[(int)LimbName.FemaleRightFoot];
			RightFootTracker = FindNearestTracker(GirlRightFoot.AnimPos.position);

			// Parent bone to tracker

			//setParentObj.FixLimbToggle(setParentObj.limbs[(int)limbName], true);
			//setParentObj.limbs[(int)limbName].AnchorObj.transform.parent = other.transform;
			setParentObj.FixLimbToggle(GirlLeftFoot, true);
			GirlLeftFoot.AnchorObj.transform.parent = LeftFootTracker.TrackerCube.transform;

			setParentObj.FixLimbToggle(GirlRightFoot, true);
			GirlRightFoot.AnchorObj.transform.parent = RightFootTracker.TrackerCube.transform;

			FPOVStarted = true;
        }

        void FPOV()
        {
			return;
        }

		ViveTracker FindNearestTracker(Vector3 pos)
        {
			//Initialize the shortest things to the first tracker found
			ViveTracker ClosestTracker = MyTrackers[0];
			float ShortestDistance = Vector3.Distance(ClosestTracker.Tracker.transform.position, pos);

			// Loop through all the trackers and compare to find the closest tracker
			foreach (ViveTracker Findtracker in MyTrackers)
            {
				Vector3 trackerpos = Findtracker.Tracker.transform.position;
				float distance = Vector3.Distance(trackerpos, pos);
				if (distance < ShortestDistance)
                {
					ShortestDistance = distance;
					ClosestTracker = Findtracker;
                }

            }

			return ClosestTracker;

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

			FPOVStarted = false;

			return;
		}


		void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0.01f)
		{
			GameObject myLine = new GameObject();
			myLine.transform.position = start;
			myLine.AddComponent<LineRenderer>();
			LineRenderer lr = myLine.GetComponent<LineRenderer>();
			lr.material = new Material(Shader.Find("Hidden/Internal-Colored")); 
			lr.startColor = color;
			lr.endColor = color;
			lr.startWidth = 0.1f;
			lr.endWidth = 0.1f;
			lr.SetPosition(0, start);
			lr.SetPosition(1, end);
			GameObject.Destroy(myLine, duration);
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

		public GameObject cameraEye;

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


		//MhamotoVR

		internal GameObject ControllerInitState = new GameObject("ControllerInitState");

		internal GameObject ControllerMhamoto = new GameObject();

		internal GameObject InitController = new GameObject();

		internal GameObject HipAbstraction = new GameObject("HipAbstraction");

		internal GameObject HipAbstractionInitState = new GameObject("HipAbstractionInitState");

		internal GameObject HMDAbstraction = new GameObject("HMDAbstraction");

        bool FPOVStarted = false;

		bool FBTCalibrationStarted = false;

		internal GameObject FPOVHeadDummy = new GameObject();

		private GameObject female_cf_j_head;

		int MhamotoCounter = 0;

		int MhamotoState = 0;

		Transform OriginalHMDParent = null;
		Transform OriginalLeftControllerParent = null;
		Transform OriginalRightControllerParent = null;
		Transform OriginalFemaleParent = null;

		internal int NumTrackers = 3;
		public List<uint> FoundTrackerIndices = new List<uint>();
		public List<ViveTracker> MyTrackers = new List<ViveTracker>();
		internal SteamVR_ControllerManager TrackersManager;
		internal ViveTracker LeftFootTracker;
		internal ViveTracker RightFootTracker;

		internal GameObject Cube1;
		internal GameObject Cube2;
		internal GameObject Cube3;
		internal GameObject Cube4;
	}
}
