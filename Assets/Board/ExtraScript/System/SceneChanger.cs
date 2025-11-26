using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

// SceneManager.GetActiveScene().name
// SceneManager.LoadScene(name)

public static class SceneData
{
    public static List<StringPair> data = new();
}

public class SceneChanger : MonoBehaviour
{
    private static Invoker _sceneChangerInvoker = null;
    private static Invoker _playerInvoker = null;
    private static Invoker _veilInvoker = null;

    private static Token sceneChangerToken = new();

    public delegate void Feature(List<string> parameter);
    public static void SplitEvaluatePhrase(string phrase, string separator, out string featureName, out List<string> param)
    {
        param = new();
        string[] words = phrase.Split(separator);
        featureName = words[0];
        for (int i = 1; i < words.Length; ++i) param.Add(words[i]);
    }
    public static string NameEvaluatePhrase(string phrase, string separator)
    {
        return phrase.Split(separator)[0];
    }
    public static void AddFeature(string key, string value)
    {
        ICommand featureAddCommand = new VectorSettingCommand("featureAddCommand",
            new List<float>() { 0f }, new(),
            new List<float>() { 0f }, (context, param) =>
            {
                StringPair newFeature = new StringPair
                {
                    Key = key,
                    Value = value
                };
                SceneData.data.Add(newFeature);
                return CommandQueue.END;
            },
            new StaticClock()
        );
        _sceneChangerInvoker.Do(GlobalConfig.Key.COMMAND_TYPE_SCENE, featureAddCommand, 1, CommandQueue.PRIORITY_NORMAL);
        
    }
    public static void RemoveFeature(string key, string featureName)
    {
        ICommand featureRemoveCommand = new VectorSettingCommand("featureRemoveCommand",
            new List<float>() { 0f }, new(),
            new List<float>() { 0f }, (context, param) =>
            {
                SceneData.data.RemoveAll((data) =>
                {
                    return data.Key == key && NameEvaluatePhrase(data.Value, ",") == featureName;
                });
                return CommandQueue.END;
            },
            new StaticClock()
        );
        _sceneChangerInvoker.Do(GlobalConfig.Key.COMMAND_TYPE_SCENE, featureRemoveCommand, 1, CommandQueue.PRIORITY_NORMAL);
    }

    private static Dictionary<string, Dictionary<string, Feature>> Features = new();

    // ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// /////
    // Feature
    // ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// ///// /////

