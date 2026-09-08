using UnityEngine;

public class PongAudio : MonoBehaviour
{
    AudioSource _source;
    AudioClip _paddleHit;
    AudioClip _wallHit;
    AudioClip _score;

    void Awake()
    {
        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _paddleHit = MakeTone(440f, 0.06f);
        _wallHit = MakeTone(330f, 0.05f);
        _score = MakeTone(220f, 0.18f);
    }

    public void PlayPaddle() => _source.PlayOneShot(_paddleHit, 0.55f);
    public void PlayWall() => _source.PlayOneShot(_wallHit, 0.4f);
    public void PlayScore() => _source.PlayOneShot(_score, 0.65f);

    static AudioClip MakeTone(float frequency, float duration)
    {
        const int sampleRate = 44100;
        int samples = Mathf.CeilToInt(sampleRate * duration);
        var data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = 1f - (i / (float)samples);
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope;
        }

        var clip = AudioClip.Create($"tone_{frequency:0}", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
