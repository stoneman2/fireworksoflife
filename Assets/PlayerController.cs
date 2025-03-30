using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.VFX;
using UnityEngine.UI;

/// <summary>
/// A simple FPP (First Person Perspective) camera rotation script.
/// Like those found in most FPS (First Person Shooter) games.
/// </summary>
public class PlayerController : MonoBehaviour {

	public float Sensitivity {
		get { return sensitivity; }
		set { sensitivity = value; }
	}
	[Range(0.1f, 9f)][SerializeField] float sensitivity = 2f;
	[Tooltip("Limits vertical camera rotation. Prevents the flipping that happens when rotation goes above 90.")]
	[Range(0f, 90f)][SerializeField] float yRotationLimit = 85f;
    [Range(0f, 90f)][SerializeField] float xRotationLimit = 85f;
    [SerializeField] VisualEffect visualEffect;
    [SerializeField] Volume volume;
    [SerializeField] GameObject fadeOut;
    [SerializeField] bool gameOver = false;
	Vector2 rotation = Vector2.zero;
	const string xAxis = "Mouse X"; //Strings in direct code generate garbage, storing and re-using them creates no garbage
	const string yAxis = "Mouse Y";
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioSource audioSource2;
    
    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        StartCoroutine(OldAgeCoroutine());
    }

    void PlayCustomOneShot(AudioSource source, AudioClip clip) {
        if (source != null && clip != null) {
            // Change pitch, sound is different each time
            source.pitch = Random.Range(0.95f, 1.05f);
            source.PlayOneShot(clip);
        }
    }

    void Update(){
        // Scroll wheel to change sensitivity
        if (Input.GetAxis("Mouse ScrollWheel") > 0f && sensitivity < 9f) {
            sensitivity += 0.1f;
        } else if (Input.GetAxis("Mouse ScrollWheel") < 0f && sensitivity > 0.1f) {
            sensitivity -= 0.1f;
        }

        // If we press any key, or even any mouse buttons, start the visual effect
        if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) {
            visualEffect.Play();
            // Play the audio clip
            if (audioSource != null) {
                PlayCustomOneShot(audioSource, audioSource.clip);

                if (audioSource2 != null) {
                    StartCoroutine(PlayDelayedAudio(2f));
                }
            }

            if (gameOver) {
                // Reload the scene
                UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            }
        }

		rotation.x += Input.GetAxis(xAxis) * sensitivity;
        rotation.x = Mathf.Clamp(rotation.x, -xRotationLimit, xRotationLimit);
		rotation.y += Input.GetAxis(yAxis) * sensitivity;
		rotation.y = Mathf.Clamp(rotation.y, -yRotationLimit, yRotationLimit);
		var xQuat = Quaternion.AngleAxis(rotation.x, Vector3.up);
		var yQuat = Quaternion.AngleAxis(rotation.y, Vector3.left);

		transform.localRotation = xQuat * yQuat; //Quaternions seem to rotate more consistently than EulerAngles. Sensitivity seemed to change slightly at certain degrees using Euler. transform.localEulerAngles = new Vector3(-rotation.y, rotation.x, 0);
	}

    private IEnumerator PlayDelayedAudio(float delay) {
        yield return new WaitForSeconds(delay);
        if (audioSource2 != null) {
            PlayCustomOneShot(audioSource2, audioSource2.clip);
        }
    }

    private IEnumerator OldAgeCoroutine()
    {
        yield return new WaitForSeconds(40f);

        // After 40 seconds, we begin some effects:
        // Start adding bloom's intensity until 1
        // Start adding vignette's intensity until 1
        // When we finally reach 60, should just be 1!
        Debug.Log("Starting old age effects...");
        volume.profile.TryGet<Bloom>(out Bloom bloom);
        volume.profile.TryGet<Vignette>(out Vignette vignette);
        bloom.intensity.Override(0f);
        vignette.intensity.Override(0f);

        // Okay, start slowly increasing the values, this will take 20 seconds
        float time = 0f;
        float duration = 20f;
        float startBloom = bloom.intensity.value;
        float startVignette = vignette.intensity.value;
        float endBloom = 1f;
        float endVignette = 1f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            bloom.intensity.Override(Mathf.Lerp(startBloom, endBloom, t));
            vignette.intensity.Override(Mathf.Lerp(startVignette, endVignette, t));
            yield return null;
        }
        // Finally, set the values to 1
        bloom.intensity.Override(1f);
        vignette.intensity.Override(1f);

        // Fade to black!
        fadeOut.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        fadeOut.SetActive(true);
        
        time = 0f;
        duration = 2f;
        Color startColor = fadeOut.GetComponent<Image>().color;
        Color endColor = new Color(0, 0, 0, 255);
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            fadeOut.GetComponent<Image>().color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }
        // Finally, set the color to black
        fadeOut.GetComponent<Image>().color = new Color(0, 0, 0, 255);

        // Give people 5 seconds, and then we can restart
        yield return new WaitForSeconds(5f);
        gameOver = true;
    }
}