using System.Collections;           
using System.Collections.Generic;   
using UnityEngine;                  

// Sound manager. Plays all sounds by name.
// Example: AudioManager.Instance.PlaySFX("coin_pickup");
[RequireComponent(typeof(AudioSource))]   // needs an AudioSource on this object (we use two)
public class AudioManager : MonoBehaviour // script that sits on a game object
{
    public static AudioManager Instance { get; private set; } // the single shared copy, used by other scripts

    [System.Serializable]           // shows this class in the Inspector
    public class SoundEntry         // one sound and its settings
    {
        public string name;         // name used to play it, like "coin_pickup"
        public AudioClip clip;      // the sound file
        [Range(0f, 1f)]             // slider from 0 to 1 in the Inspector
        public float volume = 1f;   // loudness. 1 = full
        public float delay = 0f;    // seconds to wait before playing. 0 = play now
    }

    [SerializeField]                // show in Inspector even though it is private
    private SoundEntry[] sounds;    // list of all sounds, filled in on the Inspector

    private AudioSource sfxSource;  // plays short sound effects
    private AudioSource musicSource;// plays music (loops)
    private Dictionary<string, SoundEntry> lookup; // name -> sound lookup

    private void Awake()            // runs once when the game starts
    {
        if (Instance != null && Instance != this) // another manager already exists
        {
            Destroy(gameObject);    // remove this copy
            return;                 // stop, the first one is already set up
        }
        Instance = this;            // save this as the single manager
        DontDestroyOnLoad(gameObject); // keep it alive between levels

        AudioSource[] sources = GetComponents<AudioSource>(); // get all Audio Sources on this object
        if (sources.Length < 2)     // need at least two
        {
            Debug.LogError("AudioManager: needs two Audio Source components on this object."); // error in Console
        }
        else                        // enough sources
        {
            sfxSource = sources[0];     // first one = sound effects
            musicSource = sources[1];   // second one = music
        }

        lookup = new Dictionary<string, SoundEntry>(); // empty lookup table
        foreach (var entry in sounds)   // check each sound in the list
        {
            if (string.IsNullOrEmpty(entry.name) || entry.clip == null) // missing name or file
            {
                Debug.LogWarning("AudioManager: sound entry missing a name or clip."); // warning in Console
                continue;           // skip it
            }
            if (lookup.ContainsKey(entry.name)) // name already used
            {
                Debug.LogWarning($"AudioManager: duplicate sound name '{entry.name}'."); // warning in Console
                continue;           // skip the duplicate
            }
            lookup.Add(entry.name, entry); // add to the table
        }
    }

    // Plays one short sound effect (shots, pickups, clicks, enemies).
    public void PlaySFX(string soundName) // soundName, like "crossbow_fire"
    {
        if (sfxSource == null || !TryGet(soundName, out SoundEntry entry)) return; // no player or no sound, stop

        if (entry.delay > 0f)           // delay set
            StartCoroutine(PlayAfterDelay(entry)); // wait, then play
        else                            // no delay
            sfxSource.PlayOneShot(entry.clip, entry.volume); // play now
    }

    // Plays looping background music. Replaces any music already playing.
    public void PlayMusic(string soundName) // soundName, like "level_music"
    {
        if (musicSource == null || !TryGet(soundName, out SoundEntry entry)) return; // no player or no music, stop

        musicSource.clip = entry.clip;      // set the track
        musicSource.volume = entry.volume;  // set the volume
        musicSource.loop = true;            // repeat when it ends
        musicSource.Play();                 // start
    }

    // Stops the music. Other scripts can call this, like when the game ends.
    public void StopMusic()                 // command other scripts can use
    {
        if (musicSource != null) musicSource.Stop(); // stop if the player exists
    }

    // Waits for the delay, then plays the sound.
    private IEnumerator PlayAfterDelay(SoundEntry entry) // routine that can pause
    {
        yield return new WaitForSeconds(entry.delay); // wait the delay time
        sfxSource.PlayOneShot(entry.clip, entry.volume); // play the sound
    }

    // Finds a sound by name. Returns true if found, and sends the sound back through "entry".
    private bool TryGet(string soundName, out SoundEntry entry) // out = sends the result back
    {
        if (lookup != null && lookup.TryGetValue(soundName, out entry)) // name is in the table
            return true;                    // found it

        Debug.LogWarning($"AudioManager: no sound named '{soundName}'."); // not found, warning in Console
        entry = null;                       // nothing to send back
        return false;                       // not found
    }
}