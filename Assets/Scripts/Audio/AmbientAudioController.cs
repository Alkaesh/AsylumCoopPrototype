using UnityEngine;

namespace AsylumHorror.Audio
{
    public class AmbientAudioController : MonoBehaviour
    {
        [SerializeField] private AudioSource loopSource;
        [SerializeField] private AudioSource oneShotSource;
        [SerializeField] private AudioClip[] randomAmbientClips;
        [SerializeField] private bool enableRandomOneShots = true;
        [SerializeField] private float minOneShotDelay = 10f;
        [SerializeField] private float maxOneShotDelay = 25f;

        private float nextOneShotTime;

        private void Start()
        {
            EnsureFallbackClips();
            ScheduleNextOneShot();
            if (loopSource != null && loopSource.clip != null && !loopSource.isPlaying)
            {
                loopSource.loop = true;
                loopSource.Play();
            }
        }

        private void Update()
        {
            if (!enableRandomOneShots || oneShotSource == null || randomAmbientClips == null || randomAmbientClips.Length == 0)
            {
                return;
            }

            if (Time.time < nextOneShotTime)
            {
                return;
            }

            AudioClip clip = randomAmbientClips[Random.Range(0, randomAmbientClips.Length)];
            if (clip != null)
            {
                oneShotSource.PlayOneShot(clip);
            }

            ScheduleNextOneShot();
        }

        private void ScheduleNextOneShot()
        {
            nextOneShotTime = Time.time + Random.Range(minOneShotDelay, maxOneShotDelay);
        }

        private void EnsureFallbackClips()
        {
            if (loopSource != null && loopSource.clip == null)
            {
                loopSource.clip = ProceduralAudioFactory.GetAmbientLoop();
                loopSource.volume = 0.35f;
            }

            if (enableRandomOneShots && (randomAmbientClips == null || randomAmbientClips.Length == 0))
            {
                randomAmbientClips = new[]
                {
                    ProceduralAudioFactory.GetAmbientStinger(0),
                    ProceduralAudioFactory.GetAmbientStinger(1),
                    ProceduralAudioFactory.GetAmbientStinger(2)
                };
            }
        }
    }
}
