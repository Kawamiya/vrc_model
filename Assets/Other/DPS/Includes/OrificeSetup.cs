#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering;

public class OrificeSetup : ScriptableWizard {

	public GameObject orifice;
	private string errorInfo="";
	private int[] shapes;
	private Material orificeMaterial;

	[MenuItem("Tools/Raliv/Orifice Setup")]
	static void Setup() {
		ScriptableWizard.DisplayWizard<OrificeSetup>("Orifice Setup");
	}

	private List<string> blendshapeNameList(Mesh m) {
		List<string> nameList = new List<string>();
		nameList.Add("None");
		for (int i=0; i<m.blendShapeCount; i++) {
			nameList.Add(m.GetBlendShapeName(i));
		}
		return nameList;
	}

	Vector3 ToTangentSpace(Vector3 input, Vector3 normal, Vector3 tangent) {
		Vector3 X = normal;
		Vector3 Y = new Vector3(tangent.x, tangent.y, tangent.z);
		Vector3 Z = Vector3.Cross(X, Y);
		Vector3.OrthoNormalize(ref X, ref Y, ref Z);
		Matrix4x4 toNewSpace = new Matrix4x4();
		toNewSpace.SetRow(0, X);
		toNewSpace.SetRow(1, Y);
		toNewSpace.SetRow(2, Z);
		toNewSpace[3, 3] = 1.0F;
		return toNewSpace.MultiplyPoint(input);
	}

	void BlitShape(Texture2D texture, Mesh mesh, int shapeIndex, int encodingIndex) {
		Vector3[] deltaVerts = new Vector3[mesh.vertexCount];
		Vector3[] deltaNormals = new Vector3[mesh.vertexCount];
		Vector3[] deltaTangents = new Vector3[mesh.vertexCount];
		Vector3[] vertices = mesh.vertices;
		Vector3[] normals = mesh.normals;
		Vector4[] tangents = mesh.tangents;

		mesh.GetBlendShapeFrameVertices(shapeIndex, 0, deltaVerts, deltaNormals, deltaTangents);
		BlitShapeInfo(texture, encodingIndex*2, vertices, normals, tangents, deltaVerts);
		BlitShapeInfo(texture, encodingIndex*2+1, vertices, normals, tangents, deltaNormals);
	}

	void BlitShapeInfo(Texture2D texture, float index, Vector3[] v, Vector3[] n, Vector4[] t, Vector3[] d) {
		Color color;
		int currentVert=0;
		for (int y = Mathf.FloorToInt((texture.height/8f)*index); y < Mathf.FloorToInt((texture.height/8f)*(index+1)); y++) {
			for (int x = 0; x < texture.width; x++) {
				if (currentVert<d.Length) {
					Vector3 deltaVert=Vector3.zero;
					deltaVert = d[currentVert];
					float tn=Vector3.Project(deltaVert, n[currentVert]).magnitude * Mathf.Sign(Vector3.Dot(deltaVert, n[currentVert]));
					float tt=Vector3.Project(deltaVert, t[currentVert]).magnitude * Mathf.Sign(Vector3.Dot(deltaVert, t[currentVert]));
					float tb=Vector3.Project(deltaVert, Vector3.Cross(n[currentVert].normalized, t[currentVert].normalized)).magnitude * Mathf.Sign(Vector3.Dot(deltaVert, Vector3.Cross(n[currentVert].normalized, t[currentVert].normalized)));
					tn+=1f;
					tt+=1f;
					tb+=1f;
					color = new Color(tn, tt, tb);
					//Vector3 ntb=ToTangentSpace(deltaVert, n[currentVert], t[currentVert]);
					//ntb+=Vector3.one;
					//color = new Color(ntb.x, ntb.y, ntb.z);
					texture.SetPixel(x, y, color);
					currentVert++;
				} else {
					//texture.SetPixel(x, y, new Color(128f/255f,128f/255f,128f/255f));
					texture.SetPixel(x, y, new Color(1f,1f,1f));
				}
			}
		}
	}

