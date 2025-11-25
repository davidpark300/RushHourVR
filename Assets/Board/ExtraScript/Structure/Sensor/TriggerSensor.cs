using UnityEngine;

public class TriggerSensor : MonoBehaviour, ISensor<bool>
{
    public string sensorId;
    public GameObject targetObject;

    // ISensor
    public string SensorId { get { return sensorId; } }
    private bool _check;
    public bool Check
    {
        get
        {
            if (this.enabled == false) return false;
            return _check;
        }
    }

    void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject == targetObject)
        {
            _check = true;
        }
    }

    void OnTriggerExit(Collider collider)
    {
        if (collider.gameObject == targetObject)
        {
            _check = false;
        }
    }
}

public class TriggerReceiver : ISensor<bool>
{

    private GameObject _receiverObject;
    private TriggerSensor _triggerSensor;

    public string SensorId { get { return _triggerSensor.SensorId; } }
    public bool Check
    {
        get
        {
            if (enabled == false) return false;
            return _triggerSensor.Check;
        }
    }
    public bool enabled { get; set; } = true;

    public TriggerReceiver(GameObject receiverObject, string triggerAttribute)
    {
        _receiverObject = receiverObject;
        if (_receiverObject == null)
        {
            GlobalLogger.Error("", nameof(TriggerReceiver), nameof(TriggerReceiver), "receiverObject == null");
            return;
        }
        GameObject[] triggerObjects = Util.FindChildrenWithAttribute(_receiverObject, triggerAttribute);
        if (triggerObjects == null || triggerObjects.Length != 1)
        {
            GlobalLogger.Error(_receiverObject.name, nameof(TriggerReceiver), nameof(TriggerReceiver), "triggerObjects == null || triggerObjects.Length != 1");
            return;
        }
        _triggerSensor = triggerObjects[0].GetComponent<TriggerSensor>();
        if (_triggerSensor == null)
        {
            GlobalLogger.Error(_receiverObject.name, nameof(TriggerReceiver), nameof(TriggerReceiver), "triggerSensor == null");
            return;
        }
    }

}