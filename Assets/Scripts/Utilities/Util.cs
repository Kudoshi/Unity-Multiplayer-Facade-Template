using System;
using System.Collections;
using UnityEngine;
public class Util
{
    public static void WaitNextFrame(MonoBehaviour monoBehaviour, Action func)
    {
        monoBehaviour.StartCoroutine(_CallFunctionNextFrame(func));
    }

    public static void WaitForSeconds(MonoBehaviour monoBehaviour, Action func, float seconds, bool inRealtime = false)
    {
        if (inRealtime)
            monoBehaviour.StartCoroutine(_CallFunctionNextFrame(func, seconds));
        else
            monoBehaviour.StartCoroutine(_CallFunctionRealtime(func, seconds));
    }

    private static IEnumerator _CallFunctionNextFrame(Action func, float seconds = 0)
    {

        if (seconds > 0)
        {
            yield return new WaitForSeconds(seconds);
        }
        else
            yield return null;

        func();
    }

    private static IEnumerator _CallFunctionRealtime(Action func, float seconds = 0)
    {
        yield return new WaitForSecondsRealtime(seconds);

        func();
    }

    public static void PrintSelectedLayers(LayerMask mask)
    {
        for (int i = 0; i < 32; i++) // Unity has 32 layers (0-31)
        {
            if ((mask.value & (1 << i)) != 0)
            {
                Debug.Log($"Layer {i} is selected: {LayerMask.LayerToName(i)}");
            }
        }
    }

    public static bool LayerMaskContain(int checkLayer, LayerMask targetMask)
    {
        return (targetMask & (1 << checkLayer)) != 0;
    }

    public static string FormatTimeStandard(float time) // "02:30"
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public static string FormatTimeShort(float time) // "2:30"
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        return string.Format("{0:#0}:{1:00}", minutes, seconds);
    }

    public static void DestroyAllChildren(GameObject parent)
    {
        Transform t = parent.transform;
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            GameObject child = t.GetChild(i).gameObject;
            UnityEngine.Object.Destroy(child);
        }
    }
}

