using UnityEngine;

public class StageDoor : MonoBehaviour
{
    [Header("Door")]
    [SerializeField] private int doorSection = 1;
    [SerializeField] private Rigidbody2D targetRigidbody2D;
    [SerializeField] private Rigidbody targetRigidbody3D;

    private bool _isOpen;

    private void Awake()
    {
        if (!targetRigidbody2D)
        {
            targetRigidbody2D = GetComponent<Rigidbody2D>();
        }

        if (!targetRigidbody3D)
        {
            targetRigidbody3D = GetComponent<Rigidbody>();
        }
    }

    private void OnEnable()
    {
        RefreshDoorState(force: true);
    }

    private void Update()
    {
        RefreshDoorState(force: false);
    }

    private void RefreshDoorState(bool force)
    {
        if (!GameManager.Instance)
        {
            return;
        }

        bool shouldOpen = GameManager.Instance.CurrentStage == doorSection;
        if (!force && shouldOpen == _isOpen)
        {
            return;
        }

        _isOpen = shouldOpen;
        SetDoorBlockEnabled(!_isOpen);
    }

    private void SetDoorBlockEnabled(bool enabled)
    {
        if (targetRigidbody2D)
        {
            targetRigidbody2D.simulated = enabled;
        }

        if (targetRigidbody3D)
        {
            targetRigidbody3D.detectCollisions = enabled;
            targetRigidbody3D.isKinematic = !enabled;
        }
    }
}
