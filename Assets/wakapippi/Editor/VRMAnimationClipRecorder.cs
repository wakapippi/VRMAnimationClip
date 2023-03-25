using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Entum;
using Unity.VisualScripting;
using UnityEditor;
using VRM;

namespace wakapippi
{
    [RequireComponent(typeof(MotionDataRecorder))]
    public class VRMAnimationClipRecorder : MonoBehaviour
    {
        [Header("VRMAnimationClipの記録を同時に行う場合はtrueにします")] [SerializeField]
        private bool _recordVRMAnimationClip = false;

        /*
        [Tooltip("記録するFPS。0で制限しない。UpdateのFPSは超えられません。")]
        public float TargetFPS = 60.0f;
        */

        [Tooltip("録画対象のVRMBlendShapeProxy")] public VRMBlendShapeProxy BlendShapeProxy;

        private MotionDataRecorder _animRecorder;
        private bool _recording = false;
        private int _frameCount = 0;
        private float _recordedTime = 0f;
        private float _startTime;

        private Dictionary<BlendShapePreset, List<VRMRuntimeAnimationData>> _recordingDataPresetDict;
        private Dictionary<string, List<VRMRuntimeAnimationData>> _recordingDataClipNameDict;


        private void OnEnable()
        {
            _animRecorder = GetComponent<MotionDataRecorder>();
            _animRecorder.OnRecordStart += RecordStart;
            _animRecorder.OnRecordEnd += RecordEnd;
        }

        private void RecordStart()
        {
            if (_animRecorder == null)
            {
                Debug.LogError("VRMBlendShapeProxyがアタッチされていないため、収録できません。");
                return;
            }

            _recording = true;
            _recordedTime = 0f;
            _startTime = Time.time;
            _frameCount = 0;
            _recordingDataPresetDict = new Dictionary<BlendShapePreset, List<VRMRuntimeAnimationData>>();
            _recordingDataClipNameDict = new Dictionary<string, List<VRMRuntimeAnimationData>>();
        }

        private void RecordEnd()
        {
            if (!_recordVRMAnimationClip)
            {
                return;
            }

            ExportVRMAnimationClip();
            _recording = false;
        }

        private void LateUpdate()
        {
            if (!_recording)
            {
                return;
            }
            _recordedTime = Time.time - _startTime;
            /*

            if (TargetFPS != 0.0f)
            {
                var nextTime = (1.0f * (_frameCount + 1)) / TargetFPS;
                if (nextTime > _recordedTime)
                {
                    return;
                }

                if (_frameCount % TargetFPS == 0)
                {
                    print("VRM_Fps=" + 1 / (_recordedTime / _frameCount));
                }
            }
            else
            {
                if (Time.frameCount % Application.targetFrameRate == 0)
                {
                    print("VRM_Fps=" + 1 / Time.deltaTime);
                }
            }
            */

            var values = BlendShapeProxy.GetValues();
            foreach (var keyValuePair in values)
            {
                var data = new VRMRuntimeAnimationData()
                {
                    FrameCount = _frameCount,
                    Time = _recordedTime,
                    Value = keyValuePair.Value
                };

                _frameCount++;

                var key = keyValuePair.Key;
                if (key.Preset == BlendShapePreset.Unknown)
                {
                    // Unknownの場合は名前で記録する
                    if (!_recordingDataClipNameDict.ContainsKey(key.Name))
                    {
                        _recordingDataClipNameDict[key.Name] = new List<VRMRuntimeAnimationData>();
                    }

                    _recordingDataClipNameDict[key.Name].Add(data);
                    continue;
                }

                // それ以外はPresetで記録する
                if (!_recordingDataPresetDict.ContainsKey(key.Preset))
                {
                    _recordingDataPresetDict[key.Preset] = new List<VRMRuntimeAnimationData>();
                }

                _recordingDataPresetDict[key.Preset].Add(data);
            }
        }