    // Scene_Change(departureScene, destinationScene, x, y, z)
    /*
     * destinationScene에 도착했다면 플레이어의 위치를 (x, y, z)로 변경하는 Feature를 sceneChangeData에 추가하고, 스스로를 제거합니다.
     * sceneChangeData에 Key가 FEATURE_KEY_THROUGH인 요소가 있다면 아무 행동도 하지 않습니다. 목표 Scene으로 가기 전에 통과하는 Scene을 거쳐야 하기 때문입니다.
     * sceneChangeData에 Key가 FEATURE_KEY_THROUGH인 요소가 없다면 목표 씬으로 전환합니다.
     */
    private void Scene_Change(List<string> parameter)
    {
        string departureScene,destinationScene;
        float x, y, z;
        try
        {
            departureScene = parameter[0];
            destinationScene = parameter[1];
            x = float.Parse(parameter[2]);
            y = float.Parse(parameter[3]);
            z = float.Parse(parameter[4]);
        } catch {
            throw new ArgumentException("Scene_Change(departureScene, destinationScene, x, y, z)");
        }

        bool through = false;
        foreach (StringPair data in SceneData.data)
        {
            if (data.Key == GlobalConfig.Key.FEATURE_KEY_THROUGH)
            {
                through = true;
                break;
            }
        }
        if (through == false)
        {
            ICommand sceneChangeCommand = new VectorSettingCommand("sceneChangeCommand",
                new List<float>() { 0f }, new(),
                new List<float>() { 0f }, (context, param) =>
                {
                    SceneManager.LoadScene(destinationScene);
                    return CommandQueue.END;
                },
                new StaticClock()
            );
            _sceneChangerInvoker.Do(GlobalConfig.Key.COMMAND_TYPE_SCENE, sceneChangeCommand, 1, CommandQueue.PRIORITY_LOW);
            AddFeature(GlobalConfig.Key.FEATURE_KEY_LOAD, $"{GlobalConfig.Key.FEATURE_VALUE_PLAYER_POSITION},{x},{y},{z}");
            RemoveFeature(GlobalConfig.Key.FEATURE_KEY_SCENE, GlobalConfig.Key.FEATURE_VALUE_CHANGE);
        }
    }
    // Load_PlayerPosition(x, y, z)
    /*
     * 플레이어의 위치를 (x, y, z)로 변경하는 명령을 Invoker에게 전달하고 스스로를 제거합니다.
     */
    private void Load_PlayerPosition(List<string> parameter)
    {
        float x, y, z;
        try
        {
            x = float.Parse(parameter[0]);
            y = float.Parse(parameter[1]);
            z = float.Parse(parameter[2]);
        }
        catch
        {
            throw new ArgumentException("Load_PlayerPosition(x, y, z)");
        }

        ICommand sceneLoadPlayerPosition = new VectorSettingCommand("sceneLoadPlayerPosition",
            new List<float>() { 0f, 0f, 0f }, new(),
            new List<float>() { x, y, z }, (context, param) =>
            {
                context.gameObject.transform.position = new Vector3(param[0], param[1], param[2]);
                return CommandQueue.END;
            },
            new StaticClock()
        );
        _playerInvoker.Do(GlobalConfig.Key.COMMAND_TYPE_MOVEMENT, sceneLoadPlayerPosition, 1, CommandQueue.PRIORITY_HIGH);
        RemoveFeature(GlobalConfig.Key.FEATURE_KEY_LOAD, GlobalConfig.Key.FEATURE_VALUE_PLAYER_POSITION);
    }
    // Load_FadeIn(time)
    /*
     * COMMAND_TYPE_SCENE 명령을 PRIORITY_NORMAL 수준으로 화면이 FadeIn되는 동안 정지시킵니다.
     * 해당 시간 동안 화면을 FadeIn 시키며, 플레이어의 움직임을 정지시킵니다.
     */
    private void Load_FadeIn(List<string> parameter)
    {
        float time;
        try
        {
            time = float.Parse(parameter[0]);
        }
        catch
        {
            throw new ArgumentException("Load_FadeIn(time, targetScene)");
        }
        ICommand sceneChangerBlockCommand = new BlockCommand("sceneChangerBlockCommand", new StaticClock(time, (param) => param));
        _sceneChangerInvoker.Do(GlobalConfig.Key.COMMAND_TYPE_SCENE, sceneChangerBlockCommand, 1, CommandQueue.PRIORITY_NORMAL);

        ICommand veilFadeInCommand = new VectorSettingCommand("veilFadeInCommand",
            new List<float> { 1f }, new(),
            new List<float> { 0f }, (context, param) =>
            {
                RawImage rawImage = context.GetOnlyComponent<RawImage>();
                Color c = rawImage.color;
                c.a = param[0];
                rawImage.color = c;
                return CommandQueue.PROCESS;
            },
            new StaticClock(time, (param) => param)
        );
        _veilInvoker.Do(GlobalConfig.Key.COMMAND_TYPE_FRAME, veilFadeInCommand, 1, CommandQueue.PRIORITY_NORMAL);

        ICommand playerGroundWaitCommand = new GroundWaitCommand("playerGroundWaitCommand", new StaticClock(time, (param) => param), () => false);
        _playerInvoker.Do(GlobalConfig.Key.COMMAND_TYPE_MOVEMENT, playerGroundWaitCommand, 1, CommandQueue.PRIORITY_NORMAL);

        RemoveFeature(GlobalConfig.Key.FEATURE_KEY_LOAD, GlobalConfig.Key.FEATURE_VALUE_FADE_IN);
    }
    // EndScene_FadeOut(time)
    /*
     * COMMAND_TYPE_SCENE 명령을 PRIORITY_NORMAL 수준으로 화면이 FadeOut되는 동안 정지시킵니다.
     * 해당 시간 동안 화면을 FadeOut 시키며, 플레이어의 움직임을 정지시킵니다.
     */
    private void EndScene_FadeOut(List<string> parameter)
    {
        float time;
        try
        {
            time = float.Parse(parameter[0]);
        }
        catch
        {
            throw new ArgumentException("EndScene_FadeOut(time)");
        }

        ICommand sceneChangerBlockCommand = new BlockCommand("sceneChangerBlockCommand", new StaticClock(time, (param) => param));
        _sceneChangerInvoker.Do(GlobalConfig.Key.COMMAND_TYPE_SCENE, sceneChangerBlockCommand, 1, CommandQueue.PRIORITY_NORMAL);

        ICommand veilFadeOutCommand = new VectorSettingCommand("veilFadeOutCommand",
            new List<float> { 0f }, new(),
            new List<float> { 1f }, (context, param) =>
            {
                RawImage rawImage = context.GetOnlyComponent<RawImage>();
                Color c = rawImage.color;
                c.a = param[0];
                rawImage.color = c;
                return CommandQueue.PROCESS;
            },
            new StaticClock(time, (param) => param)
        );
        _veilInvoker.Do(GlobalConfig.Key.COMMAND_TYPE_FRAME, veilFadeOutCommand, 1, CommandQueue.PRIORITY_NORMAL);

        ICommand playerGroundWaitCommand = new GroundWaitCommand("playerGroundWaitCommand", new StaticClock(time, (param) => param), () => false);
        _playerInvoker.Do(GlobalConfig.Key.COMMAND_TYPE_MOVEMENT, playerGroundWaitCommand, 1, CommandQueue.PRIORITY_NORMAL);

        RemoveFeature(GlobalConfig.Key.FEATURE_KEY_END_SCENE, GlobalConfig.Key.FEATURE_VALUE_FADE_OUT);
    }
    // Through_Elevator()
    /*
     * 엘리베이터로 Scene을 전환하고 자신을 제거합니다.
     */
    private void Through_Elevator(List<string> parameter)
    {
        ICommand sceneChangeCommand = new VectorSettingCommand("sceneChangeCommand",
            new List<float>() { 0f }, new(),
            new List<float>() { 0f }, (context, param) =>
            {
                SceneManager.LoadScene("Elevator");
                return CommandQueue.END;
            },
            new StaticClock()
        );
        _sceneChangerInvoker.Do(GlobalConfig.Key.COMMAND_TYPE_SCENE, sceneChangeCommand, 1, CommandQueue.PRIORITY_LOW);
        RemoveFeature(GlobalConfig.Key.FEATURE_KEY_THROUGH, GlobalConfig.Key.FEATURE_VALUE_ELEVATOR);
    }
    // StartElevator_FadeIn(time)
    /*
     * Elevator Scene이라면 화면을 FadeIn 시키고 스스로를 제거하는 FEATURE_KEY_LOAD Feature를 추가하고, 자신을 제거합니다.
     */
    private void StartElevator_FadeIn(List<string> parameter)
    {
        float time;
        try
        {
            time = float.Parse(parameter[0]);
        }
        catch
        {
            throw new ArgumentException("StartElevator_FadeIn(time)");
        }

        AddFeature(GlobalConfig.Key.FEATURE_KEY_LOAD, $"{GlobalConfig.Key.FEATURE_VALUE_FADE_IN},{time}");
        RemoveFeature(GlobalConfig.Key.FEATURE_KEY_START_ELEVATOR, GlobalConfig.Key.FEATURE_VALUE_FADE_IN);
    }
    // EndElevator_FadeOut(time)
    /*
     * Elevator Scene이라면 화면을 FadeOut 시키고 스스로를 제거합니다.
     */
    private void EndElevator_FadeOut(List<string> parameter)
    {
        float time;
        try
        {
            time = float.Parse(parameter[0]);
        }
        catch
        {
            throw new ArgumentException("EndElevator_FadeOut(time)");
        }

        if (SceneManager.GetActiveScene().name == "Elevator")
        {
            ICommand sceneChangerBlockCommand = new BlockCommand("sceneChangerBlockCommand", new StaticClock(time, (param) => param));
            _sceneChangerInvoker.Do(GlobalConfig.Key.COMMAND_TYPE_SCENE, sceneChangerBlockCommand, 1, CommandQueue.PRIORITY_NORMAL);

            ICommand veilFadeOutCommand = new VectorSettingCommand("veilFadeOutCommand",
                new List<float> { 0f }, new(),
                new List<float> { 1f }, (context, param) =>
                {
                    RawImage rawImage = context.GetOnlyComponent<RawImage>();
                    Color c = rawImage.color;
                    c.a = param[0];
                    rawImage.color = c;
                    return CommandQueue.PROCESS;
                },
                new StaticClock(time, (param) => param)
            );
            _veilInvoker.Do(GlobalConfig.Key.COMMAND_TYPE_FRAME, veilFadeOutCommand, 1, CommandQueue.PRIORITY_NORMAL);

            ICommand playerGroundWaitCommand = new GroundWaitCommand("playerGroundWaitCommand", new StaticClock(time, (param) => param), () => false);
            _playerInvoker.Do(GlobalConfig.Key.COMMAND_TYPE_MOVEMENT, playerGroundWaitCommand, 1, CommandQueue.PRIORITY_NORMAL);

            RemoveFeature(GlobalConfig.Key.FEATURE_KEY_END_ELEVATOR, GlobalConfig.Key.FEATURE_VALUE_FADE_OUT);
        }
    }
    // StartScene_FadeIn(time)
    /*
     * sceneChangeData에 Key가 FEATURE_KEY_THROUGH인 요소가 있다면 아무 행동도 하지 않습니다. 목표 Scene으로 가기 전에 통과하는 Scene을 거쳐야 하기 때문입니다.
     * sceneChangeData에 Key가 FEATURE_KEY_THROUGH인 요소가 없다면 화면을 FadeIn하는 FEATURE_KEY_LOAD Feature를 추가합니다.
     */
    private void StartScene_FadeIn(List<string> parameter)
    {
        float time;
        try
        {
            time = float.Parse(parameter[0]);
        }
        catch
        {
            throw new ArgumentException("StartScene_FadeIn(time)");
        }

        bool through = false;
        foreach (StringPair data in SceneData.data)
        {
            if (data.Key == GlobalConfig.Key.FEATURE_KEY_THROUGH)
            {
                through = true;
                break;
            }
        }
        if (through == false)
        {
            AddFeature(GlobalConfig.Key.FEATURE_KEY_LOAD, $"{GlobalConfig.Key.FEATURE_VALUE_FADE_IN},{time}");
            RemoveFeature(GlobalConfig.Key.FEATURE_KEY_START_SCENE, GlobalConfig.Key.FEATURE_VALUE_FADE_IN);
        }
    }

