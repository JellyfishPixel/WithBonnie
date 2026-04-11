using UnityEngine;

public class DestroyZone : MonoBehaviour
{
    [SerializeField] private AudioClip interactSound;

    private void PlayInteractSound()
    {
        if (interactSound == null) return;

        AudioManager.Instance.PlaySFX(
            interactSound,
            transform.position
        );
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player") return;
        var abandonable = other.GetComponentInParent<IAbandonable>();
        if (abandonable != null)
        {
            abandonable.OnAbandoned();
        }
        else
        {
            PlayInteractSound();
            Destroy(other.gameObject);

        }
    }
}
