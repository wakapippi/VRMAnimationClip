using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace wakapippi
{
    [Serializable]
    public class VRMAnimationClipBindingOption
    {
        public string PresetName;
        public string ClipName;
        public string TargetPropertyName;
    }
    
    [Serializable]
    public class VRMAnimationClipBindingOptionList
    {
        public List<VRMAnimationClipBindingOption> List = new List<VRMAnimationClipBindingOption>();
        public string Identifier = System.Guid.NewGuid().ToString();
    }
}