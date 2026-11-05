using System;
using UnityEngine;

namespace HongikIngan
{
    [CreateAssetMenu(menuName = "HongikIngan/Student Portrait Catalog")]
    public sealed class StudentPortraitCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string studentId;
            public Sprite sprite;
        }

        public Entry[] entries = Array.Empty<Entry>();

        public Sprite Find(string studentId)
        {
            if (string.IsNullOrEmpty(studentId)) return null;
            foreach (var entry in entries)
                if (string.Equals(entry.studentId, studentId, StringComparison.OrdinalIgnoreCase))
                    return entry.sprite;
            return null;
        }
    }
}