	void CreateTexture() {
		SkinnedMeshRenderer meshRenderer = orifice.GetComponent<SkinnedMeshRenderer>();
		Mesh mesh = meshRenderer.sharedMesh;
		if (mesh.tangents.Length<mesh.vertices.Length) {
			errorInfo="Tangents not available";
		} else {
			Texture2D texture = new Texture2D(1024, 1024, TextureFormat.RGBAFloat, false, true);
			var fillColorArray =  texture.GetPixels();
			for(var i = 0; i < fillColorArray.Length; ++i)
			{
					fillColorArray[i] = new Color(1f,1f,1f);
			}
			texture.SetPixels( fillColorArray );
			if (shapes[0]>0) BlitShape(texture, mesh, shapes[0]-1, 0);
			if (shapes[1]>0) BlitShape(texture, mesh, shapes[1]-1, 1);
			if (shapes[2]>0) BlitShape(texture, mesh, shapes[2]-1, 2);
			if (shapes[3]>0) BlitShape(texture, mesh, shapes[3]-1, 3);
			texture.Apply();
			AssetDatabase.CreateAsset(texture, "Assets/RalivDynamicPenetrationSystem/MyData/"+orifice.name+".asset");
			AssetDatabase.SaveAssets();
			if (orifice.GetComponent<SkinnedMeshRenderer>()!=null) {
				orificeMaterial.SetTexture("_OrificeData",(Texture2D)AssetDatabase. LoadAssetAtPath("Assets/RalivDynamicPenetrationSystem/MyData/"+orifice.name+".asset",typeof(Texture2D)));
			}
		}
	}

	void FindOrificeMaterial(SkinnedMeshRenderer meshRenderer) {
		for (int i=0;i<meshRenderer.sharedMaterials.Length;i++) {
			if (meshRenderer.sharedMaterials[i].HasProperty("_OrificeData")) orificeMaterial=meshRenderer.sharedMaterials[i];
		}
	}

	void OnGUI() {
		GUIStyle textStyle = EditorStyles.label;
		textStyle.wordWrap = true;
		if (shapes==null) shapes=new int[4];
		orifice = (GameObject)EditorGUILayout.ObjectField("Orifice", orifice, typeof(GameObject), true);
		if (errorInfo!="") EditorGUILayout.HelpBox(errorInfo, MessageType.Error);
		if (orifice==null) {
			EditorGUILayout.HelpBox("Drop your orifice object above", MessageType.Info);
		} else {
			if (orifice.GetComponent<SkinnedMeshRenderer>()==null) {
				EditorGUILayout.HelpBox("No skinned mesh renderer detected!", MessageType.Error);
			} else {
				if (orificeMaterial==null) {
					FindOrificeMaterial(orifice.GetComponent<SkinnedMeshRenderer>());
					if (orificeMaterial==null) EditorGUILayout.HelpBox("Mesh does not have orifice material!", MessageType.Error);
				} else {
					EditorGUILayout.LabelField("Select blendshapes for penetration deformations", textStyle);
					EditorGUILayout.LabelField("", textStyle);
					EditorGUILayout.LabelField("Entrance", textStyle);
					shapes[0] = EditorGUILayout.Popup(shapes[0], blendshapeNameList(orifice.GetComponent<SkinnedMeshRenderer>().sharedMesh).ToArray()); 
					EditorGUILayout.LabelField("Depth1", textStyle);
					shapes[1] = EditorGUILayout.Popup(shapes[1], blendshapeNameList(orifice.GetComponent<SkinnedMeshRenderer>().sharedMesh).ToArray()); 
					EditorGUILayout.LabelField("Depth2", textStyle);
					shapes[2] = EditorGUILayout.Popup(shapes[2], blendshapeNameList(orifice.GetComponent<SkinnedMeshRenderer>().sharedMesh).ToArray()); 
					EditorGUILayout.LabelField("Depth3", textStyle);
					shapes[3] = EditorGUILayout.Popup(shapes[3], blendshapeNameList(orifice.GetComponent<SkinnedMeshRenderer>().sharedMesh).ToArray()); 
					EditorGUILayout.LabelField("", textStyle);
					if (GUILayout.Button("Generate Texture")) {
						CreateTexture();
						Close();
					}
				}
			}
		}
	}
}

#endif