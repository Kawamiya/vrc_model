// Ragdoll System by ksivl @ VRLabs 3.0 Assets https://discord.gg/THCRsJc https://vrlabs.dev
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using static UnityEngine.HumanBodyBones;
using Vector3 = UnityEngine.Vector3;

namespace VRLabs.RagdollSystem
{
    [CustomEditor(typeof(Ragdoll))]
    class RagdollEditor : Editor
    {
        private VRCAvatarDescriptor descriptor;
        private Animator animator;
        private readonly List<string> warnings = new List<string>();
        private RagdollBone[] ragdollBones;
        SerializedProperty wdSetting, includeAudio, bodyMeshes, collisionDetect, seat, followsHipHeight;

        private bool isWdAutoSet;
        private readonly ScriptFunctions.PlayableLayer[] playablesUsed = { ScriptFunctions.PlayableLayer.FX };

        public int bitCount;

        public const string assetPath = "Assets/VRLabs/Ragdoll System/Resources";
        public const string assetGuid = "52320fbcacd6dd34990774bf3b039418";

        public string currentLocation;

        // Limits for CharacterJoints, depending on the type of bone it is.
        struct JointLimit // low twist limit, high twist limit, spring 1 limit
        {
            public readonly static int[] Hips = new int[3] { 0, 0, 0 };
            public readonly static int[] Leg = new int[3] { -20, 80, 30 };
            public readonly static int[] Knee = new int[3] { -80, 0, 0 };
            public readonly static int[] Spine = new int[3] { -20, 40, 20 };
            public readonly static int[] Arm = new int[3] { -90, 30, 70 };
            public readonly static int[] Elbow = new int[3] { -110, 0, 0 };
            public readonly static int[] Head = new int[3] { -60, 45, 45 };
        }

        // Stores the bones we will generate colliders on and some of its properties.
        class RagdollBone
        {
            public string Name;
            public string NameInWizard;
            public HumanBodyBones HumanBodyBone;
            public int[] Limits;

            public Transform old = null; // the corresponding bone in the real avatar
            public Transform clone = null; // the actual ragdoll's bone; we move it under "Rigidbodies"
            public GameObject proxy = null; // replaces the above object we moved

            public RagdollBone(string name, string nameInWizard, HumanBodyBones humanBodyBone, int[] limits)
            {
                Name = name;
                NameInWizard = nameInWizard;
                HumanBodyBone = humanBodyBone;
                Limits = limits;
            }
        }

        public void OnEnable()
        {
            // Set up the SerializedProperties
            wdSetting = serializedObject.FindProperty("wdSetting");
            bodyMeshes = serializedObject.FindProperty("bodyMeshes");
            includeAudio = serializedObject.FindProperty("includeAudio");
            collisionDetect = serializedObject.FindProperty("collisionDetect");
            seat = serializedObject.FindProperty("seat");
            followsHipHeight = serializedObject.FindProperty("followsHipHeight");

            // Initialize the RagdollBones
            ragdollBones = new RagdollBone[11];
            ragdollBones[0] = new RagdollBone("Hips", "pelvis", Hips, JointLimit.Hips);
            ragdollBones[1] = new RagdollBone("Left leg", "leftHips", LeftUpperLeg, JointLimit.Leg);
            ragdollBones[2] = new RagdollBone("Left knee", "leftKnee", LeftLowerLeg, JointLimit.Knee);
            ragdollBones[3] = new RagdollBone("Right leg", "rightHips", RightUpperLeg, JointLimit.Leg);
            ragdollBones[4] = new RagdollBone("Right knee", "rightKnee", RightLowerLeg, JointLimit.Knee);
            ragdollBones[5] = new RagdollBone("Spine", "middleSpine", Spine, JointLimit.Spine);
            ragdollBones[6] = new RagdollBone("Left arm", "leftArm", LeftUpperArm, JointLimit.Arm);
            ragdollBones[7] = new RagdollBone("Left elbow", "leftElbow", LeftLowerArm, JointLimit.Elbow);
            ragdollBones[8] = new RagdollBone("Head", "head", Head, JointLimit.Head);
            ragdollBones[9] = new RagdollBone("Right arm", "rightArm", RightUpperArm, JointLimit.Arm);
            ragdollBones[10] = new RagdollBone("Right elbow", "rightElbow", RightLowerArm, JointLimit.Elbow);
        }

        public void Reset()
        {
            // Automatically select a descriptor if one is attached
            if (((Ragdoll)target).gameObject.GetComponent<VRCAvatarDescriptor>() != null && descriptor == null && ((Ragdoll)target).bodyMeshes[0] == null)
            {
                descriptor = ((Ragdoll)target).gameObject.GetComponent<VRCAvatarDescriptor>();

                SkinnedMeshRenderer[] skinnedMeshes = ((Ragdoll)target).gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (skinnedMeshes.Length != 0)
                {
                    ((Ragdoll)target).bodyMeshes = new GameObject[skinnedMeshes.Length];

                    for (int i = 0; i < skinnedMeshes.Length; i++)
                    {
                        ((Ragdoll)target).bodyMeshes[i] = skinnedMeshes[i].gameObject;
                    }
                }
            }
            SetPreviousInstallSettings();
        }

        public override void OnInspectorGUI()
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
            GUIStyle boxStyle = new GUIStyle("box") { stretchWidth = true };
            boxStyle.normal.textColor = new GUIStyle("label").normal.textColor;
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true};
            GUIStyle labelStyle = new GUIStyle(EditorStyles.wordWrappedLabel) { stretchWidth = true };

            GUILayout.Space(8);
            EditorGUILayout.BeginHorizontal(boxStyle);
            EditorGUILayout.LabelField("<b><size=14>Ragdoll System</size></b> <size=10>by ksivl @ VRLabs</size>", titleStyle, GUILayout.MinHeight(20f));
            EditorGUILayout.EndHorizontal();

