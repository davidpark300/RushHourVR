using UnityEngine;

public class MovementState : MonoBehaviour, IState
{
    public string stateId = "NewMovementState";

    public MovementData movementData = null;

    public float speed;

    private Transform _transform;
    private Rigidbody _rigidbody;
    private Renderer _frameRenderer;

    private float _timer;

    // IState
    public string StateId
    {
        get { return stateId; }
    }
    public void Initialize()
    {
        if (movementData != null)
        {
            speed = movementData.speed;
        }

        _transform = gameObject.GetComponent<Transform>();
        _rigidbody = gameObject.GetComponent<Rigidbody>();
        if (_rigidbody == null)
        {
            GlobalLogger.Error(gameObject.name, StateId, nameof(Initialize), "_rigidbody == null");
            return;
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
    public float Move(float horizontalWeight, CollisionEntities collisionEntities)
    {
        if (horizontalWeight == 0) return 0f;
        float delta = Time.deltaTime;
        Vector3 direction = new Vector3(horizontalWeight, 0, 0);

        bool collidingLeft = false;
        bool collidingRight = false;
        bool collidingFloor = false;

        GameObject floorObject = null;
        Vector3 floorNormal = new Vector3(horizontalWeight, 0, 0);

        if (collisionEntities != null)
        {
            foreach (var collisionEntity in collisionEntities.EntityDictionary)
            {
                var collisionObject = collisionEntity.Key;
                if (Util.ContainAttribute(collisionObject, GlobalConfig.Key.ATTRIBUTE_WALL_FLOOR))
                {
                    collidingFloor = true;
                    floorObject = collisionObject;
                    floorNormal = collisionEntity.Value;
                }
                if (Util.ContainAttribute(collisionObject, GlobalConfig.Key.ATTRIBUTE_WALL_LEFT)) collidingLeft = true;
                if (Util.ContainAttribute(collisionObject, GlobalConfig.Key.ATTRIBUTE_WALL_RIGHT)) collidingRight = true;
            }
        }

        if (horizontalWeight > 0)
        {
            _frameRenderer.material.mainTextureScale = new Vector2(1, 1);
        }
        else if (horizontalWeight < 0)
        {
            _frameRenderer.material.mainTextureScale = new Vector2(-1, 1);
        }

        if ((horizontalWeight < 0 && collidingLeft) || (horizontalWeight > 0 && collidingRight)) return 0f;

        if (collidingFloor)
        {
            direction = Vector3.ProjectOnPlane(direction, floorNormal).normalized;
        }
        GetComponent<Rigidbody>().MovePosition(transform.position + direction * speed * delta);
        return (direction * speed * delta).magnitude;
    }

    // MonoBehaviour
    void Start()
    {
        Initialize();
    }
}
