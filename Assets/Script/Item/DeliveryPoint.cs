using UnityEngine;

public class DeliveryPoint : MonoBehaviour, IInteractable
{
    public string destinationId;

     string successMessage = "Delivery successful!!";
     string noBoxMessage = "You don't have any items for this destination.";
    [SerializeField] private AudioClip interactSound;

    public void Interact(PlayerInteractionSystem interactor,
                         PlayerInteractionSystem.InteractionType type)
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            bool hasItem = HasItemToDeliver();
            DeliveryConfirmUI.Instance.Show(this, hasItem);
        }
    }

    public void ConfirmDelivery()
    {
        if (BoxInventory.Instance == null)
        {
            ShowMessage(noBoxMessage);
            return;
        }

        int reward;
        bool ok = BoxInventory.Instance.TryDeliverFromInventory(destinationId, out reward);

        if (!ok)
        {
            ShowMessage(noBoxMessage);
            return;
        }

        if (reward > 0)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.MarkDeliveredByDestination(destinationId);
                GameManager.Instance.AddMoney(reward);

                // 💰 popup เงิน
                AddSalesPopupUI.ShowNotice(reward, true);
            }
        }

        ShowMessage(successMessage);
        PlayInteractSound();
    }

    void PlayInteractSound()
    {
        if (interactSound == null) return;

        AudioManager.Instance.PlaySFX(
            interactSound,
            transform.position
        );
    }

    void ShowMessage(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return;

        Debug.Log($"[DeliveryPoint] {msg}");
    }

    public bool HasItemToDeliver()
    {
        if (BoxInventory.Instance == null) return false;

        int reward;
        return BoxInventory.Instance.TryCheckHasItem(destinationId, out reward);
    }
}