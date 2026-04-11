using UnityEngine;
using TMPro;
using UnityEngine.Rendering;

public class ShopSignInteract : MonoBehaviour, IInteractable
{
    public TMP_Text signLabel;
    public string openText = "OPEN";
    public string closedText = "CLOSED";
    [SerializeField] private AudioClip interactSound;

    private Vector3 _originalScale;
    private void PlayInteractSound()
    {
        if (interactSound == null) return;

        AudioManager.Instance.PlaySFX(
            interactSound,
            transform.position
        );

    }
    public void Start()
    {
        var gm = GameManager.Instance;
        gm.shopIsOpen = false;

        _originalScale = transform.localScale;
    }
    void PlayBellPress()
    {
        LeanTween.cancel(gameObject);

        transform.localScale = _originalScale;

        // หดลงเฉพาะแกน Y
        Vector3 pressScale = new Vector3(
            _originalScale.x,
            _originalScale.y * 0.7f,
            _originalScale.z
        );

        LeanTween.scale(gameObject, pressScale, 0.07f)
            .setEase(LeanTweenType.easeInQuad)
            .setOnComplete(() =>
            {
                // เด้งเกินนิดนึง
                Vector3 bounceScale = new Vector3(
                    _originalScale.x,
                    _originalScale.y * 1.15f,
                    _originalScale.z
                );

                LeanTween.scale(gameObject, bounceScale, 0.12f)
                    .setEase(LeanTweenType.easeOutBack)
                    .setOnComplete(() =>
                    {
                        LeanTween.scale(gameObject, _originalScale, 0.08f)
                            .setEase(LeanTweenType.easeOutQuad);
                    });
            });
    }
    public void Interact(PlayerInteractionSystem interactor,
                       PlayerInteractionSystem.InteractionType type)
    {
        // Mouse0 เท่านั้น
     

        var gm = GameManager.Instance;

        if (type != PlayerInteractionSystem.InteractionType.Primary)
            return;
       
        // ถ้าร้านกำลัง "เปิดอยู่" → พยายามจะปิด
        if (gm.shopIsOpen)
        {
            // ถ้ายังมีลูกค้าที่กำลังให้บริการอยู่ ห้ามปิด
            if (gm.currentCustomer != null)
            {
                Debug.Log("[ShopSignInteract] Cannot close: still serving current customer.");
                AddSalesPopupUI.ShowMessage("Cannot close shop while\na customer is being served.");
                return;
            }

            // ปิดร้าน
            gm.shopIsOpen = false;

            // บอก Spawner ให้หยุด + ไล่ลูกค้าที่เหลือออก
            if (NPCSpawner.Instance != null)
            {
                NPCSpawner.Instance.CloseShopAndClearNPCs();
            }

            // อัปเดตป้าย
            if (signLabel != null)
                signLabel.text = closedText;

            // popup แจ้งเตือน
            AddSalesPopupUI.ShowMessage("Shop CLOSED");

        }
        else
        {
            // เปิดร้าน
            gm.shopIsOpen = true;

            // เปิดให้ Spawner กลับมาทำงานต่อ (เริ่มนับเวลาสุ่ม spawn ใหม่)
            if (NPCSpawner.Instance != null)
            {
                NPCSpawner.Instance.shopIsOpen = true;   // หรือเขียนเมธอด OpenShop() เพิ่มก็ได้
            }

            // อัปเดตป้าย
            if (signLabel != null)
                signLabel.text = openText;

            // popup แจ้งเตือน
            AddSalesPopupUI.ShowMessage("Shop OPEN");
            GuideArrowManager.Instance?.NextTarget();
        }
        PlayInteractSound();
        PlayBellPress();
    }
}
