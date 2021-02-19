using System;

namespace DarkLink.AutoNotify
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class AutoNotifyAttribute : Attribute
    {
        public AutoNotifyAttribute()
        {
        }
    }
}