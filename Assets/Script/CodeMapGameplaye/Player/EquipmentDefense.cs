
using UnityEngine;
using Fusion;

public class EquipmentDefense : MonoBehaviour
{
    [Header("Equipment Reference")]
    public PlayerEquipmentFusion equipmentFusion;

    [Header("Damage Reduction")]
    [Range(0f, 1f)]
    public float helmetReduction = 0.10f;

    [Range(0f, 1f)]
    public float armorReduction = 0.15f;

    [Range(0f, 0.95f)]
    public float maxReduction = 0.60f;

    private void Awake()
    {
        if (equipmentFusion == null)
        {
            equipmentFusion =
                GetComponent<PlayerEquipmentFusion>();
        }
    }

    public float GetDamageReduction()
    {
        if (equipmentFusion == null ||
            equipmentFusion.Object == null ||
            !equipmentFusion.Object.IsValid)
        {
            return 0f;
        }

        float reduction = 0f;

        if (equipmentFusion.HelmetID > 0)
        {
            reduction += helmetReduction;
        }

        if (equipmentFusion.ArmorID > 0)
        {
            reduction += armorReduction;
        }

        return Mathf.Clamp(
            reduction,
            0f,
            maxReduction
        );
    }

    public float CalculateFinalDamage(float originalDamage)
    {
        float reduction = GetDamageReduction();

        return Mathf.Max(
            0f,
            originalDamage * (1f - reduction)
        );
    }
}
