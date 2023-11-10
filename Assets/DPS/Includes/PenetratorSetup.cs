#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RalivDynamicPenetrationSystem {

	public class PenetratorSetup : ScriptableWizard {

		public GameObject penetratorPrefab;
		public GameObject penetratorModel;
		public Vector3 penetratorBase;
		public Vector3 penetratorTip;
		public Material processingMaterial;
		private Material penetratorMaterial;
		private Material cachedMaterial;
		private GameObject newPenetrator;
		private GameObject originalPenetratorModel;
		private float cachedCurvature;
		private float cachedRecurvature;
		private float length;
		private float cachedLength;
		bool dots;
		bool done;
		private bool hasMovedDot;
		private Light trackerLight;

		[MenuItem("Tools/Raliv/Create Penetrator")]
		static void Setup() {
			DisplayWizard<PenetratorSetup>("Create Penetrator", "Go", "Cancel");
		}
		
		void OnEnable() {
			SceneView.duringSceneGui += OnSceneGUI;
		}
	
		void OnDisable() {
			SceneView.duringSceneGui -= OnSceneGUI;
		}

		void OnSceneGUI(SceneView sceneView) {
			if (dots) {
				Tools.current = Tool.View;
				EditorGUI.BeginChangeCheck();
				Quaternion handleRotation = Quaternion.identity;
				if (penetratorModel.transform.parent!=null) handleRotation = penetratorModel.transform.parent.rotation;
				Vector3 newPenetratorBase = Handles.PositionHandle(penetratorModel.transform.TransformPoint(penetratorBase), handleRotation);
				Vector3 newPenetratorTip = Handles.PositionHandle(penetratorModel.transform.TransformPoint(penetratorTip), handleRotation);
				if (EditorGUI.EndChangeCheck())	{
					Undo.RecordObject(this, "Changed Penetrator Base");
					penetratorBase = penetratorModel.transform.InverseTransformPoint(newPenetratorBase);
					penetratorTip = penetratorModel.transform.InverseTransformPoint(newPenetratorTip);
					hasMovedDot = true;
				}
				GUIStyle style = new GUIStyle();
				style.normal.textColor=Color.white;
				Handles.color=Color.white;
				Handles.DrawSolidDisc(penetratorModel.transform.TransformPoint(penetratorBase), penetratorModel.transform.right, 0.01f);
				Handles.DrawSolidDisc(penetratorModel.transform.TransformPoint(penetratorBase), penetratorModel.transform.up, 0.01f);
				Handles.DrawSolidDisc(penetratorModel.transform.TransformPoint(penetratorBase), penetratorModel.transform.forward, 0.01f);
				Handles.Label(penetratorModel.transform.TransformPoint(penetratorBase), "BASE");
				Handles.color=Color.blue;
				Handles.DrawSolidDisc(penetratorModel.transform.TransformPoint(penetratorTip), penetratorModel.transform.right, 0.01f);
				Handles.DrawSolidDisc(penetratorModel.transform.TransformPoint(penetratorTip), penetratorModel.transform.up, 0.01f);
				Handles.DrawSolidDisc(penetratorModel.transform.TransformPoint(penetratorTip), penetratorModel.transform.forward, 0.01f);
				Handles.Label(penetratorModel.transform.TransformPoint(penetratorTip), "TIP");
			}
		}

		void OnGUI() {
			// REQUIRE MODEL
			if (penetratorModel==null) {
				ReadPenetratorModel();
				if (penetratorModel != null) {
					penetratorBase=Vector3.zero;
					penetratorTip = Quaternion.Inverse(penetratorModel.transform.localRotation) * Vector3.forward * 0.3f * (1f/penetratorModel.transform.lossyScale.x);
				}
				return;
			}
			// VALIDATE MODEL HAS MESH
			if (penetratorModel.GetComponent<SkinnedMeshRenderer>()==null && penetratorModel.GetComponent<MeshRenderer>()==null) {
				ReadPenetratorModel();
				EditorGUILayout.HelpBox("You must choose a model with a MeshFilter or SkinnedMeshRenderer!", MessageType.Error);
				EditorGUILayout.HelpBox("This error typically happens when you have added an object with your mesh in it's hierarchy, rather than the mesh object itself. Look through the hierarchy of your model for an object with a Mesh Filter or Skinned Mesh Renderer component.", MessageType.Info);
				return;
			}
			// VALIDATE WE HAVE CACHED MATERIAL
			if (cachedMaterial == null) {
				// VALIDATE MODEL HAS A MATERIAL
				if (penetratorMaterial == null) {
					if (penetratorModel.GetComponent<SkinnedMeshRenderer>()!=null) 
						penetratorMaterial = penetratorModel.GetComponent<SkinnedMeshRenderer>().sharedMaterial;
					if (penetratorModel.GetComponent<MeshRenderer>()!=null) 
						penetratorMaterial = penetratorModel.GetComponent<MeshRenderer>().sharedMaterial;
					if (penetratorMaterial == null) {
						EditorGUILayout.HelpBox("Material not detected..", MessageType.Error);
						return;
					}
				}
				// VALIDATE MODEL HAS PENETRATOR MATERIAL
				if (!penetratorMaterial.HasProperty("_Length")) {
					EditorGUILayout.HelpBox("Your penetrator model must have a material with a penetrator shader selected!", MessageType.Error);
					EditorGUILayout.HelpBox("Change the shader your penetrator uses to a penetrator shader. Try Raliv > Penetrator.", MessageType.Info);
					return;
				}
				cachedMaterial = penetratorMaterial;
				//penetratorMaterial=(Material)Instantiate(penetratorMaterialDefault);
				if (penetratorModel.GetComponent<SkinnedMeshRenderer>()!=null) 
					penetratorModel.GetComponent<SkinnedMeshRenderer>().sharedMaterial = processingMaterial;
				if (penetratorModel.GetComponent<MeshRenderer>()!=null) 
					penetratorModel.GetComponent<MeshRenderer>().sharedMaterial = processingMaterial;
			}
			// MODEL ALIGNMENT
			dots = true;
			GUIStyle textStyle = EditorStyles.label;
			textStyle.wordWrap = true;
			EditorGUILayout.LabelField("Move the white position dot onto the center of the base of the penetrator, everything in front of the dot will deform.", textStyle);
			EditorGUILayout.LabelField("Move the blue dot to the center of the tip of the penetrator", textStyle);
			if (!hasMovedDot) {
				GUI.color = new Color(1, 1, 1, 0.2f);
				if (GUILayout.Button("Generate Custom Model!")) { }
				return;
			}
			if (GUILayout.Button("Generate Custom Model!")) {
				PreparePenetrator();
				int error = SetupPenetratorModel();
				if (error > 0) {
					switch (error) {
						case 1:
							EditorGUILayout.HelpBox("Something went wrong!", MessageType.Error);
							break;
					}
				} else {
					penetratorModel.GetComponent<MeshRenderer>().sharedMaterial=penetratorMaterial;
					penetratorMaterial.SetFloat("_Curvature", cachedCurvature);
					penetratorMaterial.SetFloat("_ReCurvature", cachedRecurvature);
					penetratorMaterial.SetFloat("_EntranceStiffness", 0.01f);
					newPenetrator.transform.position=penetratorModel.transform.TransformPoint(penetratorBase);
					newPenetrator.transform.rotation=Quaternion.LookRotation(penetratorModel.transform.TransformPoint(penetratorTip)-penetratorModel.transform.TransformPoint(penetratorBase), Vector3.up);
					penetratorModel.transform.parent = newPenetrator.transform;
					penetratorModel.transform.localPosition=Vector3.zero;
					penetratorModel.transform.localRotation=Quaternion.identity;
					EditorGUILayout.Space();
					EditorGUILayout.HelpBox("Done!", MessageType.Info);
					FinalizePenetrator();
					done=true;
					Close();
				}
			}
			GUI.color = new Color(1,1,1,0.2f);
			if (GUILayout.Button("Skip Penetrator Validation")) {
				PreparePenetrator();
				FinalizePenetrator();
			}
			GUI.color = new Color(1,1,1,1f);
			EditorGUILayout.HelpBox("If your model has been exported from blender in full compliance with the dynamic penetration system, you can click the Skip Penetrator Validation button.", MessageType.Info);
		}

		private void ReadPenetratorModel() {
			penetratorModel = (GameObject)EditorGUILayout.ObjectField("My Model", penetratorModel, typeof(GameObject), true);
			GUIStyle textStyle = EditorStyles.label;
			textStyle.wordWrap = true;
			EditorGUILayout.LabelField("Drag custom model into the slot provided",textStyle);
		}

		void TranslateMesh(Mesh mesh, Vector3 translation) {
			Vector3[] vertices = mesh.vertices;

			for (int i=0;i<vertices.Length;i++) {
				vertices[i] += translation;
			}

			mesh.vertices = vertices;
		}

		void RotateMesh(Mesh mesh, Quaternion rotation) {
			Vector3[] vertices = mesh.vertices;
			Vector3[] normals = mesh.normals;
			Vector4[] tangents = mesh.tangents;
			Vector3 tempTangent;

			for (int i=0;i<vertices.Length;i++) {
				vertices[i] = rotation * vertices[i];
				normals[i] = rotation * normals[i];
				tempTangent = new Vector3(tangents[i].x, tangents[i].y, tangents[i].z);
				tempTangent = rotation * tempTangent;
				tangents[i] = new Vector4(tempTangent.x, tempTangent.y, tempTangent.z, tangents[i].w);
			}

			mesh.vertices = vertices;
			mesh.normals = normals;
			mesh.tangents = tangents;
		}

		void ScaleMesh(Mesh mesh, float ScaleFactor) {
			Vector3[] vertices = mesh.vertices;

			for (int i=0;i<vertices.Length;i++) {
				vertices[i] *= ScaleFactor;
			}

			mesh.vertices = vertices;
		}

		void GetPenetratorLength() {
			Mesh sharedMesh = null;
			if (penetratorModel.GetComponent<SkinnedMeshRenderer>()!=null) {
				sharedMesh = new Mesh();
				penetratorModel.GetComponent<SkinnedMeshRenderer>().BakeMesh(sharedMesh);
			}
			if (penetratorModel.GetComponent<MeshFilter>()!=null) 
				sharedMesh = penetratorModel.GetComponent<MeshFilter>().sharedMesh;
			Vector3 farthestVert = Vector3.zero;
			for (int i=0;i<sharedMesh.vertices.Length;i++) {
				float dist = sharedMesh.vertices[i].magnitude;
				if (dist>farthestVert.magnitude) {
					farthestVert=sharedMesh.vertices[i];
				}
			}
			length = farthestVert.magnitude;
		}

		private void PreparePenetrator() {
			originalPenetratorModel = penetratorModel;
			trackerLight = null;
			if (penetratorModel.transform.parent != null)
				trackerLight = penetratorModel.transform.parent.GetComponentInChildren<Light>();
			if (trackerLight == null || trackerLight.color.maxColorComponent > 0.1f) {
				newPenetrator = (GameObject) Instantiate(penetratorPrefab, penetratorModel.transform.position, penetratorModel.transform.rotation);
				newPenetrator.transform.parent = penetratorModel.transform.parent;
				if (PrefabUtility.IsPartOfAnyPrefab(penetratorModel)) {
					GameObject newPenetratorModel = GameObject.Instantiate(penetratorModel, penetratorModel.transform.parent);
					newPenetratorModel.transform.localPosition = penetratorModel.transform.localPosition;
					newPenetratorModel.transform.localRotation = penetratorModel.transform.localRotation;
					newPenetratorModel.transform.localScale = penetratorModel.transform.localScale;
					newPenetratorModel.transform.parent = null;
					penetratorModel.SetActive(false);
					penetratorModel = newPenetratorModel;
				}
				newPenetrator.name = penetratorPrefab.name + "_" + penetratorModel.name;
				trackerLight = newPenetrator.GetComponentInChildren<Light>();
			}
		}

		int SetupPenetratorModel() {
			if (penetratorModel.GetComponent<SkinnedMeshRenderer>()!=null) {
				if (penetratorModel.GetComponent<SkinnedMeshRenderer>().rootBone!=null) {
					GameObject newPenetratorModel = new GameObject(penetratorModel.name);
					newPenetratorModel.transform.parent = penetratorModel.transform.parent;
					newPenetratorModel.transform.localPosition = penetratorModel.transform.localPosition;
					newPenetratorModel.transform.localRotation = penetratorModel.transform.localRotation;
					newPenetratorModel.transform.localScale = penetratorModel.transform.localScale;
					newPenetratorModel.transform.parent = null;
					newPenetratorModel.AddComponent<MeshFilter>();
					newPenetratorModel.GetComponent<MeshFilter>().sharedMesh=new Mesh();
					penetratorModel.GetComponent<SkinnedMeshRenderer>().BakeMesh(newPenetratorModel.GetComponent<MeshFilter>().sharedMesh);
					newPenetratorModel.AddComponent<MeshRenderer>();
					newPenetratorModel.GetComponent<MeshFilter>().sharedMesh.RecalculateBounds();
					penetratorModel.GetComponent<SkinnedMeshRenderer>().sharedMaterial = cachedMaterial;
					penetratorModel.SetActive(false);
					penetratorModel=newPenetratorModel;
					EditorUtility.SetDirty(penetratorModel);
				}
			}
			EditorUtility.SetDirty(penetratorModel);
			cachedCurvature = penetratorMaterial.GetFloat("_Curvature");
			cachedRecurvature = penetratorMaterial.GetFloat("_ReCurvature");
			penetratorMaterial.SetFloat("_Curvature", 0f);
			penetratorMaterial.SetFloat("_ReCurvature", 0f);
			cachedLength = penetratorMaterial.GetFloat("_Length");
			penetratorMaterial.SetFloat("_Length", 100f);

			Mesh sharedMesh = null;
			if (penetratorModel.GetComponent<SkinnedMeshRenderer>()!=null) {
				sharedMesh = new Mesh();
				penetratorModel.GetComponent<SkinnedMeshRenderer>().BakeMesh(sharedMesh);
			}
			sharedMesh = penetratorModel.GetComponent<MeshFilter>().sharedMesh;
			Mesh mesh = (Mesh)Instantiate(sharedMesh);

			TranslateMesh(mesh, -penetratorBase);

			Vector3 farthestVert = Vector3.zero;
			RotateMesh(mesh, Quaternion.Inverse(Quaternion.LookRotation(penetratorTip - penetratorBase, penetratorModel.transform.InverseTransformDirection(Vector3.up))));
			ScaleMesh(mesh, penetratorModel.transform.localScale.x);
			penetratorModel.transform.localScale=Vector3.one;

			for (int i=0;i<mesh.vertices.Length;i++) {
				float dist = mesh.vertices[i].magnitude;
				if (dist>farthestVert.magnitude) {
					farthestVert=mesh.vertices[i];
				}
			}

			length = farthestVert.magnitude;

			Bounds bigBounds=mesh.bounds;
			bigBounds.center=Vector3.zero;
			bigBounds.extents = new Vector3(length*2f, length*2f, length*2f);
			mesh.bounds=bigBounds;

			AssetDatabase.CreateAsset(mesh, "Assets/RalivDynamicPenetrationSystem/MyData/"+penetratorModel.name+".asset");
			AssetDatabase.SaveAssets();

			if (penetratorModel.GetComponent<SkinnedMeshRenderer>()!=null) {
				penetratorModel.GetComponent<SkinnedMeshRenderer>().sharedMesh = mesh;
				penetratorModel.GetComponent<SkinnedMeshRenderer>().updateWhenOffscreen=false;
				bigBounds = penetratorModel.GetComponent<SkinnedMeshRenderer>().localBounds;
				bigBounds.center=Vector3.zero;
				bigBounds.extents = new Vector3(length*2f, length*2f, length*2f);
				penetratorModel.GetComponent<SkinnedMeshRenderer>().localBounds=bigBounds;
				EditorUtility.SetDirty(penetratorModel.GetComponent<SkinnedMeshRenderer>().sharedMesh);
			}
			if (penetratorModel.GetComponent<MeshFilter>()!=null) {
				penetratorModel.GetComponent<MeshFilter>().sharedMesh = mesh;
				EditorUtility.SetDirty(penetratorModel.GetComponent<MeshFilter>());
			}
			return 0;
		}

		private void FinalizePenetrator() {
			GetPenetratorLength();
			penetratorMaterial.SetFloat("_Length", length);
			trackerLight.intensity=length;
			done=true;
			Close();
		}
		
		void OnDestroy() {
			Tools.current = Tool.Move;
			if (penetratorModel!=null) {
				if (penetratorMaterial!=null) {
					if (penetratorModel.GetComponent<SkinnedMeshRenderer>()!=null) {
						penetratorModel.GetComponent<SkinnedMeshRenderer>().sharedMaterial = penetratorMaterial;
						penetratorModel.GetComponent<SkinnedMeshRenderer>().receiveShadows=false;
					}
					if (penetratorModel.GetComponent<MeshRenderer>()!=null) {
						penetratorModel.GetComponent<MeshRenderer>().sharedMaterial = penetratorMaterial;
						penetratorModel.GetComponent<MeshRenderer>().receiveShadows=false;
					}
					if (penetratorMaterial.GetFloat("_Length")==100f) penetratorMaterial.SetFloat("_Length", cachedLength);
				}
				if (!done && cachedMaterial!=null) {
					if (penetratorModel.GetComponent<SkinnedMeshRenderer>()!=null) 
						penetratorModel.GetComponent<SkinnedMeshRenderer>().sharedMaterial = cachedMaterial;
					if (penetratorModel.GetComponent<MeshRenderer>()!=null) 
						penetratorModel.GetComponent<MeshRenderer>().sharedMaterial = cachedMaterial;
				}
			}
		}

	}

}
#endif
