using Kudoshi.Utilities;
using System;
using System.Collections;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.GraphicsBuffer;
public class Util
{
    public static void WaitNextFrame(MonoBehaviour monoBehaviour, Action func)
    {
        monoBehaviour.StartCoroutine(_CallFunctionNextFrame(func));
    }

    public static void WaitForSeconds(MonoBehaviour monoBehaviour, Action func, float seconds, bool inRealtime = false)
    {
        if (inRealtime)
            monoBehaviour.StartCoroutine(_CallFunctionRealtime(func, seconds));
        else
            monoBehaviour.StartCoroutine(_CallFunctionNextFrame(func, seconds));

    }

    public static void WaitUntil(MonoBehaviour monoBehaviour, Action func, Func<bool> predicate)
    {
        monoBehaviour.StartCoroutine(_WaitUntilCr(func, predicate));
    }

    private static IEnumerator _WaitUntilCr(Action func, Func<bool> predicate)
    {
        yield return new WaitUntil(predicate);

        func?.Invoke();
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


    /// <summary>
    /// Use to set dont destroy on load on a game object
    /// 
    /// Checks the root game object to see if it is DontDestroyOnLoad, if not separate the component itself and mark itself as dont destroy on load
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="gameObj">The script to </param>
    public static void SetDontDestroyOnLoad(GameObject gameObj)
    {
        if (gameObj == null)
        {
            Debug.LogWarning($"[{gameObj.transform.name}] is null, cannot mark as DontDestroyOnLoad.");
            return;
        }
        
        // Check root object is DontDestroyOnLoad

        // Get the topmost parent
        Transform root = gameObj.transform;
        while (root.parent != null)
            root = root.parent;

        GameObject rootObj = root.gameObject;

        // Check what scene the root is in
        Scene scene = rootObj.scene;

        if (scene.name == "DontDestroyOnLoad")
        {
            // Already marked as dont destroy at root object. Just leave it be
            return;
        }
        else
        {
            // Root object is not dont destroy on load. Separate the component and mark itself as dont destroy
            gameObj.transform.SetParent(null);
            UnityEngine.Object.DontDestroyOnLoad(gameObj.gameObject);
        }
    }

    public static void SpawnDebugSphere(Vector3 position, Vector3? scale = null)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = position;
        sphere.GetComponent<Collider>().enabled = false;

        if (scale.HasValue)
        {
            sphere.transform.localScale = scale.Value;
        }
        else
            sphere.transform.localScale = new Vector3(0.4f, .4f, .4f);
    }
}