        void ExportVRMAnimationClip()
        {
            var bindOptionList = new VRMAnimationClipBindingOptionList();
            var eventSetTimeList = new List<float>();
            var animEventList = new List<AnimationEvent>();
            
            const string prefix = "Property";
            var currentUnknownPropertyIndex = 0;

            var filedNameList = typeof(AnimatedValueStruct).GetFields().Select(x => x.Name);
            var animationClip = new AnimationClip();

            foreach (var keyValuePair in _recordingDataPresetDict)
            {
                var key = keyValuePair.Key;
                var PropertyNameToUse = "";
                var knownPropertyName = Enum.GetName(typeof(BlendShapePreset), key);

                if (filedNameList.Contains(knownPropertyName))
                {
                    PropertyNameToUse = knownPropertyName;
                }
                else
                {
                    // 普通はここに入らない
                    Debug.LogWarning("異常が発生している可能性があります。");
                    var unknownPropertyName = prefix + currentUnknownPropertyIndex.ToString();
                    currentUnknownPropertyIndex++;
                    if (!filedNameList.Contains(unknownPropertyName))
                    {
                        Debug.LogError("使えるプロパティ名がありません。クリップが多すぎる可能性があります。");
                        continue;
                    }

                    PropertyNameToUse = unknownPropertyName;
                }

                bindOptionList.List.Add(new VRMAnimationClipBindingOption()
                {
                    ClipName = "",
                    PresetName = knownPropertyName, // 操作する元のBlendShapeProxyのプリセット
                    TargetPropertyName = PropertyNameToUse // 操作するMonoBehaviorプロパティ名
                });
                var events =SetAnimation(PropertyNameToUse, animationClip, keyValuePair.Value, eventSetTimeList);
                animEventList.AddRange(events);
            }

            foreach (var keyValuePair in _recordingDataClipNameDict)
            {
                var key = keyValuePair.Key;
                var PropertyNameToUse = "";
                
                var unknownPropertyName = prefix + currentUnknownPropertyIndex.ToString();
                currentUnknownPropertyIndex++;
                if (!filedNameList.Contains(unknownPropertyName))
                {
                    Debug.LogError("使えるプロパティ名がありません。クリップが多すぎる可能性があります。");
                    continue;
                }

                PropertyNameToUse = unknownPropertyName;

                bindOptionList.List.Add(new VRMAnimationClipBindingOption()
                {
                    ClipName = key,
                    PresetName = "", // 操作する元のBlendShapeProxyのプリセット
                    TargetPropertyName = PropertyNameToUse // 操作するMonoBehaviorプロパティ名
                });
                var events = SetAnimation(PropertyNameToUse, animationClip, keyValuePair.Value, eventSetTimeList);
                animEventList.AddRange(events);
            }

            var binding = JsonUtility.ToJson(bindOptionList);
            foreach (var animationEvent in animEventList)
            {
                animationEvent.stringParameter = binding;
                animationEvent.functionName = "SetBinding";
            }
            AnimationUtility.SetAnimationEvents(animationClip, animEventList.ToArray());
            
            MotionDataRecorder.SafeCreateDirectory("Assets/Resources");

            var outputPath = "Assets/Resources/VRMRecordMotion_" + _animRecorder.CharacterAnimator.name + "_" +
                             DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "_Clip.anim";

            Debug.Log("outputPath:" + outputPath);
            AssetDatabase.CreateAsset(animationClip,
                AssetDatabase.GenerateUniqueAssetPath(outputPath));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
        }

        private List<AnimationEvent> SetAnimation(string propertyName, AnimationClip clip, List<VRMRuntimeAnimationData> dataList, List<float> eventSetTimeList)
        {
            var output = new List<AnimationEvent>();
            
            var curveBinding = new EditorCurveBinding();
            curveBinding.type = typeof(AnimatedValueStruct);
            curveBinding.path = "";
            curveBinding.propertyName = propertyName;
            AnimationCurve curve = new AnimationCurve();

            float pastBlendShapeWeight = -1;
            for (int k = 0; k < dataList.Count; k++)
            {
                if (k != 0 && !(Mathf.Abs(pastBlendShapeWeight - dataList[k].Value) >
                                0.01f)) continue;
                curve.AddKey(new Keyframe(dataList[k].Time, dataList[k].Value, float.PositiveInfinity, 0f));
                pastBlendShapeWeight = dataList[k].Value;

                if (!eventSetTimeList.Contains(dataList[k].Time))
                {
                    var animEvent = new AnimationEvent();
                    animEvent.time = dataList[k].Time;
                    eventSetTimeList.Add(dataList[k].Time);
                    output.Add(animEvent);
                }
                
            }

            AnimationUtility.SetEditorCurve(clip, curveBinding, curve);
            return output;
        }
    }

    struct VRMRuntimeAnimationData
    {
        public float Value;
        public int FrameCount;
        public float Time;
    }
}