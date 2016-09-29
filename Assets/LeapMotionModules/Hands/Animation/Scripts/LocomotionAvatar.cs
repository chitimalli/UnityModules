﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR;
using System.Collections;

namespace Leap.Unity {
  public class LocomotionAvatar : MonoBehaviour {
    protected Animator animator;

    private float speed = 0;
    private float direction = 0;
    private Locomotion locomotion = null;

    private Vector3 moveDirection;

    
    private Vector3 distanceToRoot;
    private Vector3 rootDirection;
    public Transform LMRig;
    public bool LMRigToFollowAnimator;

    public Text standWalkStateText;
    public Text m_AnimatorStateText;
    public Text CenteringText;
    public Text DistanceText;
    public Text SpeedText;

    void Awake() {
      LMRig = GameObject.FindObjectOfType<LeapHandController>().transform.root;
    }
    
    void Start() {
      CenteringText.text = "";

      InputTracking.Recenter();
      animator = GetComponent<Animator>();
      locomotion = new Locomotion(animator);
      rootDirection = transform.forward;
    }

    void Update() {
      if (LMRigToFollowAnimator == true) {
        LMRigLococmotion();
      }
      else AnimatorLocomotion();

      if (standing) {
        standWalkStateText.text = "Idle/Turning";
      }
      else standWalkStateText.text = "WalkRun";

      AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(1);
      if (state.IsName("Locomotion.Idle")) {
        m_AnimatorStateText.text = "State: Idle";
      }
      if (state.IsName("Locomotion.TurnOnSpot")) {
        m_AnimatorStateText.text = "State: TurnOnSpot";
      }
      if (state.IsName("Locomotion.WalkRun")) {
        m_AnimatorStateText.text = "State: WalkRun";
      }
      DistanceText.text = distanceToRoot.magnitude.ToString("F2");
      SpeedText.text = animator.GetFloat("Speed").ToString("F2");

    }
    void LMRigLococmotion() {
      //Requires positioning of LMHeadMountedRig OnAnimatorIK()
      Vector3 flatCamPosition = transform.InverseTransformPoint(Camera.main.transform.position);
      flatCamPosition.y = 0;
      Vector3 flatRootPosition = transform.InverseTransformPoint(transform.position);
      flatRootPosition.y = 0;
      distanceToRoot = flatCamPosition - flatRootPosition;
      speed = distanceToRoot.magnitude;

      // Convert joystick input in Worldspace coordinates
      moveDirection = MoveDirectionCameraDirection();
      //Vector3 moveDirection = referentialShift * CameraDirection;
      Vector3 axis = Vector3.Cross(rootDirection, moveDirection);
      direction = Vector3.Angle(rootDirection, moveDirection) / 180f * (axis.y < 0 ? -1 : 1);
      if (speed > .01f) { //Dead "stick"
      }
      else {
        speed = 0.0f;
      }
      float joySpeed = 0;
      float joyDirection = 0;
      if (animator && Camera.main) {
        JoystickToEvents.Do(transform, Camera.main.transform, ref joySpeed, ref joyDirection);
        if (joyDirection != 0 || joyDirection != 0) {
          //direction += joyDirection;
          speed += joySpeed;
        }
        locomotion.Do(speed * 2 , (direction * 180 ), 1);
      }
    }
    Vector3 MoveDirectionCameraDirection() {
      // Get camera rotation.
      rootDirection = transform.forward;// +transform.position;
      Vector3 CameraDirection = Camera.main.transform.forward;
      CameraDirection.y = 0.0f;
      return CameraDirection;
    }
    Vector3 MoveDirectionTowardCamera () {
      // Get camera rotation.
      rootDirection = transform.forward;// +transform.position;
      Vector3 DirectionToCamera = Camera.main.transform.position - transform.position;
      DirectionToCamera.y = 0.0f;
      Quaternion referentialShift = Quaternion.FromToRotation(Vector3.forward, DirectionToCamera);
      // Convert joystick input in Worldspace coordinates
      return DirectionToCamera;
    }
    bool standing = true;
    
    void AnimatorLocomotion() {
      float reverse = 1;
      Vector3 flatCamPosition = Camera.main.transform.position;
      flatCamPosition.y = 0;
      Vector3 flatRootPosition = transform.position;
      flatRootPosition.y = 0;
      distanceToRoot = flatCamPosition - flatRootPosition;
      speed = distanceToRoot.magnitude;
      //Debug.Log("speed: " + speed);
      if (!standing && speed < .15f) {
        standing = true;
        if (!isCentering && distanceToRoot.magnitude > .05f) {
          StartCoroutine(centerUnderCamera());
        }
        Debug.Log("Switching Standing to True ++++++++++++++++++++++++++++++++++++++++++++++++");
      }
      if (standing && speed > .30) {
        StopAllCoroutines();
        standing = false;
        Debug.Log("Switching Standing to False -----------------------------------------------");
      }
      if (standing ) { //Dead "stick" and matching LMRigLococmotion method for turning in place
        speed = 0.0f;
        moveDirection = MoveDirectionCameraDirection();
      }
      else {
        moveDirection = MoveDirectionTowardCamera();
        //if (transform.InverseTransformPoint(Camera.main.transform.position).z < -.1f
        //  && direction > -20 && direction < 20) {
        //  Debug.Log("Reversing");
        //  moveDirection = MoveDirectionCameraDirection();
        //  reverse = -1;
        //}
      }

      //Vector3 moveDirection = referentialShift * CameraDirection;
      Vector3 axis = Vector3.Cross(rootDirection, moveDirection);
      direction = Vector3.Angle(rootDirection, moveDirection) / 180f * (axis.y < 0 ? -1 : 1);

      if (animator && Camera.main) {
        locomotion.Do(speed , (direction * 180), reverse);
        Debug.DrawLine(transform.position, moveDirection * 2, Color.red);
      }
    }

    void OnAnimatorIK() {
      if (LMRigToFollowAnimator) {
        LMRig.position = new Vector3(transform.position.x, LMRig.position.y, transform.position.z);
      }
      Vector3 placeAnimatorUnderCam = new Vector3(Camera.main.transform.position.x, transform.position.y, Camera.main.transform.position.z);
      //transform.position = Vector3.Lerp(transform.position, placeAnimatorUnderCam, .001f);
    }
    private bool isCentering = false;

    private IEnumerator centerUnderCamera () {
      CenteringText.text = "Centering Start";
      isCentering = true;
      while (distanceToRoot.magnitude > .05f) {
        Vector3 placeAnimatorUnderCam = new Vector3(Camera.main.transform.position.x, transform.position.y, Camera.main.transform.position.z);
        transform.position = Vector3.Lerp(transform.position, placeAnimatorUnderCam, .1f);
        //Debug.Log("Centering");
        CenteringText.text = "Centering";
        yield return null;
        isCentering = false;
        CenteringText.text = "";
      }
    }
  }
}