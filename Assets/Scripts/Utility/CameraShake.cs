using UnityEngine;
using System.Collections;

public class CameraShake : Singleton<CameraShake>
{
	// Transform of the camera to shake. Grabs the gameObject's transform
	// if null.
	public Transform camTransform;

	// How long the object should shake for.
	public float shakeDuration = 0f;

	// Amplitude of the shake. A larger value shakes the camera harder.
	public float shakeAmount = 0.7f;
	public float decreaseFactor = 1.0f;

	Vector3 originalPos;

	public override void Awake()
	{
		base.Awake();

		if (camTransform == null)
		{
			camTransform = transform;
		}

		GameManager.instance.EventManager.AddListener<PerkSelectionEvent>(OnDisplayPerkSelection);
	}

    private void OnDisplayPerkSelection(PerkSelectionEvent obj)
    {
		// stop shaking if any
		shakeDuration = 0;
    }

    public void SetShake(float duration)
    {
		shakeDuration = duration;
    }

	private void OnEnable()
	{
		originalPos = camTransform.localPosition;
	}

	private void Update()
	{
		if (shakeDuration > 0)
		{
			camTransform.localPosition = originalPos + Random.insideUnitSphere * shakeAmount;

			shakeDuration -= Time.deltaTime * decreaseFactor;
		}
		else
		{
			shakeDuration = 0f;
			camTransform.localPosition = originalPos;
		}
	}
}