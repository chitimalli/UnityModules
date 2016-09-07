﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Leap.Unity {
  /**LeapHandAutoRig automates setting up the scripts that drive 3D skinned mesh hands. */
  [AddComponentMenu("Leap/Auto Rig Hands")]
  public class LeapHandsAutoRig : MonoBehaviour {
    private HandPool HandPoolToPopulate;
    public Animator AnimatorForMapping;
    private RuntimeAnimatorController runtimeAnimatorController;
    public string ModelGroupName = null;
    [Tooltip("Set to True if each finger has an extra trasform between palm and base of the finger.")] 
    public bool UseMetaCarpals;
    [Tooltip("When True, hands will be put into a Leap editor pose near the LeapServiceProvider's transform.  When False, the hands will be returned to their Start Pose if it has been saved.")]
    public bool SetEditorLeapPose = false;
    [Header("RiggedHand Components")]
    public RiggedHand RiggedHand_L;
    public RiggedHand RiggedHand_R;
    [Header("HandTransitionBehavior Components")]
    public HandTransitionBehavior HandTransitionBehavior_L;
    public HandTransitionBehavior HandTransitionBehavior_R;
    [Tooltip("Test")]
    [Header("RiggedFinger Components")]
    public RiggedFinger RiggedFinger_L_Thumb;
    public RiggedFinger RiggedFinger_L_Index;
    public RiggedFinger RiggedFinger_L_Mid;
    public RiggedFinger RiggedFinger_L_Ring;
    public RiggedFinger RiggedFinger_L_Pinky;
    public RiggedFinger RiggedFinger_R_Thumb;
    public RiggedFinger RiggedFinger_R_Index;
    public RiggedFinger RiggedFinger_R_Mid;
    public RiggedFinger RiggedFinger_R_Ring;
    public RiggedFinger RiggedFinger_R_Pinky;
    [Header("Palm & Finger Direction Vectors.")]
    public Vector3 modelFingerPointing_L = new Vector3(0, 0, 0);
    public Vector3 modelPalmFacing_L = new Vector3(0, 0, 0);
    public Vector3 modelFingerPointing_R = new Vector3(0, 0, 0);
    public Vector3 modelPalmFacing_R = new Vector3(0, 0, 0);
    [Tooltip("Toggling this value will reverse the ModelPalmFacing vectors to both RiggedHand's and all RiggedFingers.  Change if hands appear backward when tracking.")]
    [SerializeField]
    public bool FlipPalms = false;
    [SerializeField]
    [HideInInspector]
    private bool flippedPalmsState = false;
    private IKMarkersAssembly ikMarkersAssemblyPrefab;
    private IKMarkersAssembly iKMarkersAssembly;

    /**AutoRig() Calls AutoRigMecanim() if a Unity Avatar exists.  Otherwise, AutoRigByName() is called.  
     * Then it immediately RiggedHand.StoreJointStartPose() to store the rigged asset's original state.*/
    [ContextMenu("AutoRig")]
    public void AutoRig() {
      HandPoolToPopulate = GameObject.FindObjectOfType<HandPool>();
      AnimatorForMapping = gameObject.GetComponent<Animator>();
      if (AnimatorForMapping != null) {
        if (AnimatorForMapping.isHuman == true) {
          AnimatorForMapping.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("TestController");
          AutoRigMecanim();
          HandTransitionBehavior_L.OnSetup();
          HandTransitionBehavior_R.OnSetup();
          RiggedHand_L.StoreJointsStartPose();
          RiggedHand_R.StoreJointsStartPose();
          return;
        }
        else {
          Debug.LogWarning("The Mecanim Avatar for this asset does not contain a valid IsHuman definition.  Attempting to auto map by name.");
        }
      }
      AutoRigByName();
      RiggedHand_L.StoreJointsStartPose();
      RiggedHand_R.StoreJointsStartPose();
    }
    /**Allows a start pose for the rigged hands to be created and stored anytime. */
    [ContextMenu("StoreStartPose")]
    public void StoreStartPose() {
      if (RiggedHand_L && RiggedHand_R) {
        RiggedHand_L.StoreJointsStartPose();
        RiggedHand_R.StoreJointsStartPose();
      }
      else Debug.LogWarning("Please AutoRig before attempting to Store Start Pose");
    }
    /**Returns all hand joint transforms to their original local positions and local rotations */
    [ContextMenu("ResetStartPose")]
    public void RestoreStartPose() {
      if (RiggedHand_L && RiggedHand_R) {
        RiggedHand_L.RestoreJointsStartPose();
        RiggedHand_R.RestoreJointsStartPose();
      }
      else Debug.LogWarning("Please AutoRig and Start Pose before attempting to Reset to Start Pose");
    }
    /**Uses transform names to discover and assign RiggedHands scripts,
     * then calls methods in the RiggedHands that use transform nanes to discover fingers.*/
    [ContextMenu("AutoRigByName")]
    void AutoRigByName() {
      List<string> LeftHandStrings = new List<string> { "left", "_l" };
      List<string> RightHandStrings = new List<string> { "right", "_r" };

      //Assigning these here since this component gets added and used at editor time
      HandPoolToPopulate = GameObject.FindObjectOfType<HandPool>();
      Reset();

      //Find hands and assigns RiggedHands
      Transform Hand_L = null;
      foreach (Transform t in transform) {
        if (LeftHandStrings.Any(w => t.name.ToLower().Contains(w))) {
          Hand_L = t;
        }
      }
      RiggedHand_L = Hand_L.gameObject.AddComponent<RiggedHand>();
      HandTransitionBehavior_L = Hand_L.gameObject.AddComponent<HandEnableDisable>();
      RiggedHand_L.Handedness = Chirality.Left;
      RiggedHand_L.SetEditorLeapPose = false;
      RiggedHand_L.UseMetaCarpals = UseMetaCarpals;

      Transform Hand_R = null;
      foreach (Transform t in transform) {
        if (RightHandStrings.Any(w => t.name.ToLower().Contains(w))) {
          Hand_R = t;
        }
      }
      RiggedHand_R = Hand_R.gameObject.AddComponent<RiggedHand>();
      HandTransitionBehavior_R = Hand_R.gameObject.AddComponent<HandEnableDisable>();
      RiggedHand_R.Handedness = Chirality.Right;
      RiggedHand_R.SetEditorLeapPose = false;
      RiggedHand_R.UseMetaCarpals = UseMetaCarpals;

      //Find palms and assign to RiggedHands
      //RiggedHand_L.palm = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftHand);
      //RiggedHand_R.palm = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightHand);
      RiggedHand_L.SetupRiggedHand();
      RiggedHand_R.SetupRiggedHand();

      if (ModelGroupName == "" || ModelGroupName != null) {
        ModelGroupName = transform.name;
      }
      HandPoolToPopulate.AddNewGroup(ModelGroupName, RiggedHand_L, RiggedHand_R);

      RiggedFinger_L_Thumb = (RiggedFinger)RiggedHand_L.fingers[0];
      RiggedFinger_L_Index = (RiggedFinger)RiggedHand_L.fingers[1];
      RiggedFinger_L_Mid = (RiggedFinger)RiggedHand_L.fingers[2];
      RiggedFinger_L_Ring = (RiggedFinger)RiggedHand_L.fingers[3];
      RiggedFinger_L_Pinky = (RiggedFinger)RiggedHand_L.fingers[4];
      RiggedFinger_R_Thumb = (RiggedFinger)RiggedHand_R.fingers[0];
      RiggedFinger_R_Index = (RiggedFinger)RiggedHand_R.fingers[1];
      RiggedFinger_R_Mid = (RiggedFinger)RiggedHand_R.fingers[2];
      RiggedFinger_R_Ring = (RiggedFinger)RiggedHand_R.fingers[3];
      RiggedFinger_R_Pinky = (RiggedFinger)RiggedHand_R.fingers[4];

      modelFingerPointing_L = RiggedHand_L.modelFingerPointing;
      modelPalmFacing_L = RiggedHand_L.modelPalmFacing;
      modelFingerPointing_R = RiggedHand_R.modelFingerPointing;
      modelPalmFacing_R = RiggedHand_R.modelPalmFacing;
    }

    /**Uses Mecanim transform mapping to find hands and assign RiggedHands scripts 
     * and to find base of each finger and asisng RiggedFinger script.
     * Then calls methods in the RiggedHands that use transform names to discover fingers */
    [ContextMenu("AutoRigMecanim")]
    void AutoRigMecanim() {
      //Assigning these here since this component gets added and used at editor time
      AnimatorForMapping = gameObject.GetComponent<Animator>();
      HandPoolToPopulate = GameObject.FindObjectOfType<HandPool>();
      Reset();

      ikMarkersAssemblyPrefab = Resources.Load<IKMarkersAssembly>("IKMarkersAssembly");
      iKMarkersAssembly = GameObject.Instantiate(ikMarkersAssemblyPrefab) as IKMarkersAssembly;
      iKMarkersAssembly.transform.parent = transform;
      iKMarkersAssembly.transform.localPosition = new Vector3(0, 0, 0);

      //Find hands and assign RiggedHands
      Transform Hand_L = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftHand);
      if (Hand_L.GetComponent<RiggedHand>()) {
        RiggedHand_L = Hand_L.GetComponent<RiggedHand>();
      }
      else RiggedHand_L = Hand_L.gameObject.AddComponent<RiggedHand>();
      WristLeapToIKBlend WristLeapToIKBlend_L = Hand_L.gameObject.AddComponent<WristLeapToIKBlend>();
      HandTransitionBehavior_L = WristLeapToIKBlend_L;
      WristLeapToIKBlend_L.m_IKMarkerAssembly = iKMarkersAssembly;
      //WristLeapToIKBlend_L.AssignIKMarkers();
      RiggedHand_L.Handedness = Chirality.Left;
      RiggedHand_L.SetEditorLeapPose = false;

      Transform Hand_R = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightHand);
      if (Hand_R.GetComponent<RiggedHand>()) {
        RiggedHand_R = Hand_R.GetComponent<RiggedHand>();
      }
      else RiggedHand_R = Hand_R.gameObject.AddComponent<RiggedHand>();
      WristLeapToIKBlend WristLeapToIKBlend_R = Hand_R.gameObject.AddComponent<WristLeapToIKBlend>();
      HandTransitionBehavior_R = WristLeapToIKBlend_R;
      WristLeapToIKBlend_R.m_IKMarkerAssembly = iKMarkersAssembly;
      //WristLeapToIKBlend_R.AssignIKMarkers();
      RiggedHand_R.Handedness = Chirality.Right;
      RiggedHand_R.SetEditorLeapPose = false;

      //Find palms and assign to RiggedHands
      RiggedHand_L.palm = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftHand);
      RiggedHand_R.palm = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightHand);

      //Find Fingers and assign RiggedFingers
      RiggedFinger_L_Thumb = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftThumbProximal).gameObject.AddComponent<RiggedFinger>();
      RiggedFinger_L_Thumb.fingerType = Finger.FingerType.TYPE_THUMB;
      RiggedFinger_L_Index = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftIndexProximal).gameObject.AddComponent<RiggedFinger>();
      RiggedFinger_L_Index.fingerType = Finger.FingerType.TYPE_INDEX;
      RiggedFinger_L_Mid = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftMiddleProximal).gameObject.AddComponent<RiggedFinger>();
      RiggedFinger_L_Mid.fingerType = Finger.FingerType.TYPE_MIDDLE;
      RiggedFinger_L_Ring = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftRingProximal).gameObject.AddComponent<RiggedFinger>();
      RiggedFinger_L_Ring.fingerType = Finger.FingerType.TYPE_RING;
      RiggedFinger_L_Pinky = AnimatorForMapping.GetBoneTransform(HumanBodyBones.LeftLittleProximal).gameObject.AddComponent<RiggedFinger>();
      RiggedFinger_L_Pinky.fingerType = Finger.FingerType.TYPE_PINKY;
      RiggedFinger_R_Thumb = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightThumbProximal).gameObject.AddComponent<RiggedFinger>();
      RiggedFinger_R_Thumb.fingerType = Finger.FingerType.TYPE_THUMB;
      RiggedFinger_R_Index = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightIndexProximal).gameObject.AddComponent<RiggedFinger>();
      RiggedFinger_R_Index.fingerType = Finger.FingerType.TYPE_INDEX;
      RiggedFinger_R_Mid = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightMiddleProximal).gameObject.AddComponent<RiggedFinger>();
      RiggedFinger_R_Mid.fingerType = Finger.FingerType.TYPE_MIDDLE;
      RiggedFinger_R_Ring = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightRingProximal).gameObject.AddComponent<RiggedFinger>();
      RiggedFinger_R_Ring.fingerType = Finger.FingerType.TYPE_RING;
      RiggedFinger_R_Pinky = AnimatorForMapping.GetBoneTransform(HumanBodyBones.RightLittleProximal).gameObject.AddComponent<RiggedFinger>();
      RiggedFinger_R_Pinky.fingerType = Finger.FingerType.TYPE_PINKY;

      RiggedHand_L.AutoRigRiggedHand(RiggedHand_L.palm, RiggedFinger_L_Pinky.transform, RiggedFinger_L_Index.transform);
      RiggedHand_R.AutoRigRiggedHand(RiggedHand_R.palm, RiggedFinger_R_Pinky.transform, RiggedFinger_R_Index.transform);
      if (ModelGroupName == "" || ModelGroupName != null) {
        ModelGroupName = transform.name;
      }
      HandPoolToPopulate.AddNewGroup(ModelGroupName, RiggedHand_L, RiggedHand_R);

      modelFingerPointing_L = RiggedHand_L.modelFingerPointing;
      modelPalmFacing_L = RiggedHand_L.modelPalmFacing;
      modelFingerPointing_R = RiggedHand_R.modelFingerPointing;
      modelPalmFacing_R = RiggedHand_R.modelPalmFacing;
      AutoRigUpperBody();
    }
    /**Removes existing RiggedFinger components so the auto rigging process can be rerun. */
    void Reset() {
      RiggedFinger[] riggedFingers = GetComponentsInChildren<RiggedFinger>();
      foreach (RiggedFinger finger in riggedFingers) {
        DestroyImmediate(finger);
      }
      DestroyImmediate(RiggedHand_L);
      DestroyImmediate(RiggedHand_R);
      DestroyImmediate(HandTransitionBehavior_L);
      DestroyImmediate(HandTransitionBehavior_R);
      if (HandPoolToPopulate != null) {
        HandPoolToPopulate.RemoveGroup(ModelGroupName);
      }
    }
    [ContextMenu("AutoRigUpperBody")]
    public void AutoRigUpperBody() {
      OnAnimatorIKCaller onAnimatorIKCaller =  gameObject.AddComponent<OnAnimatorIKCaller>();
      onAnimatorIKCaller.wristLeapToIKBlend_L = (WristLeapToIKBlend)HandTransitionBehavior_L;
      onAnimatorIKCaller.wristLeapToIKBlend_R = (WristLeapToIKBlend)HandTransitionBehavior_R;
      SpineFollowTargetBehavior spineFollowTargetBehavior = gameObject.AddComponent<SpineFollowTargetBehavior>();
      ShoulderTurnBehavior shoulderTurnBehavior = gameObject.AddComponent<ShoulderTurnBehavior>();
      //Assign and configure Animator Controller to Animator
      //AnimatorForMapping.runtimeAnimatorController = runtimeAnimatorController;
      AnimatorForMapping.updateMode = AnimatorUpdateMode.AnimatePhysics;
      AnimatorForMapping.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }
    //Monobehavior's OnValidate() is used to push LeapHandsAutoRig values to RiggedHand and RiggedFinger components
    void OnValidate() {
      if (FlipPalms != flippedPalmsState) {
        modelPalmFacing_L = modelPalmFacing_L * -1f;
        modelPalmFacing_R = modelPalmFacing_R * -1f;
        flippedPalmsState = FlipPalms;
      }


      //push palm and finger facing values to RiggedHand's and RiggedFinger's
      if (RiggedHand_L) {
        RiggedHand_L.SetEditorLeapPose = SetEditorLeapPose;
        RiggedHand_L.modelFingerPointing = modelFingerPointing_L;
        RiggedHand_L.modelPalmFacing = modelPalmFacing_L;
      }
      if (RiggedHand_R) {
        RiggedHand_R.SetEditorLeapPose = SetEditorLeapPose;
        RiggedHand_R.modelFingerPointing = modelFingerPointing_R;
        RiggedHand_R.modelPalmFacing = modelPalmFacing_R;
      }
      if (RiggedFinger_L_Thumb) {
        RiggedFinger_L_Thumb.modelFingerPointing = modelFingerPointing_L;
        RiggedFinger_L_Thumb.modelPalmFacing = modelPalmFacing_L;
      }
      if (RiggedFinger_L_Index) {
        RiggedFinger_L_Index.modelFingerPointing = modelFingerPointing_L;
        RiggedFinger_L_Index.modelPalmFacing = modelPalmFacing_L;
      }
      if (RiggedFinger_L_Mid) {
        RiggedFinger_L_Mid.modelFingerPointing = modelFingerPointing_L;
        RiggedFinger_L_Mid.modelPalmFacing = modelPalmFacing_L;
      }
      if (RiggedFinger_L_Ring) {
        RiggedFinger_L_Ring.modelFingerPointing = modelFingerPointing_L;
        RiggedFinger_L_Ring.modelPalmFacing = modelPalmFacing_L;
      }
      if (RiggedFinger_L_Pinky) {
        RiggedFinger_L_Pinky.modelFingerPointing = modelFingerPointing_L;
        RiggedFinger_L_Pinky.modelPalmFacing = modelPalmFacing_L;
      }
      if (RiggedFinger_R_Thumb) {
        RiggedFinger_R_Thumb.modelFingerPointing = modelFingerPointing_R;
        RiggedFinger_R_Thumb.modelPalmFacing = modelPalmFacing_R;
      }
      if (RiggedFinger_R_Index) {
        RiggedFinger_R_Index.modelFingerPointing = modelFingerPointing_R;
        RiggedFinger_R_Index.modelPalmFacing = modelPalmFacing_R;
      }
      if (RiggedFinger_R_Mid) {
        RiggedFinger_R_Mid.modelFingerPointing = modelFingerPointing_R;
        RiggedFinger_R_Mid.modelPalmFacing = modelPalmFacing_R;
      }
      if (RiggedFinger_R_Ring) {
        RiggedFinger_R_Ring.modelFingerPointing = modelFingerPointing_R;
        RiggedFinger_R_Ring.modelPalmFacing = modelPalmFacing_R;
      }
      if (RiggedFinger_R_Pinky) {
        RiggedFinger_R_Pinky.modelFingerPointing = modelFingerPointing_R;
        RiggedFinger_R_Pinky.modelPalmFacing = modelPalmFacing_R;
      }
    }
    /**Removes the ModelGroup from HandPool that corresponds to this instance of LeapHandsAutoRig */
    void OnDestroy() {
      if (HandPoolToPopulate != null) {
        HandPoolToPopulate.RemoveGroup(ModelGroupName);
      }
    }
  }
}