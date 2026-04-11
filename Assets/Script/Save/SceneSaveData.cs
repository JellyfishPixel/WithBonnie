using System;
using UnityEngine;

[Serializable]
public class SceneSaveData
{
    public string sceneName;

    // 🔥 ตำแหน่งจริงของผู้เล่น
    public Vector3 playerPosition;
    public Quaternion playerRotation;

    // กล้อง
    public CameraMode cameraMode;
    public Vector2 cameraLook;

    // ใช้เช็คว่าเป็น save ที่ valid
    public bool hasPlayerTransform;
}
