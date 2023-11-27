using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.Animations;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;
using System.Reflection;
using Object = UnityEngine.Object;
using System.Text;

public class GrabFullBody : EditorWindow
{
    private VRCAvatarDescriptor avatarDescriptor;
    private Animator animator;
    private bool debugMode = false;
    private bool footIK = false;
    private bool headon = true;
    private bool hipon = true;
    private bool lhandon = true;
    private bool rhandon = true;
    private bool llegon = true;
    private bool rlegon = true;
    private bool tracking = true;
    private int handXY = 1;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    [MenuItem("nHaruka/GrabFullBody")]
    private static void Init()
    {
        var window = GetWindowWithRect<GrabFullBody>(new Rect(0, 0, 500, 380));
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        avatarDescriptor =
            (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", avatarDescriptor, typeof(VRCAvatarDescriptor),true);
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.wordWrap = true;
        EditorGUILayout.LabelField("アバターに全身Grabシステムを導入します。\nアバターのボーンの一部とFXレイヤーを書き換えるのでバックアップを推奨します。", style);
        GUILayout.Space(10);

        GUILayout.Label("Grab可能部位の選択");
        headon = GUILayout.Toggle(headon, "頭Grab");
        hipon = GUILayout.Toggle(hipon, "腰Grab");
        rhandon = GUILayout.Toggle(rhandon, "右手Grab");
        lhandon = GUILayout.Toggle(lhandon, "左手Grab");
        rlegon = GUILayout.Toggle(rlegon, "右足Grab");
        llegon = GUILayout.Toggle(llegon, "左足Grab");

        //GUILayout.BeginHorizontal();
        GUILayout.Label("Handボーンの軸の向き");
        handXY = GUILayout.SelectionGrid(handXY, new string[] { "X軸", "Y軸" }, 2,GUI.skin.toggle);
        //GUILayout.EndHorizontal();

        GUILayout.Space(10);

        GUILayout.Label("視点追従の有無");
        tracking = GUILayout.Toggle(tracking, "視点追従あり");
        //EditorGUILayout.LabelField("※ExpressionMenuに空きがないと視点追従の調整メニューが追加できないのでご注意ください。", style);
        //footIK = GUILayout.Toggle(footIK, "FootIK On : 足がおかしくなる場合はチェックを入れてみて下さい。");
        GUILayout.Space(10);

        if (GUILayout.Button("Setup"))
        {
            if (debugMode)
            {
                GenerateBone();
            }
            else
            {
                try
                {
                    GenerateBone();
                    EditorUtility.DisplayDialog("Finished", "Finished!", "OK");
                }
                catch
                {
                    EditorUtility.DisplayDialog("Error", "An error occurred", "OK");
                }
            }
        }
        if (GUILayout.Button("Remove"))
        {
            if (debugMode)
            {
                remove();
            }
            else
            {
                try
                {
                    remove();
                    EditorUtility.DisplayDialog("Finished", "Finished!", "OK");
                }
                catch
                {
                    EditorUtility.DisplayDialog("Error", "An error occurred", "OK");
                }
            }
        }
        debugMode = GUILayout.Toggle(debugMode, "DebugMode");
        
    }


    void GenerateBone()
    {
        animator = avatarDescriptor.GetComponent<Animator>();

        var Root = new GameObject("GrabSystemRoot");
        Root.transform.parent = avatarDescriptor.transform;
        Root.transform.position = avatarDescriptor.transform.position;
        Root.transform.rotation = avatarDescriptor.transform.rotation;

        var Hips = new GameObject("Hips");
        Hips.transform.parent = Root.transform;
        Hips.transform.position = animator.GetBoneTransform(HumanBodyBones.Hips).position;
        Hips.transform.rotation = animator.GetBoneTransform(HumanBodyBones.Hips).rotation;
        var HipsConstraint = Hips.gameObject.GetOrAddComponent<ParentConstraint>();
        HipsConstraint.locked = true;
        HipsConstraint.constraintActive = true;
        HipsConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Hips) });
        HipsConstraint.rotationAtRest = Vector3.zero;
        HipsConstraint.enabled = true;

        var HipsDummyBone0 = new GameObject("HipsDummyBone0");
        HipsDummyBone0.transform.parent = Hips.transform;
        HipsDummyBone0.transform.position = animator.GetBoneTransform(HumanBodyBones.Hips).parent.transform.position;
        HipsDummyBone0.transform.rotation = animator.GetBoneTransform(HumanBodyBones.Hips).rotation;
        
        var HipsDummyBone1 = new GameObject("HipsDummyBone1");
        HipsDummyBone1.transform.parent = HipsDummyBone0.transform;
        HipsDummyBone1.transform.position = HipsDummyBone0.transform.position + new Vector3(0, 0.5f, 0);
        
        var HipsDummyBone2 = new GameObject("HipsDummyBone2");
        HipsDummyBone2.transform.parent = HipsDummyBone1.transform;
        HipsDummyBone2.transform.position = Hips.transform.position;
        
        var GrabPoint1 = new GameObject("GrabPoint1");
        GrabPoint1.transform.parent = HipsDummyBone2.transform;
        GrabPoint1.transform.position = HipsDummyBone2.transform.position + new Vector3(0.1f, 0, 0); ;

        var GrabPoint2 = new GameObject("GrabPoint2");
        GrabPoint2.transform.parent = HipsDummyBone2.transform;
        GrabPoint2.transform.position = HipsDummyBone2.transform.position + new Vector3(-0.1f, 0, 0);

        var HipsDummy = new GameObject("HipsDummy");
        HipsDummy.transform.parent = HipsDummyBone2.transform;
        HipsDummy.transform.position = animator.GetBoneTransform(HumanBodyBones.Hips).position;
        HipsDummy.transform.rotation = animator.GetBoneTransform(HumanBodyBones.Hips).rotation;
        var HipsDummyConstraint = HipsDummy.gameObject.GetOrAddComponent<PositionConstraint>();
        HipsDummyConstraint.locked = true;
        HipsDummyConstraint.constraintActive = true;
        HipsDummyConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Hips) });
        HipsDummyConstraint.enabled = true;
        var HipsDummyConstraintRot = HipsDummy.gameObject.GetOrAddComponent<RotationConstraint>();
        HipsDummyConstraintRot.locked = true;
        HipsDummyConstraintRot.constraintActive = true;
        HipsDummyConstraintRot.AddSource(new ConstraintSource { weight = 1, sourceTransform = Hips.transform });
        HipsDummyConstraintRot.rotationAtRest = Vector3.zero;
        HipsDummyConstraintRot.enabled = true;

        var HeadDummy = new GameObject("HeadDummy");
        HeadDummy.transform.parent = HipsDummy.transform;
        HeadDummy.transform.position = animator.GetBoneTransform(HumanBodyBones.Head).position;
        HeadDummy.transform.rotation = animator.GetBoneTransform(HumanBodyBones.Head).rotation;
        var HeadDummyConstraint = HeadDummy.gameObject.GetOrAddComponent<ParentConstraint>();
        HeadDummyConstraint.locked = true;
        HeadDummyConstraint.constraintActive = true;
        HeadDummyConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Head) });
        HeadDummyConstraint.rotationAtRest = Vector3.zero;
        HeadDummyConstraint.enabled = true;

        var Spine = new GameObject("Spine");
        Spine.transform.parent = Hips.transform;
        Spine.transform.position = animator.GetBoneTransform(HumanBodyBones.Spine).position;
        Spine.transform.rotation = animator.GetBoneTransform(HumanBodyBones.Spine).rotation;
        var SpineConstraint = Spine.gameObject.GetOrAddComponent<ParentConstraint>();
        SpineConstraint.locked = true;
        SpineConstraint.constraintActive = true;
        SpineConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Spine) });
        SpineConstraint.rotationAtRest = Vector3.zero;
        SpineConstraint.enabled = true;

        var Chest = new GameObject("Chest");
        Chest.transform.parent = Spine.transform;
        Chest.transform.position = animator.GetBoneTransform(HumanBodyBones.Chest).position;
        Chest.transform.rotation = animator.GetBoneTransform(HumanBodyBones.Chest).rotation;
        var ChestConstraint = Chest.gameObject.GetOrAddComponent<ParentConstraint>();
        ChestConstraint.locked = true;
        ChestConstraint.constraintActive = true;
        ChestConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Chest) });
        ChestConstraint.rotationAtRest = Vector3.zero;
        ChestConstraint.enabled = true;

        var UpperChest = new GameObject("UpperChest");
        if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
        {
            UpperChest.transform.parent = Chest.transform;
            UpperChest.transform.position = animator.GetBoneTransform(HumanBodyBones.UpperChest).position;
            UpperChest.transform.rotation = animator.GetBoneTransform(HumanBodyBones.UpperChest).rotation;
            var UpperChestConstraint = UpperChest.gameObject.GetOrAddComponent<ParentConstraint>();
            UpperChestConstraint.locked = true;
            UpperChestConstraint.constraintActive = true;
            UpperChestConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.UpperChest) });
            UpperChestConstraint.rotationAtRest = Vector3.zero;
            UpperChestConstraint.enabled = true;
        }
        else
        {
            DestroyImmediate(UpperChest);
        }

        var Neck = new GameObject("Neck");
        if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
        { Neck.transform.parent = UpperChest.transform; }
        else 
        { Neck.transform.parent = Chest.transform; }
        Neck.transform.position = animator.GetBoneTransform(HumanBodyBones.Neck).position;
        Neck.transform.rotation = animator.GetBoneTransform(HumanBodyBones.Neck).rotation;
        var NeckConstraint = Neck.gameObject.GetOrAddComponent<ParentConstraint>();
        NeckConstraint.locked = true;
        NeckConstraint.constraintActive = true;
        NeckConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Neck) });
        NeckConstraint.rotationAtRest = Vector3.zero;
        NeckConstraint.enabled = true;

        var Head = new GameObject("Head");
        Head.transform.parent = Neck.transform;
        Head.transform.position = animator.GetBoneTransform(HumanBodyBones.Head).position;
        Head.transform.rotation = animator.GetBoneTransform(HumanBodyBones.Head).rotation;
        var HeadConstraint = Head.gameObject.GetOrAddComponent<ParentConstraint>();
        HeadConstraint.locked = true;
        HeadConstraint.constraintActive = true;
        HeadConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Head) });
        HeadConstraint.rotationAtRest = Vector3.zero;
        HeadConstraint.enabled = true;

        var RightShoulder = new GameObject("RightShoulder");
        if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
        { RightShoulder.transform.parent = UpperChest.transform; }
        else
        { RightShoulder.transform.parent = Chest.transform; }
        RightShoulder.transform.position = animator.GetBoneTransform(HumanBodyBones.RightShoulder).position;
        RightShoulder.transform.rotation = animator.GetBoneTransform(HumanBodyBones.RightShoulder).rotation;
        var RightShoulderConstraint = RightShoulder.gameObject.GetOrAddComponent<ParentConstraint>();
        RightShoulderConstraint.locked = true;
        RightShoulderConstraint.constraintActive = true;
        RightShoulderConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.RightShoulder) });
        RightShoulderConstraint.rotationAtRest = Vector3.zero;
        RightShoulderConstraint.enabled = true;

        var RightUpperArm = new GameObject("RightUpperArm");
        RightUpperArm.transform.parent = RightShoulder.transform;
        RightUpperArm.transform.position = animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position;
        RightUpperArm.transform.rotation = animator.GetBoneTransform(HumanBodyBones.RightUpperArm).rotation;
        var RightUpperArmConstraint = RightUpperArm.gameObject.GetOrAddComponent<ParentConstraint>();
        RightUpperArmConstraint.locked = true;
        RightUpperArmConstraint.constraintActive = true;
        RightUpperArmConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.RightUpperArm) });
        RightUpperArmConstraint.rotationAtRest = Vector3.zero;
        RightUpperArmConstraint.enabled = true;

        var RightLowerArm = new GameObject("RightLowerArm");
        RightLowerArm.transform.parent = RightUpperArm.transform;
        RightLowerArm.transform.position = animator.GetBoneTransform(HumanBodyBones.RightLowerArm).position;
        RightLowerArm.transform.rotation = animator.GetBoneTransform(HumanBodyBones.RightLowerArm).rotation;
        var RightLowerArmConstraint = RightLowerArm.gameObject.GetOrAddComponent<ParentConstraint>();
        RightLowerArmConstraint.locked = true;
        RightLowerArmConstraint.constraintActive = true;
        RightLowerArmConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.RightLowerArm) });
        RightLowerArmConstraint.rotationAtRest = Vector3.zero;
        RightLowerArmConstraint.enabled = true;

        var RightHand = new GameObject("RightHand");
        RightHand.transform.parent = RightLowerArm.transform;
        RightHand.transform.position = animator.GetBoneTransform(HumanBodyBones.RightHand).position;
        RightHand.transform.rotation = animator.GetBoneTransform(HumanBodyBones.RightHand).rotation;
        var RightHandConstraint = RightHand.gameObject.GetOrAddComponent<ParentConstraint>();
        RightHandConstraint.locked = true;
        RightHandConstraint.constraintActive = true;
        RightHandConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.RightHand) });
        RightHandConstraint.rotationAtRest = Vector3.zero;
        if (handXY == 0)
        {
            RightHandConstraint.SetTranslationOffset(0, new Vector3(-0.05f, 0, 0));
        }
        else 
        {
            RightHandConstraint.SetTranslationOffset(0, new Vector3(0, -0.05f, 0));
        }
        RightHandConstraint.enabled = true;
        var RightHandContactSnd = RightHand.gameObject.GetOrAddComponent<VRCContactSender>();
        RightHandContactSnd.rootTransform = RightHand.transform;
        RightHandContactSnd.radius = 0.01f;
        RightHandContactSnd.collisionTags = new List<string>() { "Pose" };



        var LeftShoulder = new GameObject("LeftShoulder");
        if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
        { LeftShoulder.transform.parent = UpperChest.transform; }
        else
        { LeftShoulder.transform.parent = Chest.transform; }
        LeftShoulder.transform.position = animator.GetBoneTransform(HumanBodyBones.LeftShoulder).position;
        LeftShoulder.transform.rotation = animator.GetBoneTransform(HumanBodyBones.LeftShoulder).rotation;
        var LeftShoulderConstraint = LeftShoulder.gameObject.GetOrAddComponent<ParentConstraint>();
        LeftShoulderConstraint.locked = true;
        LeftShoulderConstraint.constraintActive = true;
        LeftShoulderConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.LeftShoulder) });
        LeftShoulderConstraint.rotationAtRest = Vector3.zero;
        LeftShoulderConstraint.enabled = true;

        var LeftUpperArm = new GameObject("LeftUpperArm");
        LeftUpperArm.transform.parent = LeftShoulder.transform;
        LeftUpperArm.transform.position = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position;
        LeftUpperArm.transform.rotation = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).rotation;
        var LeftUpperArmConstraint = LeftUpperArm.gameObject.GetOrAddComponent<ParentConstraint>();
        LeftUpperArmConstraint.locked = true;
        LeftUpperArmConstraint.constraintActive = true;
        LeftUpperArmConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm) });
        LeftUpperArmConstraint.rotationAtRest = Vector3.zero;
        LeftUpperArmConstraint.enabled = true;

        var LeftLowerArm = new GameObject("LeftLowerArm");
        LeftLowerArm.transform.parent = LeftUpperArm.transform;
        LeftLowerArm.transform.position = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).position;
        LeftLowerArm.transform.rotation = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).rotation;
        var LeftLowerArmConstraint = LeftLowerArm.gameObject.GetOrAddComponent<ParentConstraint>();
        LeftLowerArmConstraint.locked = true;
        LeftLowerArmConstraint.constraintActive = true;
        LeftLowerArmConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm) });
        LeftLowerArmConstraint.rotationAtRest = Vector3.zero;
        LeftLowerArmConstraint.enabled = true;

        var LeftHand = new GameObject("LeftHand");
        LeftHand.transform.parent = LeftLowerArm.transform;
        LeftHand.transform.position = animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
        LeftHand.transform.rotation = animator.GetBoneTransform(HumanBodyBones.LeftHand).rotation;
        var LeftHandConstraint = LeftHand.gameObject.GetOrAddComponent<ParentConstraint>();
        LeftHandConstraint.locked = true;
        LeftHandConstraint.constraintActive = true;
        LeftHandConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.LeftHand) });
        LeftHandConstraint.rotationAtRest = Vector3.zero;
        if (handXY == 0)
        {
            LeftHandConstraint.SetTranslationOffset(0, new Vector3(0.05f, 0, 0));
        }
        else
        {
            LeftHandConstraint.SetTranslationOffset(0, new Vector3(0, -0.05f, 0));
        }
        LeftHandConstraint.enabled = true;
        var LeftHandContactSnd = LeftHand.gameObject.GetOrAddComponent<VRCContactSender>();
        LeftHandContactSnd.rootTransform = LeftHand.transform;
        LeftHandContactSnd.radius = 0.01f;
        LeftHandContactSnd.collisionTags = new List<string>() { "Pose" };

        var RightUpperLeg = new GameObject("RightUpperLeg");
        RightUpperLeg.transform.parent = Hips.transform;
        RightUpperLeg.transform.position = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).position;
        RightUpperLeg.transform.rotation = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).rotation;
        var RightUpperLegConstraint = RightUpperLeg.gameObject.GetOrAddComponent<ParentConstraint>();
        RightUpperLegConstraint.locked = true;
        RightUpperLegConstraint.constraintActive = true;
        RightUpperLegConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg) });
        RightUpperLegConstraint.rotationAtRest = Vector3.zero;
        RightUpperLegConstraint.enabled = true;


        var RightLowerLeg = new GameObject("RightLowerLeg");
        RightLowerLeg.transform.parent = RightUpperLeg.transform;
        RightLowerLeg.transform.position = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).position;
        RightLowerLeg.transform.rotation = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).rotation;
        var RightLowerLegConstraint = RightLowerLeg.gameObject.GetOrAddComponent<ParentConstraint>();
        RightLowerLegConstraint.locked = true;
        RightLowerLegConstraint.constraintActive = true;
        RightLowerLegConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg) });
        RightLowerLegConstraint.rotationAtRest = Vector3.zero;
        RightLowerLegConstraint.enabled = true;


        var RightFoot = new GameObject("RightFoot");
        RightFoot.transform.parent = RightLowerLeg.transform;
        RightFoot.transform.position = animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
        RightFoot.transform.rotation = animator.GetBoneTransform(HumanBodyBones.RightFoot).rotation;
        var RightFootConstraint = RightFoot.gameObject.GetOrAddComponent<ParentConstraint>();
        RightFootConstraint.locked = true;
        RightFootConstraint.constraintActive = true;
        RightFootConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.RightFoot) });
        RightFootConstraint.rotationAtRest = Vector3.zero;
        RightFootConstraint.enabled = true;
        var RightFootContactSnd = RightFoot.gameObject.GetOrAddComponent<VRCContactSender>();
        RightFootContactSnd.rootTransform = RightFoot.transform;
        RightFootContactSnd.radius = 0.01f;
        RightFootContactSnd.collisionTags = new List<string>() { "Pose" };


        var RightToes = new GameObject("RightToes");
        if (animator.GetBoneTransform(HumanBodyBones.RightToes) != null)
        {
            RightToes.transform.parent = RightFoot.transform;
            RightToes.transform.position = animator.GetBoneTransform(HumanBodyBones.RightToes).position;
            RightToes.transform.rotation = animator.GetBoneTransform(HumanBodyBones.RightToes).rotation;
            var RightToesConstraint = RightToes.gameObject.GetOrAddComponent<ParentConstraint>();
            RightToesConstraint.locked = true;
            RightToesConstraint.constraintActive = true;
            RightToesConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.RightToes) });
            RightToesConstraint.rotationAtRest = Vector3.zero;
            RightToesConstraint.enabled = true;
        }
        else
        {
            DestroyImmediate(RightToes);
        }

        var LeftUpperLeg = new GameObject("LeftUpperLeg");
        LeftUpperLeg.transform.parent = Hips.transform;
        LeftUpperLeg.transform.position = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position;
        LeftUpperLeg.transform.rotation = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).rotation;
        var LeftUpperLegConstraint = LeftUpperLeg.gameObject.GetOrAddComponent<ParentConstraint>();
        LeftUpperLegConstraint.locked = true;
        LeftUpperLegConstraint.constraintActive = true;
        LeftUpperLegConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg) });
        LeftUpperLegConstraint.rotationAtRest = Vector3.zero;
        LeftUpperLegConstraint.enabled = true;


        var LeftLowerLeg = new GameObject("LeftLowerLeg");
        LeftLowerLeg.transform.parent = LeftUpperLeg.transform;
        LeftLowerLeg.transform.position = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position;
        LeftLowerLeg.transform.rotation = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).rotation;
        var LeftLowerLegConstraint = LeftLowerLeg.gameObject.GetOrAddComponent<ParentConstraint>();
        LeftLowerLegConstraint.locked = true;
        LeftLowerLegConstraint.constraintActive = true;
        LeftLowerLegConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg) });
        LeftLowerLegConstraint.rotationAtRest = Vector3.zero;
        LeftLowerLegConstraint.enabled = true;


        var LeftFoot = new GameObject("LeftFoot");
        LeftFoot.transform.parent = LeftLowerLeg.transform;
        LeftFoot.transform.position = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
        LeftFoot.transform.rotation = animator.GetBoneTransform(HumanBodyBones.LeftFoot).rotation;
        var LeftFootConstraint = LeftFoot.gameObject.GetOrAddComponent<ParentConstraint>();
        LeftFootConstraint.locked = true;
        LeftFootConstraint.constraintActive = true;
        LeftFootConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.LeftFoot) });
        LeftFootConstraint.rotationAtRest = Vector3.zero;
        LeftFootConstraint.enabled = true;
        var LeftFootContactSnd = LeftFoot.gameObject.GetOrAddComponent<VRCContactSender>();
        LeftFootContactSnd.rootTransform = LeftFoot.transform;
        LeftFootContactSnd.radius = 0.01f;
        LeftFootContactSnd.collisionTags = new List<string>() { "Pose" };

        var LeftToes = new GameObject("LeftToes");
        if (animator.GetBoneTransform(HumanBodyBones.LeftToes) != null)
        {
            LeftToes.transform.parent = LeftFoot.transform;
            LeftToes.transform.position = animator.GetBoneTransform(HumanBodyBones.LeftToes).position;
            LeftToes.transform.rotation = animator.GetBoneTransform(HumanBodyBones.LeftToes).rotation;
            var LeftToesConstraint = LeftToes.gameObject.GetOrAddComponent<ParentConstraint>();
            LeftToesConstraint.locked = true;
            LeftToesConstraint.constraintActive = true;
            LeftToesConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.LeftToes) });
            LeftToesConstraint.rotationAtRest = Vector3.zero;
            LeftToesConstraint.enabled = true;
        }
        else
        {
            DestroyImmediate(LeftToes);
        }

        var Hips2 = new GameObject("Hips2");
        Hips2.transform.parent = Root.transform;
        Hips2.transform.position = animator.GetBoneTransform(HumanBodyBones.Hips).position;
        Hips2.transform.rotation = animator.GetBoneTransform(HumanBodyBones.Hips).rotation;
        var Hips2Constraint = Hips2.gameObject.GetOrAddComponent<ParentConstraint>();
        Hips2Constraint.locked = true;
        Hips2Constraint.constraintActive = true;
        Hips2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Hips) });
        //Hips2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = Hips2.transform });
        Hips2Constraint.rotationAtRest = Vector3.zero;
        Hips2Constraint.enabled = true;

        var Hips0Constraint = animator.GetBoneTransform(HumanBodyBones.Hips).gameObject.GetOrAddComponent<ParentConstraint>();
        Hips0Constraint.locked = true;
        Hips0Constraint.constraintActive = true;
        Hips0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Hips) });
        Hips0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = Hips2.transform });
        Hips0Constraint.rotationAtRest = Vector3.zero;
        Hips0Constraint.enabled = false;

        var Spine2 = new GameObject("Spine2");
        Spine2.transform.parent = Hips2.transform;
        Spine2.transform.position = animator.GetBoneTransform(HumanBodyBones.Spine).position;
        Spine2.transform.rotation = animator.GetBoneTransform(HumanBodyBones.Spine).rotation;
        var Spine2Constraint = Spine2.gameObject.GetOrAddComponent<ParentConstraint>();
        Spine2Constraint.locked = true;
        Spine2Constraint.constraintActive = true;
        Spine2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Spine) });
       //Spine2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = Spine2.transform });
        Spine2Constraint.rotationAtRest = Vector3.zero;
        Spine2Constraint.enabled = true;

        var Spine0Constraint = animator.GetBoneTransform(HumanBodyBones.Spine).gameObject.GetOrAddComponent<RotationConstraint>();
        Spine0Constraint.locked = true;
        Spine0Constraint.constraintActive = true;
        Spine0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Spine) });
        Spine0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = Spine2.transform });
        Spine0Constraint.rotationAtRest = Vector3.zero;
        Spine0Constraint.enabled = false;

        var Chest2 = new GameObject("Chest2");
        Chest2.transform.parent = Spine2.transform;
        Chest2.transform.position = animator.GetBoneTransform(HumanBodyBones.Chest).position;
        Chest2.transform.rotation = animator.GetBoneTransform(HumanBodyBones.Chest).rotation;
        var Chest2Constraint = Chest2.gameObject.GetOrAddComponent<ParentConstraint>();
        Chest2Constraint.locked = true;
        Chest2Constraint.constraintActive = true;
        Chest2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Chest) });
        //Chest2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = Chest2.transform });
        Chest2Constraint.rotationAtRest = Vector3.zero;
        Chest2Constraint.enabled = true;

        var Chest0Constraint = animator.GetBoneTransform(HumanBodyBones.Chest).gameObject.GetOrAddComponent<RotationConstraint>();
        Chest0Constraint.locked = true;
        Chest0Constraint.constraintActive = true;
        Chest0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Chest) });
        Chest0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = Chest2.transform });
        Chest0Constraint.rotationAtRest = Vector3.zero;
        Chest0Constraint.enabled = false;

        var UpperChest2 = new GameObject("UpperChest2");
        if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
        {
            UpperChest2.transform.parent = Chest2.transform;
            UpperChest2.transform.position = animator.GetBoneTransform(HumanBodyBones.UpperChest).position;
            UpperChest2.transform.rotation = animator.GetBoneTransform(HumanBodyBones.UpperChest).rotation;
            var UpperChest2Constraint = UpperChest2.gameObject.GetOrAddComponent<ParentConstraint>();
            UpperChest2Constraint.locked = true;
            UpperChest2Constraint.constraintActive = true;
            UpperChest2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.UpperChest) });
            //UpperChest2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = UpperChest2.transform });
            UpperChest2Constraint.rotationAtRest = Vector3.zero;
            UpperChest2Constraint.enabled = true;

            var UpperChest0Constraint = animator.GetBoneTransform(HumanBodyBones.UpperChest).gameObject.GetOrAddComponent<RotationConstraint>();
            UpperChest0Constraint.locked = true;
            UpperChest0Constraint.constraintActive = true;
            UpperChest0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.UpperChest) });
            UpperChest0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = UpperChest2.transform });
            UpperChest0Constraint.rotationAtRest = Vector3.zero;
            UpperChest0Constraint.enabled = false;

        }
        else
        {
            DestroyImmediate(UpperChest2);
        }

        var Neck2 = new GameObject("Neck2");
        if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
        { Neck2.transform.parent = UpperChest2.transform; }
        else
        { Neck2.transform.parent = Chest2.transform; }
        Neck2.transform.position = animator.GetBoneTransform(HumanBodyBones.Neck).position;
        Neck2.transform.rotation = animator.GetBoneTransform(HumanBodyBones.Neck).rotation;
        var Neck2Constraint = Neck2.gameObject.GetOrAddComponent<ParentConstraint>();
        Neck2Constraint.locked = true;
        Neck2Constraint.constraintActive = true;
        Neck2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Neck) });
        //Neck2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = Neck2.transform });
        Neck2Constraint.rotationAtRest = Vector3.zero;
        Neck2Constraint.enabled = true;

        var Neck0Constraint = animator.GetBoneTransform(HumanBodyBones.Neck).gameObject.GetOrAddComponent<RotationConstraint>();
        Neck0Constraint.locked = true;
        Neck0Constraint.constraintActive = true;
        Neck0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Neck) });
        Neck0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = Neck2.transform });
        Neck0Constraint.rotationAtRest = Vector3.zero;
        Neck0Constraint.enabled = false;

        var Head2 = new GameObject("Head2");
        Head2.transform.parent = Neck2.transform;
        Head2.transform.position = animator.GetBoneTransform(HumanBodyBones.Head).position;
        Head2.transform.rotation = animator.GetBoneTransform(HumanBodyBones.Head).rotation;
        var Head2Constraint = Head2.gameObject.GetOrAddComponent<ParentConstraint>();
        Head2Constraint.locked = true;
        Head2Constraint.constraintActive = true;
        Head2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Head) });
        //Head2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = Head2.transform });
        Head2Constraint.rotationAtRest = Vector3.zero;
        Head2Constraint.enabled = true;

        var CameraRig = new GameObject("CameraRig");
        CameraRig.transform.parent = Head2.transform;
        CameraRig.transform.localScale = Vector3.zero;
        CameraRig.transform.position = avatarDescriptor.ViewPosition;
        CameraRig.transform.rotation = avatarDescriptor.transform.rotation;

        var Camera = new GameObject("Camera");
        Camera.transform.parent = CameraRig.transform;
        Camera.transform.position = avatarDescriptor.ViewPosition;
        Camera.transform.rotation = avatarDescriptor.transform.rotation;
        var Cam = Camera.GetOrAddComponent<Camera>();
        Cam.cameraType = CameraType.VR;
        Cam.cullingMask = 257815;
        Cam.depth = 99;
        Cam.nearClipPlane = 0.05f;
        Cam.fieldOfView = 120;
        Cam.enabled = false;

        var Head0Constraint = animator.GetBoneTransform(HumanBodyBones.Head).gameObject.GetOrAddComponent<RotationConstraint>();
        Head0Constraint.locked = true;
        Head0Constraint.constraintActive = true;
        Head0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Head) });
        Head0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = Camera.transform });
        Head0Constraint.rotationAtRest = Vector3.zero;
        Head0Constraint.enabled = false;

        var RightShoulder2 = new GameObject("RightShoulder2");
        if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
        { RightShoulder2.transform.parent = UpperChest2.transform; }
        else
        { RightShoulder2.transform.parent = Chest2.transform; }
        RightShoulder2.transform.position = animator.GetBoneTransform(HumanBodyBones.RightShoulder).position;
        RightShoulder2.transform.rotation = animator.GetBoneTransform(HumanBodyBones.RightShoulder).rotation;
        var RightShoulder2Constraint = RightShoulder2.gameObject.GetOrAddComponent<ParentConstraint>();
        RightShoulder2Constraint.locked = true;
        RightShoulder2Constraint.constraintActive = true;
        RightShoulder2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.RightShoulder) });
        //RightShoulder2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = RightShoulder2.transform });
        RightShoulder2Constraint.rotationAtRest = Vector3.zero;
        RightShoulder2Constraint.enabled = true;

        var RightShoulder0Constraint = animator.GetBoneTransform(HumanBodyBones.RightShoulder).gameObject.GetOrAddComponent<RotationConstraint>();
        RightShoulder0Constraint.locked = true;
        RightShoulder0Constraint.constraintActive = true;
        RightShoulder0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.RightShoulder) });
        RightShoulder0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = RightShoulder2.transform });
        RightShoulder0Constraint.rotationAtRest = Vector3.zero;
        RightShoulder0Constraint.enabled = false;

        var RightUpperArm2 = new GameObject("RightUpperArm2");
        RightUpperArm2.transform.parent = RightShoulder2.transform;
        RightUpperArm2.transform.position = animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position;
        RightUpperArm2.transform.rotation = animator.GetBoneTransform(HumanBodyBones.RightUpperArm).rotation;
        var RightUpperArm2Constraint = RightUpperArm2.gameObject.GetOrAddComponent<ParentConstraint>();
        RightUpperArm2Constraint.locked = true;
        RightUpperArm2Constraint.constraintActive = true;
        RightUpperArm2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.RightUpperArm) });
        //RightUpperArm2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = RightUpperArm2.transform });
        RightUpperArm2Constraint.rotationAtRest = Vector3.zero;
        RightUpperArm2Constraint.enabled = true;

        var RightUpperArm0Constraint = animator.GetBoneTransform(HumanBodyBones.RightUpperArm).gameObject.GetOrAddComponent<RotationConstraint>();
        RightUpperArm0Constraint.locked = true;
        RightUpperArm0Constraint.constraintActive = true;
        RightUpperArm0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.RightUpperArm) });
        RightUpperArm0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = RightUpperArm2.transform });
        RightUpperArm0Constraint.rotationAtRest = Vector3.zero;
        RightUpperArm0Constraint.enabled = false;

        var RightLowerArm2 = new GameObject("RightLowerArm2");
        RightLowerArm2.transform.parent = RightUpperArm2.transform;
        RightLowerArm2.transform.position = animator.GetBoneTransform(HumanBodyBones.RightLowerArm).position;
        RightLowerArm2.transform.rotation = animator.GetBoneTransform(HumanBodyBones.RightLowerArm).rotation;
        var RightLowerArm2Constraint = RightLowerArm2.gameObject.GetOrAddComponent<ParentConstraint>();
        RightLowerArm2Constraint.locked = true;
        RightLowerArm2Constraint.constraintActive = true;
        RightLowerArm2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.RightLowerArm) });
        //RightLowerArm2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = RightLowerArm2.transform });
        RightLowerArm2Constraint.rotationAtRest = Vector3.zero;
        RightLowerArm2Constraint.enabled = true;

        var RightLowerArm0Constraint = animator.GetBoneTransform(HumanBodyBones.RightLowerArm).gameObject.GetOrAddComponent<RotationConstraint>();
        RightLowerArm0Constraint.locked = true;
        RightLowerArm0Constraint.constraintActive = true;
        RightLowerArm0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.RightLowerArm) });
        RightLowerArm0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = RightLowerArm2.transform });
        RightLowerArm0Constraint.rotationAtRest = Vector3.zero;
        RightLowerArm0Constraint.enabled = false;

        var RightHand2 = new GameObject("RightHand2");
        RightHand2.transform.parent = RightLowerArm2.transform;
        RightHand2.transform.position = animator.GetBoneTransform(HumanBodyBones.RightHand).position;
        RightHand2.transform.rotation = animator.GetBoneTransform(HumanBodyBones.RightHand).rotation;
        var RightHand2Constraint = RightHand2.gameObject.GetOrAddComponent<ParentConstraint>();
        RightHand2Constraint.locked = true;
        RightHand2Constraint.constraintActive = true;
        RightHand2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.RightHand) });
        //RightHand2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = RightHand2.transform });
        RightHand2Constraint.rotationAtRest = Vector3.zero;
        RightHand2Constraint.enabled = true;

        var RightHand0Constraint = animator.GetBoneTransform(HumanBodyBones.RightHand).gameObject.GetOrAddComponent<RotationConstraint>();
        RightHand0Constraint.locked = true;
        RightHand0Constraint.constraintActive = true;
        RightHand0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.RightHand) });
        RightHand0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = RightHand2.transform });
        RightHand0Constraint.rotationAtRest = Vector3.zero;
        RightHand0Constraint.enabled = false;


        var LeftShoulder2 = new GameObject("LeftShoulder2");
        if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
        { LeftShoulder2.transform.parent = UpperChest2.transform; }
        else
        { LeftShoulder2.transform.parent = Chest2.transform; }
        LeftShoulder2.transform.position = animator.GetBoneTransform(HumanBodyBones.LeftShoulder).position;
        LeftShoulder2.transform.rotation = animator.GetBoneTransform(HumanBodyBones.LeftShoulder).rotation;
        var LeftShoulder2Constraint = LeftShoulder2.gameObject.GetOrAddComponent<ParentConstraint>();
        LeftShoulder2Constraint.locked = true;
        LeftShoulder2Constraint.constraintActive = true;
        LeftShoulder2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.LeftShoulder) });
        //LeftShoulder2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = LeftShoulder2.transform });
        LeftShoulder2Constraint.rotationAtRest = Vector3.zero;
        LeftShoulder2Constraint.enabled = true;

        var LeftShoulder0Constraint = animator.GetBoneTransform(HumanBodyBones.LeftShoulder).gameObject.GetOrAddComponent<RotationConstraint>();
        LeftShoulder0Constraint.locked = true;
        LeftShoulder0Constraint.constraintActive = true;
        LeftShoulder0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.LeftShoulder) });
        LeftShoulder0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = LeftShoulder2.transform });
        LeftShoulder0Constraint.rotationAtRest = Vector3.zero;
        LeftShoulder0Constraint.enabled = false;

        var LeftUpperArm2 = new GameObject("LeftUpperArm2");
        LeftUpperArm2.transform.parent = LeftShoulder2.transform;
        LeftUpperArm2.transform.position = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position;
        LeftUpperArm2.transform.rotation = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).rotation;
        var LeftUpperArm2Constraint = LeftUpperArm2.gameObject.GetOrAddComponent<ParentConstraint>();
        LeftUpperArm2Constraint.locked = true;
        LeftUpperArm2Constraint.constraintActive = true;
        LeftUpperArm2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm) });
        //LeftUpperArm2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = LeftUpperArm2.transform });
        LeftUpperArm2Constraint.rotationAtRest = Vector3.zero;
        LeftUpperArm2Constraint.enabled = true;

        var LeftUpperArm0Constraint = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).gameObject.GetOrAddComponent<RotationConstraint>();
        LeftUpperArm0Constraint.locked = true;
        LeftUpperArm0Constraint.constraintActive = true;
        LeftUpperArm0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm) });
        LeftUpperArm0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = LeftUpperArm2.transform });
        LeftUpperArm0Constraint.rotationAtRest = Vector3.zero;
        LeftUpperArm0Constraint.enabled = false;

        var LeftLowerArm2 = new GameObject("LeftLowerArm2");
        LeftLowerArm2.transform.parent = LeftUpperArm2.transform;
        LeftLowerArm2.transform.position = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).position;
        LeftLowerArm2.transform.rotation = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).rotation;
        var LeftLowerArm2Constraint = LeftLowerArm2.gameObject.GetOrAddComponent<ParentConstraint>();
        LeftLowerArm2Constraint.locked = true;
        LeftLowerArm2Constraint.constraintActive = true;
        LeftLowerArm2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm) });
        //LeftLowerArm2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = LeftLowerArm2.transform });
        LeftLowerArm2Constraint.rotationAtRest = Vector3.zero;
        LeftLowerArm2Constraint.enabled = true;

        var LeftLowerArm0Constraint = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).gameObject.GetOrAddComponent<RotationConstraint>();
        LeftLowerArm0Constraint.locked = true;
        LeftLowerArm0Constraint.constraintActive = true;
        LeftLowerArm0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm) });
        LeftLowerArm0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = LeftLowerArm2.transform });
        LeftLowerArm0Constraint.rotationAtRest = Vector3.zero;
        LeftLowerArm0Constraint.enabled = false;

        var LeftHand2 = new GameObject("LeftHand2");
        LeftHand2.transform.parent = LeftLowerArm2.transform;
        LeftHand2.transform.position = animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
        LeftHand2.transform.rotation = animator.GetBoneTransform(HumanBodyBones.LeftHand).rotation;
        var LeftHand2Constraint = LeftHand2.gameObject.GetOrAddComponent<ParentConstraint>();
        LeftHand2Constraint.locked = true;
        LeftHand2Constraint.constraintActive = true;
        LeftHand2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.LeftHand) });
        //LeftHand2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = LeftHand2.transform });
        LeftHand2Constraint.rotationAtRest = Vector3.zero;
        LeftHand2Constraint.enabled = true;

        var LeftHand0Constraint = animator.GetBoneTransform(HumanBodyBones.LeftHand).gameObject.GetOrAddComponent<RotationConstraint>();
        LeftHand0Constraint.locked = true;
        LeftHand0Constraint.constraintActive = true;
        LeftHand0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.LeftHand) });
        LeftHand0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = LeftHand2.transform });
        LeftHand0Constraint.rotationAtRest = Vector3.zero;
        LeftHand0Constraint.enabled = false;

        var RightUpperLeg2 = new GameObject("RightUpperLeg2");
        RightUpperLeg2.transform.parent = Hips2.transform;
        RightUpperLeg2.transform.position = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).position;
        RightUpperLeg2.transform.rotation = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).rotation;
        var RightUpperLeg2Constraint = RightUpperLeg2.gameObject.GetOrAddComponent<ParentConstraint>();
        RightUpperLeg2Constraint.locked = true;
        RightUpperLeg2Constraint.constraintActive = true;
        RightUpperLeg2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg) });
        //RightUpperLeg2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = RightUpperLeg2.transform });
        RightUpperLeg2Constraint.rotationAtRest = Vector3.zero;
        RightUpperLeg2Constraint.enabled = true;

        var RightUpperLeg0Constraint = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).gameObject.GetOrAddComponent<RotationConstraint>();
        RightUpperLeg0Constraint.locked = true;
        RightUpperLeg0Constraint.constraintActive = true;
        RightUpperLeg0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg) });
        RightUpperLeg0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = RightUpperLeg2.transform });
        RightUpperLeg0Constraint.rotationAtRest = Vector3.zero;
        RightUpperLeg0Constraint.enabled = false;


        var RightLowerLeg2 = new GameObject("RightLowerLeg2");
        RightLowerLeg2.transform.parent = RightUpperLeg2.transform;
        RightLowerLeg2.transform.position = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).position;
        RightLowerLeg2.transform.rotation = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).rotation;
        var RightLowerLeg2Constraint = RightLowerLeg2.gameObject.GetOrAddComponent<ParentConstraint>();
        RightLowerLeg2Constraint.locked = true;
        RightLowerLeg2Constraint.constraintActive = true;
        RightLowerLeg2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg) });
        //RightLowerLeg2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = RightLowerLeg2.transform });
        RightLowerLeg2Constraint.rotationAtRest = Vector3.zero;
        RightLowerLeg2Constraint.enabled = true;

        var RightLowerLeg0Constraint = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).gameObject.GetOrAddComponent<RotationConstraint>();
        RightLowerLeg0Constraint.locked = true;
        RightLowerLeg0Constraint.constraintActive = true;
        RightLowerLeg0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg) });
        RightLowerLeg0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = RightLowerLeg2.transform });
        RightLowerLeg0Constraint.rotationAtRest = Vector3.zero;
        RightLowerLeg0Constraint.enabled = false;


        var RightFoot2 = new GameObject("RightFoot2");
        RightFoot2.transform.parent = RightLowerLeg2.transform;
        RightFoot2.transform.position = animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
        RightFoot2.transform.rotation = animator.GetBoneTransform(HumanBodyBones.RightFoot).rotation;
        var RightFoot2Constraint = RightFoot2.gameObject.GetOrAddComponent<ParentConstraint>();
        RightFoot2Constraint.locked = true;
        RightFoot2Constraint.constraintActive = true;
        RightFoot2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.RightFoot) });
        //RightFoot2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = RightFoot2.transform });
        RightFoot2Constraint.rotationAtRest = Vector3.zero;
        RightFoot2Constraint.enabled = true;

        var RightFoot0Constraint = animator.GetBoneTransform(HumanBodyBones.RightFoot).gameObject.GetOrAddComponent<RotationConstraint>();
        RightFoot0Constraint.locked = true;
        RightFoot0Constraint.constraintActive = true;
        RightFoot0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.RightFoot) });
        RightFoot0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = RightFoot2.transform });
        RightFoot0Constraint.rotationAtRest = Vector3.zero;
        RightFoot0Constraint.enabled = false;


        /*
        var RightToes2 = new GameObject("RightToes2");
        if (animator.GetBoneTransform(HumanBodyBones.RightToes) != null)
        {
            RightToes2.transform.parent = RightFoot2.transform;
            RightToes2.transform.position = animator.GetBoneTransform(HumanBodyBones.RightToes).position;
            RightToes2.transform.rotation = animator.GetBoneTransform(HumanBodyBones.RightToes).rotation;
            var RightToes2Constraint = RightToes2.gameObject.GetOrAddComponent<ParentConstraint>();
            RightToes2Constraint.locked = true;
            RightToes2Constraint.constraintActive = true;
            RightToes2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.RightToes) });
            RightToes2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = RightToes2.transform });
            RightToes2Constraint.rotationAtRest = Vector3.zero;
            RightToes2Constraint.enabled = true;
        }
        else
        {
            DestroyImmediate(RightToes2);
        }
        */

        var LeftUpperLeg2 = new GameObject("LeftUpperLeg2");
        LeftUpperLeg2.transform.parent = Hips2.transform;
        LeftUpperLeg2.transform.position = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position;
        LeftUpperLeg2.transform.rotation = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).rotation;
        var LeftUpperLeg2Constraint = LeftUpperLeg2.gameObject.GetOrAddComponent<ParentConstraint>();
        LeftUpperLeg2Constraint.locked = true;
        LeftUpperLeg2Constraint.constraintActive = true;
        LeftUpperLeg2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg) });
        //LeftUpperLeg2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = LeftUpperLeg2.transform });
        LeftUpperLeg2Constraint.rotationAtRest = Vector3.zero;
        LeftUpperLeg2Constraint.enabled = true;

        var LeftUpperLeg0Constraint = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).gameObject.GetOrAddComponent<RotationConstraint>();
        LeftUpperLeg0Constraint.locked = true;
        LeftUpperLeg0Constraint.constraintActive = true;
        LeftUpperLeg0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg) });
        LeftUpperLeg0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = LeftUpperLeg2.transform });
        LeftUpperLeg0Constraint.rotationAtRest = Vector3.zero;
        LeftUpperLeg0Constraint.enabled = false;


        var LeftLowerLeg2 = new GameObject("LeftLowerLeg2");
        LeftLowerLeg2.transform.parent = LeftUpperLeg2.transform;
        LeftLowerLeg2.transform.position = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position;
        LeftLowerLeg2.transform.rotation = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).rotation;
        var LeftLowerLeg2Constraint = LeftLowerLeg2.gameObject.GetOrAddComponent<ParentConstraint>();
        LeftLowerLeg2Constraint.locked = true;
        LeftLowerLeg2Constraint.constraintActive = true;
        LeftLowerLeg2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg) });
        //LeftLowerLeg2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = LeftLowerLeg2.transform });
        LeftLowerLeg2Constraint.rotationAtRest = Vector3.zero;
        LeftLowerLeg2Constraint.enabled = true;

        var LeftLowerLeg0Constraint = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).gameObject.GetOrAddComponent<RotationConstraint>();
        LeftLowerLeg0Constraint.locked = true;
        LeftLowerLeg0Constraint.constraintActive = true;
        LeftLowerLeg0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg) });
        LeftLowerLeg0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = LeftLowerLeg2.transform });
        LeftLowerLeg0Constraint.rotationAtRest = Vector3.zero;
        LeftLowerLeg0Constraint.enabled = false;


        var LeftFoot2 = new GameObject("LeftFoot2");
        LeftFoot2.transform.parent = LeftLowerLeg2.transform;
        LeftFoot2.transform.position = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
        LeftFoot2.transform.rotation = animator.GetBoneTransform(HumanBodyBones.LeftFoot).rotation;
        var LeftFoot2Constraint = LeftFoot2.gameObject.GetOrAddComponent<ParentConstraint>();
        LeftFoot2Constraint.locked = true;
        LeftFoot2Constraint.constraintActive = true;
        LeftFoot2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.LeftFoot) });
        //LeftFoot2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = LeftFoot2.transform });
        LeftFoot2Constraint.rotationAtRest = Vector3.zero;
        LeftFoot2Constraint.enabled = true;

        var LeftFoot0Constraint = animator.GetBoneTransform(HumanBodyBones.LeftFoot).gameObject.GetOrAddComponent<RotationConstraint>();
        LeftFoot0Constraint.locked = true;
        LeftFoot0Constraint.constraintActive = true;
        LeftFoot0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.LeftFoot) });
        LeftFoot0Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = LeftFoot2.transform });
        LeftFoot0Constraint.rotationAtRest = Vector3.zero;
        LeftFoot0Constraint.enabled = false;


        /*

        var LeftToes2 = new GameObject("LeftToes2");
        if (animator.GetBoneTransform(HumanBodyBones.LeftToes) != null)
        {
            LeftToes2.transform.parent = LeftFoot2.transform;
            LeftToes2.transform.position = animator.GetBoneTransform(HumanBodyBones.LeftToes).position;
            LeftToes2.transform.rotation = animator.GetBoneTransform(HumanBodyBones.LeftToes).rotation;
            var LeftToes2Constraint = LeftToes2.gameObject.GetOrAddComponent<ParentConstraint>();
            LeftToes2Constraint.locked = true;
            LeftToes2Constraint.constraintActive = true;
            LeftToes2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.LeftToes) });
            LeftToes2Constraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = LeftToes2.transform });
            LeftToes2Constraint.rotationAtRest = Vector3.zero;
            LeftToes2Constraint.enabled = true;
        }
        else
        {
            DestroyImmediate(LeftToes2);
        }
        */


        var Targets = new GameObject("Targets");
        Targets.transform.parent = Root.transform;

        var RHandTarget = new GameObject("RHand");
        RHandTarget.transform.parent = Targets.transform;
        RHandTarget.transform.position = animator.GetBoneTransform(HumanBodyBones.RightHand).position;
        RHandTarget.transform.rotation = animator.GetBoneTransform(HumanBodyBones.RightHand).rotation;
        var RHandTargetConstraint = RHandTarget.gameObject.GetOrAddComponent<ParentConstraint>();
        RHandTargetConstraint.locked = true;
        RHandTargetConstraint.constraintActive = true;
        RHandTargetConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.RightHand) });
        RHandTargetConstraint.rotationAtRest = Vector3.zero;
        RHandTargetConstraint.enabled = true;

        var LHandTarget = new GameObject("LHand");
        LHandTarget.transform.parent = Targets.transform;
        LHandTarget.transform.position = animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
        LHandTarget.transform.rotation = animator.GetBoneTransform(HumanBodyBones.LeftHand).rotation;
        var LHandTargetConstraint = LHandTarget.gameObject.GetOrAddComponent<ParentConstraint>();
        LHandTargetConstraint.locked = true;
        LHandTargetConstraint.constraintActive = true;
        LHandTargetConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.LeftHand) });
        LHandTargetConstraint.rotationAtRest = Vector3.zero;
        LHandTargetConstraint.enabled = true;

        var RLegTarget = new GameObject("RLeg");
        RLegTarget.transform.parent = Targets.transform;
        RLegTarget.transform.position = animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
        RLegTarget.transform.rotation = animator.GetBoneTransform(HumanBodyBones.RightFoot).rotation;
        var RLegTargetConstraint = RLegTarget.gameObject.GetOrAddComponent<ParentConstraint>();
        RLegTargetConstraint.locked = true;
        RLegTargetConstraint.constraintActive = true;
        RLegTargetConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.RightFoot) });
        RLegTargetConstraint.rotationAtRest = Vector3.zero;
        RLegTargetConstraint.enabled = true;

        var LLegTarget = new GameObject("LLeg");
        LLegTarget.transform.parent = Targets.transform;
        LLegTarget.transform.position = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
        LLegTarget.transform.rotation = animator.GetBoneTransform(HumanBodyBones.LeftFoot).rotation;
        var LLegTargetConstraint = LLegTarget.gameObject.GetOrAddComponent<ParentConstraint>();
        LLegTargetConstraint.locked = true;
        LLegTargetConstraint.constraintActive = true;
        LLegTargetConstraint.AddSource(new ConstraintSource { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.LeftFoot) });
        LLegTargetConstraint.rotationAtRest = Vector3.zero;
        LLegTargetConstraint.enabled = true;



        var HipsphysBone = HipsDummyBone0.GetOrAddComponent<VRCPhysBone>();
        HipsphysBone.rootTransform = HipsDummyBone0.transform;
        HipsphysBone.multiChildType = VRCPhysBoneBase.MultiChildType.Ignore;
        HipsphysBone.ignoreTransforms = new List<Transform>() { HipsDummy.transform };
        HipsphysBone.immobile = 1;
        HipsphysBone.pull = 1f;
        HipsphysBone.gravity = 0;
        HipsphysBone.radius = 0.1f;
        HipsphysBone.spring = 0.2f;
        HipsphysBone.radiusCurve = new AnimationCurve(
            new Keyframe(0, 0f),
            new Keyframe(0.8f, 0),
            new Keyframe(1, 1)
        );
        HipsphysBone.endpointPosition = new Vector3(0,0.01f,0);
        HipsphysBone.isAnimated = true;
        HipsphysBone.allowPosing = VRCPhysBoneBase.AdvancedBool.False; 
        HipsphysBone.parameter = "Hip";
        HipsphysBone.integrationType = VRCPhysBoneBase.IntegrationType.Simplified;
        HipsphysBone.allowCollision = VRCPhysBoneBase.AdvancedBool.False;
        HipsphysBone.enabled = hipon;
        HipsphysBone.grabMovement = 1f;
        HipsphysBone.maxStretch = 5f;
        HipsphysBone.limitType = VRCPhysBoneBase.LimitType.None;

        var HeadphysBone = Chest.GetOrAddComponent<VRCPhysBone>();
        HeadphysBone.rootTransform = Chest.transform;
        HeadphysBone.multiChildType = VRCPhysBoneBase.MultiChildType.Ignore;
        HeadphysBone.ignoreTransforms = new List<Transform>() { RightShoulder.transform, LeftShoulder.transform};
        HeadphysBone.immobile = 1;
        HeadphysBone.pull = 1f;
        HeadphysBone.gravity = 0;
        HeadphysBone.radius = 0.12f;
        HeadphysBone.spring = 0.2f;
        HeadphysBone.radiusCurve = new AnimationCurve(
            new Keyframe(0, 0f),
            new Keyframe(0.8f, 0),
            new Keyframe(1, 1)
        );
        HeadphysBone.endpointPosition = new Vector3(0, 0.1f, 0);
        HeadphysBone.isAnimated = true;
        HeadphysBone.allowPosing = VRCPhysBoneBase.AdvancedBool.False;
        HeadphysBone.parameter = "Head";
        HeadphysBone.integrationType = VRCPhysBoneBase.IntegrationType.Simplified;
        HeadphysBone.allowCollision = VRCPhysBoneBase.AdvancedBool.False;
        HeadphysBone.enabled = headon;
        HeadphysBone.grabMovement = 1f;
        HeadphysBone.maxStretch = 5f;
        HeadphysBone.limitType = VRCPhysBoneBase.LimitType.None;

        var RHandphysBone = RightShoulder.GetOrAddComponent<VRCPhysBone>();
        RHandphysBone.rootTransform = RightShoulder.transform;
        RHandphysBone.multiChildType = VRCPhysBoneBase.MultiChildType.Ignore;
        RHandphysBone.immobile = 1;
        RHandphysBone.pull = 1f;
        RHandphysBone.gravity = 0;
        RHandphysBone.radius = 0.02f;
        RHandphysBone.spring = 0.2f;
        RHandphysBone.isAnimated = true;
        RHandphysBone.allowPosing = VRCPhysBoneBase.AdvancedBool.True;
        RHandphysBone.parameter = "RHand";
        RHandphysBone.integrationType = VRCPhysBoneBase.IntegrationType.Simplified;
        RHandphysBone.allowCollision = VRCPhysBoneBase.AdvancedBool.False;
        RHandphysBone.enabled = rhandon;
        RHandphysBone.grabMovement = 1f;
        RHandphysBone.maxStretch = 5f;
        RHandphysBone.limitType = VRCPhysBoneBase.LimitType.None;

        var LHandphysBone = LeftShoulder.GetOrAddComponent<VRCPhysBone>();
        LHandphysBone.rootTransform = LeftShoulder.transform;
        LHandphysBone.multiChildType = VRCPhysBoneBase.MultiChildType.Ignore;
        LHandphysBone.immobile = 1;
        LHandphysBone.pull = 1f;
        LHandphysBone.gravity = 0;
        LHandphysBone.radius = 0.02f;
        LHandphysBone.spring = 0.2f;
        LHandphysBone.isAnimated = true;
        LHandphysBone.allowPosing = VRCPhysBoneBase.AdvancedBool.True;
        LHandphysBone.parameter = "LHand";
        LHandphysBone.integrationType = VRCPhysBoneBase.IntegrationType.Simplified;
        LHandphysBone.allowCollision = VRCPhysBoneBase.AdvancedBool.False;
        LHandphysBone.enabled = lhandon;
        LHandphysBone.grabMovement = 1f;
        LHandphysBone.maxStretch = 5f;
        LHandphysBone.limitType = VRCPhysBoneBase.LimitType.None;

        var RLegphysBone = RightUpperLeg.GetOrAddComponent<VRCPhysBone>();
        RLegphysBone.rootTransform = RightUpperLeg.transform;
        RLegphysBone.multiChildType = VRCPhysBoneBase.MultiChildType.Ignore;
        RLegphysBone.immobile = 1;
        RLegphysBone.pull = 1f;
        RLegphysBone.gravity = 0;
        RLegphysBone.radius = 0.05f;
        RLegphysBone.spring = 0.2f;
        RLegphysBone.isAnimated = true;
        RLegphysBone.allowPosing = VRCPhysBoneBase.AdvancedBool.True;
        RLegphysBone.parameter = "RLeg";
        RLegphysBone.integrationType = VRCPhysBoneBase.IntegrationType.Simplified;
        RLegphysBone.allowCollision = VRCPhysBoneBase.AdvancedBool.False;
        RLegphysBone.enabled = rlegon;
        RLegphysBone.grabMovement = 1f;
        RLegphysBone.maxStretch = 0f;
        RLegphysBone.limitType = VRCPhysBoneBase.LimitType.None;

        var LLegphysBone = LeftUpperLeg.GetOrAddComponent<VRCPhysBone>();
        LLegphysBone.rootTransform = LeftUpperLeg.transform;
        LLegphysBone.multiChildType = VRCPhysBoneBase.MultiChildType.Ignore;
        LLegphysBone.immobile = 1;
        LLegphysBone.pull = 1f;
        LLegphysBone.gravity = 0;
        LLegphysBone.radius = 0.05f;
        LLegphysBone.spring = 0.2f;
        LLegphysBone.isAnimated = true;
        LLegphysBone.allowPosing = VRCPhysBoneBase.AdvancedBool.True;
        LLegphysBone.parameter = "LLeg";
        LLegphysBone.integrationType = VRCPhysBoneBase.IntegrationType.Simplified;
        LLegphysBone.allowCollision = VRCPhysBoneBase.AdvancedBool.False;
        LLegphysBone.enabled = llegon;
        LLegphysBone.grabMovement = 1f;
        LLegphysBone.maxStretch = 0f;
        LLegphysBone.limitType = VRCPhysBoneBase.LimitType.None;

        var HipsIK = new GameObject("HipsIK");
        HipsIK.transform.parent = Root.transform;
        var HipsIK1st = new GameObject("1st");
        HipsIK1st.transform.parent = HipsIK.transform;
        var HipsIK2nd = new GameObject("2nd");
        HipsIK2nd.transform.parent = HipsIK.transform;
        var HipsVRIK1st = HipsIK1st.GetOrAddComponent<RootMotion.FinalIK.VRIK>();
        HipsVRIK1st.references.root = avatarDescriptor.gameObject.transform;
        HipsVRIK1st.references.pelvis = animator.GetBoneTransform(HumanBodyBones.Hips);
        HipsVRIK1st.references.spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        HipsVRIK1st.references.chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        HipsVRIK1st.references.neck = animator.GetBoneTransform(HumanBodyBones.Neck);
        HipsVRIK1st.references.head = animator.GetBoneTransform(HumanBodyBones.Head);
        HipsVRIK1st.references.leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
        HipsVRIK1st.references.leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        HipsVRIK1st.references.leftForearm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        HipsVRIK1st.references.leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        HipsVRIK1st.references.rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
        HipsVRIK1st.references.rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        HipsVRIK1st.references.rightForearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        HipsVRIK1st.references.rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        HipsVRIK1st.references.leftThigh = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        HipsVRIK1st.references.leftCalf = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        HipsVRIK1st.references.leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        if (animator.GetBoneTransform(HumanBodyBones.LeftToes) != null)
        {
            //HipsVRIK1st.references.leftToes = animator.GetBoneTransform(HumanBodyBones.LeftToes);
        }
        HipsVRIK1st.references.rightThigh = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        HipsVRIK1st.references.rightCalf = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        HipsVRIK1st.references.rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        if (animator.GetBoneTransform(HumanBodyBones.RightToes) != null)
        {
            //HipsVRIK1st.references.rightToes = animator.GetBoneTransform(HumanBodyBones.RightToes);
        }
        HipsVRIK1st.fixTransforms = false;
        HipsVRIK1st.solver.LOD = 0;

        HipsVRIK1st.solver.IKPositionWeight = 1f;
        HipsVRIK1st.solver.plantFeet = false;
        HipsVRIK1st.solver.spine.headTarget = HeadDummy.transform;
        HipsVRIK1st.solver.spine.positionWeight = 1f;
        HipsVRIK1st.solver.spine.rotationWeight = 1f;
        HipsVRIK1st.solver.spine.headClampWeight = 0f;
        HipsVRIK1st.solver.spine.minHeadHeight = 0;
        HipsVRIK1st.solver.spine.rotateChestByHands = 0;

        HipsVRIK1st.solver.spine.pelvisTarget = HipsDummy.transform;
        HipsVRIK1st.solver.spine.pelvisPositionWeight = 1f;
        HipsVRIK1st.solver.spine.pelvisRotationWeight = 0f;
        HipsVRIK1st.solver.spine.maintainPelvisPosition = 0f;

        HipsVRIK1st.solver.spine.maxRootAngle = 180f;

        HipsVRIK1st.solver.rightArm.target = animator.GetBoneTransform(HumanBodyBones.RightHand);
        HipsVRIK1st.solver.rightArm.positionWeight = 1f;
        HipsVRIK1st.solver.rightArm.rotationWeight = 1f;
        HipsVRIK1st.solver.rightArm.shoulderRotationWeight = 0f;
        HipsVRIK1st.solver.rightArm.wristToPalmAxis = new Vector3(1, 0, 0);
        HipsVRIK1st.solver.rightArm.palmToThumbAxis = new Vector3(1, 0, 0);

        HipsVRIK1st.solver.leftArm.target = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        HipsVRIK1st.solver.leftArm.positionWeight = 1;
        HipsVRIK1st.solver.leftArm.rotationWeight = 1;
        HipsVRIK1st.solver.leftArm.shoulderRotationWeight = 0f;
        HipsVRIK1st.solver.leftArm.wristToPalmAxis = new Vector3(-1, 0, 0);
        HipsVRIK1st.solver.leftArm.palmToThumbAxis = new Vector3(1, 0, 0);

        HipsVRIK1st.solver.rightLeg.target = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        HipsVRIK1st.solver.rightLeg.positionWeight = 0;
        HipsVRIK1st.solver.rightLeg.rotationWeight = 0;
        HipsVRIK1st.solver.leftLeg.target = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        HipsVRIK1st.solver.leftLeg.positionWeight = 0;
        HipsVRIK1st.solver.leftLeg.rotationWeight = 0;


        HipsVRIK1st.solver.locomotion.weight = 0f;

        HipsVRIK1st.enabled = false;

        var HipsVRIK2nd = HipsIK2nd.GetOrAddComponent<RootMotion.FinalIK.VRIK>();
        HipsVRIK2nd.references.root = avatarDescriptor.gameObject.transform;
        HipsVRIK2nd.references.pelvis = Hips2.transform;
        HipsVRIK2nd.references.spine = Spine2.transform;
        HipsVRIK2nd.references.chest = Chest2.transform;
        HipsVRIK2nd.references.neck = Neck2.transform;
        HipsVRIK2nd.references.head = Head2.transform;
        HipsVRIK2nd.references.leftShoulder = LeftShoulder2.transform;
        HipsVRIK2nd.references.leftUpperArm = LeftUpperArm2.transform;
        HipsVRIK2nd.references.leftForearm = LeftLowerArm2.transform;
        HipsVRIK2nd.references.leftHand = LeftHand2.transform;
        HipsVRIK2nd.references.rightShoulder = RightShoulder2.transform;
        HipsVRIK2nd.references.rightUpperArm = RightUpperArm2.transform;
        HipsVRIK2nd.references.rightForearm = RightLowerArm2.transform;
        HipsVRIK2nd.references.rightHand = RightHand2.transform;
        HipsVRIK2nd.references.leftThigh = LeftUpperLeg2.transform;
        HipsVRIK2nd.references.leftCalf = LeftLowerLeg2.transform;
        HipsVRIK2nd.references.leftFoot = LeftFoot2.transform;
        if (animator.GetBoneTransform(HumanBodyBones.LeftToes) != null)
        {
            //HipsVRIK2nd.references.leftToes = 
        }
        HipsVRIK2nd.references.rightThigh = RightUpperLeg2.transform;
        HipsVRIK2nd.references.rightCalf = RightLowerLeg2.transform;
        HipsVRIK2nd.references.rightFoot = RightFoot2.transform;
        if (animator.GetBoneTransform(HumanBodyBones.RightToes) != null)
        {
            //HipsVRIK2nd.references.rightToes = animator.GetBoneTransform(HumanBodyBones.RightToes);
        }
        HipsVRIK2nd.fixTransforms = true;
        HipsVRIK2nd.solver.LOD = 0;

        HipsVRIK2nd.solver.IKPositionWeight = 1f;
        HipsVRIK2nd.solver.plantFeet = false;
        HipsVRIK2nd.solver.spine.headTarget = HeadDummy.transform;
        HipsVRIK2nd.solver.spine.positionWeight = 1f;
        HipsVRIK2nd.solver.spine.rotationWeight = 1f;
        HipsVRIK2nd.solver.spine.headClampWeight = 0f;
        HipsVRIK2nd.solver.spine.minHeadHeight = 0;
        HipsVRIK2nd.solver.spine.rotateChestByHands = 0;

        HipsVRIK2nd.solver.spine.pelvisTarget = HipsDummy.transform;
        HipsVRIK2nd.solver.spine.pelvisPositionWeight = 1f;
        HipsVRIK2nd.solver.spine.pelvisRotationWeight = 0f;
        HipsVRIK2nd.solver.spine.maintainPelvisPosition = 0f;

        HipsVRIK2nd.solver.spine.maxRootAngle = 180f;

        HipsVRIK2nd.solver.rightArm.target = RHandTarget.transform;
        HipsVRIK2nd.solver.rightArm.positionWeight = 1f;
        HipsVRIK2nd.solver.rightArm.rotationWeight = 0f;
        HipsVRIK2nd.solver.rightArm.wristToPalmAxis = new Vector3(1, 0, 0);
        HipsVRIK2nd.solver.rightArm.palmToThumbAxis = new Vector3(1, 0, 0);
        HipsVRIK2nd.solver.rightArm.shoulderRotationWeight = 0f;

        HipsVRIK2nd.solver.leftArm.target = LHandTarget.transform;
        HipsVRIK2nd.solver.leftArm.positionWeight = 1;
        HipsVRIK2nd.solver.leftArm.rotationWeight = 0;
        HipsVRIK2nd.solver.leftArm.wristToPalmAxis = new Vector3(-1, 0, 0);
        HipsVRIK2nd.solver.leftArm.palmToThumbAxis = new Vector3(1, 0, 0);
        HipsVRIK2nd.solver.leftArm.shoulderRotationWeight = 0f;

        HipsVRIK2nd.solver.rightLeg.target = RLegTarget.transform;
        HipsVRIK2nd.solver.rightLeg.positionWeight = 1;
        HipsVRIK2nd.solver.rightLeg.rotationWeight = 0;
        HipsVRIK2nd.solver.leftLeg.target = LLegTarget.transform;
        HipsVRIK2nd.solver.leftLeg.positionWeight = 1;
        HipsVRIK2nd.solver.leftLeg.rotationWeight = 0;

        HipsVRIK2nd.solver.locomotion.weight = 0f;

        HipsVRIK2nd.enabled = false;

        var HipsIKEO = HipsIK.GetOrAddComponent<RootMotion.FinalIK.IKExecutionOrder>();
        HipsIKEO.IKComponents = new RootMotion.FinalIK.IK[] { HipsVRIK1st , HipsVRIK2nd };
        HipsIKEO.animator = animator;
        HipsIKEO.enabled = false;


        var HeadIK = new GameObject("HeadIK");
        HeadIK.transform.parent = Root.transform;
        var HeadIK1st = new GameObject("1st");
        HeadIK1st.transform.parent = HeadIK.transform;
        var HeadIK2nd = new GameObject("2nd");
        HeadIK2nd.transform.parent = HeadIK.transform;
        var HeadVRIK1st = HeadIK1st.GetOrAddComponent<RootMotion.FinalIK.VRIK>();
        HeadVRIK1st.references.root = avatarDescriptor.gameObject.transform;
        HeadVRIK1st.references.pelvis = animator.GetBoneTransform(HumanBodyBones.Hips);
        HeadVRIK1st.references.spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        HeadVRIK1st.references.chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        HeadVRIK1st.references.neck = animator.GetBoneTransform(HumanBodyBones.Neck);
        HeadVRIK1st.references.head = animator.GetBoneTransform(HumanBodyBones.Head);
        HeadVRIK1st.references.leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
        HeadVRIK1st.references.leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        HeadVRIK1st.references.leftForearm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        HeadVRIK1st.references.leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        HeadVRIK1st.references.rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
        HeadVRIK1st.references.rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        HeadVRIK1st.references.rightForearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        HeadVRIK1st.references.rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        HeadVRIK1st.references.leftThigh = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        HeadVRIK1st.references.leftCalf = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        HeadVRIK1st.references.leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        if (animator.GetBoneTransform(HumanBodyBones.LeftToes) != null)
        {
            //HeadVRIK1st.references.leftToes = animator.GetBoneTransform(HumanBodyBones.LeftToes);
        }
        HeadVRIK1st.references.rightThigh = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        HeadVRIK1st.references.rightCalf = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        HeadVRIK1st.references.rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        if (animator.GetBoneTransform(HumanBodyBones.RightToes) != null)
        {
            //HeadVRIK1st.references.rightToes = animator.GetBoneTransform(HumanBodyBones.RightToes);
        }
        HeadVRIK1st.fixTransforms = false;
        HeadVRIK1st.solver.LOD = 0;

        HeadVRIK1st.solver.IKPositionWeight = 1f;
        HeadVRIK1st.solver.plantFeet = false;
        HeadVRIK1st.solver.spine.headTarget = Head.transform;
        HeadVRIK1st.solver.spine.positionWeight = 1f;
        HeadVRIK1st.solver.spine.rotationWeight = 0f;
        HeadVRIK1st.solver.spine.headClampWeight = 0f;
        HeadVRIK1st.solver.spine.minHeadHeight = 0;
        HeadVRIK1st.solver.spine.rotateChestByHands = 0;

        HeadVRIK1st.solver.spine.pelvisTarget = Hips.transform;
        HeadVRIK1st.solver.spine.pelvisPositionWeight = 1f;
        HeadVRIK1st.solver.spine.pelvisRotationWeight = 1f;
        HeadVRIK1st.solver.spine.maintainPelvisPosition = 0f;

        HeadVRIK1st.solver.spine.maxRootAngle = 180f;

        HeadVRIK1st.solver.rightArm.target = animator.GetBoneTransform(HumanBodyBones.RightHand);
        HeadVRIK1st.solver.rightArm.positionWeight = 1f;
        HeadVRIK1st.solver.rightArm.rotationWeight = 1f;
        HeadVRIK1st.solver.rightArm.shoulderRotationWeight = 0f;
        HeadVRIK1st.solver.rightArm.wristToPalmAxis = new Vector3(1, 0, 0);
        HeadVRIK1st.solver.rightArm.palmToThumbAxis = new Vector3(1, 0, 0);

        HeadVRIK1st.solver.leftArm.target = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        HeadVRIK1st.solver.leftArm.positionWeight = 1;
        HeadVRIK1st.solver.leftArm.rotationWeight = 1;
        HeadVRIK1st.solver.leftArm.shoulderRotationWeight = 0f;
        HeadVRIK1st.solver.leftArm.wristToPalmAxis = new Vector3(-1, 0, 0);
        HeadVRIK1st.solver.leftArm.palmToThumbAxis = new Vector3(1, 0, 0);

        HeadVRIK1st.solver.rightLeg.target = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        HeadVRIK1st.solver.rightLeg.positionWeight = 0;
        HeadVRIK1st.solver.rightLeg.rotationWeight = 0;
        HeadVRIK1st.solver.leftLeg.target = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        HeadVRIK1st.solver.leftLeg.positionWeight = 0;
        HeadVRIK1st.solver.leftLeg.rotationWeight = 0;


        HeadVRIK1st.solver.locomotion.weight = 0f;

        HeadVRIK1st.enabled = false;

        var HeadVRIK2nd = HeadIK2nd.GetOrAddComponent<RootMotion.FinalIK.VRIK>();
        HeadVRIK2nd.references.root = avatarDescriptor.gameObject.transform;
        HeadVRIK2nd.references.pelvis = Hips2.transform;
        HeadVRIK2nd.references.spine = Spine2.transform;
        HeadVRIK2nd.references.chest = Chest2.transform;
        HeadVRIK2nd.references.neck = Neck2.transform;
        HeadVRIK2nd.references.head = Head2.transform;
        HeadVRIK2nd.references.leftShoulder = LeftShoulder2.transform;
        HeadVRIK2nd.references.leftUpperArm = LeftUpperArm2.transform;
        HeadVRIK2nd.references.leftForearm = LeftLowerArm2.transform;
        HeadVRIK2nd.references.leftHand = LeftHand2.transform;
        HeadVRIK2nd.references.rightShoulder = RightShoulder2.transform;
        HeadVRIK2nd.references.rightUpperArm = RightUpperArm2.transform;
        HeadVRIK2nd.references.rightForearm = RightLowerArm2.transform;
        HeadVRIK2nd.references.rightHand = RightHand2.transform;
        HeadVRIK2nd.references.leftThigh = LeftUpperLeg2.transform;
        HeadVRIK2nd.references.leftCalf = LeftLowerLeg2.transform;
        HeadVRIK2nd.references.leftFoot = LeftFoot2.transform;
        if (animator.GetBoneTransform(HumanBodyBones.LeftToes) != null)
        {
            //HeadVRIK2nd.references.leftToes = 
        }
        HeadVRIK2nd.references.rightThigh = RightUpperLeg2.transform;
        HeadVRIK2nd.references.rightCalf = RightLowerLeg2.transform;
        HeadVRIK2nd.references.rightFoot = RightFoot2.transform;
        if (animator.GetBoneTransform(HumanBodyBones.RightToes) != null)
        {
            //HeadVRIK2nd.references.rightToes = animator.GetBoneTransform(HumanBodyBones.RightToes);
        }
        HeadVRIK2nd.fixTransforms = true;
        HeadVRIK2nd.solver.LOD = 0;

        HeadVRIK2nd.solver.IKPositionWeight = 1f;
        HeadVRIK2nd.solver.plantFeet = false;
        HeadVRIK2nd.solver.spine.headTarget = Head.transform;
        HeadVRIK2nd.solver.spine.positionWeight = 1f;
        HeadVRIK2nd.solver.spine.rotationWeight = 0f;
        HeadVRIK2nd.solver.spine.headClampWeight = 0f;
        HeadVRIK2nd.solver.spine.minHeadHeight = 0;
        HeadVRIK2nd.solver.spine.rotateChestByHands = 0;

        HeadVRIK2nd.solver.spine.pelvisTarget = Hips.transform;
        HeadVRIK2nd.solver.spine.pelvisPositionWeight = 1f;
        HeadVRIK2nd.solver.spine.pelvisRotationWeight = 1f;
        HeadVRIK2nd.solver.spine.maintainPelvisPosition = 0f;

        HeadVRIK2nd.solver.spine.maxRootAngle = 180f;

        HeadVRIK2nd.solver.rightArm.target = RHandTarget.transform;
        HeadVRIK2nd.solver.rightArm.positionWeight = 1f;
        HeadVRIK2nd.solver.rightArm.rotationWeight = 0f;
        HeadVRIK2nd.solver.rightArm.wristToPalmAxis = new Vector3(1, 0, 0);
        HeadVRIK2nd.solver.rightArm.palmToThumbAxis = new Vector3(1, 0, 0);
        HeadVRIK2nd.solver.rightArm.shoulderRotationWeight = 0f;

        HeadVRIK2nd.solver.leftArm.target = LHandTarget.transform;
        HeadVRIK2nd.solver.leftArm.positionWeight = 1;
        HeadVRIK2nd.solver.leftArm.rotationWeight = 0;
        HeadVRIK2nd.solver.leftArm.wristToPalmAxis = new Vector3(-1, 0, 0);
        HeadVRIK2nd.solver.leftArm.palmToThumbAxis = new Vector3(1, 0, 0);
        HeadVRIK2nd.solver.leftArm.shoulderRotationWeight = 0f;

        HeadVRIK2nd.solver.rightLeg.target = RLegTarget.transform;
        HeadVRIK2nd.solver.rightLeg.positionWeight = 1;
        HeadVRIK2nd.solver.rightLeg.rotationWeight = 0;
        HeadVRIK2nd.solver.leftLeg.target = LLegTarget.transform;
        HeadVRIK2nd.solver.leftLeg.positionWeight = 1;
        HeadVRIK2nd.solver.leftLeg.rotationWeight = 0;

        HeadVRIK2nd.solver.locomotion.weight = 0f;

        HeadVRIK2nd.enabled = false;

        var HeadIKEO = HeadIK.GetOrAddComponent<RootMotion.FinalIK.IKExecutionOrder>();
        HeadIKEO.IKComponents = new RootMotion.FinalIK.IK[] { HeadVRIK1st, HeadVRIK2nd };
        HeadIKEO.animator = animator;
        HeadIKEO.enabled = false;


        var RHandIK = new GameObject("RHandIK");
        RHandIK.transform.parent = Root.transform;
        var RHandIK1st = new GameObject("1st");
        RHandIK1st.transform.parent = RHandIK.transform;
        var RHandIK2nd = new GameObject("2nd");
        RHandIK2nd.transform.parent = RHandIK.transform;


        var RHandVRIK = RHandIK.GetOrAddComponent<RootMotion.FinalIK.VRIK>();
        RHandVRIK.references.root = avatarDescriptor.gameObject.transform;
        RHandVRIK.references.pelvis = animator.GetBoneTransform(HumanBodyBones.Hips);
        RHandVRIK.references.spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        RHandVRIK.references.chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        RHandVRIK.references.neck = animator.GetBoneTransform(HumanBodyBones.Neck);
        RHandVRIK.references.head = animator.GetBoneTransform(HumanBodyBones.Head);
        RHandVRIK.references.leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
        RHandVRIK.references.leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        RHandVRIK.references.leftForearm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        RHandVRIK.references.leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        RHandVRIK.references.rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
        RHandVRIK.references.rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        RHandVRIK.references.rightForearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        RHandVRIK.references.rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);

        RHandVRIK.fixTransforms = false;
        RHandVRIK.solver.LOD = 0;

        RHandVRIK.solver.IKPositionWeight = 1f;
        RHandVRIK.solver.plantFeet = false;

        RHandVRIK.solver.spine.headTarget = animator.GetBoneTransform(HumanBodyBones.Head);
        RHandVRIK.solver.spine.positionWeight = 0f;
        RHandVRIK.solver.spine.rotationWeight = 0f;
        RHandVRIK.solver.spine.headClampWeight = 0f;
        RHandVRIK.solver.spine.minHeadHeight = 0;
        RHandVRIK.solver.spine.rotateChestByHands = 0;

        RHandVRIK.solver.spine.pelvisTarget = animator.GetBoneTransform(HumanBodyBones.Hips);
        RHandVRIK.solver.spine.pelvisPositionWeight = 1f;
        RHandVRIK.solver.spine.pelvisRotationWeight = 1f;
        RHandVRIK.solver.spine.maintainPelvisPosition = 0f;

        RHandVRIK.solver.spine.maxRootAngle = 180f;

        RHandVRIK.solver.rightArm.target = RightHand.transform;
        RHandVRIK.solver.rightArm.positionWeight = 1f;
        RHandVRIK.solver.rightArm.rotationWeight = 0f;
        RHandVRIK.solver.rightArm.shoulderRotationWeight = 1;
        RHandVRIK.solver.rightArm.shoulderTwistWeight = 0;
        RHandVRIK.solver.rightArm.wristToPalmAxis = new Vector3(1, 0, 0);
        RHandVRIK.solver.rightArm.palmToThumbAxis = new Vector3(1, 0, 0);

        RHandVRIK.solver.leftArm.positionWeight = 0;
        RHandVRIK.solver.leftArm.rotationWeight = 0;
        RHandVRIK.solver.leftArm.shoulderRotationWeight = 0;
        RHandVRIK.solver.leftArm.wristToPalmAxis = new Vector3(-1, 0, 0);
        RHandVRIK.solver.leftArm.palmToThumbAxis = new Vector3(1, 0, 0);

        RHandVRIK.solver.rightLeg.positionWeight = 0;
        RHandVRIK.solver.rightLeg.rotationWeight = 0;

        RHandVRIK.solver.leftLeg.positionWeight = 0;
        RHandVRIK.solver.leftLeg.rotationWeight = 0;

        RHandVRIK.solver.locomotion.weight = 0f;

        RHandVRIK.enabled = false;

        var RHandLimbIK1st = RHandIK1st.GetOrAddComponent<RootMotion.FinalIK.LimbIK>();
        RHandLimbIK1st.fixTransforms = false;
        FieldInfo field = typeof(RootMotion.FinalIK.IKSolverLimb).GetField
            ("root", BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(RHandLimbIK1st.solver, (Transform)RHandLimbIK1st.transform);
        RHandLimbIK1st.solver.bone1.transform = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        RHandLimbIK1st.solver.bone2.transform = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        RHandLimbIK1st.solver.bone3.transform = animator.GetBoneTransform(HumanBodyBones.RightHand);
        RHandLimbIK1st.solver.target = RightHand.transform;
        //RHandLimbIK1st.solver.Initiate(animator.GetBoneTransform(HumanBodyBones.RightUpperArm));
        RHandLimbIK1st.solver.IKPositionWeight = 1f;
        RHandLimbIK1st.solver.IKRotationWeight = 0f;
        RHandLimbIK1st.solver.bendModifier = RootMotion.FinalIK.IKSolverLimb.BendModifier.Arm;
        RHandLimbIK1st.solver.goal = AvatarIKGoal.RightHand;
        RHandLimbIK1st.solver.maintainRotationWeight = 0f;
        RHandLimbIK1st.solver.bendModifierWeight = 1f;

        RHandLimbIK1st.enabled = false;

        var RHandLimbIK2nd = RHandIK2nd.GetOrAddComponent<RootMotion.FinalIK.LimbIK>();
        RHandLimbIK2nd.fixTransforms = true;
        field = typeof(RootMotion.FinalIK.IKSolverLimb).GetField
            ("root", BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(RHandLimbIK2nd.solver, (Transform)RHandLimbIK2nd.transform);
        RHandLimbIK2nd.solver.bone1.transform = RightUpperArm2.transform;
        RHandLimbIK2nd.solver.bone2.transform = RightLowerArm2.transform;
        RHandLimbIK2nd.solver.bone3.transform = RightHand2.transform;
        RHandLimbIK2nd.solver.target = RightHand.transform;
        //RHandLimbIK2nd.solver.Initiate(RightUpperArm2.transform);
        RHandLimbIK2nd.solver.IKPositionWeight = 1f;
        RHandLimbIK2nd.solver.IKRotationWeight = 0f;
        RHandLimbIK2nd.solver.bendModifier = RootMotion.FinalIK.IKSolverLimb.BendModifier.Arm;
        RHandLimbIK2nd.solver.goal = AvatarIKGoal.RightHand;
        RHandLimbIK2nd.solver.maintainRotationWeight = 0f;
        RHandLimbIK2nd.solver.bendModifierWeight = 1f;

        RHandLimbIK2nd.enabled = false;

        var RHandIKEO = RHandIK.GetOrAddComponent<RootMotion.FinalIK.IKExecutionOrder>();
        RHandIKEO.IKComponents = new RootMotion.FinalIK.IK[] {RHandVRIK, RHandLimbIK1st,RHandLimbIK2nd };
        RHandIKEO.animator = animator;
        RHandIKEO.enabled = false;

        var LHandIK = new GameObject("LHandIK");
        LHandIK.transform.parent = Root.transform;
        var LHandIK1st = new GameObject("1st");
        LHandIK1st.transform.parent = LHandIK.transform;
        var LHandIK2nd = new GameObject("2nd");
        LHandIK2nd.transform.parent = LHandIK.transform;

        var LHandVRIK = LHandIK.GetOrAddComponent<RootMotion.FinalIK.VRIK>();
        LHandVRIK.references.root = avatarDescriptor.gameObject.transform;
        LHandVRIK.references.pelvis = animator.GetBoneTransform(HumanBodyBones.Hips);
        LHandVRIK.references.spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        LHandVRIK.references.chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        LHandVRIK.references.neck = animator.GetBoneTransform(HumanBodyBones.Neck);
        LHandVRIK.references.head = animator.GetBoneTransform(HumanBodyBones.Head);
        LHandVRIK.references.leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
        LHandVRIK.references.leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        LHandVRIK.references.leftForearm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        LHandVRIK.references.leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        LHandVRIK.references.rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
        LHandVRIK.references.rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        LHandVRIK.references.rightForearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        LHandVRIK.references.rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);

        LHandVRIK.fixTransforms = false;
        LHandVRIK.solver.LOD = 0;

        LHandVRIK.solver.IKPositionWeight = 1f;
        LHandVRIK.solver.plantFeet = false;
        LHandVRIK.solver.spine.headTarget = animator.GetBoneTransform(HumanBodyBones.Head);
        LHandVRIK.solver.spine.positionWeight = 0f;
        LHandVRIK.solver.spine.rotationWeight = 0f;
        LHandVRIK.solver.spine.headClampWeight = 0f;
        LHandVRIK.solver.spine.minHeadHeight = 0;
        LHandVRIK.solver.spine.rotateChestByHands = 0;

        LHandVRIK.solver.spine.pelvisTarget = animator.GetBoneTransform(HumanBodyBones.Hips);
        LHandVRIK.solver.spine.pelvisPositionWeight = 1f;
        LHandVRIK.solver.spine.pelvisRotationWeight = 1f;
        LHandVRIK.solver.spine.maintainPelvisPosition = 0f;

        LHandVRIK.solver.spine.maxRootAngle = 180f;

        LHandVRIK.solver.rightArm.positionWeight = 0f;
        LHandVRIK.solver.rightArm.rotationWeight = 0f;
        LHandVRIK.solver.rightArm.shoulderRotationWeight = 0;
        LHandVRIK.solver.rightArm.shoulderTwistWeight = 0;
        LHandVRIK.solver.rightArm.wristToPalmAxis = new Vector3(1, 0, 0);
        LHandVRIK.solver.rightArm.palmToThumbAxis = new Vector3(1, 0, 0);

        LHandVRIK.solver.leftArm.target = LeftHand.transform;
        LHandVRIK.solver.leftArm.positionWeight = 1;
        LHandVRIK.solver.leftArm.rotationWeight = 0;
        LHandVRIK.solver.leftArm.shoulderRotationWeight = 1;
        LHandVRIK.solver.leftArm.shoulderTwistWeight = 0;
        LHandVRIK.solver.leftArm.wristToPalmAxis = new Vector3(-1, 0, 0);
        LHandVRIK.solver.leftArm.palmToThumbAxis = new Vector3(1, 0, 0);

        LHandVRIK.solver.rightLeg.positionWeight = 0;
        LHandVRIK.solver.rightLeg.rotationWeight = 0;

        LHandVRIK.solver.leftLeg.positionWeight = 0;
        LHandVRIK.solver.leftLeg.rotationWeight = 0;

        LHandVRIK.solver.locomotion.weight = 0f;

        LHandVRIK.enabled = false;


        var LHandLimbIK1st = LHandIK1st.GetOrAddComponent<RootMotion.FinalIK.LimbIK>();
        LHandLimbIK1st.fixTransforms = false;
        field = typeof(RootMotion.FinalIK.IKSolverLimb).GetField
            ("root", BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(LHandLimbIK1st.solver, (Transform)LHandLimbIK1st.transform);
        LHandLimbIK1st.solver.bone1.transform = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        LHandLimbIK1st.solver.bone2.transform = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        LHandLimbIK1st.solver.bone3.transform = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        LHandLimbIK1st.solver.target = LeftHand.transform;
        //LHandLimbIK1st.solver.Initiate(animator.GetBoneTransform(HumanBodyBones.LeftUpperArm));
        LHandLimbIK1st.solver.IKPositionWeight = 1f;
        LHandLimbIK1st.solver.IKRotationWeight = 0f;
        LHandLimbIK1st.solver.bendModifier = RootMotion.FinalIK.IKSolverLimb.BendModifier.Arm;
        LHandLimbIK1st.solver.goal = AvatarIKGoal.LeftHand;
        LHandLimbIK1st.solver.maintainRotationWeight = 0f;
        LHandLimbIK1st.solver.bendModifierWeight = 1f;

        LHandLimbIK1st.enabled = false;

        var LHandLimbIK2nd = LHandIK2nd.GetOrAddComponent<RootMotion.FinalIK.LimbIK>();
        LHandLimbIK2nd.fixTransforms = true;
        field = typeof(RootMotion.FinalIK.IKSolverLimb).GetField
            ("root", BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(LHandLimbIK2nd.solver, (Transform)LHandLimbIK2nd.transform);
        LHandLimbIK2nd.solver.bone1.transform = LeftUpperArm2.transform;
        LHandLimbIK2nd.solver.bone2.transform = LeftLowerArm2.transform;
        LHandLimbIK2nd.solver.bone3.transform = LeftHand2.transform;
        LHandLimbIK2nd.solver.target = LeftHand.transform;
        //LHandLimbIK2nd.solver.Initiate(LeftUpperArm2.transform);
        LHandLimbIK2nd.solver.IKPositionWeight = 1f;
        LHandLimbIK2nd.solver.IKRotationWeight = 0f;
        LHandLimbIK2nd.solver.bendModifier = RootMotion.FinalIK.IKSolverLimb.BendModifier.Arm;
        LHandLimbIK2nd.solver.goal = AvatarIKGoal.LeftHand;
        LHandLimbIK2nd.solver.maintainRotationWeight = 0f;
        LHandLimbIK2nd.solver.bendModifierWeight = 1f;

        LHandLimbIK2nd.enabled = false;

        var LHandIKEO = LHandIK.GetOrAddComponent<RootMotion.FinalIK.IKExecutionOrder>();
        LHandIKEO.IKComponents = new RootMotion.FinalIK.IK[] {LHandVRIK, LHandLimbIK1st, LHandLimbIK2nd };
        LHandIKEO.animator = animator;
        LHandIKEO.enabled = false;


        var RLegIK = new GameObject("RLegIK");
        RLegIK.transform.parent = Root.transform;
        var RLegIK1st = new GameObject("1st");
        RLegIK1st.transform.parent = RLegIK.transform;
        var RLegIK2nd = new GameObject("2nd");
        RLegIK2nd.transform.parent = RLegIK.transform;

        var RLegVRIK = RLegIK.GetOrAddComponent<RootMotion.FinalIK.VRIK>();
        RLegVRIK.references.root = avatarDescriptor.gameObject.transform;
        RLegVRIK.references.pelvis = animator.GetBoneTransform(HumanBodyBones.Hips);
        RLegVRIK.references.spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        RLegVRIK.references.chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        RLegVRIK.references.neck = animator.GetBoneTransform(HumanBodyBones.Neck);
        RLegVRIK.references.head = animator.GetBoneTransform(HumanBodyBones.Head);
        RLegVRIK.references.leftThigh = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        RLegVRIK.references.leftCalf = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        RLegVRIK.references.leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        RLegVRIK.references.rightThigh = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        RLegVRIK.references.rightCalf = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        RLegVRIK.references.rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);

        RLegVRIK.fixTransforms = false;
        RLegVRIK.solver.LOD = 0;

        RLegVRIK.solver.IKPositionWeight = 1f;
        RLegVRIK.solver.plantFeet = false;
        RLegVRIK.solver.spine.positionWeight = 0f;
        RLegVRIK.solver.spine.rotationWeight = 0f;
        RLegVRIK.solver.spine.headClampWeight = 0f;
        RLegVRIK.solver.spine.minHeadHeight = 0;
        RLegVRIK.solver.spine.rotateChestByHands = 0;

        RLegVRIK.solver.spine.pelvisTarget = Hips.transform;
        RLegVRIK.solver.spine.pelvisPositionWeight = 1f;
        RLegVRIK.solver.spine.pelvisRotationWeight = 1f;
        RLegVRIK.solver.spine.maintainPelvisPosition = 0f;

        RLegVRIK.solver.spine.maxRootAngle = 180f;

        RLegVRIK.solver.rightArm.positionWeight = 0f;
        RLegVRIK.solver.rightArm.rotationWeight = 0f;
        RLegVRIK.solver.rightArm.shoulderRotationWeight = 0;
        RLegVRIK.solver.rightArm.wristToPalmAxis = new Vector3(1, 0, 0);
        RLegVRIK.solver.rightArm.palmToThumbAxis = new Vector3(1, 0, 0);

        RLegVRIK.solver.leftArm.positionWeight = 0;
        RLegVRIK.solver.leftArm.rotationWeight = 0;
        RLegVRIK.solver.leftArm.shoulderRotationWeight = 0;
        RLegVRIK.solver.leftArm.wristToPalmAxis = new Vector3(-1, 0, 0);
        RLegVRIK.solver.leftArm.palmToThumbAxis = new Vector3(1, 0, 0);

        RLegVRIK.solver.rightLeg.positionWeight = 0;
        RLegVRIK.solver.rightLeg.rotationWeight = 0;

        RLegVRIK.solver.leftLeg.target = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        RLegVRIK.solver.leftLeg.positionWeight = 1;
        RLegVRIK.solver.leftLeg.rotationWeight = 1;

        RLegVRIK.solver.locomotion.weight = 0f;

        RLegVRIK.enabled = false;

        var RLegLimbIK1st = RLegIK1st.GetOrAddComponent<RootMotion.FinalIK.LimbIK>();
        RLegLimbIK1st.fixTransforms = false;
        field = typeof(RootMotion.FinalIK.IKSolverLimb).GetField
            ("root", BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(RLegLimbIK1st.solver, (Transform)RLegLimbIK1st.transform);
        RLegLimbIK1st.solver.bone1.transform = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        RLegLimbIK1st.solver.bone2.transform = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        RLegLimbIK1st.solver.bone3.transform = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        RLegLimbIK1st.solver.target = RightFoot.transform;
        //RLegLimbIK1st.solver.Initiate(animator.GetBoneTransform(HumanBodyBones.RightUpperLeg));
        RLegLimbIK1st.solver.IKPositionWeight = 1f;
        RLegLimbIK1st.solver.IKRotationWeight = 0f;
        RLegLimbIK1st.solver.bendModifier = RootMotion.FinalIK.IKSolverLimb.BendModifier.Arm;
        RLegLimbIK1st.solver.bendNormal = new Vector3(0.004862062f, 8.604703e-08f, -4.122271e-06f);
        RLegLimbIK1st.solver.goal = AvatarIKGoal.RightFoot;
        RLegLimbIK1st.solver.maintainRotationWeight = 0f;
        RLegLimbIK1st.solver.bendModifierWeight = 1f;

        RLegLimbIK1st.enabled = false;

        var RLegLimbIK2nd = RLegIK2nd.GetOrAddComponent<RootMotion.FinalIK.LimbIK>();
        RLegLimbIK2nd.fixTransforms = true;
        field = typeof(RootMotion.FinalIK.IKSolverLimb).GetField
            ("root", BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(RLegLimbIK2nd.solver, (Transform)RLegLimbIK2nd.transform);
        RLegLimbIK2nd.solver.bone1.transform = RightUpperLeg2.transform;
        RLegLimbIK2nd.solver.bone2.transform = RightLowerLeg2.transform;
        RLegLimbIK2nd.solver.bone3.transform = RightFoot2.transform;
        RLegLimbIK2nd.solver.target = RightFoot.transform;
        //RLegLimbIK2nd.solver.Initiate(RightUpperLeg2.transform);
        RLegLimbIK2nd.solver.IKPositionWeight = 1f;
        RLegLimbIK2nd.solver.IKRotationWeight = 0f;
        RLegLimbIK2nd.solver.bendModifier = RootMotion.FinalIK.IKSolverLimb.BendModifier.Arm;
        RLegLimbIK2nd.solver.bendNormal = new Vector3(0.004862062f, 8.604703e-08f, -4.122271e-06f);
        RLegLimbIK2nd.solver.goal = AvatarIKGoal.RightFoot;
        RLegLimbIK2nd.solver.maintainRotationWeight = 0f;
        RLegLimbIK2nd.solver.bendModifierWeight = 0f;

        RLegLimbIK2nd.enabled = false;

        var RLegIKEO = RLegIK.GetOrAddComponent<RootMotion.FinalIK.IKExecutionOrder>();
        RLegIKEO.IKComponents = new RootMotion.FinalIK.IK[] { RLegVRIK ,RLegLimbIK1st,RLegLimbIK2nd};
        RLegIKEO.animator = animator;
        RLegIKEO.enabled = false;

        var LLegIK = new GameObject("LLegIK");
        LLegIK.transform.parent = Root.transform;
        var LLegIK1st = new GameObject("1st");
        LLegIK1st.transform.parent = LLegIK.transform;
        var LLegIK2nd = new GameObject("2nd");
        LLegIK2nd.transform.parent = LLegIK.transform;

        var LLegVRIK = LLegIK.GetOrAddComponent<RootMotion.FinalIK.VRIK>();
        LLegVRIK.references.root = avatarDescriptor.gameObject.transform;
        LLegVRIK.references.pelvis = animator.GetBoneTransform(HumanBodyBones.Hips);
        LLegVRIK.references.spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        LLegVRIK.references.chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        LLegVRIK.references.neck = animator.GetBoneTransform(HumanBodyBones.Neck);
        LLegVRIK.references.head = animator.GetBoneTransform(HumanBodyBones.Head);
        LLegVRIK.references.leftThigh = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        LLegVRIK.references.leftCalf = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        LLegVRIK.references.leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        LLegVRIK.references.rightThigh = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        LLegVRIK.references.rightCalf = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        LLegVRIK.references.rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);

        LLegVRIK.fixTransforms = false;
        LLegVRIK.solver.LOD = 0;

        LLegVRIK.solver.IKPositionWeight = 1f;
        LLegVRIK.solver.plantFeet = false;
        LLegVRIK.solver.spine.positionWeight = 0f;
        LLegVRIK.solver.spine.rotationWeight = 0f;
        LLegVRIK.solver.spine.headClampWeight = 0f;
        LLegVRIK.solver.spine.minHeadHeight = 0;
        LLegVRIK.solver.spine.rotateChestByHands = 0;

        LLegVRIK.solver.spine.pelvisTarget = Hips.transform;
        LLegVRIK.solver.spine.pelvisPositionWeight = 1f;
        LLegVRIK.solver.spine.pelvisRotationWeight = 1f;
        LLegVRIK.solver.spine.maintainPelvisPosition = 0f;

        LLegVRIK.solver.spine.maxRootAngle = 180f;

        LLegVRIK.solver.rightArm.positionWeight = 0f;
        LLegVRIK.solver.rightArm.rotationWeight = 0f;
        LLegVRIK.solver.rightArm.shoulderRotationWeight = 0;
        LLegVRIK.solver.rightArm.wristToPalmAxis = new Vector3(1, 0, 0);
        LLegVRIK.solver.rightArm.palmToThumbAxis = new Vector3(1, 0, 0);

        LLegVRIK.solver.leftArm.positionWeight = 0;
        LLegVRIK.solver.leftArm.rotationWeight = 0;
        LLegVRIK.solver.leftArm.shoulderRotationWeight = 0;
        LLegVRIK.solver.leftArm.wristToPalmAxis = new Vector3(-1, 0, 0);
        LLegVRIK.solver.leftArm.palmToThumbAxis = new Vector3(1, 0, 0);

        LLegVRIK.solver.rightLeg.target = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        LLegVRIK.solver.rightLeg.positionWeight = 1;
        LLegVRIK.solver.rightLeg.rotationWeight = 1;

        LLegVRIK.solver.leftLeg.positionWeight = 0;
        LLegVRIK.solver.leftLeg.rotationWeight = 0;

        LLegVRIK.solver.locomotion.weight = 0f;

        LLegVRIK.enabled = false;


                var LLegLimbIK1st = LLegIK1st.GetOrAddComponent<RootMotion.FinalIK.LimbIK>();
        LLegLimbIK1st.fixTransforms = false;
        field = typeof(RootMotion.FinalIK.IKSolverLimb).GetField
            ("root", BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(LLegLimbIK1st.solver, (Transform)LLegLimbIK1st.transform);
        LLegLimbIK1st.solver.bone1.transform = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        LLegLimbIK1st.solver.bone2.transform = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        LLegLimbIK1st.solver.bone3.transform = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        LLegLimbIK1st.solver.target = LeftFoot.transform;
        //LLegLimbIK1st.solver.Initiate(animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg));
        LLegLimbIK1st.solver.IKPositionWeight = 1f;
        LLegLimbIK1st.solver.IKRotationWeight = 0f;
        LLegLimbIK1st.solver.bendModifier = RootMotion.FinalIK.IKSolverLimb.BendModifier.Arm;
        LLegLimbIK1st.solver.goal = AvatarIKGoal.LeftFoot;
        LLegLimbIK1st.solver.bendNormal = new Vector3(0.004862062f, -8.604703e-08f, 4.122271e-06f);
        LLegLimbIK1st.solver.maintainRotationWeight = 0f;
        LLegLimbIK1st.solver.bendModifierWeight = 1f;

        LLegLimbIK1st.enabled = false;

        var LLegLimbIK2nd = LLegIK2nd.GetOrAddComponent<RootMotion.FinalIK.LimbIK>();
        LLegLimbIK2nd.fixTransforms = true;
        field = typeof(RootMotion.FinalIK.IKSolverLimb).GetField
            ("root", BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(LLegLimbIK2nd.solver, (Transform)LLegLimbIK2nd.transform);
        LLegLimbIK2nd.solver.bone1.transform = LeftUpperLeg2.transform;
        LLegLimbIK2nd.solver.bone2.transform = LeftLowerLeg2.transform;
        LLegLimbIK2nd.solver.bone3.transform = LeftFoot2.transform;
        LLegLimbIK2nd.solver.target = LeftFoot.transform;
        //LLegLimbIK2nd.solver.Initiate(LeftUpperLeg2.transform);
        LLegLimbIK2nd.solver.IKPositionWeight = 1f;
        LLegLimbIK2nd.solver.IKRotationWeight = 0f;
        LLegLimbIK2nd.solver.bendModifier = RootMotion.FinalIK.IKSolverLimb.BendModifier.Arm;
        LLegLimbIK2nd.solver.goal = AvatarIKGoal.LeftFoot;
        LLegLimbIK2nd.solver.bendNormal = new Vector3(0.004862062f, -8.604703e-08f, 4.122271e-06f);
        LLegLimbIK2nd.solver.maintainRotationWeight = 0f;
        LLegLimbIK2nd.solver.bendModifierWeight = 0f;

        LLegLimbIK2nd.enabled = false;

        var LLegIKEO = LLegIK.GetOrAddComponent<RootMotion.FinalIK.IKExecutionOrder>();
        LLegIKEO.IKComponents = new RootMotion.FinalIK.IK[] { LLegVRIK, LLegLimbIK1st, LLegLimbIK2nd };
        LLegIKEO.animator = animator;
        LLegIKEO.enabled = false;


        var fxAnimatorLayer =
            avatarDescriptor.baseAnimationLayers.First(item => item.type == VRCAvatarDescriptor.AnimLayerType.FX && item.animatorController != null);
        var fxAnimator = (AnimatorController)fxAnimatorLayer.animatorController;

        EditorUtility.SetDirty(fxAnimator);

        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "Hip_IsGrabbed") == null)
            fxAnimator.AddParameter("Hip_IsGrabbed", AnimatorControllerParameterType.Bool);
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "Head_IsGrabbed") == null)
            fxAnimator.AddParameter("Head_IsGrabbed", AnimatorControllerParameterType.Bool);
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "LHand_IsGrabbed") == null)
            fxAnimator.AddParameter("LHand_IsGrabbed", AnimatorControllerParameterType.Bool);
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "LHand_IsPosed") == null)
            fxAnimator.AddParameter("LHand_IsPosed", AnimatorControllerParameterType.Bool);
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "RHand_IsGrabbed") == null)
            fxAnimator.AddParameter("RHand_IsGrabbed", AnimatorControllerParameterType.Bool);
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "RHand_IsPosed") == null)
            fxAnimator.AddParameter("RHand_IsPosed", AnimatorControllerParameterType.Bool);
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "LLeg_IsGrabbed") == null)
            fxAnimator.AddParameter("LLeg_IsGrabbed", AnimatorControllerParameterType.Bool);
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "LLeg_IsPosed") == null)
            fxAnimator.AddParameter("LLeg_IsPosed", AnimatorControllerParameterType.Bool);
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "RLeg_IsGrabbed") == null)
            fxAnimator.AddParameter("RLeg_IsGrabbed", AnimatorControllerParameterType.Bool);
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "RLeg_IsPosed") == null)
            fxAnimator.AddParameter("RLeg_IsPosed", AnimatorControllerParameterType.Bool);
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "TrackingType") == null)
            fxAnimator.AddParameter("TrackingType", AnimatorControllerParameterType.Int);
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "GFB_IA_Interactive") == null)
            fxAnimator.AddParameter("GFB_IA_Interactive", AnimatorControllerParameterType.Bool);
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "GFB_PistonOn") == null)
            fxAnimator.AddParameter("GFB_PistonOn", AnimatorControllerParameterType.Bool);

        var grabLayerIndex = Array.FindIndex(fxAnimator.layers, layer => layer.name == "HipGrab");
        if (grabLayerIndex >= 0) fxAnimator.RemoveLayer(grabLayerIndex);
        grabLayerIndex = Array.FindIndex(fxAnimator.layers, layer => layer.name == "Hip3pGrab");
        if (grabLayerIndex >= 0) fxAnimator.RemoveLayer(grabLayerIndex);
        grabLayerIndex = Array.FindIndex(fxAnimator.layers, layer => layer.name == "HeadGrab");
        if (grabLayerIndex >= 0) fxAnimator.RemoveLayer(grabLayerIndex);
        grabLayerIndex = Array.FindIndex(fxAnimator.layers, layer => layer.name == "Head3pGrab");
        if (grabLayerIndex >= 0) fxAnimator.RemoveLayer(grabLayerIndex);
        grabLayerIndex = Array.FindIndex(fxAnimator.layers, layer => layer.name == "LHandGrab");
        if (grabLayerIndex >= 0) fxAnimator.RemoveLayer(grabLayerIndex);
        grabLayerIndex = Array.FindIndex(fxAnimator.layers, layer => layer.name == "RHandGrab");
        if (grabLayerIndex >= 0) fxAnimator.RemoveLayer(grabLayerIndex);
        grabLayerIndex = Array.FindIndex(fxAnimator.layers, layer => layer.name == "LLegGrab");
        if (grabLayerIndex >= 0) fxAnimator.RemoveLayer(grabLayerIndex);
        grabLayerIndex = Array.FindIndex(fxAnimator.layers, layer => layer.name == "RLegGrab");
        if (grabLayerIndex >= 0) fxAnimator.RemoveLayer(grabLayerIndex);

        var hipLayer = new AnimatorControllerLayer();
        hipLayer.name = "HipGrab";
        hipLayer.defaultWeight = 1f;

        var hipunGrabAnim = new AnimationClip();
        var hipgrabAnim1 = new AnimationClip();
        var hipgrabAnim2 = new AnimationClip();

        AnimationUtility.SetEditorCurve(hipgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HipsDummy.transform), typeof(PositionConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HeadDummy.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Spine.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));

        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HipsDummy.transform), typeof(PositionConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HeadDummy.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Spine.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips2.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Spine2.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Chest2.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
        {
            AnimationUtility.SetEditorCurve(hipgrabAnim2,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, UpperChest2.transform), typeof(ParentConstraint),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        }
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Neck2.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Head2.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HipsIK1st.transform), typeof(RootMotion.FinalIK.VRIK),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HipsIK2nd.transform), typeof(RootMotion.FinalIK.VRIK),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HipsIK.transform), typeof(RootMotion.FinalIK.IKExecutionOrder),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Hips)), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Spine)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Chest)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
        {
            AnimationUtility.SetEditorCurve(hipgrabAnim2,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.UpperChest)), typeof(RotationConstraint),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        }
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Neck)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Head)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightShoulder)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightUpperArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightLowerArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightHand)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftShoulder)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftUpperArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftLowerArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftHand)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightUpperLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightLowerLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightFoot)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftFoot)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        if (tracking)
        {
            AnimationUtility.SetEditorCurve(hipgrabAnim2,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Camera.transform), typeof(Camera),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        }

        AnimationUtility.SetEditorCurve(hipunGrabAnim,
    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HipsDummy.transform), typeof(PositionConstraint),
        "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HeadDummy.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Spine.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips2.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Spine2.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Chest2.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
        {
            AnimationUtility.SetEditorCurve(hipunGrabAnim,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, UpperChest2.transform), typeof(ParentConstraint),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        }
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
           EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Neck2.transform), typeof(ParentConstraint),
                  "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Head2.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HipsIK1st.transform), typeof(RootMotion.FinalIK.VRIK),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HipsIK2nd.transform), typeof(RootMotion.FinalIK.VRIK),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HipsIK.transform), typeof(RootMotion.FinalIK.IKExecutionOrder),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));

        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Hips)), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Spine)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Chest)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
        {
            AnimationUtility.SetEditorCurve(hipunGrabAnim,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.UpperChest)), typeof(RotationConstraint),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        }
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Neck)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Head)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightShoulder)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightUpperArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightLowerArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightHand)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftShoulder)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftUpperArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftLowerArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftHand)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightUpperLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightLowerLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightFoot)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(hipunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftFoot)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        if (tracking)
        {
            AnimationUtility.SetEditorCurve(hipunGrabAnim,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Camera.transform), typeof(Camera),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        }

        var fxAnimatorPath = AssetDatabase.GetAssetPath(fxAnimator);
        var fxAnimatorDirPath = Path.GetDirectoryName(fxAnimatorPath);

        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/Hip_UnGrab.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/Hip_Grab1.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/Hip_Grab2.anim");
        AssetDatabase.CreateAsset(hipunGrabAnim, fxAnimatorDirPath + "/Hip_UnGrab.anim");
        AssetDatabase.CreateAsset(hipgrabAnim1, fxAnimatorDirPath + "/Hip_Grab1.anim");
        AssetDatabase.CreateAsset(hipgrabAnim2, fxAnimatorDirPath + "/Hip_Grab2.anim");

        hipLayer.stateMachine = new AnimatorStateMachine();
        hipLayer.stateMachine.hideFlags = HideFlags.HideInHierarchy;
        if (AssetDatabase.GetAssetPath(fxAnimator) != "")
            AssetDatabase.AddObjectToAsset(hipLayer.stateMachine, AssetDatabase.GetAssetPath(fxAnimator));
        EditorUtility.SetDirty(hipLayer.stateMachine);

        var hipUnGrabState = hipLayer.stateMachine.AddState("UnGrab", new Vector3(100, 150, 1));
        var hipDefaultState = hipLayer.stateMachine.AddState("Default", new Vector3(400, 150, 1));
        var hipGrabState1 = hipLayer.stateMachine.AddState("Grab1", new Vector3(400, 0, 1));
        var hipGrabState2 = hipLayer.stateMachine.AddState("Grab2", new Vector3(100, 0, 1));

        EditorUtility.SetDirty(hipDefaultState);
        EditorUtility.SetDirty(hipGrabState1);
        EditorUtility.SetDirty(hipGrabState2);
        EditorUtility.SetDirty(hipUnGrabState);

        hipLayer.stateMachine.defaultState = hipDefaultState;

        var hipTrackingoff = hipGrabState2.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
        hipTrackingoff.trackingHip = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Animation;
        var hipTrackingon = hipUnGrabState.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
        hipTrackingon.trackingHip = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Tracking;

        if (footIK)
        {
            hipGrabState2.iKOnFeet = true;
        }

        hipGrabState1.motion = hipgrabAnim1;
        hipGrabState2.motion = hipgrabAnim2;
        hipUnGrabState.motion = hipunGrabAnim;

        var HipGrabTransition1 = hipDefaultState.AddTransition(hipGrabState1);
        HipGrabTransition1.hasExitTime = false;
        HipGrabTransition1.duration = 0;
        HipGrabTransition1.exitTime = 0;
        HipGrabTransition1.offset = 0;
        HipGrabTransition1.canTransitionToSelf = false;
        HipGrabTransition1.AddCondition(AnimatorConditionMode.If, 1, "Hip_IsGrabbed");
        HipGrabTransition1.AddCondition(AnimatorConditionMode.Equals, 6, "TrackingType");

        var HipGrabTransition1a = hipDefaultState.AddTransition(hipGrabState1);
        HipGrabTransition1a.hasExitTime = false;
        HipGrabTransition1a.duration = 0;
        HipGrabTransition1a.exitTime = 0;
        HipGrabTransition1a.offset = 0;
        HipGrabTransition1a.canTransitionToSelf = false;
        HipGrabTransition1a.AddCondition(AnimatorConditionMode.If, 1, "GFB_PistonOn");
        HipGrabTransition1a.AddCondition(AnimatorConditionMode.Equals, 6, "TrackingType");

        var HipGrabTransition1b = hipDefaultState.AddTransition(hipGrabState1);
        HipGrabTransition1b.hasExitTime = false;
        HipGrabTransition1b.duration = 0;
        HipGrabTransition1b.exitTime = 0;
        HipGrabTransition1b.offset = 0;
        HipGrabTransition1b.canTransitionToSelf = false;
        HipGrabTransition1b.AddCondition(AnimatorConditionMode.If, 1, "GFB_IA_Interactive");
        HipGrabTransition1b.AddCondition(AnimatorConditionMode.Equals, 6, "TrackingType");

        var HipGrabTransition2 =hipGrabState1.AddTransition(hipGrabState2);
        HipGrabTransition2.hasExitTime = true;
        HipGrabTransition2.duration = 0;
        HipGrabTransition2.exitTime = 0.25f;
        HipGrabTransition2.offset = 0;
        HipGrabTransition2.canTransitionToSelf = false;

        var HipGrabTransition3 = hipGrabState2.AddTransition(hipUnGrabState);
        HipGrabTransition3.hasExitTime = false;
        HipGrabTransition3.duration = 0;
        HipGrabTransition3.exitTime = 0;
        HipGrabTransition3.offset = 0;
        HipGrabTransition3.canTransitionToSelf = false;
        HipGrabTransition3.AddCondition(AnimatorConditionMode.IfNot, 1, "Hip_IsGrabbed");
        HipGrabTransition3.AddCondition(AnimatorConditionMode.IfNot, 1, "GFB_IA_Interactive");
        HipGrabTransition3.AddCondition(AnimatorConditionMode.IfNot, 1, "GFB_PistonOn");

        var HipGrabTransition4 = hipUnGrabState.AddTransition(hipDefaultState);
        HipGrabTransition4.hasExitTime = true;
        HipGrabTransition4.duration = 0;
        HipGrabTransition4.exitTime = 0.25f;
        HipGrabTransition4.offset = 0;
        HipGrabTransition4.canTransitionToSelf = false;

        fxAnimator.AddLayer(hipLayer);


        var Hip3pLayer = new AnimatorControllerLayer();
        Hip3pLayer.name = "Hip3pGrab";
        Hip3pLayer.defaultWeight = 1f;


        /*
        var Hip3punGrabAnim = new AnimationClip();
        var Hip3pgrabAnim1 = new AnimationClip();
        var Hip3pgrabAnim2 = new AnimationClip();

        AnimationUtility.SetEditorCurve(Hip3pgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HipsDummy.transform), typeof(PositionConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(Hip3pgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HeadDummy.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(Hip3pgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(Hip3pgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Spine.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));

        AnimationUtility.SetEditorCurve(Hip3pgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HipsDummy.transform), typeof(PositionConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(Hip3pgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HeadDummy.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(Hip3pgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(Hip3pgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Spine.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(Hip3pgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips3pIK.transform), typeof(RootMotion.FinalIK.FullBodyBipedIK),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(Hip3pgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips3pIK.transform), typeof(RootMotion.FinalIK.IKExecutionOrder),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));


        AnimationUtility.SetEditorCurve(Hip3punGrabAnim,
    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HipsDummy.transform), typeof(PositionConstraint),
        "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(Hip3punGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HeadDummy.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(Hip3punGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(Hip3punGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Spine.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(Hip3punGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips3pIK.transform), typeof(RootMotion.FinalIK.FullBodyBipedIK),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(Hip3punGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips3pIK.transform), typeof(RootMotion.FinalIK.IKExecutionOrder),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));


        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/Hip3p_UnGrab.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/Hip3p_Grab1.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/Hip3p_Grab2.anim");
        AssetDatabase.CreateAsset(Hip3punGrabAnim, fxAnimatorDirPath + "/Hip3p_UnGrab.anim");
        AssetDatabase.CreateAsset(Hip3pgrabAnim1, fxAnimatorDirPath + "/Hip3p_Grab1.anim");
        AssetDatabase.CreateAsset(Hip3pgrabAnim2, fxAnimatorDirPath + "/Hip3p_Grab2.anim");
        */

        Hip3pLayer.stateMachine = new AnimatorStateMachine();
        Hip3pLayer.stateMachine.hideFlags = HideFlags.HideInHierarchy;
        if (AssetDatabase.GetAssetPath(fxAnimator) != "")
            AssetDatabase.AddObjectToAsset(Hip3pLayer.stateMachine, AssetDatabase.GetAssetPath(fxAnimator));
        EditorUtility.SetDirty(Hip3pLayer.stateMachine);


        var Hip3pUnGrabState = Hip3pLayer.stateMachine.AddState("UnGrab", new Vector3(100, 150, 1));
        var Hip3pDefaultState = Hip3pLayer.stateMachine.AddState("Default", new Vector3(400, 150, 1));
        var Hip3pGrabState1 = Hip3pLayer.stateMachine.AddState("Grab1", new Vector3(400, 0, 1));
        var Hip3pGrabState2 = Hip3pLayer.stateMachine.AddState("Grab2", new Vector3(100, 0, 1));

        EditorUtility.SetDirty(Hip3pDefaultState);
        EditorUtility.SetDirty(Hip3pGrabState1);
        EditorUtility.SetDirty(Hip3pGrabState2);
        EditorUtility.SetDirty(Hip3pUnGrabState);

        var Hip3pTrackingoff = Hip3pGrabState2.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
        Hip3pTrackingoff.trackingHip = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Animation;
        Hip3pTrackingoff.trackingHead = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Animation;
        Hip3pTrackingoff.trackingLeftFoot = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Animation;
        Hip3pTrackingoff.trackingRightFoot = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Animation;
        var Hip3pTrackingon = Hip3pUnGrabState.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
        Hip3pTrackingon.trackingHip = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Tracking;
        Hip3pTrackingon.trackingHead = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Tracking;
        Hip3pTrackingon.trackingLeftFoot = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Tracking;
        Hip3pTrackingon.trackingRightFoot = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Tracking;


        Hip3pLayer.stateMachine.defaultState = Hip3pDefaultState;


        Hip3pGrabState1.motion = hipgrabAnim1;
        Hip3pGrabState2.motion = hipgrabAnim2;
        Hip3pUnGrabState.motion = hipunGrabAnim;

        if (footIK)
        {
            Hip3pGrabState2.iKOnFeet = true;
        }

        var Hip3pGrabTransition1 = Hip3pDefaultState.AddTransition(Hip3pGrabState1);
        Hip3pGrabTransition1.hasExitTime = false;
        Hip3pGrabTransition1.duration = 0;
        Hip3pGrabTransition1.exitTime = 0;
        Hip3pGrabTransition1.offset = 0;
        Hip3pGrabTransition1.canTransitionToSelf = false;
        Hip3pGrabTransition1.AddCondition(AnimatorConditionMode.If, 1, "Hip_IsGrabbed");
        Hip3pGrabTransition1.AddCondition(AnimatorConditionMode.NotEqual, 6, "TrackingType");

        var Hip3pGrabTransition1a = Hip3pDefaultState.AddTransition(Hip3pGrabState1);
        Hip3pGrabTransition1a.hasExitTime = false;
        Hip3pGrabTransition1a.duration = 0;
        Hip3pGrabTransition1a.exitTime = 0;
        Hip3pGrabTransition1a.offset = 0;
        Hip3pGrabTransition1a.canTransitionToSelf = false;
        Hip3pGrabTransition1a.AddCondition(AnimatorConditionMode.If, 1, "GFB_PistonOn");
        Hip3pGrabTransition1a.AddCondition(AnimatorConditionMode.NotEqual, 6, "TrackingType");

        var Hip3pGrabTransition1b = Hip3pDefaultState.AddTransition(Hip3pGrabState1);
        Hip3pGrabTransition1b.hasExitTime = false;
        Hip3pGrabTransition1b.duration = 0;
        Hip3pGrabTransition1b.exitTime = 0;
        Hip3pGrabTransition1b.offset = 0;
        Hip3pGrabTransition1b.canTransitionToSelf = false;
        Hip3pGrabTransition1b.AddCondition(AnimatorConditionMode.If, 1, "GFB_IA_Interactive");
        Hip3pGrabTransition1b.AddCondition(AnimatorConditionMode.NotEqual, 6, "TrackingType");

        var Hip3pGrabTransition2 = Hip3pGrabState1.AddTransition(Hip3pGrabState2);
        Hip3pGrabTransition2.hasExitTime = true;
        Hip3pGrabTransition2.duration = 0;
        Hip3pGrabTransition2.exitTime = 0.25f;
        Hip3pGrabTransition2.offset = 0;
        Hip3pGrabTransition2.canTransitionToSelf = false;

        var Hip3pGrabTransition3 = Hip3pGrabState2.AddTransition(Hip3pUnGrabState);
        Hip3pGrabTransition3.hasExitTime = false;
        Hip3pGrabTransition3.duration = 0;
        Hip3pGrabTransition3.exitTime = 0;
        Hip3pGrabTransition3.offset = 0;
        Hip3pGrabTransition3.canTransitionToSelf = false;
        Hip3pGrabTransition3.AddCondition(AnimatorConditionMode.IfNot, 1, "Hip_IsGrabbed");
        Hip3pGrabTransition3.AddCondition(AnimatorConditionMode.IfNot, 1, "GFB_IA_Interactive");
        Hip3pGrabTransition3.AddCondition(AnimatorConditionMode.IfNot, 1, "GFB_PistonOn");

        var Hip3pGrabTransition4 = Hip3pUnGrabState.AddTransition(Hip3pDefaultState);
        Hip3pGrabTransition4.hasExitTime = true;
        Hip3pGrabTransition4.duration = 0;
        Hip3pGrabTransition4.exitTime = 0.25f;
        Hip3pGrabTransition4.offset = 0;
        Hip3pGrabTransition4.canTransitionToSelf = false;

        fxAnimator.AddLayer(Hip3pLayer);


        var headLayer = new AnimatorControllerLayer();
        headLayer.name = "HeadGrab";
        headLayer.defaultWeight = 1f;

        var headunGrabAnim = new AnimationClip();
        var headgrabAnim1 = new AnimationClip();
        var headgrabAnim2 = new AnimationClip();

        AnimationUtility.SetEditorCurve(headgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Head.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Neck.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        if (UpperChest != null)
        {
            AnimationUtility.SetEditorCurve(headgrabAnim1,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, UpperChest.transform), typeof(ParentConstraint),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        }
        AnimationUtility.SetEditorCurve(headgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Chest.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Spine.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftUpperLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightUpperLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));



        AnimationUtility.SetEditorCurve(headgrabAnim2,
    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Head.transform), typeof(ParentConstraint),
        "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Neck.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        if (UpperChest != null)
        {
            AnimationUtility.SetEditorCurve(headgrabAnim2,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, UpperChest.transform), typeof(ParentConstraint),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        }
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Chest.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Spine.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftUpperLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightUpperLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));

        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips2.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Spine2.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Chest2.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
        {
            AnimationUtility.SetEditorCurve(headgrabAnim2,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, UpperChest2.transform), typeof(ParentConstraint),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        }
        AnimationUtility.SetEditorCurve(headgrabAnim2,
                 EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Neck2.transform), typeof(ParentConstraint),
                 "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Head2.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));

        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HeadIK1st.transform), typeof(RootMotion.FinalIK.VRIK),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HeadIK2nd.transform), typeof(RootMotion.FinalIK.VRIK),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HeadIK.transform), typeof(RootMotion.FinalIK.IKExecutionOrder),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));

        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Hips)), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Spine)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Chest)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
        {
            AnimationUtility.SetEditorCurve(headgrabAnim2,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.UpperChest)), typeof(RotationConstraint),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        }
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Neck)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Head)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightShoulder)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightUpperArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightLowerArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightHand)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftShoulder)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftUpperArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftLowerArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftHand)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightUpperLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightLowerLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightFoot)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftFoot)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        if (tracking)
        {
            AnimationUtility.SetEditorCurve(headgrabAnim2,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Camera.transform), typeof(Camera),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        }


        AnimationUtility.SetEditorCurve(headunGrabAnim,
    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Head.transform), typeof(ParentConstraint),
        "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Neck.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        if (UpperChest != null)
        {
            AnimationUtility.SetEditorCurve(headunGrabAnim,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, UpperChest.transform), typeof(ParentConstraint),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        }
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Chest.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Spine.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftUpperLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightUpperLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));

        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips2.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Spine2.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Chest2.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
        {
            AnimationUtility.SetEditorCurve(headunGrabAnim,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, UpperChest2.transform), typeof(ParentConstraint),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        }
        AnimationUtility.SetEditorCurve(headunGrabAnim,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Neck2.transform), typeof(ParentConstraint),
                  "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Head2.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));


        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HeadIK1st.transform), typeof(RootMotion.FinalIK.VRIK),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HeadIK2nd.transform), typeof(RootMotion.FinalIK.VRIK),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HeadIK.transform), typeof(RootMotion.FinalIK.IKExecutionOrder),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));

        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Hips)), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Spine)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Chest)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
        {
            AnimationUtility.SetEditorCurve(headunGrabAnim,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.UpperChest)), typeof(RotationConstraint),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        }
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Neck)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Head)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightShoulder)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightUpperArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightLowerArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightHand)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftShoulder)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftUpperArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftLowerArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftHand)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightUpperLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightLowerLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightFoot)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(headunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftFoot)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        if (tracking)
        {
            AnimationUtility.SetEditorCurve(headunGrabAnim,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Camera.transform), typeof(Camera),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        }

        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/head_UnGrab.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/head_Grab1.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/head_Grab2.anim");
        AssetDatabase.CreateAsset(headunGrabAnim, fxAnimatorDirPath + "/head_UnGrab.anim");
        AssetDatabase.CreateAsset(headgrabAnim1, fxAnimatorDirPath + "/head_Grab1.anim");
        AssetDatabase.CreateAsset(headgrabAnim2, fxAnimatorDirPath + "/head_Grab2.anim");

        headLayer.stateMachine = new AnimatorStateMachine();
        headLayer.stateMachine.hideFlags = HideFlags.HideInHierarchy;
        if (AssetDatabase.GetAssetPath(fxAnimator) != "")
            AssetDatabase.AddObjectToAsset(headLayer.stateMachine, AssetDatabase.GetAssetPath(fxAnimator));
        EditorUtility.SetDirty(headLayer.stateMachine);

        var headUnGrabState = headLayer.stateMachine.AddState("UnGrab", new Vector3(100, 150, 1));
        var headDefaultState = headLayer.stateMachine.AddState("Default", new Vector3(400, 150, 1));
        var headGrabState1 = headLayer.stateMachine.AddState("Grab1", new Vector3(400, 0, 1));
        var headGrabState2 = headLayer.stateMachine.AddState("Grab2", new Vector3(100, 0, 1));

        EditorUtility.SetDirty(headDefaultState);
        EditorUtility.SetDirty(headGrabState1);
        EditorUtility.SetDirty(headGrabState2);
        EditorUtility.SetDirty(headUnGrabState);

        var headTrackingoff = headGrabState2.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
        headTrackingoff.trackingHead = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Animation;
        headTrackingoff.trackingHip = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Animation;
        var headTrackingon = headUnGrabState.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
        headTrackingon.trackingHead = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Tracking;
        headTrackingon.trackingHip = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Tracking;


        headLayer.stateMachine.defaultState = headDefaultState;


        headGrabState1.motion = headgrabAnim1;
        headGrabState2.motion = headgrabAnim2;
        headUnGrabState.motion = headunGrabAnim;

        var headGrabTransition1 = headDefaultState.AddTransition(headGrabState1);
        headGrabTransition1.hasExitTime = false;
        headGrabTransition1.duration = 0;
        headGrabTransition1.exitTime = 0;
        headGrabTransition1.offset = 0;
        headGrabTransition1.canTransitionToSelf = false;
        headGrabTransition1.AddCondition(AnimatorConditionMode.If, 1, "Head_IsGrabbed");
        headGrabTransition1.AddCondition(AnimatorConditionMode.Equals, 6, "TrackingType");

        var headGrabTransition2 = headGrabState1.AddTransition(headGrabState2);
        headGrabTransition2.hasExitTime = true;
        headGrabTransition2.duration = 0;
        headGrabTransition2.exitTime = 0.25f;
        headGrabTransition2.offset = 0;
        headGrabTransition2.canTransitionToSelf = false;

        var headGrabTransition3 = headGrabState2.AddTransition(headUnGrabState);
        headGrabTransition3.hasExitTime = false;
        headGrabTransition3.duration = 0;
        headGrabTransition3.exitTime = 0;
        headGrabTransition3.offset = 0;
        headGrabTransition3.canTransitionToSelf = false;
        headGrabTransition3.AddCondition(AnimatorConditionMode.IfNot, 1, "Head_IsGrabbed");

        var headGrabTransition4 = headUnGrabState.AddTransition(headDefaultState);
        headGrabTransition4.hasExitTime = true;
        headGrabTransition4.duration = 0;
        headGrabTransition4.exitTime = 0.25f;
        headGrabTransition4.offset = 0;
        headGrabTransition4.canTransitionToSelf = false;

        fxAnimator.AddLayer(headLayer);


        var Head3pLayer = new AnimatorControllerLayer();
        Head3pLayer.name = "Head3pGrab";
        Head3pLayer.defaultWeight = 1f;

        /*

        var Head3punGrabAnim = new AnimationClip();
        var Head3pgrabAnim1 = new AnimationClip();
        var Head3pgrabAnim2 = new AnimationClip();

        AnimationUtility.SetEditorCurve(Head3pgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Head.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(Head3pgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Neck.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        if (UpperChest != null)
        {
            AnimationUtility.SetEditorCurve(Head3pgrabAnim1,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, UpperChest.transform), typeof(ParentConstraint),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        }
        AnimationUtility.SetEditorCurve(Head3pgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Chest.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(Head3pgrabAnim1,
    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Spine.transform), typeof(ParentConstraint),
        "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(Head3pgrabAnim1,
    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips.transform), typeof(ParentConstraint),
        "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(Head3pgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftUpperLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(Head3pgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightUpperLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));

        AnimationUtility.SetEditorCurve(Head3pgrabAnim2,
    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Head.transform), typeof(ParentConstraint),
        "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(Head3pgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Neck.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        if (UpperChest != null)
        {
            AnimationUtility.SetEditorCurve(Head3pgrabAnim2,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, UpperChest.transform), typeof(ParentConstraint),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        }
        AnimationUtility.SetEditorCurve(Head3pgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Chest.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(Head3pgrabAnim2,
    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Spine.transform), typeof(ParentConstraint),
        "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(Head3pgrabAnim2,
    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips.transform), typeof(ParentConstraint),
        "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(Head3pgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftUpperLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(Head3pgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightUpperLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));

        AnimationUtility.SetEditorCurve(Head3pgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HeadIK.transform), typeof(RootMotion.FinalIK.FullBodyBipedIK),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(Head3pgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HeadIK.transform), typeof(RootMotion.FinalIK.IKExecutionOrder),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));



        AnimationUtility.SetEditorCurve(Head3punGrabAnim,
    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Head.transform), typeof(ParentConstraint),
        "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(Head3punGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Neck.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        if (UpperChest != null)
        {
            AnimationUtility.SetEditorCurve(Head3punGrabAnim,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, UpperChest.transform), typeof(ParentConstraint),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        }
        AnimationUtility.SetEditorCurve(Head3punGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Chest.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(Head3punGrabAnim,
    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Spine.transform), typeof(ParentConstraint),
        "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(Head3punGrabAnim,
    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips.transform), typeof(ParentConstraint),
        "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(Head3punGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftUpperLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(Head3punGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightUpperLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));


        AnimationUtility.SetEditorCurve(Head3punGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HeadIK.transform), typeof(RootMotion.FinalIK.FullBodyBipedIK),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(Head3punGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, HeadIK.transform), typeof(RootMotion.FinalIK.IKExecutionOrder),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));



        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/Head3p_UnGrab.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/Head3p_Grab1.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/Head3p_Grab2.anim");
        AssetDatabase.CreateAsset(Head3punGrabAnim, fxAnimatorDirPath + "/Head3p_UnGrab.anim");
        AssetDatabase.CreateAsset(Head3pgrabAnim1, fxAnimatorDirPath + "/Head3p_Grab1.anim");
        AssetDatabase.CreateAsset(Head3pgrabAnim2, fxAnimatorDirPath + "/Head3p_Grab2.anim");

        */

        Head3pLayer.stateMachine = new AnimatorStateMachine();
        Head3pLayer.stateMachine.hideFlags = HideFlags.HideInHierarchy;
        if (AssetDatabase.GetAssetPath(fxAnimator) != "")
            AssetDatabase.AddObjectToAsset(Head3pLayer.stateMachine, AssetDatabase.GetAssetPath(fxAnimator));
        EditorUtility.SetDirty(Head3pLayer.stateMachine);



        var Head3pUnGrabState = Head3pLayer.stateMachine.AddState("UnGrab", new Vector3(100, 150, 1));
        var Head3pDefaultState = Head3pLayer.stateMachine.AddState("Default", new Vector3(400, 150, 1));
        var Head3pGrabState1 = Head3pLayer.stateMachine.AddState("Grab1", new Vector3(400, 0, 1));
        var Head3pGrabState2 = Head3pLayer.stateMachine.AddState("Grab2", new Vector3(100, 0, 1));

        EditorUtility.SetDirty(Head3pDefaultState);
        EditorUtility.SetDirty(Head3pGrabState1);
        EditorUtility.SetDirty(Head3pGrabState2);
        EditorUtility.SetDirty(Head3pUnGrabState);

        var Head3pTrackingoff = Head3pGrabState2.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
        Head3pTrackingoff.trackingHead = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Animation;
        var Head3pTrackingon = Head3pUnGrabState.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
        Head3pTrackingon.trackingHead = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Tracking;

        Head3pLayer.stateMachine.defaultState = Head3pDefaultState;


        Head3pGrabState1.motion = headgrabAnim1;
        Head3pGrabState2.motion = headgrabAnim2;
        Head3pUnGrabState.motion = headunGrabAnim;

        var Head3pGrabTransition1 = Head3pDefaultState.AddTransition(Head3pGrabState1);
        Head3pGrabTransition1.hasExitTime = false;
        Head3pGrabTransition1.duration = 0;
        Head3pGrabTransition1.exitTime = 0;
        Head3pGrabTransition1.offset = 0;
        Head3pGrabTransition1.canTransitionToSelf = false;
        Head3pGrabTransition1.AddCondition(AnimatorConditionMode.If, 1, "Head_IsGrabbed");
        Head3pGrabTransition1.AddCondition(AnimatorConditionMode.NotEqual, 6, "TrackingType");

        var Head3pGrabTransition2 = Head3pGrabState1.AddTransition(Head3pGrabState2);
        Head3pGrabTransition2.hasExitTime = true;
        Head3pGrabTransition2.duration = 0;
        Head3pGrabTransition2.exitTime = 0.25f;
        Head3pGrabTransition2.offset = 0;
        Head3pGrabTransition2.canTransitionToSelf = false;

        var Head3pGrabTransition3 = Head3pGrabState2.AddTransition(Head3pUnGrabState);
        Head3pGrabTransition3.hasExitTime = false;
        Head3pGrabTransition3.duration = 0;
        Head3pGrabTransition3.exitTime = 0;
        Head3pGrabTransition3.offset = 0;
        Head3pGrabTransition3.canTransitionToSelf = false;
        Head3pGrabTransition3.AddCondition(AnimatorConditionMode.IfNot, 1, "Head_IsGrabbed");

        var Head3pGrabTransition4 = Head3pUnGrabState.AddTransition(Head3pDefaultState);
        Head3pGrabTransition4.hasExitTime = true;
        Head3pGrabTransition4.duration = 0;
        Head3pGrabTransition4.exitTime = 0.25f;
        Head3pGrabTransition4.offset = 0;
        Head3pGrabTransition4.canTransitionToSelf = false;

        fxAnimator.AddLayer(Head3pLayer);


        var RHandLayer = new AnimatorControllerLayer();
        RHandLayer.name = "RHandGrab";
        RHandLayer.defaultWeight = 1f;

        var RHandunGrabAnim = new AnimationClip();
        var RHandgrabAnim1 = new AnimationClip();
        var RHandgrabAnim2 = new AnimationClip();
        var RHandgrabAnim3 = new AnimationClip();

        AnimationUtility.SetEditorCurve(RHandgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightHand.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RHandgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightLowerArm.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RHandgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightUpperArm.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RHandgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightShoulder.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));

        AnimationUtility.SetEditorCurve(RHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightHand.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightLowerArm.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightUpperArm.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightShoulder.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));

        AnimationUtility.SetEditorCurve(RHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Hips)), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Spine)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Chest)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
        {
            AnimationUtility.SetEditorCurve(RHandgrabAnim2,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.UpperChest)), typeof(RotationConstraint),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        }
        AnimationUtility.SetEditorCurve(RHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Neck)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Head)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightShoulder)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightUpperArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightLowerArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightHand)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftUpperArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftLowerArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftHand)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightUpperLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightLowerLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightFoot)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftFoot)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));

       AnimationUtility.SetEditorCurve(RHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RHandIK.transform), typeof(RootMotion.FinalIK.IKExecutionOrder),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RHandIK.transform), typeof(RootMotion.FinalIK.VRIK),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
       

        AnimationUtility.SetEditorCurve(RHandgrabAnim3,
    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightHand.transform), typeof(ParentConstraint),
        "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RHandgrabAnim3,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightLowerArm.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RHandgrabAnim3,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightUpperArm.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RHandgrabAnim3,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightShoulder.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));

        
        AnimationUtility.SetEditorCurve(RHandgrabAnim3,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RHandIK.transform), typeof(RootMotion.FinalIK.IKExecutionOrder),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RHandgrabAnim3,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RHandIK.transform), typeof(RootMotion.FinalIK.VRIK),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
       

        AnimationUtility.SetEditorCurve(RHandunGrabAnim,
    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightHand.transform), typeof(ParentConstraint),
        "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightLowerArm.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightUpperArm.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightShoulder.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));

        
        AnimationUtility.SetEditorCurve(RHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RHandIK.transform), typeof(RootMotion.FinalIK.IKExecutionOrder),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RHandIK.transform), typeof(RootMotion.FinalIK.VRIK),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        
        AnimationUtility.SetEditorCurve(RHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Hips)), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Spine)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Chest)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
        {
            AnimationUtility.SetEditorCurve(RHandunGrabAnim,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.UpperChest)), typeof(RotationConstraint),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        }
        AnimationUtility.SetEditorCurve(RHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Neck)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Head)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightShoulder)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightUpperArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightLowerArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightHand)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftUpperArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftLowerArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftHand)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightUpperLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightLowerLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightFoot)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftFoot)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));

        /*
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(RHandgrabAnim2);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(RHandgrabAnim2, settings);
        */

        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/RHand_UnGrab.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/RHand_Grab1.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/RHand_Grab2.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/RHand_Grab3.anim");
        AssetDatabase.CreateAsset(RHandunGrabAnim, fxAnimatorDirPath + "/RHand_UnGrab.anim");
        AssetDatabase.CreateAsset(RHandgrabAnim1, fxAnimatorDirPath + "/RHand_Grab1.anim");
        AssetDatabase.CreateAsset(RHandgrabAnim2, fxAnimatorDirPath + "/RHand_Grab2.anim");
        AssetDatabase.CreateAsset(RHandgrabAnim3, fxAnimatorDirPath + "/RHand_Grab3.anim");

        RHandLayer.stateMachine = new AnimatorStateMachine();
        RHandLayer.stateMachine.hideFlags = HideFlags.HideInHierarchy;
        if (AssetDatabase.GetAssetPath(fxAnimator) != "")
            AssetDatabase.AddObjectToAsset(RHandLayer.stateMachine, AssetDatabase.GetAssetPath(fxAnimator));
        EditorUtility.SetDirty(RHandLayer.stateMachine);

        var RHandUnGrabState = RHandLayer.stateMachine.AddState("UnGrab", new Vector3(100, 150, 1));
        var RHandDefaultState = RHandLayer.stateMachine.AddState("Default", new Vector3(400, 150, 1));
        var RHandGrabState1 = RHandLayer.stateMachine.AddState("Grab1", new Vector3(400, 0, 1));
        var RHandGrabState2 = RHandLayer.stateMachine.AddState("Grab2", new Vector3(100, 0, 1));
        var RHandGrabState4 = RHandLayer.stateMachine.AddState("Posing", new Vector3(300, 75, 1));

        EditorUtility.SetDirty(RHandDefaultState);
        EditorUtility.SetDirty(RHandGrabState1);
        EditorUtility.SetDirty(RHandGrabState2);
        EditorUtility.SetDirty(RHandGrabState4);
        EditorUtility.SetDirty(RHandUnGrabState);

        var RHandTrackingoff = RHandGrabState2.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
        RHandTrackingoff.trackingRightHand = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Animation;
        var RHandTrackingon = RHandUnGrabState.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
        RHandTrackingon.trackingRightHand = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Tracking;

        RHandLayer.stateMachine.defaultState = RHandDefaultState;

        if (footIK)
        {
            RHandGrabState2.iKOnFeet = true;
        }

        RHandGrabState1.motion = RHandgrabAnim1;
        RHandGrabState2.motion = RHandgrabAnim2;
        RHandGrabState4.motion = RHandgrabAnim3;
        RHandUnGrabState.motion = RHandunGrabAnim;

        var RHandGrabTransition1 = RHandDefaultState.AddTransition(RHandGrabState1);
        RHandGrabTransition1.hasExitTime = false;
        RHandGrabTransition1.duration = 0;
        RHandGrabTransition1.exitTime = 0;
        RHandGrabTransition1.offset = 0;
        RHandGrabTransition1.canTransitionToSelf = false;
        RHandGrabTransition1.AddCondition(AnimatorConditionMode.If, 1, "RHand_IsGrabbed");

        var RHandGrabTransition2 = RHandGrabState1.AddTransition(RHandGrabState2);
        RHandGrabTransition2.hasExitTime = true;
        RHandGrabTransition2.duration = 0;
        RHandGrabTransition2.exitTime = 0.25f;
        RHandGrabTransition2.offset = 0;
        RHandGrabTransition2.canTransitionToSelf = false;

        var RHandGrabTransition3 = RHandGrabState2.AddTransition(RHandGrabState4);
        RHandGrabTransition3.hasExitTime = false;
        RHandGrabTransition3.duration = 0;
        RHandGrabTransition3.exitTime = 0;
        RHandGrabTransition3.offset = 0;
        RHandGrabTransition3.canTransitionToSelf = false;
        RHandGrabTransition3.AddCondition(AnimatorConditionMode.If, 1, "RHand_IsPosed");

        var RHandGrabTransition5 = RHandGrabState2.AddTransition(RHandUnGrabState);
        RHandGrabTransition5.hasExitTime = false;
        RHandGrabTransition5.duration = 0;
        RHandGrabTransition5.exitTime = 0;
        RHandGrabTransition5.offset = 0;
        RHandGrabTransition5.canTransitionToSelf = false;
        RHandGrabTransition5.AddCondition(AnimatorConditionMode.IfNot, 1, "RHand_IsGrabbed");

        var RHandGrabTransition6 = RHandUnGrabState.AddTransition(RHandDefaultState);
        RHandGrabTransition6.hasExitTime = true;
        RHandGrabTransition6.duration = 0;
        RHandGrabTransition6.exitTime = 0.25f;
        RHandGrabTransition6.offset = 0;
        RHandGrabTransition6.canTransitionToSelf = false;

        var RHandGrabTransition7 = RHandGrabState4.AddTransition(RHandGrabState2);
        RHandGrabTransition7.hasExitTime = false;
        RHandGrabTransition7.duration = 0;
        RHandGrabTransition7.exitTime = 0;
        RHandGrabTransition7.offset = 0;
        RHandGrabTransition7.canTransitionToSelf = false;
        RHandGrabTransition7.AddCondition(AnimatorConditionMode.IfNot, 1, "RHand_IsPosed");

        fxAnimator.AddLayer(RHandLayer);


        var LHandLayer = new AnimatorControllerLayer();
        LHandLayer.name = "LHandGrab";
        LHandLayer.defaultWeight = 1f;

        var LHandunGrabAnim = new AnimationClip();
        var LHandgrabAnim1 = new AnimationClip();
        var LHandgrabAnim2 = new AnimationClip();
        var LHandgrabAnim3 = new AnimationClip();

        AnimationUtility.SetEditorCurve(LHandgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftHand.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LHandgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftLowerArm.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LHandgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftUpperArm.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LHandgrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftShoulder.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));

        AnimationUtility.SetEditorCurve(LHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftHand.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftLowerArm.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftUpperArm.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftShoulder.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));

        
        AnimationUtility.SetEditorCurve(LHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LHandIK.transform), typeof(RootMotion.FinalIK.IKExecutionOrder),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LHandIK.transform), typeof(RootMotion.FinalIK.VRIK),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        
        AnimationUtility.SetEditorCurve(LHandgrabAnim2,
    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Hips)), typeof(ParentConstraint),
        "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Spine)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Chest)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
        {
            AnimationUtility.SetEditorCurve(LHandgrabAnim2,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.UpperChest)), typeof(RotationConstraint),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        }
        AnimationUtility.SetEditorCurve(LHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Neck)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Head)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightShoulder)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightUpperArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightLowerArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightHand)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftUpperArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftLowerArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftHand)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightUpperLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightLowerLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightFoot)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LHandgrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftFoot)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));

        AnimationUtility.SetEditorCurve(LHandgrabAnim3,
    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftHand.transform), typeof(ParentConstraint),
        "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LHandgrabAnim3,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftLowerArm.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LHandgrabAnim3,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftUpperArm.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LHandgrabAnim3,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftShoulder.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));

        AnimationUtility.SetEditorCurve(LHandgrabAnim3,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LHandIK.transform), typeof(RootMotion.FinalIK.IKExecutionOrder),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LHandgrabAnim3,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LHandIK.transform), typeof(RootMotion.FinalIK.VRIK),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        
        AnimationUtility.SetEditorCurve(LHandunGrabAnim,
    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftHand.transform), typeof(ParentConstraint),
        "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftLowerArm.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftUpperArm.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftShoulder.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));

        AnimationUtility.SetEditorCurve(LHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LHandIK.transform), typeof(RootMotion.FinalIK.IKExecutionOrder),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LHandIK.transform), typeof(RootMotion.FinalIK.VRIK),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        
        AnimationUtility.SetEditorCurve(LHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Hips)), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Spine)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Chest)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
        {
            AnimationUtility.SetEditorCurve(LHandunGrabAnim,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.UpperChest)), typeof(RotationConstraint),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        }
        AnimationUtility.SetEditorCurve(LHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Neck)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Head)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightShoulder)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightUpperArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightLowerArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightHand)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftUpperArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftLowerArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftHand)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightUpperLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightLowerLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightFoot)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LHandunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftFoot)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));

        /*
        AnimationClipSettings settingsL = AnimationUtility.GetAnimationClipSettings(LHandgrabAnim2);
        settingsL.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(LHandgrabAnim2, settingsL);
        */

        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/LHand_UnGrab.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/LHand_Grab1.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/LHand_Grab2.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/LHand_Grab3.anim");
        AssetDatabase.CreateAsset(LHandunGrabAnim, fxAnimatorDirPath + "/LHand_UnGrab.anim");
        AssetDatabase.CreateAsset(LHandgrabAnim1, fxAnimatorDirPath + "/LHand_Grab1.anim");
        AssetDatabase.CreateAsset(LHandgrabAnim2, fxAnimatorDirPath + "/LHand_Grab2.anim");
        AssetDatabase.CreateAsset(LHandgrabAnim3, fxAnimatorDirPath + "/LHand_Grab3.anim");

        LHandLayer.stateMachine = new AnimatorStateMachine();
        LHandLayer.stateMachine.hideFlags = HideFlags.HideInHierarchy;
        if (AssetDatabase.GetAssetPath(fxAnimator) != "")
            AssetDatabase.AddObjectToAsset(LHandLayer.stateMachine, AssetDatabase.GetAssetPath(fxAnimator));
        EditorUtility.SetDirty(LHandLayer.stateMachine);


        var LHandUnGrabState = LHandLayer.stateMachine.AddState("UnGrab", new Vector3(100, 150, 1));
        var LHandDefaultState = LHandLayer.stateMachine.AddState("Default", new Vector3(400, 150, 1));
        var LHandGrabState1 = LHandLayer.stateMachine.AddState("Grab1", new Vector3(400, 0, 1));
        var LHandGrabState2 = LHandLayer.stateMachine.AddState("Grab2", new Vector3(100, 0, 1));

        var LHandGrabState4 = LHandLayer.stateMachine.AddState("Posing", new Vector3(300, 75, 1));

        EditorUtility.SetDirty(LHandDefaultState);
        EditorUtility.SetDirty(LHandGrabState1);
        EditorUtility.SetDirty(LHandGrabState2);

        EditorUtility.SetDirty(LHandGrabState4);
        EditorUtility.SetDirty(LHandUnGrabState);

        var LHandTrackingoff = LHandGrabState2.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
        LHandTrackingoff.trackingLeftHand = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Animation;
        var LHandTrackingon = LHandUnGrabState.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
        LHandTrackingon.trackingLeftHand = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Tracking;

        LHandLayer.stateMachine.defaultState = LHandDefaultState;

        if (footIK)
        {
            LHandGrabState2.iKOnFeet = true;
        }

        LHandGrabState1.motion = LHandgrabAnim1;
        LHandGrabState2.motion = LHandgrabAnim2;

        LHandGrabState4.motion = LHandgrabAnim3;
        LHandUnGrabState.motion = LHandunGrabAnim;

        var LHandGrabTransition1 = LHandDefaultState.AddTransition(LHandGrabState1);
        LHandGrabTransition1.hasExitTime = false;
        LHandGrabTransition1.duration = 0;
        LHandGrabTransition1.exitTime = 0;
        LHandGrabTransition1.offset = 0;
        LHandGrabTransition1.canTransitionToSelf = false;
        LHandGrabTransition1.AddCondition(AnimatorConditionMode.If, 1, "LHand_IsGrabbed");

        var LHandGrabTransition2 = LHandGrabState1.AddTransition(LHandGrabState2);
        LHandGrabTransition2.hasExitTime = true;
        LHandGrabTransition2.duration = 0;
        LHandGrabTransition2.exitTime = 0.25f;
        LHandGrabTransition2.offset = 0;
        LHandGrabTransition2.canTransitionToSelf = false;

        var LHandGrabTransition3 = LHandGrabState2.AddTransition(LHandGrabState4);
        LHandGrabTransition3.hasExitTime = false;
        LHandGrabTransition3.duration = 0;
        LHandGrabTransition3.exitTime = 0;
        LHandGrabTransition3.offset = 0;
        LHandGrabTransition3.canTransitionToSelf = false;
        LHandGrabTransition3.AddCondition(AnimatorConditionMode.If, 1, "LHand_IsPosed");


        var LHandGrabTransition5 = LHandGrabState2.AddTransition(LHandUnGrabState);
        LHandGrabTransition5.hasExitTime = false;
        LHandGrabTransition5.duration = 0;
        LHandGrabTransition5.exitTime = 0;
        LHandGrabTransition5.offset = 0;
        LHandGrabTransition5.canTransitionToSelf = false;
        LHandGrabTransition5.AddCondition(AnimatorConditionMode.IfNot, 1, "LHand_IsGrabbed");

        var LHandGrabTransition6 = LHandUnGrabState.AddTransition(LHandDefaultState);
        LHandGrabTransition6.hasExitTime = true;
        LHandGrabTransition6.duration = 0;
        LHandGrabTransition6.exitTime = 0.25f;
        LHandGrabTransition6.offset = 0;
        LHandGrabTransition6.canTransitionToSelf = false;

        var LHandGrabTransition7 = LHandGrabState4.AddTransition(LHandGrabState2);
        LHandGrabTransition7.hasExitTime = false;
        LHandGrabTransition7.duration = 0;
        LHandGrabTransition7.exitTime = 0;
        LHandGrabTransition7.offset = 0;
        LHandGrabTransition7.canTransitionToSelf = false;
        LHandGrabTransition7.AddCondition(AnimatorConditionMode.IfNot, 1, "LHand_IsPosed");

        fxAnimator.AddLayer(LHandLayer);


        var RLegLayer = new AnimatorControllerLayer();
        RLegLayer.name = "RLegGrab";
        RLegLayer.defaultWeight = 1f;

        var RLegunGrabAnim = new AnimationClip();
        var RLeggrabAnim1 = new AnimationClip();
        var RLeggrabAnim2 = new AnimationClip();
        var RLeggrabAnim3 = new AnimationClip();
        if (RightToes != null)
        {
            AnimationUtility.SetEditorCurve(RLeggrabAnim1,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightToes.transform), typeof(ParentConstraint),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        }
        AnimationUtility.SetEditorCurve(RLeggrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightFoot.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RLeggrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightLowerLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RLeggrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightUpperLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        //AnimationUtility.SetEditorCurve(RLeggrabAnim1,
        //    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips.transform), typeof(ParentConstraint),
        //        "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        //AnimationUtility.SetEditorCurve(RLeggrabAnim1,
        //    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Spine.transform), typeof(ParentConstraint),
        //        "m_Enabled"), AnimationCurve.Constant(0, 0, 0));

        if (RightToes != null)
        {
            AnimationUtility.SetEditorCurve(RLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightToes.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        }
        AnimationUtility.SetEditorCurve(RLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightFoot.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightLowerLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightUpperLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        //AnimationUtility.SetEditorCurve(RLeggrabAnim2,
        //    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips.transform), typeof(ParentConstraint),
        //        "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        //AnimationUtility.SetEditorCurve(RLeggrabAnim2,
        //    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Spine.transform), typeof(ParentConstraint),
        //        "m_Enabled"), AnimationCurve.Constant(0, 0, 0));

        AnimationUtility.SetEditorCurve(RLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RLegIK.transform), typeof(RootMotion.FinalIK.IKExecutionOrder),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RLegIK.transform), typeof(RootMotion.FinalIK.VRIK),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
       
        AnimationUtility.SetEditorCurve(RLeggrabAnim2,
    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Hips)), typeof(ParentConstraint),
        "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Spine)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Chest)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
        {
            AnimationUtility.SetEditorCurve(RLeggrabAnim2,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.UpperChest)), typeof(RotationConstraint),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        }
        AnimationUtility.SetEditorCurve(RLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Neck)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Head)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightShoulder)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightUpperArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightLowerArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightHand)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftUpperArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftLowerArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftHand)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightUpperLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightLowerLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightFoot)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftFoot)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));


        if (RightToes != null)
        {
            AnimationUtility.SetEditorCurve(RLeggrabAnim3,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightToes.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        }
        AnimationUtility.SetEditorCurve(RLeggrabAnim3,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightFoot.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RLeggrabAnim3,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightLowerLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RLeggrabAnim3,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightUpperLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        //AnimationUtility.SetEditorCurve(RLeggrabAnim3,
        //    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips.transform), typeof(ParentConstraint),
        //        "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        //AnimationUtility.SetEditorCurve(RLeggrabAnim3,
        //    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Spine.transform), typeof(ParentConstraint),
        //        "m_Enabled"), AnimationCurve.Constant(0, 0, 0));

        AnimationUtility.SetEditorCurve(RLeggrabAnim3,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RLegIK.transform), typeof(RootMotion.FinalIK.IKExecutionOrder),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RLeggrabAnim3,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RLegIK.transform), typeof(RootMotion.FinalIK.VRIK),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        
        if (RightToes != null)
        {
            AnimationUtility.SetEditorCurve(RLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightToes.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        }
        AnimationUtility.SetEditorCurve(RLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightFoot.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightLowerLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(RLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RightUpperLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        //AnimationUtility.SetEditorCurve(RLegunGrabAnim,
        //    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips.transform), typeof(ParentConstraint),
        //        "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        //AnimationUtility.SetEditorCurve(RLegunGrabAnim,
        //    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Spine.transform), typeof(ParentConstraint),
        //        "m_Enabled"), AnimationCurve.Constant(0, 0, 1));

        AnimationUtility.SetEditorCurve(RLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RLegIK.transform), typeof(RootMotion.FinalIK.IKExecutionOrder),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, RLegIK.transform), typeof(RootMotion.FinalIK.VRIK),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
       
        AnimationUtility.SetEditorCurve(RLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Hips)), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Spine)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Chest)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
        {
            AnimationUtility.SetEditorCurve(RLegunGrabAnim,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.UpperChest)), typeof(RotationConstraint),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        }
        AnimationUtility.SetEditorCurve(RLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Neck)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Head)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightShoulder)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightUpperArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightLowerArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightHand)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftUpperArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftLowerArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftHand)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightUpperLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightLowerLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightFoot)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(RLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftFoot)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));


        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/RLeg_UnGrab.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/RLeg_Grab1.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/RLeg_Grab2.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/RLeg_Grab3.anim");
        AssetDatabase.CreateAsset(RLegunGrabAnim, fxAnimatorDirPath + "/RLeg_UnGrab.anim");
        AssetDatabase.CreateAsset(RLeggrabAnim1, fxAnimatorDirPath + "/RLeg_Grab1.anim");
        AssetDatabase.CreateAsset(RLeggrabAnim2, fxAnimatorDirPath + "/RLeg_Grab2.anim");
        AssetDatabase.CreateAsset(RLeggrabAnim3, fxAnimatorDirPath + "/RLeg_Grab3.anim");

        RLegLayer.stateMachine = new AnimatorStateMachine();
        RLegLayer.stateMachine.hideFlags = HideFlags.HideInHierarchy;
        if (AssetDatabase.GetAssetPath(fxAnimator) != "")
            AssetDatabase.AddObjectToAsset(RLegLayer.stateMachine, AssetDatabase.GetAssetPath(fxAnimator));
        EditorUtility.SetDirty(RLegLayer.stateMachine);


        var RLegUnGrabState = RLegLayer.stateMachine.AddState("UnGrab", new Vector3(100, 150, 1));
        var RLegDefaultState = RLegLayer.stateMachine.AddState("Default", new Vector3(400, 150, 1));
        var RLegGrabState1 = RLegLayer.stateMachine.AddState("Grab1", new Vector3(400, 0, 1));
        var RLegGrabState2 = RLegLayer.stateMachine.AddState("Grab2", new Vector3(100, 0, 1));
        
        var RLegGrabState4 = RLegLayer.stateMachine.AddState("Posing", new Vector3(300, 75, 1));

        EditorUtility.SetDirty(RLegDefaultState);
        EditorUtility.SetDirty(RLegGrabState1);
        EditorUtility.SetDirty(RLegGrabState2);
        
        EditorUtility.SetDirty(RLegGrabState4);
        EditorUtility.SetDirty(RLegUnGrabState);

        var RLegTrackingoff = RLegGrabState2.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
        RLegTrackingoff.trackingRightFoot = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Animation;
        var RLegTrackingon = RLegUnGrabState.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
        RLegTrackingon.trackingRightFoot = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Tracking;

        RLegLayer.stateMachine.defaultState = RLegDefaultState;

        if (footIK)
        {
            RLegGrabState2.iKOnFeet = true;
        }

        RLegGrabState1.motion = RLeggrabAnim1;
        RLegGrabState2.motion = RLeggrabAnim2;
        
        RLegGrabState4.motion = RLeggrabAnim3;
        RLegUnGrabState.motion = RLegunGrabAnim;

        var RLegGrabTransition1 = RLegDefaultState.AddTransition(RLegGrabState1);
        RLegGrabTransition1.hasExitTime = false;
        RLegGrabTransition1.duration = 0;
        RLegGrabTransition1.exitTime = 0;
        RLegGrabTransition1.offset = 0;
        RLegGrabTransition1.canTransitionToSelf = false;
        RLegGrabTransition1.AddCondition(AnimatorConditionMode.If, 1, "RLeg_IsGrabbed");

        var RLegGrabTransition2 = RLegGrabState1.AddTransition(RLegGrabState2);
        RLegGrabTransition2.hasExitTime = true;
        RLegGrabTransition2.duration = 0;
        RLegGrabTransition2.exitTime = 0.25f;
        RLegGrabTransition2.offset = 0;
        RLegGrabTransition2.canTransitionToSelf = false;

        var RLegGrabTransition3 = RLegGrabState2.AddTransition(RLegGrabState4);
        RLegGrabTransition3.hasExitTime = false;
        RLegGrabTransition3.duration = 0;
        RLegGrabTransition3.exitTime = 0;
        RLegGrabTransition3.offset = 0;
        RLegGrabTransition3.canTransitionToSelf = false;
        RLegGrabTransition3.AddCondition(AnimatorConditionMode.If, 1, "RLeg_IsPosed");


        var RLegGrabTransition5 = RLegGrabState2.AddTransition(RLegUnGrabState);
        RLegGrabTransition5.hasExitTime = false;
        RLegGrabTransition5.duration = 0;
        RLegGrabTransition5.exitTime = 0;
        RLegGrabTransition5.offset = 0;
        RLegGrabTransition5.canTransitionToSelf = false;
        RLegGrabTransition5.AddCondition(AnimatorConditionMode.IfNot, 1, "RLeg_IsGrabbed");

        var RLegGrabTransition6 = RLegUnGrabState.AddTransition(RLegDefaultState);
        RLegGrabTransition6.hasExitTime = true;
        RLegGrabTransition6.duration = 0;
        RLegGrabTransition6.exitTime = 0.25f;
        RLegGrabTransition6.offset = 0;
        RLegGrabTransition6.canTransitionToSelf = false;

        var RLegGrabTransition7 = RLegGrabState4.AddTransition(RLegGrabState2);
        RLegGrabTransition7.hasExitTime = false;
        RLegGrabTransition7.duration = 0;
        RLegGrabTransition7.exitTime = 0;
        RLegGrabTransition7.offset = 0;
        RLegGrabTransition7.canTransitionToSelf = false;
        RLegGrabTransition7.AddCondition(AnimatorConditionMode.IfNot, 1, "RLeg_IsPosed");


        fxAnimator.AddLayer(RLegLayer);

        var LLegLayer = new AnimatorControllerLayer();
        LLegLayer.name = "LLegGrab";
        LLegLayer.defaultWeight = 1f;

        var LLegunGrabAnim = new AnimationClip();
        var LLeggrabAnim1 = new AnimationClip();
        var LLeggrabAnim2 = new AnimationClip();
        var LLeggrabAnim3 = new AnimationClip();

        if (LeftToes != null)
        {
            AnimationUtility.SetEditorCurve(LLeggrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftToes.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        }
        AnimationUtility.SetEditorCurve(LLeggrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftFoot.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LLeggrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftLowerLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LLeggrabAnim1,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftUpperLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        //AnimationUtility.SetEditorCurve(LLeggrabAnim1,
        //    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips.transform), typeof(ParentConstraint),
        //        "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        //AnimationUtility.SetEditorCurve(LLeggrabAnim1,
        //    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Spine.transform), typeof(ParentConstraint),
        //        "m_Enabled"), AnimationCurve.Constant(0, 0, 0));

        if (LeftToes != null)
        {
            AnimationUtility.SetEditorCurve(LLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftToes.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        }
        AnimationUtility.SetEditorCurve(LLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftFoot.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftLowerLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftUpperLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        //AnimationUtility.SetEditorCurve(LLeggrabAnim2,
        //    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips.transform), typeof(ParentConstraint),
        //        "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        //AnimationUtility.SetEditorCurve(LLeggrabAnim2,
        //    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Spine.transform), typeof(ParentConstraint),
        //        "m_Enabled"), AnimationCurve.Constant(0, 0, 0));

        AnimationUtility.SetEditorCurve(LLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LLegIK.transform), typeof(RootMotion.FinalIK.IKExecutionOrder),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LLegIK.transform), typeof(RootMotion.FinalIK.VRIK),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        

        AnimationUtility.SetEditorCurve(LLeggrabAnim2,
EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Hips)), typeof(ParentConstraint),
"m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Spine)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Chest)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
        {
            AnimationUtility.SetEditorCurve(LLeggrabAnim2,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.UpperChest)), typeof(RotationConstraint),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        }
        AnimationUtility.SetEditorCurve(LLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Neck)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Head)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightShoulder)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightUpperArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightLowerArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightHand)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftUpperArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftLowerArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftHand)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightUpperLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightLowerLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightFoot)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LLeggrabAnim2,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftFoot)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));

        if (LeftToes != null)
        {
            AnimationUtility.SetEditorCurve(LLeggrabAnim3,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftToes.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        }
        AnimationUtility.SetEditorCurve(LLeggrabAnim3,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftFoot.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LLeggrabAnim3,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftLowerLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LLeggrabAnim3,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftUpperLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        //AnimationUtility.SetEditorCurve(LLeggrabAnim3,
        //    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips.transform), typeof(ParentConstraint),
        //        "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        //AnimationUtility.SetEditorCurve(LLeggrabAnim3,
        //    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Spine.transform), typeof(ParentConstraint),
        //        "m_Enabled"), AnimationCurve.Constant(0, 0, 0));

        AnimationUtility.SetEditorCurve(LLeggrabAnim3,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LLegIK.transform), typeof(RootMotion.FinalIK.IKExecutionOrder),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LLeggrabAnim3,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LLegIK.transform), typeof(RootMotion.FinalIK.VRIK),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        
        if (LeftToes != null)
        {
            AnimationUtility.SetEditorCurve(LLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftToes.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        }
        AnimationUtility.SetEditorCurve(LLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftFoot.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftLowerLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        AnimationUtility.SetEditorCurve(LLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LeftUpperLeg.transform), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        //AnimationUtility.SetEditorCurve(LLegunGrabAnim,
        //    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Hips.transform), typeof(ParentConstraint),
        //        "m_Enabled"), AnimationCurve.Constant(0, 0, 1));
        //AnimationUtility.SetEditorCurve(LLegunGrabAnim,
        //    EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, Spine.transform), typeof(ParentConstraint),
        //        "m_Enabled"), AnimationCurve.Constant(0, 0, 1));

        AnimationUtility.SetEditorCurve(LLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LLegIK.transform), typeof(RootMotion.FinalIK.IKExecutionOrder),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, LLegIK.transform), typeof(RootMotion.FinalIK.VRIK),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        
        AnimationUtility.SetEditorCurve(LLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Hips)), typeof(ParentConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Spine)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Chest)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        if (animator.GetBoneTransform(HumanBodyBones.UpperChest) != null)
        {
            AnimationUtility.SetEditorCurve(LLegunGrabAnim,
                EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.UpperChest)), typeof(RotationConstraint),
                    "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        }
        AnimationUtility.SetEditorCurve(LLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Neck)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.Head)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightShoulder)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightUpperArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightLowerArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightHand)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftUpperArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftLowerArm)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftHand)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightUpperLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightLowerLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.RightFoot)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));
        AnimationUtility.SetEditorCurve(LLegunGrabAnim,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, animator.GetBoneTransform(HumanBodyBones.LeftFoot)), typeof(RotationConstraint),
                "m_Enabled"), AnimationCurve.Constant(0, 0, 0));



        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/LLeg_UnGrab.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/LLeg_Grab1.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/LLeg_Grab2.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/LLeg_Grab3.anim");
        AssetDatabase.CreateAsset(LLegunGrabAnim, fxAnimatorDirPath + "/LLeg_UnGrab.anim");
        AssetDatabase.CreateAsset(LLeggrabAnim1, fxAnimatorDirPath + "/LLeg_Grab1.anim");
        AssetDatabase.CreateAsset(LLeggrabAnim2, fxAnimatorDirPath + "/LLeg_Grab2.anim");
        AssetDatabase.CreateAsset(LLeggrabAnim3, fxAnimatorDirPath + "/LLeg_Grab3.anim");

        LLegLayer.stateMachine = new AnimatorStateMachine();
        LLegLayer.stateMachine.hideFlags = HideFlags.HideInHierarchy;
        if (AssetDatabase.GetAssetPath(fxAnimator) != "")
            AssetDatabase.AddObjectToAsset(LLegLayer.stateMachine, AssetDatabase.GetAssetPath(fxAnimator));
        EditorUtility.SetDirty(LLegLayer.stateMachine);

        var LLegUnGrabState = LLegLayer.stateMachine.AddState("UnGrab", new Vector3(100, 150, 1));
        var LLegDefaultState = LLegLayer.stateMachine.AddState("Default", new Vector3(400, 150, 1));
        var LLegGrabState1 = LLegLayer.stateMachine.AddState("Grab1", new Vector3(400, 0, 1));
        var LLegGrabState2 = LLegLayer.stateMachine.AddState("Grab2", new Vector3(100, 0, 1));
        
        var LLegGrabState4 = LLegLayer.stateMachine.AddState("Posing", new Vector3(300, 75, 1));

        EditorUtility.SetDirty(LLegDefaultState);
        EditorUtility.SetDirty(LLegGrabState1);
        EditorUtility.SetDirty(LLegGrabState2);
        
        EditorUtility.SetDirty(LLegGrabState4);
        EditorUtility.SetDirty(LLegUnGrabState);

        var LLegTrackingoff = LLegGrabState2.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
        LLegTrackingoff.trackingLeftFoot = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Animation;
        var LLegTrackingon = LLegUnGrabState.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
        LLegTrackingon.trackingLeftFoot = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Tracking;

        LLegLayer.stateMachine.defaultState = LLegDefaultState;

        if (footIK)
        {
            LLegGrabState2.iKOnFeet = true;
        }

        LLegGrabState1.motion = LLeggrabAnim1;
        LLegGrabState2.motion = LLeggrabAnim2;
       
        LLegGrabState4.motion = LLeggrabAnim3;
        LLegUnGrabState.motion = LLegunGrabAnim;

        var LLegGrabTransition1 = LLegDefaultState.AddTransition(LLegGrabState1);
        LLegGrabTransition1.hasExitTime = false;
        LLegGrabTransition1.duration = 0;
        LLegGrabTransition1.exitTime = 0;
        LLegGrabTransition1.offset = 0;
        LLegGrabTransition1.canTransitionToSelf = false;
        LLegGrabTransition1.AddCondition(AnimatorConditionMode.If, 1, "LLeg_IsGrabbed");

        var LLegGrabTransition2 = LLegGrabState1.AddTransition(LLegGrabState2);
        LLegGrabTransition2.hasExitTime = true;
        LLegGrabTransition2.duration = 0;
        LLegGrabTransition2.exitTime = 0.25f;
        LLegGrabTransition2.offset = 0;
        LLegGrabTransition2.canTransitionToSelf = false;

        var LLegGrabTransition3 = LLegGrabState2.AddTransition(LLegGrabState4);
        LLegGrabTransition3.hasExitTime = false;
        LLegGrabTransition3.duration = 0;
        LLegGrabTransition3.exitTime = 0;
        LLegGrabTransition3.offset = 0;
        LLegGrabTransition3.canTransitionToSelf = false;
        LLegGrabTransition3.AddCondition(AnimatorConditionMode.If, 1, "LLeg_IsPosed");


        var LLegGrabTransition5 = LLegGrabState2.AddTransition(LLegUnGrabState);
        LLegGrabTransition5.hasExitTime = false;
        LLegGrabTransition5.duration = 0;
        LLegGrabTransition5.exitTime = 0;
        LLegGrabTransition5.offset = 0;
        LLegGrabTransition5.canTransitionToSelf = false;
        LLegGrabTransition5.AddCondition(AnimatorConditionMode.IfNot, 1, "LLeg_IsGrabbed");

        var LLegGrabTransition6 = LLegUnGrabState.AddTransition(LLegDefaultState);
        LLegGrabTransition6.hasExitTime = true;
        LLegGrabTransition6.duration = 0;
        LLegGrabTransition6.exitTime = 0.25f;
        LLegGrabTransition6.offset = 0;
        LLegGrabTransition6.canTransitionToSelf = false;

        var LLegGrabTransition7 = LLegGrabState4.AddTransition(LLegGrabState2);
        LLegGrabTransition7.hasExitTime = false;
        LLegGrabTransition7.duration = 0;
        LLegGrabTransition7.exitTime = 0;
        LLegGrabTransition7.offset = 0;
        LLegGrabTransition7.canTransitionToSelf = false;
        LLegGrabTransition7.AddCondition(AnimatorConditionMode.IfNot, 1, "LLeg_IsPosed");

        fxAnimator.AddLayer(LLegLayer);


        /*

        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "CamControlX") == null)
            fxAnimator.AddParameter("CamControlX", AnimatorControllerParameterType.Float);
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "CamControlY") == null)
            fxAnimator.AddParameter("CamControlY", AnimatorControllerParameterType.Float);
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "CamControlZ") == null)
            fxAnimator.AddParameter("CamControlZ", AnimatorControllerParameterType.Float);
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "CamControlRot") == null)
            fxAnimator.AddParameter("CamControlRot", AnimatorControllerParameterType.Float);
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "CamControlOn") == null)
            fxAnimator.AddParameter("CamControlOn", AnimatorControllerParameterType.Float);


        var CamControlLayer = new AnimatorControllerLayer();
        CamControlLayer.name = "GFB_CamControll";
        CamControlLayer.defaultWeight = 1f;

        var CamXp = new AnimationClip();
        var CamXm = new AnimationClip();
        var CamYp = new AnimationClip();
        var CamYm = new AnimationClip();
        var CamZp = new AnimationClip();
        var CamZm = new AnimationClip();
        var CamRotp = new AnimationClip();
        var CamRotm = new AnimationClip();


        AnimationUtility.SetEditorCurve(CamXp,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, CameraRig.transform), typeof(Transform),
                "m_LocalPosition.x"), AnimationCurve.Constant(0, 0, CameraRig.transform.localPosition.x + 1));
        AnimationUtility.SetEditorCurve(CamXm,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, CameraRig.transform), typeof(Transform),
                "m_LocalPosition.x"), AnimationCurve.Constant(0, 0, CameraRig.transform.localPosition.x - 1));
        AnimationUtility.SetEditorCurve(CamYp,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, CameraRig.transform), typeof(Transform),
                "m_LocalPosition.y"), AnimationCurve.Constant(0, 0, CameraRig.transform.localPosition.y + 1));
        AnimationUtility.SetEditorCurve(CamYm,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, CameraRig.transform), typeof(Transform),
                "m_LocalPosition.y"), AnimationCurve.Constant(0, 0, CameraRig.transform.localPosition.y - 1));
        AnimationUtility.SetEditorCurve(CamZp,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, CameraRig.transform), typeof(Transform),
                "m_LocalPosition.z"), AnimationCurve.Constant(0, 0, CameraRig.transform.localPosition.z+ 1));
        AnimationUtility.SetEditorCurve(CamZm,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, CameraRig.transform), typeof(Transform),
                "m_LocalPosition.z"), AnimationCurve.Constant(0, 0, CameraRig.transform.localPosition.z - 1));
        
        AnimationUtility.SetEditorCurve(CamRotp,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, CameraRig.transform), typeof(Transform),
                "localEulerAnglesRaw.y"), AnimationCurve.Constant(0, 0, CameraRig.transform.localRotation.eulerAngles.y + 45));
        AnimationUtility.SetEditorCurve(CamRotm,
            EditorCurveBinding.FloatCurve(GetPath(avatarDescriptor.gameObject.transform, CameraRig.transform), typeof(Transform),
                "localEulerAnglesRaw.y"), AnimationCurve.Constant(0, 0, CameraRig.transform.localRotation.eulerAngles.y - 45));



        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/CamControlXp.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/CamControlXm.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/CamControlYp.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/CamControlYm.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/CamControlZp.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/CamControlZm.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/CamControlRotp.anim");
        AssetDatabase.DeleteAsset(fxAnimatorDirPath + "/CamControlRotm.anim");
        AssetDatabase.CreateAsset(CamXp, fxAnimatorDirPath + "/CamControlXp.anim");
        AssetDatabase.CreateAsset(CamXm, fxAnimatorDirPath + "/CamControlXm.anim");
        AssetDatabase.CreateAsset(CamYp, fxAnimatorDirPath + "/CamControlYp.anim");
        AssetDatabase.CreateAsset(CamYm, fxAnimatorDirPath + "/CamControlYm.anim");
        AssetDatabase.CreateAsset(CamZp, fxAnimatorDirPath + "/CamControlZp.anim");
        AssetDatabase.CreateAsset(CamZm, fxAnimatorDirPath + "/CamControlZm.anim");
        AssetDatabase.CreateAsset(CamRotp, fxAnimatorDirPath + "/CamControlRotp.anim");
        AssetDatabase.CreateAsset(CamRotm, fxAnimatorDirPath + "/CamControlRotm.anim");

        CamControlLayer.stateMachine = new AnimatorStateMachine();
        CamControlLayer.stateMachine.hideFlags = HideFlags.HideInHierarchy;
        if (AssetDatabase.GetAssetPath(fxAnimator) != "")
            AssetDatabase.AddObjectToAsset(CamControlLayer.stateMachine, AssetDatabase.GetAssetPath(fxAnimator));
        EditorUtility.SetDirty(CamControlLayer.stateMachine);

        var CamControlState = CamControlLayer.stateMachine.AddState("CamControl", new Vector3(100, 150, 1));

        EditorUtility.SetDirty(CamControlState);

        var Blendtree = new BlendTree() { name = "BlendRoot", blendType = BlendTreeType.Direct };
        var Children = new ChildMotion[4];

        var BlendX = new BlendTree() {name = "BlendX", blendType = BlendTreeType.Simple1D, blendParameter = "CamControlX" , useAutomaticThresholds = false, minThreshold = 0 , maxThreshold = 1};
        BlendX.AddChild(CamXm, 0);
        BlendX.AddChild(CamXp, 1);
        var BlendY = new BlendTree() { name = "BlendY", blendType = BlendTreeType.Simple1D, blendParameter = "CamControlY", useAutomaticThresholds = false, minThreshold = 0, maxThreshold = 1 };
        BlendY.AddChild(CamYm, 0);
        BlendY.AddChild(CamYp, 1);
        var BlendZ = new BlendTree() { name = "BlendZ", blendType = BlendTreeType.Simple1D, blendParameter = "CamControlZ", useAutomaticThresholds = false, minThreshold = 0, maxThreshold = 1 };
        BlendZ.AddChild(CamZm, 0);
        BlendZ.AddChild(CamZp, 1);

        var BlendRot = new BlendTree() { name = "BlendRot", blendType = BlendTreeType.Simple1D, blendParameter = "CamControlRot", useAutomaticThresholds = false, minThreshold = 0, maxThreshold = 1 };
        BlendRot.AddChild(CamRotm, 0);
        BlendRot.AddChild(CamRotp, 1);

        Children[0].directBlendParameter = "CamControlOn";
        Children[1].directBlendParameter = "CamControlOn";
        Children[2].directBlendParameter = "CamControlOn";
        Children[3].directBlendParameter = "CamControlOn";
        Children[0].motion = BlendX;
        Children[1].motion = BlendY;
        Children[2].motion = BlendZ;
        Children[3].motion = BlendRot;

        Blendtree.children = Children;

        CamControlState.motion = Blendtree;

        EditorUtility.SetDirty(Blendtree);
        EditorUtility.SetDirty(BlendX);
        EditorUtility.SetDirty(BlendY);
        EditorUtility.SetDirty(BlendZ);
        EditorUtility.SetDirty(BlendRot);

        var driver = CamControlState.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
        var para = new VRCAvatarParameterDriver.Parameter();
        para.type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set;
        para.value = 1;
        para.name = "CamControlOn";
        driver.parameters.Add(para);

        if (tracking)
        {
            fxAnimator.AddLayer(CamControlLayer);
        }

        var newparamCamControlX = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.Parameter();
        newparamCamControlX.name = "CamControlX";
        newparamCamControlX.defaultValue = 0.5f;
        newparamCamControlX.saved = true;
        newparamCamControlX.valueType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Float;

        var newparamCamControlY = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.Parameter();
        newparamCamControlY.name = "CamControlY";
        newparamCamControlY.defaultValue = 0.5f;
        newparamCamControlY.saved = true;
        newparamCamControlY.valueType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Float;

        var newparamCamControlZ = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.Parameter();
        newparamCamControlZ.name = "CamControlZ";
        newparamCamControlZ.defaultValue = 0.5f;
        newparamCamControlZ.saved = true;
        newparamCamControlZ.valueType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Float;

        var newparamCamControlRot = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.Parameter();
        newparamCamControlRot.name = "CamControlRot";
        newparamCamControlRot.defaultValue = 0.5f;
        newparamCamControlRot.saved = true;
        newparamCamControlRot.valueType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Float;

        EditorUtility.SetDirty(avatarDescriptor.expressionParameters);

        if (tracking)
        {
            avatarDescriptor.expressionParameters.parameters = avatarDescriptor.expressionParameters.parameters.Append(newparamCamControlX).ToArray();
            avatarDescriptor.expressionParameters.parameters = avatarDescriptor.expressionParameters.parameters.Append(newparamCamControlY).ToArray();
            avatarDescriptor.expressionParameters.parameters = avatarDescriptor.expressionParameters.parameters.Append(newparamCamControlZ).ToArray();
            avatarDescriptor.expressionParameters.parameters = avatarDescriptor.expressionParameters.parameters.Append(newparamCamControlRot).ToArray();
        }

        if (avatarDescriptor.expressionsMenu.controls.Count != 8 && tracking)
        {
            EditorUtility.SetDirty(avatarDescriptor.expressionsMenu);

            VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu newSubmenu = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.CreateInstance<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>();
            var newCtrlX = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control();
            newCtrlX.name = "X Axis";
            newCtrlX.type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.RadialPuppet;
            newCtrlX.subParameters = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter[] { new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter() { name = "CamControlX" } };
            newCtrlX.parameter = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter() { name = null };


            var newCtrlY = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control();
            newCtrlY.name = "Y Axis";
            newCtrlY.type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.RadialPuppet;
            newCtrlY.subParameters = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter[] { new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter() { name = "CamControlY" } };
            newCtrlY.parameter = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter() { name = null };

            var newCtrlZ = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control();
            newCtrlZ.name = "Z Axis";
            newCtrlZ.type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.RadialPuppet;
            newCtrlZ.subParameters = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter[] { new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter() { name = "CamControlZ" } };
            newCtrlZ.parameter = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter() { name = null };

            var newCtrlRot = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control();
            newCtrlRot.name = "Y Rotation";
            newCtrlRot.type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.RadialPuppet;
            newCtrlRot.subParameters = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter[] { new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter() { name = "CamControlRot" } };
            newCtrlRot.parameter = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter() { name = null };


            newSubmenu.controls.Add(newCtrlX);
            newSubmenu.controls.Add(newCtrlY);
            newSubmenu.controls.Add(newCtrlZ);
            newSubmenu.controls.Add(newCtrlRot);

            AssetDatabase.CreateAsset(newSubmenu, "Assets/nHaruka/GFB_CamCtrl.asset");


            var newMenu = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control();
            newMenu.name = "Grab System CamControl";
            newMenu.type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu;
            newMenu.subMenu = newSubmenu;

            avatarDescriptor.expressionsMenu.controls.Add(newMenu);

            
        }
        else
        if(tracking)
        {
            Debug.LogError("There are no available slot in Expression Menu");
        }
        */
        AssetDatabase.SaveAssets();

    }


    void remove()
    {
        animator = avatarDescriptor.GetComponent<Animator>();

        if (avatarDescriptor.transform.Find("GrabSystemRoot") != null)
        {
            DestroyImmediate(avatarDescriptor.transform.Find("GrabSystemRoot").gameObject);
        }
        foreach(var obj in
            animator.GetBoneTransform(HumanBodyBones.Hips).gameObject.GetComponentsInChildren<RotationConstraint>())
        {
            DestroyImmediate(obj);
        }
        DestroyImmediate(animator.GetBoneTransform(HumanBodyBones.Hips).gameObject.GetComponentInChildren<ParentConstraint>());

        var fxAnimatorLayer =
            avatarDescriptor.baseAnimationLayers.First(item => item.type == VRCAvatarDescriptor.AnimLayerType.FX && item.animatorController != null);
        var fxAnimator = (AnimatorController)fxAnimatorLayer.animatorController;

        EditorUtility.SetDirty(fxAnimator);

        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "Hip_IsGrabbed") != null)
            fxAnimator.RemoveParameter(fxAnimator.parameters.FirstOrDefault(param => param.name == "Hip_IsGrabbed"));
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "Head_IsGrabbed") != null)
            fxAnimator.RemoveParameter(fxAnimator.parameters.FirstOrDefault(param => param.name == "Head_IsGrabbed"));
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "LHand_IsGrabbed") != null)
            fxAnimator.RemoveParameter(fxAnimator.parameters.FirstOrDefault(param => param.name == "LHand_IsGrabbed"));
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "RHand_IsGrabbed") != null)
            fxAnimator.RemoveParameter(fxAnimator.parameters.FirstOrDefault(param => param.name == "RHand_IsGrabbed"));
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "LLeg_IsGrabbed") != null)
            fxAnimator.RemoveParameter(fxAnimator.parameters.FirstOrDefault(param => param.name == "LLeg_IsGrabbed"));
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "RLeg_IsGrabbed") != null)
            fxAnimator.RemoveParameter(fxAnimator.parameters.FirstOrDefault(param => param.name == "RLeg_IsGrabbed"));
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "RHandPosing") != null)
            fxAnimator.RemoveParameter(fxAnimator.parameters.FirstOrDefault(param => param.name == "RHandPosing"));
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "LHandPosing") != null)
            fxAnimator.RemoveParameter(fxAnimator.parameters.FirstOrDefault(param => param.name == "LHandPosing"));
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "RLegPosing") != null)
            fxAnimator.RemoveParameter(fxAnimator.parameters.FirstOrDefault(param => param.name == "RLegPosing"));
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "LLegPosing") != null)
            fxAnimator.RemoveParameter(fxAnimator.parameters.FirstOrDefault(param => param.name == "LLegPosing"));
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "CamControlX") != null)
            fxAnimator.RemoveParameter(fxAnimator.parameters.FirstOrDefault(param => param.name == "CamControlX"));
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "CamControlY") != null)
            fxAnimator.RemoveParameter(fxAnimator.parameters.FirstOrDefault(param => param.name == "CamControlY"));
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "CamControlZ") != null)
            fxAnimator.RemoveParameter(fxAnimator.parameters.FirstOrDefault(param => param.name == "CamControlZ"));
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "CamControlZ") != null)
            fxAnimator.RemoveParameter(fxAnimator.parameters.FirstOrDefault(param => param.name == "CamControlRot"));
        if (fxAnimator.parameters.FirstOrDefault(param => param.name == "CamControlOn") != null)
            fxAnimator.RemoveParameter(fxAnimator.parameters.FirstOrDefault(param => param.name == "CamControlOn"));



        var grabLayerIndex = Array.FindIndex(fxAnimator.layers, layer => layer.name == "HipGrab");
        if (grabLayerIndex >= 0) fxAnimator.RemoveLayer(grabLayerIndex);
        grabLayerIndex = Array.FindIndex(fxAnimator.layers, layer => layer.name == "Hip3pGrab");
        if (grabLayerIndex >= 0) fxAnimator.RemoveLayer(grabLayerIndex);
        grabLayerIndex = Array.FindIndex(fxAnimator.layers, layer => layer.name == "HeadGrab");
        if (grabLayerIndex >= 0) fxAnimator.RemoveLayer(grabLayerIndex);
        grabLayerIndex = Array.FindIndex(fxAnimator.layers, layer => layer.name == "Head3pGrab");
        if (grabLayerIndex >= 0) fxAnimator.RemoveLayer(grabLayerIndex);
        grabLayerIndex = Array.FindIndex(fxAnimator.layers, layer => layer.name == "LHandGrab");
        if (grabLayerIndex >= 0) fxAnimator.RemoveLayer(grabLayerIndex);
        grabLayerIndex = Array.FindIndex(fxAnimator.layers, layer => layer.name == "RHandGrab");
        if (grabLayerIndex >= 0) fxAnimator.RemoveLayer(grabLayerIndex);
        grabLayerIndex = Array.FindIndex(fxAnimator.layers, layer => layer.name == "LLegGrab");
        if (grabLayerIndex >= 0) fxAnimator.RemoveLayer(grabLayerIndex);
        grabLayerIndex = Array.FindIndex(fxAnimator.layers, layer => layer.name == "RLegGrab");
        if (grabLayerIndex >= 0) fxAnimator.RemoveLayer(grabLayerIndex);
        grabLayerIndex = Array.FindIndex(fxAnimator.layers, layer => layer.name == "GFB_CamControll");
        if (grabLayerIndex >= 0) fxAnimator.RemoveLayer(grabLayerIndex);

        EditorUtility.SetDirty(avatarDescriptor.expressionParameters);
        EditorUtility.SetDirty(avatarDescriptor.expressionsMenu);

        avatarDescriptor.expressionsMenu.controls = avatarDescriptor.expressionsMenu.controls.Where(item => item.name != "Grab System CamControl").ToList();
        avatarDescriptor.expressionParameters.parameters = avatarDescriptor.expressionParameters.parameters.Where(item => !item.name.Contains("CamControlX") && !item.name.Contains("CamControlY") && !item.name.Contains("CamControlZ") && !item.name.Contains("CamControlRot")).ToArray();

        AssetDatabase.SaveAssets();
    }


    private static string GetPath(Transform root, Transform self)
    {

        string path = self.gameObject.name;
        Transform parent = self.parent;

        while (root != parent)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }
 }
