using Cinemachine;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public CinemachineFreeLook thirdPersonCamera;

    private const string XAxisInput = "Mouse X";
    private const string YAxisInput = "Mouse Y";

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            thirdPersonCamera.m_XAxis.m_InputAxisName = XAxisInput;
            thirdPersonCamera.m_YAxis.m_InputAxisName = YAxisInput;
        }
        
        if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            thirdPersonCamera.m_XAxis.m_InputAxisName = string.Empty;
            thirdPersonCamera.m_YAxis.m_InputAxisName = string.Empty;
        }
    }
}
