using System;
using System.Collections.Generic;
using System.Text;

// נוסף בגרסה 3.
// מחלקת בסיס לכל הישויות במערכת.
// המחלקה מוגדרת abstract מפני שאין עצמים מסוג "ישות" בלבד, אלא רק מטיפוסים יורשים (כמו User).
namespace Model
{
    public abstract class BaseEntity
    {
        // המזהה הייחודי של הישות בבסיס הנתונים
        public int Id { get; set; }

        // בנאי ריק לשימוש המחלקות היורשות
        protected BaseEntity() { }

        // בנאי שמקבל את המזהה ומגדיר אותו
        protected BaseEntity(int id)
        {
            Id = id;
        }
    }
}