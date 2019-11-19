using UnityEngine;
using System.Collections;

public class ParticleRotation : MonoBehaviour
{
  
    public float RotateSpeed = 10f;
    public bool RotateX = false;
    public bool RotateY = false;
    public bool RotateZ = false;

    protected float OriAngleX = 0f;
    protected float OriAngleY = 0f;
    protected float OriAngleZ = 0f;

    void Start()
    {
        OriAngleX = transform.rotation.x;
        OriAngleY = transform.rotation.y;
        OriAngleZ = transform.rotation.z;
    }

    // Update is called once per frame
    void Update()
    {
        if (RotateX)
            OriAngleX += Time.deltaTime * RotateSpeed;
        if (RotateY)
            OriAngleY += Time.deltaTime * RotateSpeed;
        if (RotateZ)
            OriAngleZ += Time.deltaTime * RotateSpeed;

        transform.rotation = Quaternion.Euler(OriAngleX, OriAngleY, OriAngleZ);
    }
}
