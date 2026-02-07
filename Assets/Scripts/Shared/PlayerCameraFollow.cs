using UnityEngine;
using Player;

[DisallowMultipleComponent]
public class PlayerCameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private bool autoFindPlayer = true;

    [Header("Follow")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    [SerializeField, Min(0f)] private float smoothTime = 0.1f;

    private Vector3 _velocity;

    private void Awake()
    {
        TryResolveTarget();
    }

    private void LateUpdate()
    {
        if (!target)
        {
            TryResolveTarget();
            if (!target) return;
        }

        var desiredPosition = target.position + offset;

        if (smoothTime <= 0f)
        {
            transform.position = desiredPosition;
            return;
        }

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref _velocity,
            smoothTime
        );
    }

    private void TryResolveTarget()
    {
        if (target || !autoFindPlayer) return;

        var playerCharacter = FindObjectOfType<PlayerCharacter>();
        if (playerCharacter)
        {
            target = playerCharacter.transform;
        }
    }
}
