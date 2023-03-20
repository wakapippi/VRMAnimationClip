using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRM;
using wakapippi;

namespace wakapippi
{

    [ExecuteInEditMode]
    [RequireComponent(typeof(AnimatedValueStruct))]
    public class VRMAnimationClipPlayer : MonoBehaviour
    {

        [Tooltip("動かす対象のVRMBlendShapeProxy。同じGameObjectにある場合は指定不要です。")]
        [SerializeField] private VRMBlendShapeProxy _vrmBlendShapeProxy;
        [Tooltip("動かす対象のVRMBlendShapeProxy。同じGameObjectにある場合は指定不要です。")]
        [SerializeField] private RuntimeAnimatorController _animatorController;
        
        private AnimatedValueStruct _struct;
        private VRMAnimationClipBindingOptionList _bindingOptionList;

        private List<float> _lastValues = new List<float>();

        private static Dictionary<int, BlendShapeMerger> _mergers = new Dictionary<int, BlendShapeMerger>();
        private static void ResetMergerInstance()
        {
            _mergers = new Dictionary<int, BlendShapeMerger>();
        }

        private static BlendShapeMerger GetEditModeMergerInstance(VRMBlendShapeProxy proxy)
        {
            var go = proxy.gameObject;
            var id = go.GetInstanceID();
            if (!_mergers.ContainsKey(id))
            {
                _mergers.Add(id, new BlendShapeMerger(proxy.BlendShapeAvatar.Clips,go.transform));
            }

            return _mergers[id];

        }

        void Update()
        {
            if (_struct == null)
            {
                _struct = gameObject.GetComponent<AnimatedValueStruct>();

            }
            if (_vrmBlendShapeProxy == null)
            {
                _vrmBlendShapeProxy = GetComponent<VRMBlendShapeProxy>();
               // _animatorController = GetComponent<()
            }

            if (_vrmBlendShapeProxy == null) return;
            
            if(_bindingOptionList == null) return;

            var dict = new Dictionary<BlendShapeKey, float>();
            var values = new List<float>();
            
            foreach (var option in _bindingOptionList.List)
            {
                var targetName = option.TargetPropertyName;
                var fieldInfo = typeof(AnimatedValueStruct).GetField(targetName);
                if(fieldInfo == null) continue;
                var value = (float) fieldInfo.GetValue(_struct);
                
                if (option.PresetName != "")
                {
                    var enumValue = (BlendShapePreset) Enum.Parse(typeof(BlendShapePreset), option.PresetName);
                    var key = BlendShapeKey.CreateFromPreset(enumValue);
                    dict[key] = value;
                }
                else
                {
                    var unknownKey = BlendShapeKey.CreateUnknown(option.ClipName);
                    dict[unknownKey] = value;
                }

                values.Add(value);
            }

            // 値が変わらない場合は書き込まない（再生が終了している場合に動くことで他からのBlendShapeの操作に副作用を与える可能性があるため）
            if (!IsSame(_lastValues, values))
            {
                var isEditMode = !Application.isPlaying;

                if (!isEditMode)
                {
                    _vrmBlendShapeProxy.SetValues(dict);
                }
                else
                {
                   var merger = GetEditModeMergerInstance(_vrmBlendShapeProxy);
                   merger.SetValues(dict);
                }
            }
            _lastValues = values;
        }

        private static bool IsSame(List<float> a, List<float> b)
        {
            if (a.Count != b.Count) return false;
            return !a.Where((t, i) => t != b[i]).Any();
        }
        
        public void SetBinding(string param)
        {
            var bindingList = JsonUtility.FromJson<VRMAnimationClipBindingOptionList>(param);
            if (_bindingOptionList == null || _bindingOptionList.Identifier != bindingList.Identifier)
            {
                _bindingOptionList = bindingList;
            }
        }
    }

}
