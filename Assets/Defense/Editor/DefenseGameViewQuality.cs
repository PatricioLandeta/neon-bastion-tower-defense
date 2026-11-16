using System.Reflection;
using UnityEditor;
using UnityEngine;

// Unity's low-resolution Game view can magnify every rendered pixel on Retina displays.
// Keep this project sharp without changing the user's desktop display resolution.
public static class DefenseGameViewQuality
{
    private const string SessionKey="NeonBastionNativeResolution";
    [InitializeOnLoadMethod]
    private static void Schedule()
    {
        if(Application.isBatchMode || SessionState.GetBool(SessionKey,false)) return;
        EditorApplication.delayCall+=Apply;
    }

    [MenuItem("Tools/Neon Bastion/Sharpen Game View")]
    public static void Apply()
    {
        System.Type type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if(type==null) return;
        foreach(Object window in Resources.FindObjectsOfTypeAll(type))
        {
            var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
            PropertyInfo lowResolution=type.GetProperty("lowResolutionForAspectRatios",flags);
            if(lowResolution!=null && lowResolution.CanWrite) lowResolution.SetValue(window,false);
            SerializedObject data=new SerializedObject(window);
            SerializedProperty resolutions=data.FindProperty("m_LowResolutionForAspectRatios");
            if(resolutions!=null && resolutions.isArray)
                for(int i=0;i<resolutions.arraySize;i++)
                {
                    SerializedProperty item=resolutions.GetArrayElementAtIndex(i);
                    if(item.propertyType==SerializedPropertyType.Boolean) item.boolValue=false;
                    else if(item.propertyType==SerializedPropertyType.Integer) item.intValue=0;
                }
            SerializedProperty zoom=data.FindProperty("m_ZoomArea.m_Scale");
            if(zoom!=null && zoom.propertyType==SerializedPropertyType.Vector2) zoom.vector2Value=Vector2.one;
            SerializedProperty defaultZoom=data.FindProperty("m_defaultScale");
            if(defaultZoom!=null && defaultZoom.propertyType==SerializedPropertyType.Float) defaultZoom.floatValue=1f;
            data.ApplyModifiedPropertiesWithoutUndo();
            ((EditorWindow)window).Repaint();
        }
        SessionState.SetBool(SessionKey,true);
    }
}
