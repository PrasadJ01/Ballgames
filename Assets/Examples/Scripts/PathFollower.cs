using UnityEngine;
using PathCreation;

namespace PathCreation.Examples
{
    /// <summary>
    /// Simple Path follower that moves a GameObject along a VertexPath (from PathCreator).
    /// This version does not depend on any external GameEvents class.
    /// - Use "playOnStart" to have it begin automatically when the GameObject is enabled.
    /// - Call StartFollow() / StopFollow() from other code (or wire to UI) to control it.
    /// - Supports constant speed or distance-over-time (i.e. non-constant speed via 'speed').
    /// - Safe to use in Edit Mode (ExecuteInEditMode attribute not required here).
    /// </summary>
    [AddComponentMenu("PathCreation/Examples/PathFollower (Fixed)")]
    public class PathFollower : MonoBehaviour
    {
        [Tooltip("The PathCreator component that provides the path")]
        public PathCreator pathCreator;

        [Tooltip("If true the follower will begin moving automatically when enabled")]
        public bool playOnStart = true;

        [Tooltip("Speed in world units per second (distance along the path)")]
        public float speed = 5f;

        [Tooltip("If true the follower will rotate to match path tangent")]
        public bool rotateToPath = true;

        [Tooltip("If true movement is simulated using rigidbody.MovePosition in FixedUpdate when a Rigidbody exists")]
        public bool useRigidbodyForMovement = true;

        [Tooltip("If true the follower will loop when reaching end of path")]
        public bool loop = true;

        [Tooltip("If true the object will be placed at the path's start when Play begins")]
        public bool snapToPathOnStart = true;

        float distanceTravelled = 0f;
        bool isRunning = false;
        VertexPath vPath;
        Rigidbody rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            SetupPath();
        }

        void OnValidate()
        {
            if (speed < 0f) speed = 0f;
        }

        void SetupPath()
        {
            if (pathCreator != null)
            {
                vPath = pathCreator.path;
                // Listen to path changes so we can re-sample if the path is edited
#if UNITY_EDITOR
                // PathCreator exposes an event; guard for null on older versions
                try
                {
                    pathCreator.pathUpdated -= OnPathUpdated;
                    pathCreator.pathUpdated += OnPathUpdated;
                }
                catch { }
#endif
            }
            else
            {
                vPath = null;
            }
        }

        void Start()
        {
            if (pathCreator == null)
            {
                Debug.LogWarning("[PathFollower] No PathCreator assigned. Assign a PathCreator to follow a path.");
                return;
            }

            if (snapToPathOnStart && vPath != null)
            {
                distanceTravelled = 0f;
                SetPositionOnPath(distanceTravelled);
            }

            if (playOnStart)
                StartFollow();
        }

        void OnEnable()
        {
            if (pathCreator != null)
                SetupPath();
        }

        void OnDisable()
        {
            StopFollow();
        }

        void OnDestroy()
        {
#if UNITY_EDITOR
            if (pathCreator != null)
            {
                try { pathCreator.pathUpdated -= OnPathUpdated; } catch { }
            }
#endif
        }

        void OnPathUpdated()
        {
            // re-sample path reference when path changes in-editor
            if (pathCreator != null) vPath = pathCreator.path;
            // optionally snap to path start when path changes
            if (snapToPathOnStart && vPath != null) SetPositionOnPath(distanceTravelled);
        }

        /// <summary> Begin following the path. </summary>
        public void StartFollow()
        {
            if (vPath == null)
            {
                SetupPath();
                if (vPath == null)
                {
                    Debug.LogWarning("[PathFollower] Cannot StartFollow - path is null.");
                    return;
                }
            }
            isRunning = true;
        }

        /// <summary> Stop following the path. </summary>
        public void StopFollow()
        {
            isRunning = false;
        }

        void Update()
        {
            // If using Rigidbody for movement, we handle motion in FixedUpdate
            if (useRigidbodyForMovement && rb != null) return;

            if (!isRunning || vPath == null) return;

            float delta = Time.deltaTime * speed;
            AdvanceDistance(delta);
            ApplyPositionAndRotation();
        }

        void FixedUpdate()
        {
            if (!useRigidbodyForMovement || rb == null) return;

            if (!isRunning || vPath == null) return;

            float delta = Time.fixedDeltaTime * speed;
            AdvanceDistance(delta);
            ApplyRigidbodyMovement();
        }

        void AdvanceDistance(float delta)
        {
            distanceTravelled += delta;
            if (loop)
            {
                if (vPath != null && vPath.length > 0f)
                {
                    // wrap-around
                    while (distanceTravelled > vPath.length) distanceTravelled -= vPath.length;
                    while (distanceTravelled < 0f) distanceTravelled += vPath.length;
                }
            }
            else
            {
                // clamp to path extents
                if (vPath != null)
                    distanceTravelled = Mathf.Clamp(distanceTravelled, 0f, vPath.length);
            }
        }

        void ApplyPositionAndRotation()
        {
            if (vPath == null) return;
            Vector3 p = vPath.GetPointAtDistance(distanceTravelled);
            Vector3 t = vPath.GetDirectionAtDistance(distanceTravelled).normalized;

            transform.position = p;

            if (rotateToPath)
            {
                // Align forward to tangent while keeping world up
                transform.rotation = Quaternion.LookRotation(t, Vector3.up);
            }
        }

        void ApplyRigidbodyMovement()
        {
            if (vPath == null || rb == null) return;
            Vector3 p = vPath.GetPointAtDistance(distanceTravelled);
            Vector3 t = vPath.GetDirectionAtDistance(distanceTravelled).normalized;

            rb.MovePosition(p);

            if (rotateToPath)
            {
                Quaternion rot = Quaternion.LookRotation(t, Vector3.up);
                rb.MoveRotation(rot);
            }
        }

        void SetPositionOnPath(float dist)
        {
            if (vPath == null) return;
            float d = Mathf.Clamp(dist, 0f, vPath.length);
            Vector3 p = vPath.GetPointAtDistance(d);
            Vector3 t = vPath.GetDirectionAtDistance(d).normalized;
            transform.position = p;
            if (rotateToPath) transform.rotation = Quaternion.LookRotation(t, Vector3.up);
        }

        /// <summary> Utility: set the follower to a fraction along the path [0..1] </summary>
        public void SetNormalizedPosition(float t)
        {
            if (vPath == null) return;
            t = Mathf.Clamp01(t);
            distanceTravelled = t * vPath.length;
            SetPositionOnPath(distanceTravelled);
        }

        /// <summary> Utility: returns current fraction (0..1) along path. </summary>
        public float GetNormalizedPosition()
        {
            if (vPath == null || vPath.length <= 0f) return 0f;
            return Mathf.Clamp01(distanceTravelled / vPath.length);
        }
    }
}
