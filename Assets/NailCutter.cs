using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NailCutter : MonoBehaviour
{
    public GameObject[] nails;
    public Transform[] toePoints;
    public Transform cameraTarget;

    
    public AudioSource audioSource;
    public AudioClip cutSound;

    private int currentIndex = 0;
    private int currentToe = 0;
    private bool isFocusingToe = true;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            HandleInput();
        }
    }

    void HandleInput()
    {
        if (isFocusingToe)
        {
            FocusToe(currentToe);
            isFocusingToe = false;
            return;
        }

        if (currentIndex < nails.Length)
        {
           
            PlayCutSound();

            CutNail(nails[currentIndex]);
            currentIndex++;

            if (IsToeFinished())
            {
                currentToe++;
                isFocusingToe = true;
            }
        }
    }

    void FocusToe(int toeIndex)
    {
        cameraTarget.position = toePoints[toeIndex].position;
    }

    
    void PlayCutSound()
    {
        if (audioSource != null && cutSound != null)
        {
           
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(cutSound);
        }
    }

    void CutNail(GameObject nail)
    {
        GameObject fallingNail = Instantiate(
            nail,
            nail.transform.position,
            nail.transform.rotation,
            nail.transform.parent
        );

        nail.SetActive(false);

        if (fallingNail.GetComponent<RectTransform>() != null)
        {
            StartCoroutine(FallUI(fallingNail));
        }
        else
        {
            StartCoroutine(FallWorld(fallingNail));
        }
    }

    
    IEnumerator FallUI(GameObject obj)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();

        float speedY = -300f;
        float speedX = Random.Range(-100f, 100f);
        float rotation = Random.Range(-200f, 200f);

        float time = 0f;

        while (time < 1.2f)
        {
            rect.anchoredPosition += new Vector2(speedX, speedY) * Time.deltaTime;
            rect.Rotate(0, 0, rotation * Time.deltaTime);

            time += Time.deltaTime;
            yield return null;
        }

        Destroy(obj);
    }

    
    IEnumerator FallWorld(GameObject obj)
    {
        float speedY = -2f;
        float speedX = Random.Range(-1f, 1f);
        float rotation = Random.Range(-200f, 200f);

        float time = 0f;

        while (time < 1.2f)
        {
            obj.transform.position += new Vector3(speedX, speedY, 0) * Time.deltaTime;
            obj.transform.Rotate(0, 0, rotation * Time.deltaTime);

            time += Time.deltaTime;
            yield return null;
        }

        Destroy(obj);
    }

    bool IsToeFinished()
    {
        if (currentToe == 0 && currentIndex == 3) return true;
        if (currentToe == 1 && currentIndex == 5) return true;
        if (currentToe == 2 && currentIndex == 9) return true;
        if (currentToe == 3 && currentIndex == 11) return true;
        if (currentToe == 4 && currentIndex == 14) return true;

        return false;
    }
}