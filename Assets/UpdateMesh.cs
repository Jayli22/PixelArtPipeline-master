using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateMesh : MonoBehaviour
{
    public SkinnedMeshRenderer meshRenderer;
    public MeshCollider collider;
    private void Awake()
    {
        meshRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
        collider = gameObject.GetComponent<MeshCollider>();
    }

    private void Update()
    {
        // weapon mesh
        Mesh weaponColliderMesh = new Mesh();
        meshRenderer.BakeMesh(weaponColliderMesh);

        collider.sharedMesh = null;
        collider.sharedMesh = weaponColliderMesh;
    }
   
}
