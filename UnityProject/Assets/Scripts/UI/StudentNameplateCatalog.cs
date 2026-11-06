using UnityEngine;
using UnityEngine.UI;

namespace HongikIngan
{
    /// <summary>Role-specific nameplate art shared by the School and Battle student slots.</summary>
    [CreateAssetMenu(menuName = "HongikIngan/Student Nameplate Catalog")]
    public sealed class StudentNameplateCatalog : ScriptableObject
    {
        public Sprite dealer, buff, debuff;

        public Sprite Find(StudentRole role)
        {
            switch (role)
            {
                case StudentRole.Buff: return buff;
                case StudentRole.Debuff: return debuff;
                default: return dealer;
            }
        }

        /// <summary>Slots whose scene instance lacks the references simply keep the plain name text.</summary>
        public static void Apply(Image plate, StudentNameplateCatalog catalog, StudentRole role)
        {
            if (plate == null || catalog == null) return;
            plate.sprite = catalog.Find(role);
            plate.enabled = plate.sprite != null;
        }
    }
}
