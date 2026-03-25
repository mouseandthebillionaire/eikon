using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EdgeCollider2D))]

public class Ring : MonoBehaviour
{
    [Header("Ring geometry")]
    [SerializeField] private float innerRadius = 0.4f;
    [SerializeField] private float outerRadius = 0.5f;
    [Tooltip("Angles in degrees where openings start and end. Each pair = one opening. E.g. 0,90 = opening from 0° to 90°.")]
    [SerializeField] private float[] openingAngles = { 0f, 90f };

    [Header("Collider generation")]
    [Tooltip("Points per 90° of arc for smoother collision. Increase for larger rings.")]
    [SerializeField] private int pointsPerQuarterCircle = 8;

    [Header("FSR / Rotation")]
    [Tooltip("Pressure on this FSR rotates the ring clockwise. If null, a random FSR is assigned in Start.")]
    [SerializeField] private FSR fsrClockwise;
    [Tooltip("Pressure on this FSR rotates the ring anticlockwise. If null, a different random FSR is assigned in Start.")]
    [SerializeField] private FSR fsrAnticlockwise;
    [Tooltip("Rotation speed in degrees per second at full pressure (per FSR).")]
    [SerializeField] private float rotationSpeed = 90f;

    private const float Deg2Rad = Mathf.PI / 180f;

    private static readonly int OpeningCountId = Shader.PropertyToID("_OpeningCount");
    private static readonly int Opening0Id = Shader.PropertyToID("_Opening0");
    private static readonly int Opening1Id = Shader.PropertyToID("_Opening1");

    // Animate the Rings
    private SpriteRenderer sr;
    private Sprite[] frames;
    public float frameRate = 10f;

    private void Awake()
    {
        RebuildColliders();
        ApplyOpeningToVisual();
    }

    private void Start()
    {
        if (fsrClockwise == null || fsrAnticlockwise == null)
        {
            FSR[] availableFSRs = FindObjectsOfType<FSR>();
            if (availableFSRs.Length == 1)
            {
                if (fsrClockwise == null) fsrClockwise = availableFSRs[0];
                fsrAnticlockwise = null; // only one FSR: anticlockwise stays unassigned
            }
            else if (availableFSRs.Length >= 2)
            {
                if (fsrClockwise == null)
                {
                    fsrClockwise = availableFSRs[Random.Range(0, availableFSRs.Length)];
                }
                if (fsrAnticlockwise == null)
                {
                    var others = System.Array.FindAll(availableFSRs, f => f != fsrClockwise);
                    fsrAnticlockwise = others[Random.Range(0, others.Length)];
                }
            }
        }

        // Set a Random Rotation
        transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        // Animate the Rings
        sr = GetComponent<SpriteRenderer>();
        frames = Resources.LoadAll<Sprite>("Circles");
        StartCoroutine(RingAnimation());
    }

    public IEnumerator ResetRing(){
        // Fade out the ring
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color originalColor = sr.color;
        Color targetColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        float fadeDuration = 1f;
        float elapsedTime = 0f;
        while(elapsedTime < fadeDuration){
            elapsedTime += Time.deltaTime;
            sr.color = Color.Lerp(originalColor, targetColor, elapsedTime / fadeDuration);
            yield return null;
        }
        sr.color = targetColor;

        yield return new WaitForSeconds(3f);
        // Reset the rotation
        transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        // Rebuild the colliders
        RebuildColliders();
        // Apply the openings to the visual
        ApplyOpeningToVisual();
        // Fade in the ring
        sr.color = originalColor;
        elapsedTime = 0f;
        while(elapsedTime < fadeDuration){
            elapsedTime += Time.deltaTime;
            sr.color = Color.Lerp(targetColor, originalColor, elapsedTime / fadeDuration);
            yield return null;
        }
        sr.color = originalColor;
        yield return null;
    }

    private IEnumerator RingAnimation()
    {
        int currentFrame = Random.Range(0, frames.Length);
        sr.sprite = frames[currentFrame];
        yield return new WaitForSeconds(1f / frameRate);
        StartCoroutine(RingAnimation());
    }

    private void Update()
    {
        float clockwisePressure = fsrClockwise != null ? fsrClockwise.modifiedForce : 0f;
        float anticlockwisePressure = fsrAnticlockwise != null ? fsrAnticlockwise.modifiedForce : 0f;
        float netPressure = clockwisePressure - anticlockwisePressure;
        if (Mathf.Abs(netPressure) < 0.01f) return;
        float delta = rotationSpeed * netPressure * Time.deltaTime;
        transform.Rotate(0f, 0f, -delta); // positive net = clockwise in Unity 2D (negative Z)
    }

    public void RebuildColliders()
    {
        // Remove any existing EdgeCollider2D we may have added (e.g. for multiple segments)
        var existing = GetComponents<EdgeCollider2D>();
        for (int i = 1; i < existing.Length; i++)
            Destroy(existing[i]);

        if (openingAngles == null || openingAngles.Length < 2)
        {
            Debug.LogWarning("Ring: need at least two angles (start, end) for one opening.", this);
            return;
        }

        // Parse openings as pairs: [start0, end0, start1, end1, ...]
        var openings = new List<(float startDeg, float endDeg)>();
        for (int i = 0; i + 1 < openingAngles.Length; i += 2)
        {
            float start = Mathf.Repeat(openingAngles[i], 360f);
            float end = Mathf.Repeat(openingAngles[i + 1], 360f);
            openings.Add((start, end));
        }

        if (openings.Count == 0)
        {
            Debug.LogWarning("Ring: no valid opening pairs.", this);
            return;
        }

        // Sort openings by start angle and merge overlapping
        openings.Sort((a, b) => a.startDeg.CompareTo(b.startDeg));

        // Build one closed path per "segment" (the solid part between openings)
        float fullCircle = 360f;
        int segmentIndex = 0;

        for (int i = 0; i < openings.Count; i++)
        {
            float segStart = openings[i].endDeg;   // segment starts where this opening ends
            float segEnd = openings[(i + 1) % openings.Count].startDeg; // segment ends where next opening starts

            // Handle wrap-around
            float segSpan = segStart <= segEnd ? segEnd - segStart : (360f - segStart) + segEnd;
            if (segSpan <= 0.001f) continue; // no solid segment between these openings

            Vector2[] points = BuildSegmentPoints(segStart, segEnd, segSpan);
            if (points == null || points.Length < 3) continue;

            EdgeCollider2D edge = segmentIndex == 0 ? GetComponent<EdgeCollider2D>() : gameObject.AddComponent<EdgeCollider2D>();
            edge.points = points;
            edge.usedByComposite = false;

            segmentIndex++;
        }

        if (segmentIndex == 0)
            Debug.LogWarning("Ring: openings cover full circle; no collider generated.", this);

        ApplyOpeningToVisual();
    }

