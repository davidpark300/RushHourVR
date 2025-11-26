using UnityEngine;

public class FrameState : MonoBehaviour, IState
{
    public string stateId = "NewFrameState";

    public FrameData frameData = null;

    public Vector3 frameSize;
    public Texture2D[] textures;
    public float[] frameRates;

    public bool isCscan = false;
    public int startIndex = 0;
    public bool startForward = true;

    private BaseScan<Texture2D> _textureScan;
    private BaseScan<float> _frameScan;

    private Transform _frameTransform;
    private Renderer _frameRenderer;
    private float _timer;

    // IState
    public string StateId
    {
        get { return stateId; }
    }
    public void Initialize()
    {
        if (frameData != null)
        {
            frameSize = frameData.frameSize;
            textures = frameData.textures;
            frameRates = frameData.frameRates;
            isCscan = frameData.isCscan;
            startIndex = frameData.startIndex;
            startForward = frameData.startForward;
        }
        if (textures == null || frameRates == null)
        {
            GlobalLogger.Error(gameObject.name, StateId, nameof(Initialize), "textures == null || frameRates == null");
            return;
        }
        _timer = 0f;
        if (isCscan)
        {
            _textureScan = new Cscan<Texture2D>(textures, startIndex, startForward);
            if (startIndex >= frameRates.Length) startIndex = frameRates.Length - 1;
            _frameScan = new Cscan<float>(frameRates, startIndex, startForward);
        }
        else
        {
            _textureScan = new Scan<Texture2D>(textures, startIndex, startForward);
            if (startIndex >= frameRates.Length) startIndex = frameRates.Length - 1;
            _frameScan = new Scan<float>(frameRates, startIndex, startForward);
        }

        GameObject[] frameObjects = Util.FindChildrenWithAttribute(gameObject, "Frame");
        if (frameObjects == null)
        {
            GlobalLogger.Error(gameObject.name, StateId, nameof(Initialize), "frameObjects.Length == null");
            return;
        }
        if (frameObjects.Length != 1)
        {
            GlobalLogger.Error(gameObject.name, StateId, nameof(Initialize), "frameObjects.Length != 1");
            return;
        }
        _frameTransform = frameObjects[0].GetComponent<Transform>();
        Renderer[] frameRenderers = frameObjects[0].GetComponents<Renderer>();
        if (frameRenderers == null)
        {
            GlobalLogger.Error(gameObject.name, StateId, nameof(Initialize), "frameRenderers == null");
            return;
        }
        if (frameRenderers.Length != 1)
        {
            GlobalLogger.Error(gameObject.name, StateId, nameof(Initialize), "frameRenderers.Length != 1");
            return;
        }
        _frameRenderer = frameRenderers[0];
    }
    public void DrawFrame()
    {
        float delta = Time.deltaTime;
        _timer += delta;
        if (_timer >= _frameScan.GetCurrent())
        {
            _timer = 0f;
            _frameScan.Next();
            _textureScan.Next();
        }
        _frameTransform.localScale = frameSize;
        _frameRenderer.material.mainTexture = _textureScan.GetCurrent();
    }

    // MonoBehaviour
    void Start()
    {
        Initialize();
    }
}
