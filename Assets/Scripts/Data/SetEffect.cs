using System.Collections;
using UnityEngine;

namespace IdleGame.Data
{
    public abstract class SetEffect : ScriptableObject
    {
        public abstract IEnumerator Run();
    }
}
