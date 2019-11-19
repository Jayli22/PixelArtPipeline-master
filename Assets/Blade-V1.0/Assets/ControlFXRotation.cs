using UnityEngine;
using System.Collections;

public class ControlFXRotation : MonoBehaviour {

    private Transform m_trans;
    private Quaternion m_rotation;

    public void Awake()
    {
        m_trans = gameObject.transform;
        m_rotation = m_trans.rotation; 
    }

    public void LateUpdate()
    {
        if (gameObject.activeInHierarchy)
        {
            m_trans.rotation = m_rotation;
        }
    }
}
