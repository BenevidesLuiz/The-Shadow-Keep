using UnityEngine;

public class ControlerMusica : MonoBehaviour {

    public AudioSource AudioSource1;
    public AudioSource AudioSource2;
    public AudioSource AudioSourceDead;
    public AudioSource hitBoss;
    public AudioSource bossfootStep;
    public AudioSource hitPlayer;

    private void Awake() {
        float volume = PlayerPrefs.GetFloat("volume");
        if (volume == 0) {
            volume = 0.10f;
        }
        if (AudioSource1) {
            AudioSource1.volume = volume;
        }
        if (AudioSource2) {
            AudioSource2.volume = volume;
        }
        if (AudioSourceDead) {
            AudioSourceDead.volume = volume;
        }
        if (hitBoss) {
            hitBoss.volume = volume;
        }
        if (bossfootStep) {
            bossfootStep.volume = volume;
        }
        if (hitPlayer) {
            hitPlayer.volume = volume;
        }
    }
    void Start() {
        AudioSource1.Play();
    }
    public void HitPlayer() {
        hitPlayer.Play();
    }
    public void HitBoss() {
        hitBoss.Play();
    }
    public void HitGround() {
        bossfootStep.Play();
    }
    public void TocarMusicaFase2() {

        if (AudioSource1.isPlaying) {
            AudioSource1.Stop();
        }

        AudioSource2.Play();
    }


    private void PlayerDead() {

        if (AudioSource1.isPlaying) {
            AudioSource1.Stop();
        }

        if (AudioSource2.isPlaying) {
            AudioSource2.Stop();
        }

        if (AudioSourceDead != null) {
            AudioSourceDead.Play();
        }
    }

}
