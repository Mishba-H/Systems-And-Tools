using System;
using UnityEngine;

namespace Sciphone
{
    [Serializable]
    public class AttackData
    {
        public string attackName = "NewAttack";
        public AttackType attackType = AttackType.None;
    }
}