    IEnumerator Start()
    {
        Features[GlobalConfig.Key.FEATURE_KEY_LOAD] = new();
        Features[GlobalConfig.Key.FEATURE_KEY_LOAD][GlobalConfig.Key.FEATURE_VALUE_PLAYER_POSITION + "(3)"] = Load_PlayerPosition;
        Features[GlobalConfig.Key.FEATURE_KEY_LOAD][GlobalConfig.Key.FEATURE_VALUE_FADE_IN + "(1)"] = Load_FadeIn;

        Features[GlobalConfig.Key.FEATURE_KEY_SCENE] = new();
        Features[GlobalConfig.Key.FEATURE_KEY_SCENE][GlobalConfig.Key.FEATURE_VALUE_CHANGE + "(5)"] = Scene_Change;

        Features[GlobalConfig.Key.FEATURE_KEY_END_SCENE] = new();
        Features[GlobalConfig.Key.FEATURE_KEY_END_SCENE][GlobalConfig.Key.FEATURE_VALUE_FADE_OUT + "(1)"] = EndScene_FadeOut;

        Features[GlobalConfig.Key.FEATURE_KEY_THROUGH] = new();
        Features[GlobalConfig.Key.FEATURE_KEY_THROUGH][GlobalConfig.Key.FEATURE_VALUE_ELEVATOR + "(0)"] = Through_Elevator;

        Features[GlobalConfig.Key.FEATURE_KEY_START_ELEVATOR] = new();
        Features[GlobalConfig.Key.FEATURE_KEY_START_ELEVATOR][GlobalConfig.Key.FEATURE_VALUE_FADE_IN + "(1)"] = StartElevator_FadeIn;

        Features[GlobalConfig.Key.FEATURE_KEY_END_ELEVATOR] = new();
        Features[GlobalConfig.Key.FEATURE_KEY_END_ELEVATOR][GlobalConfig.Key.FEATURE_VALUE_FADE_OUT + "(1)"] = EndElevator_FadeOut;

        Features[GlobalConfig.Key.FEATURE_KEY_START_SCENE] = new();
        Features[GlobalConfig.Key.FEATURE_KEY_START_SCENE][GlobalConfig.Key.FEATURE_VALUE_FADE_IN + "(1)"] = StartScene_FadeIn;

        _sceneChangerInvoker = Util.GetOnlyComponent<Invoker>(gameObject);
        _playerInvoker = Util.GetOnlyComponent<Invoker>(GameObject.FindWithTag(GlobalConfig.Key.TAG_PLAYER));
        _veilInvoker = Util.FindOnlyComponentInOnlyChildWithAtrribute<Invoker>(GameObject.FindWithTag(GlobalConfig.Key.TAG_MAIN_CANVAS), GlobalConfig.Key.ATTRIBUTE_VEIL);

        yield return new WaitUntil(() =>
            _sceneChangerInvoker?.IsInitialized == true &&
            _playerInvoker?.IsInitialized == true &&
            _veilInvoker?.IsInitialized == true
        );

        sceneChangerToken.Set("sceneChangerInvokerBlock");

        ICommand sceneChangerInvokerBlockCommand = new VectorSettingCommand("sceneChangerInvokerBlockCommand",
            new List<float>() { 0f }, new(),
            new List<float>() { 0f }, (context, param) =>
            {
                if (sceneChangerToken.Get("sceneChangerInvokerBlock") == true) return CommandQueue.PROCESS;
                return CommandQueue.END;
            },
            new StaticClock()
        );
        _sceneChangerInvoker.Do(GlobalConfig.Key.COMMAND_TYPE_SCENE, sceneChangerInvokerBlockCommand, 1, CommandQueue.PRIORITY_HIGH);

        string featureName = null;
        List<string> parameter = new();

        foreach (StringPair feature in SceneData.data)
        {
            if (feature.Key != GlobalConfig.Key.FEATURE_KEY_LOAD) continue;
            
            SplitEvaluatePhrase(feature.Value, ",", out featureName, out parameter);
            if (Features[feature.Key].ContainsKey($"{featureName}({parameter.Count})") == false)
            {
                GlobalLogger.Error(feature.Value, nameof(SceneChanger), nameof(Start), $"Features[feature.Key].ContainsKey(\"{featureName}({parameter.Count})\") == false");
            }
            else
            {
                try
                {
                    Features[feature.Key][$"{featureName}({parameter.Count})"](parameter);
                }
                catch (Exception ex)
                {
                    GlobalLogger.Error($"{feature.Key},\"{featureName}({parameter.Count})\"", nameof(SceneChanger), nameof(Start), ex.Message);
                }
            }

        }

        sceneChangerToken.Release("sceneChangerInvokerBlock");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Keypad0))
        {
            ICommand veilRemoveCommand = new VectorSettingCommand("veilRemoveCommand",
                new List<float> { 1f }, new(),
                new List<float> { 0f }, (context, param) =>
                {
                    RawImage rawImage = context.GetOnlyComponent<RawImage>();
                    Color c = rawImage.color;
                    c.a = param[0];
                    rawImage.color = c;
                    return CommandQueue.END;
                },
                new StaticClock()
            );
            _veilInvoker.Do(GlobalConfig.Key.COMMAND_TYPE_FRAME, veilRemoveCommand, 1, CommandQueue.PRIORITY_HIGH);
        }
    }

    public static void Operate(List<StringPair> sceneChangeData)
    {
        sceneChangerToken.Set("sceneChangerInvokerBlock");

        ICommand sceneChangerInvokerBlockCommand = new VectorSettingCommand("sceneChangerInvokerBlockCommand",
            new List<float>() { 0f }, new(),
            new List<float>() { 0f }, (context, param) =>
            {
                if (sceneChangerToken.Get("sceneChangerInvokerBlock") == true) return CommandQueue.PROCESS;
                return CommandQueue.END;
            },
            new StaticClock()
        );
        _sceneChangerInvoker.Do(GlobalConfig.Key.COMMAND_TYPE_SCENE, sceneChangerInvokerBlockCommand, 1, CommandQueue.PRIORITY_HIGH);

        foreach (StringPair feature in sceneChangeData) SceneData.data.Add(feature);

        string featureName = null;
        List<string> parameter = new();

        foreach (StringPair feature in SceneData.data)
        {
            if (feature.Key == GlobalConfig.Key.FEATURE_KEY_LOAD) continue;
            if (Features.ContainsKey(feature.Key) == false)
            {
                GlobalLogger.Error(feature.Key, nameof(SceneChanger), nameof(Operate), "Features.ContainsKey(feature.Key) == false");
            }
            else
            {
                SplitEvaluatePhrase(feature.Value, ",", out featureName, out parameter);
                if (Features[feature.Key].ContainsKey($"{featureName}({parameter.Count})") == false)
                {
                    GlobalLogger.Error(feature.Value, nameof(SceneChanger), nameof(Operate), $"Features[feature.Key].ContainsKey(\"{featureName}({parameter.Count})\") == false");
                }
                else
                {
                    try
                    {
                        Features[feature.Key][$"{featureName}({parameter.Count})"](parameter);
                    }
                    catch (Exception ex)
                    {
                        GlobalLogger.Error($"{feature.Key},\"{featureName}({parameter.Count})\"", nameof(SceneChanger), nameof(Operate), ex.Message);
                    }
                }
            }
        }

        sceneChangerToken.Release("sceneChangerInvokerBlock");

    }
}
