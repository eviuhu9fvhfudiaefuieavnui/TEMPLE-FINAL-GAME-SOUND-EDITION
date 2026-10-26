using System.Collections;           // lets us use coroutines (code that waits, then keeps going)
using System.Collections.Generic;   // lets us use Dictionary (a lookup table: name -> sound)
using UnityEngine;                  // lets us use Unity things like AudioSource and MonoBehaviour

// This script is the sound manager. It plays every sound in the game.
// Other scripts call it by name, like: AudioManager.Instance.PlaySFX("gold_pickup");
[RequireComponent(typeof(AudioSource))]   // Unity makes sure this object has an AudioSource (we need 2, see Awake)
public class AudioManager : MonoBehaviour // makes a script that can sit on an object in the game
{
    public static AudioManager Instance { get; private set; } // the one AudioManager in the game. Other scripts use this to reach it

    [System.Serializable]           // lets Unity show this class in the Inspector, so you can fill it in
    public class SoundEntry         // one sound, with its settings
    {
        public string name;         // the name we type to play it, like "gold_pickup". Must match the sound list
        public AudioClip clip;      // the sound file itself (drag it in on the Inspector)
        [Range(0f, 1f)]             // shows a slider in the Inspector from 0 (silent) to 1 (full volume)
        public float volume = 1f;   // how loud this sound is. 1 = full volume
        public float delay = 0f;    // how many seconds to wait before playing. 0 = play right away
    }

    [SerializeField]                // shows this list in the Inspector even though it is private
    private SoundEntry[] sounds;    // the list of all our sounds. You fill this in on the Inspector

    private AudioSource sfxSource;  // the player used for short sound effects (shots, pickups, clicks)
    private AudioSource musicSource;// the player used for background music (it loops)
    private Dictionary<string, SoundEntry> lookup; // fast lookup: type a name, get that sound's settings back

    private void Awake()            // Unity runs this once, right when the game starts
    {
        if (Instance != null && Instance != this) // if another AudioManager already exists...
        {
            Destroy(gameObject);    // ...delete this copy, so there is only ever one
            return;                 // stop here, we do not need to set anything up again
        }
        Instance = this;            // this is the one AudioManager, so save it
        DontDestroyOnLoad(gameObject); // keep this object alive when we change levels/scenes

        AudioSource[] sources = GetComponents<AudioSource>(); // find all Audio Source parts on this object
        if (sources.Length < 2)     // if there are fewer than 2...
        {
            Debug.LogError("AudioManager: needs two Audio Source components on this object."); // show an error in the Console
        }
        else                        // we have at least 2
        {
            sfxSource = sources[0];     // the first Audio Source is for sound effects
            musicSource = sources[1];   // the second Audio Source is for music
        }

        lookup = new Dictionary<string, SoundEntry>(); // make an empty lookup table
        foreach (var entry in sounds)   // go through every sound in the list, one at a time
        {
            if (string.IsNullOrEmpty(entry.name) || entry.clip == null) // if it has no name or no sound file...
            {
                Debug.LogWarning("AudioManager: sound entry missing a name or clip."); // warn in the Console
                continue;           // skip this one and go to the next
            }
            if (lookup.ContainsKey(entry.name)) // if this name is already in the table...
            {
                Debug.LogWarning($"AudioManager: duplicate sound name '{entry.name}'."); // warn about the copy
                continue;           // skip the duplicate
            }
            lookup.Add(entry.name, entry); // add this sound to the table under its name
        }
    }

    // Plays a short sound effect once. Use for shots, pickups, clicks, enemy sounds.
    public void PlaySFX(string soundName) // soundName is the name we type, like "crossbow_fire"
    {
        if (sfxSource == null || !TryGet(soundName, out SoundEntry entry)) return; // if no player or no sound, do nothing

        if (entry.delay > 0f)           // if this sound should wait first...
            StartCoroutine(PlayAfterDelay(entry)); // ...start the waiting routine
        else                            // no wait needed
            sfxSource.PlayOneShot(entry.clip, entry.volume); // play the sound once, right now, at its volume
    }

    // Plays background music. It loops, and replaces whatever music was playing before.
    public void PlayMusic(string soundName) // soundName is the name of the music, like "level_music"
    {
        if (musicSource == null || !TryGet(soundName, out SoundEntry entry)) return; // if no player or no music, do nothing

        musicSource.clip = entry.clip;      // put this music track in the music player
        musicSource.volume = entry.volume;  // set how loud the music is
        musicSource.loop = true;            // play it again and again when it finishes
        musicSource.Play();                 // start the music
    }

    // Stops the music (for example when the game ends).
    public void StopMusic()                 // a command other scripts can call
    {
        if (musicSource != null) musicSource.Stop(); // if the music player exists, stop it
    }

    // Waits for the delay, then plays the sound. Used when a sound needs to line up with an animation.
    private IEnumerator PlayAfterDelay(SoundEntry entry) // a routine that can pause in the middle
    {
        yield return new WaitForSeconds(entry.delay); // pause for the number of seconds in the delay setting
        sfxSource.PlayOneShot(entry.clip, entry.volume); // then play the sound once
    }

    // Looks up a sound by name. Returns true if found, and gives back its settings through "entry".
    private bool TryGet(string soundName, out SoundEntry entry) // out means this function hands back the sound
    {
        if (lookup != null && lookup.TryGetValue(soundName, out entry)) // if the table has this name...
            return true;                    // ...say we found it

        Debug.LogWarning($"AudioManager: no sound named '{soundName}'."); // no match, so warn in the Console
        entry = null;                       // hand back nothing, because there is no sound
        return false;                       // say we did not find it
    }
}
