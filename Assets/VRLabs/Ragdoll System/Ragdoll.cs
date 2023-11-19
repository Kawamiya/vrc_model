// Ragdoll System by ksivl @ VRLabs 3.0 Assets https://discord.gg/THCRsJc https://vrlabs.dev
#if UNITY_EDITOR

using UnityEngine;

namespace VRLabs.RagdollSystem
{
    [ExecuteAlways]
    public class Ragdoll : MonoBehaviour
    {
        public bool wdSetting, finished, includeAudio, collisionDetect, seat, followsHipHeight;
        public Transform armature, rigidbodies, seatTarget, prefab;
        public GameObject[] bodyMeshes = new GameObject[1];
        public GameObject[] colliders = new GameObject[11];
		public void Update()
		{
			if (finished)
			{
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] != null && colliders[i].transform.hasChanged) //https://i.imgur.com/2TdjB4C.png
                    {
                        colliders[i].transform.localPosition = Vector3.zero;
                        colliders[i].transform.localEulerAngles = Vector3.zero;
                        colliders[i].transform.localScale = Vector3.one;
                        colliders[i].transform.hasChanged = false;
                    }
                }
                if (prefab == null)
                    DestroyImmediate(this);
            }
        }
        public void OnDestroy()
        {
            if (finished)
            {
                armature.gameObject.SetActive(false);
                rigidbodies.gameObject.SetActive(false);
                // we need to shut off the colliders for the world physics fix to work.
                for (int i = 0; i < 11; i++)
                    colliders[i].GetComponent<Collider>().enabled = false;
            }
        }
    }
}
#endif