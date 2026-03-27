using UnityEngine;

public class EngineParticleController : MonoBehaviour
{
    [Header("Engine Root — drag your engine parent GameObject here")]
    public GameObject engineRoot;

    [Header("Particle Positions (local offset from engine centre)")]
    public Vector3 intakeOffset    = new Vector3(0f,  0f,  1.5f);  // front
    public Vector3 exhaustOffset   = new Vector3(0f,  0f, -1.5f);  // rear
    public Vector3 bypassLeftOffset  = new Vector3(-1f, 0f, 0f);   // left side
    public Vector3 bypassRightOffset = new Vector3( 1f, 0f, 0f);   // right side

    private ParticleSystem intakePS;
    private ParticleSystem exhaustPS;
    private ParticleSystem bypassLeftPS;
    private ParticleSystem bypassRightPS;

    private bool isRunning = false;

    void Start()
    {
        if (engineRoot == null)
        {
            Debug.LogError("[EngineParticles] Engine root not assigned!");
            return;
        }

        intakePS      = CreateIntakeParticles();
        exhaustPS     = CreateExhaustParticles();
        bypassLeftPS  = CreateBypassParticles(bypassLeftOffset,  new Vector3(0f, 0f, -90f));
        bypassRightPS = CreateBypassParticles(bypassRightOffset, new Vector3(0f, 0f,  90f));

        StopAll();
    }

    // ── Called by EngineStartupController when engine starts ─────────────────
    public void OnEngineStarted(float n1Fraction)
    {
        isRunning = true;
        PlayAll();
        UpdateIntensity(n1Fraction);
    }

    // ── Called by EngineStartupController when engine stops ──────────────────
    public void OnEngineStopped()
    {
        isRunning = false;
        StopAll();
    }

    // ── Call this every frame from EngineStartupController with 0..1 value ───
    public void UpdateIntensity(float n1Fraction)
    {
        if (!isRunning) return;

        // Intake — subtle inward spiral, scales with RPM
        SetEmissionRate(intakePS,     Mathf.Lerp(5f,  40f,  n1Fraction));
        SetSpeed(intakePS,            Mathf.Lerp(0.5f, 3f,  n1Fraction));

        // Exhaust — heat shimmer, strong at high RPM
        SetEmissionRate(exhaustPS,    Mathf.Lerp(10f, 80f,  n1Fraction));
        SetSpeed(exhaustPS,           Mathf.Lerp(1f,  6f,   n1Fraction));

        // Bypass ducts — side airflow
        SetEmissionRate(bypassLeftPS,  Mathf.Lerp(5f, 30f,  n1Fraction));
        SetEmissionRate(bypassRightPS, Mathf.Lerp(5f, 30f,  n1Fraction));
        SetSpeed(bypassLeftPS,         Mathf.Lerp(0.5f, 4f, n1Fraction));
        SetSpeed(bypassRightPS,        Mathf.Lerp(0.5f, 4f, n1Fraction));
    }

    // ── INTAKE PARTICLES ──────────────────────────────────────────────────────
    private ParticleSystem CreateIntakeParticles()
    {
        GameObject go = new GameObject("IntakeParticles");
        go.transform.SetParent(engineRoot.transform, false);
        go.transform.localPosition = intakeOffset;
        go.transform.localRotation = Quaternion.Euler(180f, 0f, 0f); // pointing inward

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop          = true;
        main.startLifetime = 0.6f;
        main.startSpeed    = 1f;
        main.startSize     = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.startColor    = new Color(0.8f, 0.9f, 1f, 0.3f); // pale blue-white
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 20f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius    = 0.3f;

        // Fade out over lifetime
        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.8f, 0.9f, 1f), 0f),
                new GradientColorKey(new Color(0.8f, 0.9f, 1f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.4f, 0f),
                new GradientAlphaKey(0f,   1f)
            }
        );
        colorOverLife.color = g;

        return ps;
    }

    // ── EXHAUST PARTICLES (heat shimmer) ──────────────────────────────────────
    private ParticleSystem CreateExhaustParticles()
    {
        GameObject go = new GameObject("ExhaustParticles");
        go.transform.SetParent(engineRoot.transform, false);
        go.transform.localPosition = exhaustOffset;
        go.transform.localRotation = Quaternion.Euler(0f, 0f, 0f); // pointing rearward

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop          = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize     = new ParticleSystem.MinMaxCurve(0.03f, 0.1f);
        main.startColor    = new Color(1f, 0.6f, 0.2f, 0.4f); // amber/orange heat
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Add some turbulence
        var noise = ps.noise;
        noise.enabled   = true;
        noise.strength  = 0.3f;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.5f;

        var emission = ps.emission;
        emission.rateOverTime = 40f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius    = 0.2f;

        // Color over lifetime — orange → transparent
        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.8f, 0.3f), 0f),
                new GradientColorKey(new Color(1f, 0.3f, 0.1f), 0.5f),
                new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.5f, 0f),
                new GradientAlphaKey(0.3f, 0.5f),
                new GradientAlphaKey(0f,   1f)
            }
        );
        colorOverLife.color = g;

        // Size grows over lifetime
        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.3f);
        curve.AddKey(1f, 1f);
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, curve);

        return ps;
    }

    // ── BYPASS DUCT PARTICLES ─────────────────────────────────────────────────
    private ParticleSystem CreateBypassParticles(Vector3 localOffset, Vector3 eulerRotation)
    {
        GameObject go = new GameObject("BypassParticles");
        go.transform.SetParent(engineRoot.transform, false);
        go.transform.localPosition = localOffset;
        go.transform.localRotation = Quaternion.Euler(eulerRotation);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop          = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startSize     = new ParticleSystem.MinMaxCurve(0.01f, 0.04f);
        main.startColor    = new Color(0.85f, 0.9f, 1f, 0.25f); // pale blue bypass air
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 15f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Rectangle;
        shape.scale     = new Vector3(0.1f, 0.4f, 0f);

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.85f, 0.9f, 1f), 0f),
                new GradientColorKey(new Color(0.85f, 0.9f, 1f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.3f, 0f),
                new GradientAlphaKey(0f,   1f)
            }
        );
        colorOverLife.color = g;

        return ps;
    }

    // ── HELPERS ───────────────────────────────────────────────────────────────
    private void PlayAll()
    {
        intakePS?.Play();
        exhaustPS?.Play();
        bypassLeftPS?.Play();
        bypassRightPS?.Play();
    }

    private void StopAll()
    {
        intakePS?.Stop();
        exhaustPS?.Stop();
        bypassLeftPS?.Stop();
        bypassRightPS?.Stop();
    }

    private void SetEmissionRate(ParticleSystem ps, float rate)
    {
        if (ps == null) return;
        var emission = ps.emission;
        emission.rateOverTime = rate;
    }

    private void SetSpeed(ParticleSystem ps, float speed)
    {
        if (ps == null) return;
        var main = ps.main;
        main.startSpeed = speed;
    }
}