            if (EditorApplication.isPlaying)
            {
                GUILayout.Space(8);
                EditorGUILayout.LabelField("Please exit Play Mode to use this script.");
                return;
            }
            if (((Ragdoll)target).finished == false)
            {
                GUILayout.Space(8);
                EditorGUILayout.LabelField("Make sure the blue axis points in the same direction your avatar is facing.", labelStyle);

                GUILayout.Space(8);
                descriptor = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", ((Ragdoll)target).gameObject.GetComponent<VRCAvatarDescriptor>(), typeof(VRCAvatarDescriptor), true);

                if (descriptor != null)
                {
                    animator = descriptor.GetComponent<Animator>();
                }

                GUILayout.Space(8);
                EditorGUILayout.PropertyField(bodyMeshes, new GUIContent("Body Meshes"), true);

                GUILayout.Space(8);
                EditorGUILayout.PropertyField(includeAudio, new GUIContent("Sound on ragdoll/statue", "Play random meme sounds when you ragdoll or statue."));

                EditorGUILayout.PropertyField(collisionDetect, new GUIContent("Sound on collision", "Play sound when your ragdoll or statue collides with the environment."));

                EditorGUILayout.PropertyField(seat, new GUIContent("Include toggleable seat", "Include a toggleable seat on your ragdoll. Just for fun."));

                EditorGUILayout.PropertyField(followsHipHeight, new GUIContent("Mimic follows hip height", "If enabled, the 'Mimic' option (where a clone mimics you) will have the clone's hips follow the Y-axis movement of your real hips."));

                GUILayout.Space(8);

                if (isWdAutoSet)
                {
                    GUI.enabled = false;
                    EditorGUILayout.PropertyField(wdSetting, new GUIContent("Write Defaults (auto-detected)", "Check this if you are animating your avatar with Write Defaults on. Otherwise, leave unchecked."));
                    GUI.enabled = true;
                }
                else
                {
                    EditorGUILayout.PropertyField(wdSetting, new GUIContent("Write Defaults", "Could not auto-detect.\nCheck this if you are animating your avatar with Write Defaults on. Otherwise, leave unchecked."));
                }

                serializedObject.ApplyModifiedProperties();

                GUILayout.Space(8);

                GetBitCount();
                EditorGUILayout.LabelField("Parameter memory bits needed: " + bitCount);

                CheckRequirements();
                GUILayout.Space(8);
                
                // WD warnings
                if (descriptor != null)
                {
                    if (descriptor.transform.lossyScale != Vector3.one)
                    {
                        GUILayout.Box("It is detected that your avatar scale is not (1,1,1). This may affect the physics simulation. Consider setting your avatar size through the model import settings.", boxStyle);
                    }

                    var states = descriptor.AnalyzeWDState();
                    bool isMixed = states.HaveMixedWriteDefaults(out bool isOn);
                    
                    if (isMixed)
                    {
                        GUILayout.Box("Your avatar has mixed Write Defaults settings on its playable layers' states, which can cause issues with animations. The VRChat standard is Write Defaults OFF. It is recommended that Write Defaults for all states should either be all ON or all OFF.", boxStyle);
                    }
                    else
                    {
                        ((Ragdoll)target).wdSetting = isOn;
                        isWdAutoSet = true;
                    }

                    bool hasEmptyAnimations = states.HaveEmpyMotionsInStates();

                    if (hasEmptyAnimations)
                    {
                        GUILayout.Box("Some states have no motions, this can be an issue when using WD Off.", boxStyle);
                    }

                }

                if (warnings.Count == 0)
                {
                    if (GUILayout.Button("Generate Ragdoll System", buttonStyle))
                    {
                        Debug.Log("Generating Ragdoll System...");
                        try
                        {
                            Generate();
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                            EditorUtility.DisplayDialog("Error Generating Ragdoll System", "Sorry, an error occured generating the Ragdoll System. Please take a snapshot of this code monkey information and send it to ksivl#4278 so it can be resolved.\n=================================================\n" + e.Message + "\n" + e.Source + "\n" + e.StackTrace, "OK");
                        };
                    }
                }
                else
                {
                    for (int i = 0; i < warnings.Count; i++)
                    {
                        GUILayout.Box(warnings[i], boxStyle);
                    }
                    GUI.enabled = false;
                    GUILayout.Button("Generate Ragdoll System", buttonStyle);
                    GUI.enabled = true;
                }

                if (descriptor != null && ScriptFunctions.HasPreviousInstall(descriptor, "Ragdoll System", playablesUsed, "Ragdoll", "Ragdoll System", false))
                {
                    if (GUILayout.Button("Remove Ragdoll System", buttonStyle))
                    {
                        if (EditorUtility.DisplayDialog("Remove Ragdoll System", "Uninstall the VRLabs Ragdoll System from the avatar?", "Yes", "No"))
                            Uninstall();
                    }
                }
                else
                {
                    GUI.enabled = false;
                    GUILayout.Button("Remove Ragdoll System", buttonStyle);
                    GUI.enabled = true;
                }
            }
            else // finished
            {
                GUILayout.Space(8);
                EditorGUILayout.LabelField("If needed, adjust the colliders using the 'Edit Collider' button.");

                GUILayout.Space(8);
                List<List<int>> order = new List<List<int>>()
                { new List<int> { 8 }, new List<int> { 10, 9, 5, 6, 7 }, new List<int> { 0 }, new List<int> { 3, 1 }, new List<int> { 4, 2} };

                foreach (List<int> subOrder in order)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    foreach (int bone in subOrder)
                    {
                        if (GUILayout.Button(ragdollBones[bone].Name, GUILayout.ExpandWidth(false), GUILayout.Height(30f), GUILayout.MaxWidth(80f)))
                        {;
                            if (((Ragdoll)target).colliders[bone] == null)
                            {
                                Debug.LogError("Can't find the collider! It may have been moved or deleted.");
                            }
                            else
                            {
                                Selection.activeGameObject = ((Ragdoll)target).colliders[bone];
                            }
                        }
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                
                GUILayout.Space(8);
                GUILayout.BeginVertical("Box");
                if (GUILayout.Button(new GUIContent("Mirror Right to Left " + "\u2192".ToString())))
                {
                    MirrorColliders(true);
                }
                if (GUILayout.Button(new GUIContent("\u2190".ToString() + " Mirror Left to Right")))
                {
                    MirrorColliders(false);
                }
                GUILayout.EndVertical();

                if (((Ragdoll)target).seat)
                {
                    GUILayout.Space(8);
                    if (GUILayout.Button(new GUIContent("Adjust SeatTarget transform", "Adjust the seat position on the ragdoll.")))
                    {
                        Selection.activeGameObject = ((Ragdoll)target).seatTarget.gameObject;
                    }
                }

                GUILayout.Space(8);
                if (GUILayout.Button("Finish Setup", buttonStyle))
                {
                    DestroyImmediate((Ragdoll)target);
                    // fin
                }
            }

        }

        // Mirroring the collider properties from one side to another
        private void MirrorColliders(bool rightToLeft)
        {
            List<int[]> mirrorBones = new List<int[]>()
            { new int[] { 10, 7 }, new int[] { 9, 6 }, new int[] { 3, 1 }, new int[] { 4, 2 } };
            int x = rightToLeft ? 0 : 1;
            foreach (int[] pair in mirrorBones)
            {
                CapsuleCollider a = ((Ragdoll)target).colliders[pair[x]].GetComponent<CapsuleCollider>();
                CapsuleCollider b = ((Ragdoll)target).colliders[pair[1-x]].GetComponent<CapsuleCollider>();
                Vector3 center = a.center;
                center.x = -center.x;
                b.center = center;
                b.radius = a.radius;
                b.height = a.height;
            }
        }

        // Make sure the avatar passes all our requirements. Returns true if uninstall is OK, false if not.
        private void CheckRequirements()
        {
            warnings.Clear();

            if (!AssetDatabase.IsValidFolder(assetPath))
            {
                string guidAssetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                if (guidAssetPath == null)
                {
                    warnings.Add("The folder at path 'Assets/VRLabs/Ragdoll System' could not be found. Make sure you are importing a Unity package and not moving the folder.");
                }
                else
                {
                    currentLocation = guidAssetPath.Substring(0,guidAssetPath.LastIndexOf('/')); // Slightly jank way to get the folder of a prefab, but we need relative to assets folder
                }
            }
            else
            {
                currentLocation = assetPath;
            }
            if (descriptor == null)
            {
                warnings.Add("There is no avatar descriptor on this GameObject. Please move this script onto your avatar, or create an avatar descriptor here.");
            }
            else
            {
                if (descriptor.transform.position != Vector3.zero || descriptor.transform.eulerAngles != Vector3.zero)
                {
                    warnings.Add("Your avatar is not positioned or rotated at world origin. Please set both the position and rotation of the avatar to (0,0,0) in the base of the scene.");
                }
                if (descriptor.expressionParameters != null && descriptor.expressionParameters.CalcTotalCost() > (VRCExpressionParameters.MAX_PARAMETER_COST - bitCount))
                {
                    warnings.Add("You don't have enough free memory in your avatar's Expression Parameters to generate. You need " + (VRCExpressionParameters.MAX_PARAMETER_COST - bitCount) + " or less bits of parameter memory utilized.");
                }
                if (descriptor.expressionsMenu != null && descriptor.expressionsMenu.controls.Count == 8)
                {
                    warnings.Add("Your avatar's topmost menu is full. Please have at least one empty control slot available.");
                }
                
                if (((Ragdoll)target).bodyMeshes == null || ((Ragdoll)target).bodyMeshes.Length == 0)
                {
                    warnings.Add("Please add at least one reference to a body mesh on your avatar.");
                }
                else
                {
                    foreach (GameObject mesh in (((Ragdoll)target).bodyMeshes))
                    {
                        if ((mesh != null) && (mesh.transform.root != descriptor.transform))
                            warnings.Add("The mesh '" + mesh.name + "' is not a child of your avatar!");
                        else if ((mesh != null) && (mesh.GetComponent<MeshRenderer>() != null))
                            warnings.Add("The mesh '" + mesh.name + "' needs a Skinned Mesh Renderer instead of a Mesh Renderer to be copied to the ragdoll! Add the component 'Skinned Mesh Renderer' to that mesh.");
                    }
                }
                if (animator == null)
                {
                    warnings.Add("There is no Animator on this avatar. Please add an Animator component on your avatar.");
                }
                else if (animator.avatar == null)
                {
                    warnings.Add("Please add an avatar in this avatar's Animator component.");
                }
                else
                {
                    if (!animator.isHuman)
                    {
                        warnings.Add("Please use this script on an avatar with a humanoid rig.");
                    }
                    else
                    {
                        string unmapped = null;
                        for (int i = 0; i < ragdollBones.Length; i++)
                        {
                            if (animator.GetBoneTransform(ragdollBones[i].HumanBodyBone) == null)
                            {
                                unmapped += ragdollBones[i].Name + ", ";
                            }
                        }
                        if (unmapped != null)
                        {
                            unmapped = unmapped.Remove(unmapped.Length - 2, 2);
                            warnings.Add("The following bones are not mapped: " + unmapped);
                        }
                    }
                }
            }
        }

