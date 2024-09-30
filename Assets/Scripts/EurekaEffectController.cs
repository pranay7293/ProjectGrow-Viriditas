using UnityEngine;
using Photon.Pun;

public class EurekaEffectController : MonoBehaviourPunCallbacks
{
    public float effectDuration = 5f;
    public ParticleSystem beamParticles;
    public float beamRadius = 2f;
    public float beamHeight = 50f;

    private MeshRenderer beamRenderer;
    private Material beamMaterial;
    private ParticleSystemRenderer particleRenderer;
    private Color locationColor;

    public void Initialize(Color color)
    {
        locationColor = color;
        SetupBeam();
        SetupParticles();
        Invoke("DestroyEffect", effectDuration);
    }

    private void SetupBeam()
    {
        beamRenderer = GetComponentInChildren<MeshRenderer>();
        beamMaterial = beamRenderer.material;
        beamMaterial.SetColor("_BaseColor", new Color(locationColor.r, locationColor.g, locationColor.b, 0.5f));
        beamMaterial.SetColor("_EmissionColor", locationColor * 2f);

        Transform beamTransform = beamRenderer.transform;
        beamTransform.localScale = new Vector3(beamRadius, beamHeight / 2f, beamRadius);
        beamTransform.localPosition = new Vector3(0, beamHeight / 2f, 0);
    }

   private void SetupParticles()
{
    particleRenderer = beamParticles.GetComponent<ParticleSystemRenderer>();
    var particleMaterial = particleRenderer.material;
    particleMaterial.SetColor("_BaseColor", new Color(locationColor.r, locationColor.g, locationColor.b, 0.3f));
    particleMaterial.SetColor("_EmissionColor", locationColor * 1.5f);

    var main = beamParticles.main;
    main.startColor = new Color(1f, 1f, 1f, 0.5f);
    main.startLifetime = beamHeight / main.startSpeed.constant; // Adjust lifetime based on speed
    main.startSpeed = 0.1f;
    main.simulationSpace = ParticleSystemSimulationSpace.Local;

    var emission = beamParticles.emission;
    emission.rateOverTime = 50f;

    var shape = beamParticles.shape;
    shape.shapeType = ParticleSystemShapeType.Cone;
    shape.angle = 0f;
    shape.radius = beamRadius * 0.9f;
    shape.length = beamHeight;
    shape.position = new Vector3(0, beamHeight / 2, 0); // Move emission point to center of beam

    var sizeOverLifetime = beamParticles.sizeOverLifetime;
    sizeOverLifetime.enabled = true;
    sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(0.5f, 1f);

    beamParticles.transform.localPosition = Vector3.zero;
    beamParticles.transform.localRotation = Quaternion.identity;
}

    private void DestroyEffect()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}