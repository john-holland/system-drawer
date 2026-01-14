using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ModulatingSoundComponent : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioClip audioClip;
    [SerializeField] private bool playOnStart = false;
    [SerializeField] private bool loop = false;
    
    [Header("Stereo Width Control")]
    [SerializeField] [Range(0f, 2f)] private float totalWidth = 1f;
    [SerializeField] private bool modulateWidth = false;
    [SerializeField] private float widthModulationSpeed = 0.5f;
    [SerializeField] private float widthModulationDepth = 0.3f;
    [SerializeField] private AnimationCurve widthModulationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private bool useWidthCurve = false;
    
    public enum Dimension
    {
        X,
        Y,
        Z
    }
    
    [Header("Dimension Control")]
    [SerializeField] private bool updateGameObjectDimension = true;
    [SerializeField] private Dimension dimensionToSet = Dimension.X;
    [SerializeField] private float widthSmoothing = 0.1f;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogging = false;
    
    private AudioSource audioSource;
    private float widthModulationTime = 0f;
    
    // DSP buffer for processing
    private float[] leftChannelBuffer;
    private float[] rightChannelBuffer;
    
    // Dimension control
    private float startingDimensionValue = 1f;
    private float currentTargetWidth = 1f;
    private Vector3 originalScale;
    private bool hasStartedPlaying = false;
    
    // Logging throttling
    private int dspCallCount = 0;
    private float lastLoggedWidth = -1f;
    
    // Width smoothing
    private float smoothedCalculatedWidth = 1f;
    [SerializeField] private float widthSmoothingDSP = 0.1f;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        
        // Store original scale
        originalScale = transform.localScale;
        currentTargetWidth = originalScale.x;
        
        // Store starting dimension value based on selected dimension
        switch (dimensionToSet)
        {
            case Dimension.X:
                startingDimensionValue = originalScale.x;
                break;
            case Dimension.Y:
                startingDimensionValue = originalScale.y;
                break;
            case Dimension.Z:
                startingDimensionValue = originalScale.z;
                break;
        }
        
        if (enableDebugLogging)
        {
            Debug.Log($"[ModulatingSound] Start - Dimension: {dimensionToSet}, Starting Value: {startingDimensionValue}, Original Scale: {originalScale}, Update Dimension: {updateGameObjectDimension}");
        }
        
        // Initialize width modulation curve if not set
        if (widthModulationCurve == null || widthModulationCurve.keys.Length == 0)
        {
            widthModulationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }
        
        if (audioClip != null)
        {
            audioSource.clip = audioClip;
            audioSource.loop = loop;
        }
        
        if (playOnStart && audioClip != null)
        {
            audioSource.Play();
        }
    }
    
    void Update()
    {
        // Update width modulation time
        widthModulationTime += Time.deltaTime * widthModulationSpeed;
        
        // Update GameObject dimension based on DSP totalWidth
        if (updateGameObjectDimension && hasStartedPlaying)
        {
            Vector3 currentScale = transform.localScale;
            float currentDimensionValue = 0f;
            float smoothedDimensionValue = 0f;
            
            // Get current dimension value
            switch (dimensionToSet)
            {
                case Dimension.X:
                    currentDimensionValue = currentScale.x;
                    smoothedDimensionValue = Mathf.Lerp(currentDimensionValue, currentTargetWidth, widthSmoothing);
                    transform.localScale = new Vector3(smoothedDimensionValue, originalScale.y, originalScale.z);
                    break;
                case Dimension.Y:
                    currentDimensionValue = currentScale.y;
                    smoothedDimensionValue = Mathf.Lerp(currentDimensionValue, currentTargetWidth, widthSmoothing);
                    transform.localScale = new Vector3(originalScale.x, smoothedDimensionValue, originalScale.z);
                    break;
                case Dimension.Z:
                    currentDimensionValue = currentScale.z;
                    smoothedDimensionValue = Mathf.Lerp(currentDimensionValue, currentTargetWidth, widthSmoothing);
                    transform.localScale = new Vector3(originalScale.x, originalScale.y, smoothedDimensionValue);
                    break;
            }
            
            // Log dimension updates (throttled to avoid spam)
            if (enableDebugLogging && Time.frameCount % 60 == 0) // Log every 60 frames (~1 second at 60fps)
            {
                Debug.Log($"[ModulatingSound] Update - Dimension: {dimensionToSet}, Current: {currentDimensionValue:F3}, Target: {currentTargetWidth:F3}, Smoothed: {smoothedDimensionValue:F3}, Starting: {startingDimensionValue:F3}");
            }
        }
        else
        {
            if (enableDebugLogging && Time.frameCount % 120 == 0) // Log every 120 frames
            {
                Debug.Log($"[ModulatingSound] Update - Not updating dimension. UpdateEnabled: {updateGameObjectDimension}, HasStartedPlaying: {hasStartedPlaying}, IsPlaying: {audioSource != null && audioSource.isPlaying}");
            }
        }
        
        // Check if audio just started playing to capture starting dimension value
        if (audioSource != null && audioSource.isPlaying && !hasStartedPlaying)
        {
            hasStartedPlaying = true;
            // Re-capture starting dimension value when playback starts
            switch (dimensionToSet)
            {
                case Dimension.X:
                    startingDimensionValue = transform.localScale.x;
                    break;
                case Dimension.Y:
                    startingDimensionValue = transform.localScale.y;
                    break;
                case Dimension.Z:
                    startingDimensionValue = transform.localScale.z;
                    break;
            }
            if (enableDebugLogging)
            {
                Debug.Log($"[ModulatingSound] Audio Started - HasStartedPlaying: {hasStartedPlaying}, Starting Dimension Value: {startingDimensionValue:F3}, Dimension: {dimensionToSet}");
            }
        }
        
        // Reset flag when audio stops
        if (audioSource != null && !audioSource.isPlaying && hasStartedPlaying)
        {
            hasStartedPlaying = false;
            if (enableDebugLogging)
            {
                Debug.Log($"[ModulatingSound] Audio Stopped - HasStartedPlaying: {hasStartedPlaying}");
            }
        }
        
        // Update audio clip if changed
        if (audioSource.clip != audioClip)
        {
            audioSource.clip = audioClip;
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
                audioSource.Play();
            }
        }
        
        audioSource.loop = loop;
    }
    
    void OnAudioFilterRead(float[] data, int channels)
    {
        if (channels != 2) return; // Only process stereo audio
        
        int dataLength = data.Length / channels;
        
        // Separate left and right channels
        if (leftChannelBuffer == null || leftChannelBuffer.Length != dataLength)
        {
            leftChannelBuffer = new float[dataLength];
            rightChannelBuffer = new float[dataLength];
        }
        
        // Extract channels
        for (int i = 0; i < dataLength; i++)
        {
            leftChannelBuffer[i] = data[i * channels];
            rightChannelBuffer[i] = data[i * channels + 1];
        }
        
        // Calculate stereo separation from the audio content
        float stereoSeparation = CalculateStereoSeparation(leftChannelBuffer, rightChannelBuffer, dataLength);
        
        // Use stereo separation to determine width (0-2 range)
        // More separation = wider, less separation = narrower
        float calculatedWidth = Mathf.Lerp(0f, 2f, stereoSeparation);
        
        // Smooth the calculated width to avoid jitter
        smoothedCalculatedWidth = Mathf.Lerp(smoothedCalculatedWidth, calculatedWidth, widthSmoothingDSP);
        
        // Apply base totalWidth as a multiplier/offset
        float currentWidth = totalWidth * smoothedCalculatedWidth;
        
        if (modulateWidth)
        {
            float widthMod = Mathf.Sin(widthModulationTime * 2f * Mathf.PI) * widthModulationDepth;
            currentWidth = Mathf.Clamp(currentWidth + widthMod, 0f, 2f);
        }
        else
        {
            currentWidth = Mathf.Clamp(currentWidth, 0f, 2f);
        }
        
        // Apply width modulation curve if enabled
        if (useWidthCurve && widthModulationCurve != null && widthModulationCurve.keys.Length > 0)
        {
            // Normalize width to 0-1 range for curve evaluation
            float normalizedWidth = Mathf.Clamp01(currentWidth / 2f);
            // Evaluate curve and scale back to 0-2 range
            float curveValue = widthModulationCurve.Evaluate(normalizedWidth);
            currentWidth = curveValue * 2f;
        }
        
        ApplyStereoWidth(leftChannelBuffer, rightChannelBuffer, dataLength, currentWidth);
        
        // Calculate dimension value based on totalWidth (0-2 range mapped to 0-1)
        if (updateGameObjectDimension && hasStartedPlaying)
        {
            // Map totalWidth (0-2) to 0-1 range
            float proportionalValue = Mathf.Clamp01(currentWidth / 2f);
            // Apply proportionally to starting dimension value
            currentTargetWidth = startingDimensionValue * proportionalValue;
            
            // Log DSP values (throttled - only when width changes significantly or every 1000 calls)
            dspCallCount++;
            if (enableDebugLogging && (Mathf.Abs(currentWidth - lastLoggedWidth) > 0.05f || dspCallCount % 1000 == 0))
            {
                Debug.Log($"[ModulatingSound] DSP - TotalWidth: {totalWidth:F3}, CurrentWidth: {currentWidth:F3}, Proportional: {proportionalValue:F3}, TargetWidth: {currentTargetWidth:F3}, Starting: {startingDimensionValue:F3}, ModulateWidth: {modulateWidth}");
                lastLoggedWidth = currentWidth;
            }
        }
        else if (enableDebugLogging && dspCallCount % 2000 == 0) // Log occasionally even when not updating
        {
            Debug.Log($"[ModulatingSound] DSP - Not calculating dimension. UpdateEnabled: {updateGameObjectDimension}, HasStartedPlaying: {hasStartedPlaying}, TotalWidth: {totalWidth:F3}");
        }
        
        // Write processed data back
        for (int i = 0; i < dataLength; i++)
        {
            data[i * channels] = leftChannelBuffer[i];
            data[i * channels + 1] = rightChannelBuffer[i];
        }
    }
    
    float CalculateStereoSeparation(float[] left, float[] right, int length)
    {
        // Calculate the difference between left and right channels
        // This gives us a measure of stereo separation
        float separationSum = 0f;
        float energySum = 0f;
        
        for (int i = 0; i < length; i++)
        {
            float mid = (left[i] + right[i]) * 0.5f;
            float side = Mathf.Abs(left[i] - right[i]) * 0.5f;
            
            separationSum += side * side;
            energySum += mid * mid + side * side;
        }
        
        // Normalize separation (0 = mono, 1 = maximum separation)
        float separation = energySum > 0.0001f ? Mathf.Sqrt(separationSum / energySum) : 0f;
        
        // Clamp and smooth the value
        return Mathf.Clamp01(separation * 2f); // Scale to make it more sensitive
    }
    
    void ApplyStereoWidth(float[] left, float[] right, int length, float width)
    {
        // Stereo width algorithm: 0 = mono, 1 = normal stereo, 2 = wide stereo
        // Width of 0: both channels become (L+R)/2 (mono)
        // Width of 1: no change
        // Width of 2: maximum stereo separation
        
        for (int i = 0; i < length; i++)
        {
            float mid = (left[i] + right[i]) * 0.5f;
            float side = (left[i] - right[i]) * 0.5f;
            
            // Adjust side signal based on width
            side *= width;
            
            // Reconstruct left and right channels
            left[i] = mid + side;
            right[i] = mid - side;
        }
    }
    
    // Public methods for controlling playback
    public void Play()
    {
        if (audioSource != null && audioClip != null)
        {
            audioSource.Play();
        }
    }
    
    public void Stop()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
    
    public void Pause()
    {
        if (audioSource != null)
        {
            audioSource.Pause();
        }
    }
    
    // Public method to set total width
    public void SetTotalWidth(float width)
    {
        totalWidth = Mathf.Clamp(width, 0f, 2f);
    }
    
    public float GetTotalWidth()
    {
        return totalWidth;
    }
    
    // Public method to set audio clip
    public void SetAudioClip(AudioClip clip)
    {
        audioClip = clip;
        if (audioSource != null)
        {
            bool wasPlaying = audioSource.isPlaying;
            audioSource.clip = clip;
            if (wasPlaying && clip != null)
            {
                audioSource.Play();
            }
        }
    }
}