        // Get the amount of parameter bits the generation will require
        private int GetBitCount()
        {
            bitCount = 3; // bools Ragdoll
            if (((Ragdoll)target).includeAudio)
            {
                bitCount += 3; // bools Ragdoll.Audio
            }
            if (((Ragdoll)target).collisionDetect)
            {
                bitCount += 1; // bool Ragdoll.Collide.Audio
            }
            if (((Ragdoll)target).seat)
            {
                bitCount += 1;  // bool Ragdoll.Seat
            }
            return bitCount;
        }

        private void SetPreviousInstallSettings()
        {
            if (descriptor != null)
            {
                if (ScriptFunctions.HasPreviousInstall(descriptor, "Ragdoll System", playablesUsed, "Ragdoll", "Ragdoll System", false))
                {
                    if ((descriptor.baseAnimationLayers[4].animatorController is AnimatorController controller) && (controller != null))
                    {
                        ((Ragdoll)target).includeAudio = controller.HasLayer("RagdollAudio");
                        ((Ragdoll)target).collisionDetect = controller.HasLayer("RagdollCollideAudio");
                        ((Ragdoll)target).seat = controller.HasLayer("RagdollSeat");
                    }
                    if ((descriptor.transform.Find("Ragdoll System/Container/Armature/") is Transform t) && (t != null))
                    {
                        if ((t.GetChild(0) is Transform c) && (c != null))
                        {
                            if (c.name.EndsWith("_HH"))
                                ((Ragdoll)target).followsHipHeight = true;
                        }
                    }
                }
            }
        }
        private void Uninstall()
        {
            ScriptFunctions.UninstallControllerByPrefix(descriptor, "Ragdoll", ScriptFunctions.PlayableLayer.FX, false);
            ScriptFunctions.UninstallControllerByPrefix(descriptor, "Ragdoll", ScriptFunctions.PlayableLayer.Gesture, false);
            ScriptFunctions.UninstallParametersByPrefix(descriptor, "Ragdoll");
            ScriptFunctions.UninstallMenu(descriptor, "Ragdoll System");
            Transform foundRagdollSystem = descriptor.transform.Find("Ragdoll System");
            if (foundRagdollSystem != null)
                DestroyImmediate(foundRagdollSystem.gameObject);
        }

        private void Generate()
        {
            Uninstall();
            if (PrefabUtility.IsPartOfPrefabInstance(descriptor.gameObject))
                PrefabUtility.RecordPrefabInstancePropertyModifications(descriptor.gameObject);

            // Unique directory setup, named after avatar
            Directory.CreateDirectory("Assets/VRLabs/GeneratedAssets/Ragdoll System/");
            AssetDatabase.Refresh();

            // Folder name cannot contain these chars
            string cleanedName = string.Join("", descriptor.name.Split('/', '?', '<', '>', '\\', ':', '*', '|', '\"'));
            string guid = AssetDatabase.CreateFolder("Assets/VRLabs/GeneratedAssets/Ragdoll System", cleanedName);
            string directory = AssetDatabase.GUIDToAssetPath(guid) + "/";
            Directory.CreateDirectory(directory + "Animations/");
            AssetDatabase.Refresh();

            // Add the radial menu 
            VRCExpressionsMenu menu;
            AssetDatabase.CopyAsset($"{currentLocation}/Menu.asset", directory + "Menu Ragdoll.asset");
            menu = AssetDatabase.LoadAssetAtPath(directory + "Menu Ragdoll.asset", typeof(VRCExpressionsMenu)) as VRCExpressionsMenu;

            if (!((Ragdoll)target).seat)
            {
                RemoveMenuControl(menu, "Seat");
            }
            EditorUtility.SetDirty(menu); // ?

            ScriptFunctions.AddSubMenu(descriptor, menu, "Ragdoll System", directory);

            // Add the parameters
            VRCExpressionParameters.Parameter p_ragdoll = new VRCExpressionParameters.Parameter() { name = "Ragdoll", valueType = VRCExpressionParameters.ValueType.Int, saved = false, networkSynced = false }; 
            ScriptFunctions.AddParameter(descriptor, p_ragdoll, directory);
            VRCExpressionParameters.Parameter p_ragdoll1 = new VRCExpressionParameters.Parameter() { name = "Ragdoll1", valueType = VRCExpressionParameters.ValueType.Bool, saved = false, networkSynced = true };
            ScriptFunctions.AddParameter(descriptor, p_ragdoll1, directory);
            VRCExpressionParameters.Parameter p_ragdoll2 = new VRCExpressionParameters.Parameter() { name = "Ragdoll2", valueType = VRCExpressionParameters.ValueType.Bool, saved = false, networkSynced = true };
            ScriptFunctions.AddParameter(descriptor, p_ragdoll2, directory);
            VRCExpressionParameters.Parameter p_ragdoll3 = new VRCExpressionParameters.Parameter() { name = "Ragdoll3", valueType = VRCExpressionParameters.ValueType.Bool, saved = false, networkSynced = true };
            ScriptFunctions.AddParameter(descriptor, p_ragdoll3, directory);
            
            if (((Ragdoll)target).collisionDetect)
            {
                VRCExpressionParameters.Parameter p_collideAudio = new VRCExpressionParameters.Parameter() { name = "Ragdoll.Collide.Audio", valueType = VRCExpressionParameters.ValueType.Bool, saved = false, networkSynced = true };
                ScriptFunctions.AddParameter(descriptor, p_collideAudio, directory);
            }
            if (((Ragdoll)target).seat)
            {
                VRCExpressionParameters.Parameter p_seat = new VRCExpressionParameters.Parameter() { name = "Ragdoll.Seat", valueType = VRCExpressionParameters.ValueType.Bool, saved = false, networkSynced = true };
                ScriptFunctions.AddParameter(descriptor, p_seat, directory);
            }
            if (((Ragdoll)target).includeAudio)
            {
                VRCExpressionParameters.Parameter p_audio1 = new VRCExpressionParameters.Parameter() { name = "Ragdoll.Audio1", valueType = VRCExpressionParameters.ValueType.Bool, saved = false, networkSynced = true };
                ScriptFunctions.AddParameter(descriptor, p_audio1, directory);
                VRCExpressionParameters.Parameter p_audio2 = new VRCExpressionParameters.Parameter() { name = "Ragdoll.Audio2", valueType = VRCExpressionParameters.ValueType.Bool, saved = false, networkSynced = true };
                ScriptFunctions.AddParameter(descriptor, p_audio2, directory);
                VRCExpressionParameters.Parameter p_audio3 = new VRCExpressionParameters.Parameter() { name = "Ragdoll.Audio3", valueType = VRCExpressionParameters.ValueType.Bool, saved = false, networkSynced = true };
                ScriptFunctions.AddParameter(descriptor, p_audio3, directory);
            }

            // Set up starting prefab
            GameObject prefab = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath($"{currentLocation}/Ragdoll System.prefab", typeof(GameObject))) as GameObject;
            if (PrefabUtility.IsPartOfPrefabInstance(prefab))
                PrefabUtility.UnpackPrefabInstance(prefab, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            // For later on
            Transform container = prefab.transform.Find("Container");
            Transform rigidbodies = container.transform.Find("Rigidbodies");
            Transform armature = container.Find("Armature");
            Transform meshes = armature.Find("Meshes");
            Transform velocity = rigidbodies.Find("Velocity");
            Transform audioSources = container.transform.Find("Audio");
            Transform collision = container.transform.Find("Collision");
            Transform collisionParticle = collision.Find("Collide");
            Transform collisionAudio = collision.Find("Audio");
            Transform seat = container.transform.Find("Seat");
            Transform seatTarget = container.transform.Find("SeatTarget");
            Transform cull = container.transform.Find("Cullfix");

            // Set container object constraint to avatar
            ConstraintSource avi = new ConstraintSource { sourceTransform = descriptor.transform, weight = 1f };
            container.GetComponent<ParentConstraint>().SetSource(0, avi);

            // In order to clone the mesh and armature without losing their connection, we need to clone the whole avatar,
            // keep just the new cloned armature and mesh, then destroy the cloned avatar itself.
            Transform[] originalArmature = animator.GetBoneTransform(Hips).GetComponentsInChildren<Transform>(true);

            // we need to make a list of the original meshes, that aren't duplicates or null objects
            List<GameObject> originalBodyMeshesList = new List<GameObject>();
            for (int i = 0; i < ((Ragdoll)target).bodyMeshes.Length; i++)
            {
                if (((Ragdoll)target).bodyMeshes[i] != null) 
                {
                    if ((((Ragdoll)target).bodyMeshes[i].GetComponent<SkinnedMeshRenderer>() != null) && (!originalBodyMeshesList.Contains(((Ragdoll)target).bodyMeshes[i])))
                        originalBodyMeshesList.Add(((Ragdoll)target).bodyMeshes[i]);
                }
            }
            GameObject[] originalBodyMeshes = originalBodyMeshesList.ToArray();

            // for bodymeshes, copy enabled and disabled, but disable the skinned mesh renderer component of currently disabled mesh object
            // order should be retained with GetComponentsInChildren on an exact copy of the avatar (?)
            bool[] isMeshEnabled = new bool[originalBodyMeshes.Length]; // used in creating animationclips
            for (int i = 0; i < originalBodyMeshes.Length; i++)
            {
                isMeshEnabled[i] = originalBodyMeshes[i].activeSelf && originalBodyMeshes[i].GetComponent<SkinnedMeshRenderer>().enabled;
                originalBodyMeshes[i].name += "RSBodyMeshIndicator"; // we change the name so we can find it on the clone
            }

            for (int i = 0; i < ragdollBones.Length; i++)
            {
                ragdollBones[i].old = animator.GetBoneTransform(ragdollBones[i].HumanBodyBone);
                ragdollBones[i].old.name += "RSRagdollBoneIndicator" + i.ToString(); // we change the name so we can find it on the clone
            }

            // Clone the avatar
            GameObject avatarClone = Instantiate(descriptor.gameObject, armature, false);
            // Parent the system to the avatar
            prefab.transform.SetParent(descriptor.transform, false);

            // Find the meshes by the strings we added
            GameObject[] bodyMeshClone = new GameObject[originalBodyMeshes.Length];
            int count = 0;
            foreach (Transform child in avatarClone.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Contains("RSBodyMeshIndicator"))
                {
                    string originalName = child.name.Substring(0, child.name.Length - 19);
                    child.name = originalName;
                    bodyMeshClone[count] = child.gameObject;
                    originalBodyMeshes[count].name = originalBodyMeshes[count].name.Substring(0, originalBodyMeshes[count].name.Length - 19);
                    count++;
                }
            }

