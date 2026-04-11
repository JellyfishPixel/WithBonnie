using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public string spawnId;

    [Header("Rotation")]
    public bool overridePlayerRotation = true;
    public Vector3 playerEuler;  

    public bool overrideCameraRotation = true;
    public Vector2 cameraLook;    
}
