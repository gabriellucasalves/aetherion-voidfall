#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class PolishValidation
{
    const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
    public static void Run()
    {
        EditorPrefs.SetBool(T1AtmosScenery.EditorPrefKey,true);
        SessionState.SetBool("PolishQA.Enabled",true);
        Size(1920,1080);
        EditorSceneManager.OpenScene("Assets/Scenes/Ground_T1.unity");
        EditorApplication.isPlaying=true;
    }
    public static void Size(int w,int h)
    {
        var asm=typeof(Editor).Assembly;
        var sizesType=asm.GetType("UnityEditor.GameViewSizes");
        var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes=singleton.GetProperty("instance",Flags).GetValue(null);
        var group=sizesType.GetMethod("GetGroup",Flags).Invoke(sizes,new object[]{sizesType.GetProperty("currentGroupType",Flags).GetValue(sizes)});
        var type=asm.GetType("UnityEditor.GameViewSize");
        var enumType=asm.GetType("UnityEditor.GameViewSizeType");
        var size=Activator.CreateInstance(type,Flags,null,new object[]{Enum.ToObject(enumType,1),w,h,"Polish QA "+w+"x"+h},null);
        int index=(int)group.GetType().GetMethod("GetTotalCount",Flags).Invoke(group,null);
        group.GetType().GetMethod("AddCustomSize",Flags).Invoke(group,new[]{size});
        var window=EditorWindow.GetWindow(asm.GetType("UnityEditor.GameView"));
        window.Show();window.Focus();
        window.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(window,index);
    }
}
#endif