            // Find the ragdoll bones by the strings we added.
            // Assign them by the index, because hierarhcy order may differ from ragdollBones order.
            foreach (Transform child in avatarClone.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Contains("RSRagdollBoneIndicator"))
                {
                    string added = child.name.Substring(child.name.IndexOf("RSRagdollBoneIndicator"));
                    int index = Convert.ToInt32(added.Substring(22));

                    child.name = child.name.Substring(0, (child.name.Length - added.Length));
                    ragdollBones[index].clone = child;
                    ragdollBones[index].old.name = ragdollBones[index].old.name.Substring(0, (ragdollBones[index].old.name.Length - added.Length));
                }
            }
            // Get clone armature
            Transform[] cloneArmature = ragdollBones[0].clone.GetComponentsInChildren<Transform>(true);
            // if body meshes are inside objects at base of avatar, we need to keep those too by checking it's not within their hips
            // and then parenting it to the ragdoll prefab too.
            for (int i = 0; i < originalBodyMeshes.Length; i++)
            {
                if (bodyMeshClone[i].transform.parent == avatarClone.transform)
                    bodyMeshClone[i].transform.SetParent(meshes);
                else if (!cloneArmature.Contains(bodyMeshClone[i].transform) && avatarClone.GetComponentsInChildren<Transform>(true).Contains(bodyMeshClone[i].transform))
                {   // if the cloned mesh is not inside the clone's hips, (and not a direct child of the clone), but somewhere at the base of the clone...
                    GameObject toCopy = bodyMeshClone[i];
                    while (toCopy.transform.parent != avatarClone.transform)
                        toCopy = toCopy.transform.parent.gameObject;
                    toCopy.transform.SetParent(meshes);
                }
            }
            // Parent hips to container armature (the avatar), and destroy the rest of the clone.
            ragdollBones[0].clone.transform.SetParent(armature);
            DestroyImmediate(avatarClone);
            // Remove components that might break the ragdoll
            Transform[] allChildren = armature.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in allChildren)
            {
                foreach (Component component in child.GetComponents<Component>())
                {
                    if (component != null)
                    {
                        if (component.GetType().Name.Contains("DynamicBone") || component.GetType().Name.Contains("Constraint") || component.GetType().Name.Contains("Animator") || component.GetType().Name.Contains("Collider"))
                        {
                            DestroyImmediate(component);
                        }
                    }
                }
            }
            meshes.SetParent(container); // and move meshes out of armature

            // Close any existing ragdoll wizards
            foreach (ScriptableWizard window in Resources.FindObjectsOfTypeAll<ScriptableWizard>())
            {
                if (window.titleContent.text == "Create Ragdoll")
                {
                    window.Close();
                }
            }
            // Run Unity's built in ragdoll wizard
            EditorApplication.ExecuteMenuItem("GameObject/3D Object/Ragdoll...");
            ScriptableWizard[] allWindows = Resources.FindObjectsOfTypeAll<ScriptableWizard>();
            ScriptableWizard ragdollWindow = CreateInstance<ScriptableWizard>();
            foreach (ScriptableWizard window in allWindows)
            {
                if (window.titleContent.text == "Create Ragdoll")
                {
                    ragdollWindow = window;
                }
            }
            BindingFlags _flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
            object[] args = null;
            Type t = ragdollWindow.GetType();
            MethodInfo create = t.GetMethod("OnWizardCreate", _flags);
            MethodInfo prepare = t.GetMethod("OnWizardUpdate", _flags);
            for (int i = 0; i < ragdollBones.Length; i++)
            {
                FieldInfo fi = t.GetField(ragdollBones[i].NameInWizard, _flags);
                fi.SetValue(ragdollWindow, ragdollBones[i].clone);
            }
            prepare.Invoke(ragdollWindow, args);
            create.Invoke(ragdollWindow, args);
            ragdollWindow.Close();

            // Make the generated colliders into separate children, editing their bounds is more user-friendly
            GameObject[] colliders = new GameObject[ragdollBones.Length];
            for (int i = 0; i < ragdollBones.Length; i++)
            {
                colliders[i] = new GameObject("Collider");
                colliders[i].transform.SetParent(ragdollBones[i].clone, false);
                Collider oldCollider = ragdollBones[i].clone.GetComponent<Collider>();
                if (oldCollider is CapsuleCollider capsule)
                {
                    CapsuleCollider newCollider = colliders[i].AddComponent<CapsuleCollider>();
                    newCollider.center = capsule.center;
                    newCollider.height = capsule.height;
                    newCollider.radius = capsule.radius;
                }
                else if (oldCollider is BoxCollider box)
                {
                    BoxCollider newCollider = colliders[i].AddComponent<BoxCollider>();
                    newCollider.center = box.center;
                    newCollider.size = box.size;
                }
                else if (oldCollider is SphereCollider sphere)
                {
                    // Calculate head collider ourselves, unity's fails when scale changes
                    SphereCollider newCollider = colliders[i].AddComponent<SphereCollider>();
                    float radius = Vector3.Distance(ragdollBones[i].clone.InverseTransformPoint(ragdollBones[6].clone.position), ragdollBones[i].clone.InverseTransformPoint(ragdollBones[9].clone.position)); // here
                    radius /= 2f;
                    newCollider.radius = radius;
                    Vector3 center = Vector3.zero;
                    GetDirection(ragdollBones[i].clone.InverseTransformPoint(ragdollBones[0].clone.position), out int direction, out float distance);

                    if (distance > 0)
                        center[direction] = -radius;
                    else
                        center[direction] = radius;
                    newCollider.center = center;
                }
                DestroyImmediate(oldCollider);
            }

            //Setup scale test so that 0.8 = global scale 1
            Transform scaleTestTransform = descriptor.transform.Find("Ragdoll System/Container/Scale Test");
            scaleTestTransform.localScale =scaleTestTransform.lossyScale;
            
            
            // Adjust the generated character joints to our custom values
            for (int i = 1; i < ragdollBones.Length; i++)
            {
                ragdollBones[i].clone.GetComponent<CharacterJoint>().lowTwistLimit = new SoftJointLimit() { bounciness = 0, contactDistance = 0, limit = ragdollBones[i].Limits[0] };
                ragdollBones[i].clone.GetComponent<CharacterJoint>().highTwistLimit = new SoftJointLimit() { bounciness = 0, contactDistance = 0, limit = ragdollBones[i].Limits[1] };
                ragdollBones[i].clone.GetComponent<CharacterJoint>().swing1Limit = new SoftJointLimit() { bounciness = 0, contactDistance = 0, limit = ragdollBones[i].Limits[2] };
            }
            // Set rigidbodies to kinematic (so WD on doesn't explode)
            for (int i = 0; i < ragdollBones.Length; i++)
            {
                ragdollBones[i].clone.GetComponent<Rigidbody>().isKinematic = true;
                ragdollBones[i].clone.GetComponent<Rigidbody>().useGravity = false;
            }

            // Begin setting up constraints
            // We handle the ragdoll bones differently, so exclude them from the armatures
            originalArmature = originalArmature.Except(ragdollBones.Select(x => x.old)).ToArray();
            cloneArmature = cloneArmature.Except(ragdollBones.Select(x => x.clone)).ToArray();

            // Rotation constraints on all non-ragdoll bones
            for (int i = 0; i < cloneArmature.Length; i++)
            {
                RotationConstraint rotConstraint = cloneArmature[i].gameObject.AddComponent<RotationConstraint>();
                ConstraintSource rotConstraintSource = new ConstraintSource() { sourceTransform = originalArmature[i], weight = 1f };
                rotConstraint.AddSource(rotConstraintSource);
                rotConstraint.weight = 1f;
                rotConstraint.constraintActive = true;
                rotConstraint.locked = true;
            }

            // Flatten the hierarchy
            GameObject[] originalParent = new GameObject[11];
            for (int i = 0; i < ragdollBones.Length; i++)
            {
                // First save the original parent, we'll unflatten the hierarchy later
                originalParent[i] = ragdollBones[i].clone.parent.gameObject;
                // And separate all ragdoll bones
                ragdollBones[i].clone.SetParent(armature);
            }

            // Because we're going to put the ragdoll bones, or rigidbodies, in their own container,
            // we need to create proxy objects that will replace the moved bones.
            for (int i = 0; i < ragdollBones.Length; i++)
            {
                // Create our proxies in the same position as our ragdoll bones, inside the armature.
                ragdollBones[i].proxy = new GameObject { name = ragdollBones[i].clone.name + "_proxy" };
                ragdollBones[i].proxy.transform.SetParent(ragdollBones[i].clone, false);
                ragdollBones[i].proxy.transform.SetParent(armature, true);

                // Get all children excluding self and the colliders
                Transform[] children = ragdollBones[i].clone.GetComponentsInChildren<Transform>(true);
                children = children.Except(new Transform[1] { ragdollBones[i].clone }).ToArray();
                children = children.Except(colliders.Select(x => x.transform)).ToArray();

                // Move the ragdoll bones into their own container Rigidbodies to allow proper resetting
                ragdollBones[i].clone.SetParent(rigidbodies, false);

                // Sync the ragdoll bones to their proxies.
                ParentConstraint pConstraint = ragdollBones[i].clone.gameObject.AddComponent<ParentConstraint>();
                ConstraintSource pConstraintSource = new ConstraintSource() { sourceTransform = ragdollBones[i].proxy.transform, weight = 1f };
                pConstraint.AddSource(pConstraintSource);
                pConstraint.weight = 1f;
                pConstraint.constraintActive = true;
                pConstraint.locked = true;

                // If any children exist, put them into a parent constrained to the corresponding ragdoll bone
                if (children.Length > 0)
                {
                    GameObject childrenContainer = new GameObject() { name = "Container" };
                    childrenContainer.transform.SetParent(ragdollBones[i].proxy.transform, false);
                    foreach (Transform child in children)
                    {
                        if (!children.Contains(child.parent))
                        {
                            child.SetParent(childrenContainer.transform);
                        }
                    }
                    ParentConstraint containerConstraint = childrenContainer.AddComponent<ParentConstraint>();
                    ConstraintSource containerConstraintSrc = new ConstraintSource() { sourceTransform = ragdollBones[i].clone, weight = 1f };
                    containerConstraint.AddSource(containerConstraintSrc);
                    containerConstraint.weight = 1f;
                    containerConstraint.constraintActive = true;
                    containerConstraint.locked = true;

                    // also rename the children gameobject names to be distinct (sometimes happens with booth models)
                    // so that animation clips can reference all objects correctly
                    List<Transform> childrenAndContainer = children.ToList();
                    childrenAndContainer.Add(childrenContainer.transform);
                    foreach (Transform child in childrenAndContainer)
                    {
                        List<Transform> subChildren = new List<Transform>();
                        foreach (Transform subChild in child)
                            subChildren.Add(subChild);
                        List<string> subChildrenNames = subChildren.Select(x => x.name).ToList();
                        if (!(subChildrenNames.Distinct().Count() == subChildrenNames.Count())) // if duplicate names exist
                        {
                            List<string> duplicates = subChildrenNames.GroupBy(x => x)
                                                     .Where(g => g.Count() > 1)
                                                     .Select(g => g.Key)
                                                     .ToList();
                            foreach (string dupe in duplicates)
                            {
                                int countDupes = 0;
                                for (int j = 0; j < subChildren.Count; j++)
                                {
                                    if (subChildren[j].name.Equals(dupe))
                                    {
                                        if (countDupes != 0)
                                            subChildren[j].name += countDupes.ToString();
                                        countDupes++;
                                    }
                                }
                            }
                        }
                    }
                }

                // Add rotation constraints to the proxy bones
                RotationConstraint rotConstraint = ragdollBones[i].proxy.AddComponent<RotationConstraint>();
                ConstraintSource rotConstraintSource = new ConstraintSource() { sourceTransform = ragdollBones[i].old, weight = 1f };
                rotConstraint.AddSource(rotConstraintSource);
                rotConstraint.weight = 1f;
                rotConstraint.constraintActive = true;
                rotConstraint.locked = true;
            }
            // Add position constraint to proxy hip to correct offset
            PositionConstraint posConstraintHips = ragdollBones[0].proxy.AddComponent<PositionConstraint>();
            ConstraintSource posConstraintSource = new ConstraintSource() { sourceTransform = ragdollBones[0].old, weight = 1f };
            posConstraintHips.AddSource(posConstraintSource);
            posConstraintHips.weight = 1f;
            posConstraintHips.constraintActive = true;
            posConstraintHips.locked = true;
            // Rename it so previous install settings can be detected later
            if (((Ragdoll)target).followsHipHeight)
                ragdollBones[0].proxy.name += "_HH";


            // Finally, unflatten the armature (so Mimic works)
            int[] parentOrder = new int[11] { -1, 0, 1, 0, 3, 0, 5, 6, 5, 5, 9 }; // The parent's index of each ragdollBone
            Transform[] clones = ragdollBones.Select(x => x.clone).ToArray();
            for (int i = 1; i < ragdollBones.Length; i++) // Start at 1, Hips is already a direct child of Armature
            {
                if (clones.Contains(originalParent[i].transform))
                    originalParent[i] = ragdollBones[parentOrder[i]].proxy;
                ragdollBones[i].proxy.transform.SetParent(originalParent[i].transform);
            }

            // Setup velocity configurable joint
            ConstraintSource hips = new ConstraintSource() { sourceTransform = ragdollBones[0].clone, weight = 1f };
            ConstraintSource head = new ConstraintSource() { sourceTransform = ragdollBones[8].old, weight = 1f };
            ConfigurableJoint configJoint = velocity.GetComponent<ConfigurableJoint>();
            velocity.GetComponent<PositionConstraint>().SetSource(0, hips);

            Transform forwardFinder = descriptor.transform.Find("Ragdoll System/Container/Player Forward Finder");
            
            forwardFinder.GetComponent<RotationConstraint>().SetSource(0, head);

            FixedJoint fixedJoint = ragdollBones[0].clone.gameObject.AddComponent<FixedJoint>();
            fixedJoint.enablePreprocessing = false;
            fixedJoint.connectedBody = velocity.GetComponent<Rigidbody>();

            // Edit FX layer or objects depending on user setup
            AssetDatabase.CopyAsset($"{currentLocation}/FX.controller", directory + "FXtemp.controller");
            AnimatorController FX = AssetDatabase.LoadAssetAtPath(directory + "FXtemp.controller", typeof(AnimatorController)) as AnimatorController;

            // scale seat and particle by width of hip
            ConstraintSource spine = new ConstraintSource() { sourceTransform = ragdollBones[5].clone, weight = 1f };
            float hipsScale = LargestOf(colliders[0].GetComponent<BoxCollider>().size) * colliders[0].transform.lossyScale.x; // 3x the hip size
            // and divide by the avatar scale (mostly for non-(1,1,1)-scaled avatars)
            float hipsWorldScale = (Mathf.Approximately(descriptor.transform.localScale.x, 0f)) ? hipsScale : (hipsScale / descriptor.transform.lossyScale.x);
            
            
            ScaleConstraint scaleConstraint = container.GetComponent<ScaleConstraint>();
            if (scaleConstraint == null)
            {
                scaleConstraint = container.gameObject.AddComponent<ScaleConstraint>();
            }
            scaleConstraint.AddSource(avi);
            Vector3 originalScale = avi.sourceTransform.localScale;
            scaleConstraint.scaleOffset = new Vector3(1f/originalScale.x, 1f/originalScale.y, 1f/originalScale.z);
            scaleConstraint.weight = 1f;
            scaleConstraint.locked = true;
            scaleConstraint.constraintActive = true;
            
            if (!((Ragdoll)target).includeAudio)
            {
                RemoveLayer(FX, "RagdollAudio");
                DestroyImmediate(audioSources.gameObject);
            }
            else
            {
                audioSources.GetComponent<ParentConstraint>().SetSource(0, spine);
                audioSources.GetComponent<ParentConstraint>().locked = true;
            }
            if (!((Ragdoll)target).collisionDetect)
            {
                RemoveLayer(FX, "RagdollCollide");
                RemoveLayer(FX, "RagdollCollideAudio");
                RemoveParameter(FX, "Ragdoll.Collide");
                RemoveParameter(FX, "Ragdoll.Collide.Audio");
                DestroyImmediate(collision.gameObject);
            }
            else
            {
                collision.GetComponent<ParentConstraint>().SetSource(0, hips);
                collision.GetComponent<ParentConstraint>().locked = true;
                collisionAudio.GetComponent<ParentConstraint>().SetSource(0, hips);
                collisionAudio.GetComponent<ParentConstraint>().locked = true;

                ParticleSystem.MainModule main = collisionParticle.GetComponent<ParticleSystem>().main;
            }
            if (!((Ragdoll)target).seat)
            {
                RemoveLayer(FX, "RagdollSeat");
                RemoveParameter(FX, "Ragdoll.Seat");
                DestroyImmediate(seat.gameObject);
                DestroyImmediate(seatTarget.gameObject);
            }
            else
            {
                seatTarget.SetParent(ragdollBones[5].clone, false);

                float seatScale = hipsWorldScale * 1.8f; // bit less than 2x is ok
                seat.GetComponent<BoxCollider>().size = new Vector3(seatScale, seatScale, seatScale);
                Vector3 seatOffset = seatTarget.position;
                seatOffset.z -= (hipsScale * 1.8f);
                seatTarget.position = seatOffset;
            }

            // Animation clip setup
            AnimatorState state = new AnimatorState();
            AnimationClip[] clips = new AnimationClip[14];
            // 0 = disabled, 1 = clone, 2 = freeze, 3 = fly, 4 = mimic, 5 = clone/freeze/mimic off
            // 6 = prepare, 7 = rigidbody, 8 = trigger, 9 = ragdoll, 10 = statue, 11 = ragdoll/statue off
            // 12 = fix colliders, 13 = fix colliders (remote)
            for (int i = 0; i < 14; i++)
            {
                clips[i] = new AnimationClip();
            }

            // Common curves we will be utilizing
            AnimationCurve on = AnimationCurve.Linear(0f, 1f, 0.0166666666f, 1f);
            AnimationCurve off = AnimationCurve.Linear(0f, 0f, 0.0166666666f, 0f);
            AnimationCurve toOn = AnimationCurve.Linear(0f, 0f, 0.0166666666f, 1f);
            AnimationCurve toOff = AnimationCurve.Linear(0f, 1f, 0.0166666666f, 0f);

            // Animate enabling or disabling of armature and rigidbodies
            for (int i = 1; i <= 11; i++)
            {
                clips[i].SetCurve(GetPath(armature), typeof(GameObject), "m_IsActive", on);
            }
            clips[0].SetCurve(GetPath(armature), typeof(GameObject), "m_IsActive", off);
            for (int i = 1; i <= 11; i++)
            {
                clips[i].SetCurve(GetPath(rigidbodies), typeof(GameObject), "m_IsActive", on);
            }
            clips[0].SetCurve(GetPath(rigidbodies), typeof(GameObject), "m_IsActive", off);
            clips[7].SetCurve(GetPath(rigidbodies), typeof(GameObject), "m_IsActive", off);
            clips[8].SetCurve(GetPath(rigidbodies), typeof(GameObject), "m_IsActive", off);

            // Animate container constraint (again, basically only for fly animation)
            // Off for all worldspace animations, on for off-states or disable-states
            bool[] containerConstraintOn = new bool[12]
            {
                true, false, false, false, false, true, 
                true, true, true, false, false, true
            };
            for (int i = 0; i <= 11; i++)
            {
                if (containerConstraintOn[i])
                {
                    clips[i].SetCurve(GetPath(container), typeof(ParentConstraint), "m_Enabled", on);
                }
                else
                {
                    clips[i].SetCurve(GetPath(container), typeof(ParentConstraint), "m_Enabled", off);
                }
            }

            // Animate bodymeshes
            // we are going to animate the skinned mesh renderer component in case users have body mesh toggles
            // users can fix inconsistent toggle states between avatar and clone by also animating the ragdoll's meshes gameobjects in toggles
            for (int i = 0; i < originalBodyMeshes.Length; i++)
            {
                if (!isMeshEnabled[i])
                {
                    bodyMeshClone[i].SetActive(false);
                }
                bodyMeshClone[i].GetComponent<SkinnedMeshRenderer>().enabled = false;
                for (int j = 1; j <= 4; j++)
                {
                    clips[j].SetCurve(GetPath(bodyMeshClone[i].transform), typeof(SkinnedMeshRenderer), "m_Enabled", on);
                }
                clips[2].SetCurve(GetPath(originalBodyMeshes[i].transform), typeof(SkinnedMeshRenderer), "m_Enabled", off);
                clips[3].SetCurve(GetPath(originalBodyMeshes[i].transform), typeof(SkinnedMeshRenderer), "m_Enabled", off);
                clips[5].SetCurve(GetPath(originalBodyMeshes[i].transform), typeof(SkinnedMeshRenderer), "m_Enabled", on);
                clips[5].SetCurve(GetPath(bodyMeshClone[i].transform), typeof(SkinnedMeshRenderer), "m_Enabled", off);
            }
            // Animate constraints in clone's armature
            for (int i = 0; i < cloneArmature.Length; i++)
            {
                clips[1].SetCurve(GetPath(cloneArmature[i]), typeof(RotationConstraint), "m_Enabled", toOff);
                clips[2].SetCurve(GetPath(cloneArmature[i]), typeof(RotationConstraint), "m_Enabled", toOff);
                clips[3].SetCurve(GetPath(cloneArmature[i]), typeof(RotationConstraint), "m_Enabled", toOff);
                clips[5].SetCurve(GetPath(cloneArmature[i]), typeof(RotationConstraint), "m_Enabled", on);
            }
            // Animate constraints in clone's ragdoll bones
            for (int i = 0; i < ragdollBones.Length; i++)
            {
                for (int j = 1; j <= 3; j++)
                {
                    clips[j].SetCurve(GetPath(ragdollBones[i].proxy.transform), typeof(RotationConstraint), "m_Enabled", toOff);
                    clips[j].SetCurve(GetPath(ragdollBones[0].proxy.transform), typeof(PositionConstraint), "m_Enabled", toOff);
                }
                clips[5].SetCurve(GetPath(ragdollBones[i].proxy.transform), typeof(RotationConstraint), "m_Enabled", on);
            }
            if (((Ragdoll)target).followsHipHeight)
            {
                clips[4].SetCurve(GetPath(ragdollBones[0].proxy.transform), typeof(PositionConstraint), "m_Enabled", on);
                clips[4].SetCurve(GetPath(ragdollBones[0].proxy.transform), typeof(PositionConstraint), "m_AffectTranslationX", toOff);
                clips[4].SetCurve(GetPath(ragdollBones[0].proxy.transform), typeof(PositionConstraint), "m_AffectTranslationZ", toOff);
                clips[5].SetCurve(GetPath(ragdollBones[0].proxy.transform), typeof(PositionConstraint), "m_AffectTranslationX", on);
                clips[5].SetCurve(GetPath(ragdollBones[0].proxy.transform), typeof(PositionConstraint), "m_AffectTranslationZ", on);
            }
            else
            {
                clips[4].SetCurve(GetPath(ragdollBones[0].proxy.transform), typeof(PositionConstraint), "m_Enabled", toOff);
            }
            clips[5].SetCurve(GetPath(ragdollBones[0].proxy.transform), typeof(PositionConstraint), "m_Enabled", on);

            // Create fly animation
            string[] flyProperties = new string[4]
            {
                "m_LocalPosition.y", "m_LocalEulerAnglesRaw.x", "m_LocalEulerAnglesRaw.y", "m_LocalEulerAnglesRaw.z"
            };
            float[] flyValues = new float[4] // y-position and x-y-z rotation
            {
                100f, 240f, 280f, -260f
            };
            for (int i = 0; i < 4; i++)
            {
                clips[3].SetCurve(GetPath(armature), typeof(Transform), flyProperties[i], AnimationCurve.Linear(0f, 0f, 30f, flyValues[i]));
                clips[5].SetCurve(GetPath(armature), typeof(Transform), flyProperties[i], off);
            }

            // Ragdoll and statue clips
            // They are the same, but we animate the character joint limits to create the 'statue' effect
            // Character joint limits of 0 create the 'statue' effect.
            for (int i = 9; i <= 10; i++)
            {
                for (int j = 0; j < ragdollBones.Length; j++)
                {
                    clips[i].SetCurve(GetPath(ragdollBones[j].proxy.transform), typeof(RotationConstraint), "m_Enabled", toOff);
                    clips[i].SetCurve(GetPath(ragdollBones[j].clone), typeof(Rigidbody), "m_UseGravity", toOn);
                    clips[i].SetCurve(GetPath(ragdollBones[j].clone), typeof(Rigidbody), "m_IsKinematic", toOff);
                    clips[i].SetCurve(GetPath(ragdollBones[j].clone), typeof(ParentConstraint), "m_Enabled", toOff);
                }
                clips[i].SetCurve(GetPath(ragdollBones[0].proxy.transform), typeof(PositionConstraint), "m_Enabled", toOff);
                for (int j = 0; j < cloneArmature.Length; j++)
                {
                    clips[i].SetCurve(GetPath(cloneArmature[j].transform), typeof(RotationConstraint), "m_Enabled", toOff);
                }
                // handle Velocity rigidbody
                clips[i].SetCurve(GetPath(velocity), typeof(RotationConstraint), "m_Enabled", toOff);
                clips[i].SetCurve(GetPath(velocity), typeof(PositionConstraint), "m_Enabled", toOff);
                clips[i].SetCurve(GetPath(velocity), typeof(Rigidbody), "m_UseGravity", toOn);
                clips[i].SetCurve(GetPath(velocity), typeof(Rigidbody), "m_IsKinematic", toOff);

                for (int j = 0; j < originalBodyMeshes.Length; j++)
                {
                    clips[i].SetCurve(GetPath(originalBodyMeshes[j].transform), typeof(SkinnedMeshRenderer), "m_Enabled", toOff);
                    clips[i].SetCurve(GetPath(bodyMeshClone[j].transform), typeof(SkinnedMeshRenderer), "m_Enabled", toOn);
                }
            }
            // The off clip handles both ragdoll and statue.
            for (int i = 0; i < ragdollBones.Length; i++)
            {
                clips[11].SetCurve(GetPath(ragdollBones[i].proxy.transform), typeof(RotationConstraint), "m_Enabled", on);
                clips[11].SetCurve(GetPath(ragdollBones[i].clone), typeof(Rigidbody), "m_UseGravity", off);
                clips[11].SetCurve(GetPath(ragdollBones[i].clone), typeof(Rigidbody), "m_IsKinematic", on);
                clips[11].SetCurve(GetPath(ragdollBones[i].clone), typeof(ParentConstraint), "m_Enabled", on);
            }
            clips[11].SetCurve(GetPath(ragdollBones[0].proxy.transform), typeof(PositionConstraint), "m_Enabled", on);
            // handle Velocity rigidbody
            clips[11].SetCurve(GetPath(velocity), typeof(RotationConstraint), "m_Enabled", on);
            clips[11].SetCurve(GetPath(velocity), typeof(PositionConstraint), "m_Enabled", on);
            clips[11].SetCurve(GetPath(velocity), typeof(Rigidbody), "m_UseGravity", off);
            clips[11].SetCurve(GetPath(velocity), typeof(Rigidbody), "m_IsKinematic", on);

            for (int i = 0; i < cloneArmature.Length; i++)
            {
                clips[11].SetCurve(GetPath(cloneArmature[i].transform), typeof(RotationConstraint), "m_Enabled", on);
            }
            for (int i = 0; i < originalBodyMeshes.Length; i++)
            {
                clips[11].SetCurve(GetPath(originalBodyMeshes[i].transform), typeof(SkinnedMeshRenderer), "m_Enabled", on);
                clips[11].SetCurve(GetPath(bodyMeshClone[i].transform), typeof(SkinnedMeshRenderer), "m_Enabled", off);
            }

            // Animate the joint properties for ragdoll and statue
            string[] jointProperties = new string[3]
            {
                "m_LowTwistLimit.limit", "m_HighTwistLimit.limit", "m_Swing1Limit.limit"
            };
            for (int i = 1; i < ragdollBones.Length; i++) // no characterjoint on hip, skip ragdollBones[0].clone
            {
                for (int j = 0; j < 3; j++)
                {
                    clips[9].SetCurve(GetPath(ragdollBones[i].clone), typeof(CharacterJoint), jointProperties[j],
                        AnimationCurve.Linear(0f, ragdollBones[i].Limits[j], 0.0166666666f, ragdollBones[i].Limits[j]));
                    // Joint limits of 0 create the 'statue' effect.
                    clips[10].SetCurve(GetPath(ragdollBones[i].clone), typeof(CharacterJoint), jointProperties[j], off);  
                }
            }
            for (int i = 0; i < colliders.Length; i++)
            {
                clips[12].SetCurve(GetPath(colliders[i].transform), typeof(Collider), "m_Enabled", AnimationCurve.Linear(0f, 1f, 0.5f, 1f));
                clips[13].SetCurve(GetPath(colliders[i].transform), typeof(Collider), "m_Enabled", AnimationCurve.Linear(0f, 1f, 0.5f, 1f));
            }

            // Set IsKinematic for the fix collider clips
            clips[12].SetCurve(GetPath(prefab.transform), typeof(Rigidbody), "m_IsKinematic", AnimationCurve.Linear(0f, 1f, 0.5f, 1f));
            clips[13].SetCurve(GetPath(prefab.transform), typeof(Rigidbody), "m_IsKinematic", AnimationCurve.Linear(0f, 0f, 0.5f, 0f));

            // Animate the cull object large on every clip but Disabled and Fix Colliders
            string[] scaleProperties = new string[6]
                { "m_ScaleAtRest.x", "m_ScaleAtRest.y", "m_ScaleAtRest.z",
                "m_ScaleOffset.x", "m_ScaleOffset.y", "m_ScaleOffset.z" };
            for (int i = 1; i < 12; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    clips[i].SetCurve(GetPath(cull), typeof(ScaleConstraint), scaleProperties[j], AnimationCurve.Linear(0f, 10000f, 0.0166666666f, 10000f));
                }
            }
            for (int i = 0; i < 6; i++)
            {
                clips[0].SetCurve(GetPath(cull), typeof(ScaleConstraint), scaleProperties[i], off);
            }

            for (int i = 0; i < 12; i++)
            {
                bool scaleOn = i <= 5 || i >= 11;
                clips[i].SetCurve(GetPath(container), typeof(ScaleConstraint), "m_Enabled", scaleOn ? on : off);
            }

            // wowzies!
            AssetDatabase.CreateAsset(clips[0], directory + "Animations/Disabled.anim");
            AssetDatabase.CreateAsset(clips[1], directory + "Animations/Clone.anim");
            AssetDatabase.CreateAsset(clips[2], directory + "Animations/Freeze.anim"); ;
            AssetDatabase.CreateAsset(clips[3], directory + "Animations/Fly.anim");
            AssetDatabase.CreateAsset(clips[4], directory + "Animations/Mimic.anim");
            AssetDatabase.CreateAsset(clips[5], directory + "Animations/Clone, freeze, mimic off.anim");
            AssetDatabase.CreateAsset(clips[6], directory + "Animations/Prepare.anim");
            AssetDatabase.CreateAsset(clips[7], directory + "Animations/Rigidbody.anim");
            AssetDatabase.CreateAsset(clips[8], directory + "Animations/Trigger.anim");
            AssetDatabase.CreateAsset(clips[9], directory + "Animations/Ragdoll.anim");
            AssetDatabase.CreateAsset(clips[10], directory + "Animations/Statue.anim");
            AssetDatabase.CreateAsset(clips[11], directory + "Animations/Ragdoll, statue off.anim");
            AssetDatabase.CreateAsset(clips[12], directory + "Animations/Fix Colliders.anim");
            AssetDatabase.CreateAsset(clips[13], directory + "Animations/Fix Colliders (Remote).anim");

            // Set clips in FX layer
            for (int i = 0; i < 12; i++)
            {
                // Main layer
                state = FX.layers[0].stateMachine.states.FirstOrDefault(s => s.state.name.Equals(clips[i].name)).state;
                FX.SetStateEffectiveMotion(state, clips[i]);
            }

            for (int i = 12; i < 14; i++)
            {
                // Handle Physics layer
                state = FX.layers[1].stateMachine.states.FirstOrDefault(s => s.state.name.Equals(clips[i].name)).state;
                Debug.Log(clips[i].name);
                FX.SetStateEffectiveMotion(state, clips[i]);
            }

            // and merge FX
            if (((Ragdoll)target).wdSetting)
			{
				ScriptFunctions.SetWriteDefaults(FX);
			}
            ScriptFunctions.MergeController(descriptor, FX, ScriptFunctions.PlayableLayer.FX, directory);
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(FX)); // remove modified temporary FX layer
            AnimatorController avatarFX = (AnimatorController)descriptor.baseAnimationLayers[4].animatorController;
           
            // Finished, save and assign variables in the monobehaviour
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ((Ragdoll)target).colliders = colliders;
            ((Ragdoll)target).rigidbodies = rigidbodies;
            ((Ragdoll)target).armature = armature;
            ((Ragdoll)target).seatTarget = seatTarget;
            ((Ragdoll)target).prefab = prefab.transform;
            ((Ragdoll)target).finished = true;
            Debug.Log("Successfully Generated Ragdoll System!");
        }

        static string GetPath(Transform t)
        {   // helper function: Get the path of a transform for an animation clip
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                if (t.parent != null) // skip the root name for animation paths
                {
                    path = t.name + "/" + path;
                }
            }
            return path;
        }

        static void GetDirection(Vector3 point, out int direction, out float distance)
        {   // helper function: calculate longest axis
            direction = 0;
            if (Mathf.Abs(point[1]) > Mathf.Abs(point[0]))
                direction = 1;
            if (Mathf.Abs(point[2]) > Mathf.Abs(point[direction]))
                direction = 2;

            distance = point[direction];
        }

        private void RemoveLayer(AnimatorController controller, string name)
        {   // helper function: remove layer by name
            for (int i = 0; i < controller.layers.Length; i++)
            {
                if (controller.layers[i].name.Equals(name))
                {
                    controller.RemoveLayer(i);
                    break;
                }
            }
        }

        private void RemoveParameter(AnimatorController controller, string name)
        {   // helper function: remove parameter by name
            for (int i = 0; i < controller.parameters.Length; i++)
            {
                if (controller.parameters[i].name.Equals(name))
                {
                    controller.RemoveParameter(i);
                    break;
                }
            }
        }
        private void RemoveMenuControl(VRCExpressionsMenu menu, string name)
        {   // helper function: remove menu control by name
            for (int i = 0; i < menu.controls.Count; i++)
            {
                if (menu.controls[i].name.Equals(name))
                {
                    menu.controls.RemoveAt(i);
                    break;
                }
            }
        }

        static float LargestOf(Vector3 point)
        {   // helper function: largest of x,y,z in vector3
            int dir = 0;
            if (Mathf.Abs(point[1]) > Mathf.Abs(point[0]))
                dir = 1;
            if (Mathf.Abs(point[2]) > Mathf.Abs(point[dir]))
                dir = 2;
            return (Mathf.Abs(point[dir]));
        }
    }
}
#endif