    /// <summary>
    /// Updates the ring sprite material so openings are visible (pixels in opening angles are clipped).
    /// Requires the SpriteRenderer to use a material with the "2D/Ring With Openings" shader.
    /// </summary>
    public void ApplyOpeningToVisual()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sharedMaterial == null) return;

        Material mat = sr.material; // use instance so we don't modify the asset
        if (!mat.HasProperty(OpeningCountId)) return;

        if (openingAngles == null || openingAngles.Length < 2)
        {
            mat.SetFloat(OpeningCountId, 0f);
            return;
        }

        int pairCount = openingAngles.Length / 2;
        int count = Mathf.Min(pairCount, 4);
        mat.SetFloat(OpeningCountId, count);

        // _Opening0 = (start0, end0, start1, end1), _Opening1 = (start2, end2, start3, end3)
        if (count > 0)
        {
            float s0 = Mathf.Repeat(openingAngles[0], 360f);
            float e0 = Mathf.Repeat(openingAngles[1], 360f);
            if (count == 1)
                mat.SetVector(Opening0Id, new Vector4(s0, e0, 0, 0));
            else
            {
                float s1 = Mathf.Repeat(openingAngles[2], 360f);
                float e1 = Mathf.Repeat(openingAngles[3], 360f);
                mat.SetVector(Opening0Id, new Vector4(s0, e0, s1, e1));
                if (count > 2)
                {
                    float s2 = Mathf.Repeat(openingAngles[4], 360f);
                    float e2 = Mathf.Repeat(openingAngles[5], 360f);
                    mat.SetVector(Opening1Id, count > 3
                        ? new Vector4(s2, e2, Mathf.Repeat(openingAngles[6], 360f), Mathf.Repeat(openingAngles[7], 360f))
                        : new Vector4(s2, e2, 0, 0));
                }
            }
        }
    }

    private Vector2[] BuildSegmentPoints(float startDeg, float endDeg, float spanDeg)
    {
        int pointCount = Mathf.Max(2, Mathf.RoundToInt(pointsPerQuarterCircle * (spanDeg / 90f)));
        var points = new List<Vector2>(pointCount * 2 + 4);

        float startRad = startDeg * Deg2Rad;
        float spanRad = spanDeg * Deg2Rad;
        // End of arc in radians (start + span, so we sweep the SOLID segment, not the short way)
        float endRad = startRad + spanRad;

        // Outer arc: start -> start+span (the solid part)
        for (int i = 0; i <= pointCount; i++)
        {
            float t = i / (float)pointCount;
            float angle = Mathf.Lerp(startRad, endRad, t);
            points.Add(new Vector2(outerRadius * Mathf.Cos(angle), outerRadius * Mathf.Sin(angle)));
        }

        // Radial: outer at end -> inner at end
        points.Add(new Vector2(innerRadius * Mathf.Cos(endRad), innerRadius * Mathf.Sin(endRad)));

        // Inner arc: end -> start (reverse along the same solid segment)
        for (int i = pointCount - 1; i >= 0; i--)
        {
            float t = i / (float)pointCount;
            float angle = Mathf.Lerp(startRad, endRad, t);
            points.Add(new Vector2(innerRadius * Mathf.Cos(angle), innerRadius * Mathf.Sin(angle)));
        }

        // Radial: inner at start -> outer at start (close the loop)
        points.Add(new Vector2(outerRadius * Mathf.Cos(startRad), outerRadius * Mathf.Sin(startRad)));

        return points.ToArray();
    }

    /// <summary>
    /// Set a single opening (one gap) in degrees. Rebuilds colliders.
    /// </summary>
    public void SetSingleOpening(float startDeg, float endDeg)
    {
        openingAngles = new float[] { startDeg, endDeg };
        RebuildColliders();
        ApplyOpeningToVisual();
    }

    /// <summary>
    /// Set multiple openings. Each pair of values in the array is one opening (start, end) in degrees.
    /// Rebuilds colliders.
    /// </summary>
    public void SetOpenings(float[] startEndPairsInDegrees)
    {
        openingAngles = startEndPairsInDegrees;
        RebuildColliders();
        ApplyOpeningToVisual();
    }

    /// <summary>
    /// Randomize one opening of random size. Rebuilds colliders.
    /// </summary>
    public void RandomizeSingleOpening(float minSpanDeg = 45f, float maxSpanDeg = 120f)
    {
        float span = Random.Range(minSpanDeg, maxSpanDeg);
        float start = Random.Range(0f, 360f - span);
        SetSingleOpening(start, start + span);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && openingAngles != null && openingAngles.Length >= 2)
        {
            RebuildColliders();
            ApplyOpeningToVisual();
        }
    }
#endif
}
