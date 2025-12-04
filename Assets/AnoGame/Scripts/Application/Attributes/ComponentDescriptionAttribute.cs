using System;
using UnityEngine;

namespace AnoGame.Application.Direction
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ComponentDescriptionAttribute : Attribute
    {
        public string Description { get; }

        public ComponentDescriptionAttribute(string description)
        {
            Description = description;
        }
    }
}
