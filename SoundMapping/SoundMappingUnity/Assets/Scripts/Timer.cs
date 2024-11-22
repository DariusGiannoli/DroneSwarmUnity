using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI; // Import for TextMeshPro components

public class Timer : MonoBehaviour
{
    public TextMeshProUGUI timerText; // Reference to a TextMeshProUGUI component for displaying the timer
    public TextMeshProUGUI timerTextNetwork; // Reference to a TextMeshProUGUI component for displaying the timer
    private float elapsedTime = 0f; // To store the elapsed time
    private float elapsedTimeNetwork = 0f; // To store the elapsed time
    private Coroutine timerCoroutine; // Reference to the running coroutine
    private Coroutine timerCoroutineNetwork; // Reference to the running coroutine

    // Start is called before the first frame update
    void Start()
    {
        // Initialize the timer display
        UpdateTimerDisplay();
        UpdateTimerDisplayNetwork();
    }

    // Public method to start the timer
    public void StartTimer()
    {
        if (timerCoroutine == null)
        {
            timerCoroutine = StartCoroutine(TimerCoroutine());
        }
    }

    // Public method to stop the timer
    public void StopTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
    }

    public void StartTimerNetwork()
    {
        if (elapsedTime == 0) // if elapsed time is 0 then just return and do not start the timer
        {
            return;
        }
        if (timerCoroutineNetwork == null)
        {
            timerCoroutineNetwork = StartCoroutine(TimerCoroutineNetwork());
        }
    }

    public void StopTimerNetwork()
    {
        if (timerCoroutineNetwork != null)
        {
            StopCoroutine(timerCoroutineNetwork);
            timerCoroutineNetwork = null;
        }
    }

    // Coroutine to update the timer
    private IEnumerator TimerCoroutine()
    {
        while (true)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerDisplay();
            yield return null; // Wait for the next frame
        }
    }

    private IEnumerator TimerCoroutineNetwork()
    {
        while (true)
        {
            elapsedTimeNetwork += Time.deltaTime;
            UpdateTimerDisplayNetwork();
            yield return null; // Wait for the next frame
        }
    }

    // Update the timer display with two decimal places
    private void UpdateTimerDisplay()
    {
        timerText.text = elapsedTime.ToString("F2");
    }

    private void UpdateTimerDisplayNetwork()
    {
        timerTextNetwork.text = elapsedTimeNetwork.ToString("F2");
    }
}
