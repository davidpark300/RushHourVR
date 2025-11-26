using UnityEngine;

[CreateAssetMenu(fileName = "NewFrameData", menuName = "Resource/FrameData")]
public class FrameData : ScriptableObject
{
    public Vector3 frameSize;
    public Texture2D[] textures;
    public float[] frameRates;

    public bool isCscan = false;
    public int startIndex = 0;
    public bool startForward = true;
}
