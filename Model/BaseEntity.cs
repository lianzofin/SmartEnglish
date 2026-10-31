using System;
using System.Collections.Generic;
using System.Text;

// מחלקת בסיס לכל הישויות במערכת.
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