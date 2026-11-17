using UnityEngine;

// One persistent-in-session music source, plus bounded independent voices for overlapping shots.
[DisallowMultipleComponent]
public class DefenseAudio : MonoBehaviour
{
    private const int VoiceCount=32;
    public AudioSource Music { get; private set; }
    public AudioClip PulseLaser { get; private set; }
    public AudioClip SiegeLaser { get; private set; }
    public int ShotsPlayed { get; private set; }
    public float LastShotPitch { get; private set; }
    private readonly AudioSource[] voices=new AudioSource[VoiceCount];
    private int nextVoice;
    private DefenseGame game;

    public void Initialize(DefenseGame owner)
    {
        game=owner;
        PulseLaser=Resources.Load<AudioClip>("DefenseAudio/PulseLaser");
        SiegeLaser=Resources.Load<AudioClip>("DefenseAudio/SiegeLaser");
        Music=NewSource("Background music");Music.loop=true;Music.volume=.22f;Music.priority=32;
        Music.ignoreListenerPause=true;
        Music.clip=Resources.Load<AudioClip>("DefenseAudio/DearlyBeloved");
        if(Music.clip!=null) Music.Play();
        for(int i=0;i<voices.Length;i++) voices[i]=NewSource("Tower laser voice "+i);
    }
    private AudioSource NewSource(string sourceName)
    {
        GameObject root=new GameObject(sourceName);root.transform.SetParent(transform,false);
        AudioSource source=root.AddComponent<AudioSource>();source.playOnAwake=false;
        source.spatialBlend=0;source.dopplerLevel=0;
        return source;
    }
    public static float PitchFor(int paidLevel,int mergeLevel)
    {
        // Two semitones per paid upgrade, three per merge tier: consistently sharper at each step.
        return Mathf.Pow(2f,(2f*(paidLevel-1)+3f*(mergeLevel-1))/12f);
    }
    public void PlayShot(DefenseTower tower)
    {
        AudioClip clip=tower.Kind==TowerKind.Pulse ? PulseLaser : SiegeLaser;
        if(clip==null) return;
        AudioSource voice=voices[nextVoice];nextVoice=(nextVoice+1)%voices.Length;
        voice.Stop();voice.clip=clip;voice.loop=false;
        voice.pitch=PitchFor(tower.Level,tower.MergeLevel);
        voice.volume=tower.Kind==TowerKind.Pulse ? .14f : .18f;
        voice.panStereo=Mathf.Clamp((game.View.WorldToViewportPoint(tower.transform.position).x-.5f)*1.2f,-.65f,.65f);
        voice.Play();LastShotPitch=voice.pitch;ShotsPlayed++;
    }
}
