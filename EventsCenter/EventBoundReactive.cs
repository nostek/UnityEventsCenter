using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEventsCenter
{
#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(EventBoundReactive), true)]
	class EventBoundReactiveDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			position.height = EditorGUIUtility.singleLineHeight;
			EditorGUI.BeginProperty(position, label, property);
			EditorGUI.PropertyField(position, property.FindPropertyRelative("Target"), label);
			EditorGUI.indentLevel++;
			position.y += EditorGUIUtility.singleLineHeight + 2;

			var propertyName = property.FindPropertyRelative("PropertyName");

			var methods = GetMethods(property);
			var current = Array.FindIndex(methods, (a) => a.tooltip == propertyName.stringValue);
			if (current < 0)
				current = 0;

			var content = new GUIContent("Property", current == 0 ? EditorGUIUtility.IconContent("console.warnicon.sml").image : null);
			var selected = EditorGUI.Popup(position, content, current, methods);

			if (selected == 0 && propertyName.stringValue != null)
				propertyName.stringValue = null;
			else if (methods[selected].tooltip != propertyName.stringValue)
				propertyName.stringValue = methods[selected].tooltip;

			EditorGUI.indentLevel--;
			EditorGUI.EndProperty();
		}

		static GUIContent[] GetMethods(SerializedProperty property)
		{
			var methods = new List<GUIContent> { new("[Invalid]") };

			var prop = property.FindPropertyRelative("Target");
			if (prop == null)
				goto exit;

			var target = prop.objectReferenceValue;
			if (target == null)
				goto exit;

			var type = target.GetType();
			foreach (var methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
			{
				if (methodInfo.ReturnType.Name != "EventReactive`1")
					continue;

				methods.Add(new GUIContent($"{type.Name}.{(methodInfo.Name.StartsWith("get_") ? methodInfo.Name[4..] : methodInfo.Name)}", methodInfo.Name));
			}

		exit:
			return methods.ToArray();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight * 2;
		}
	}
#endif

	[Serializable]
	public abstract class EventBoundReactive
	{
		[SerializeField] protected Object Target;
		[SerializeField] protected string PropertyName;
	}

	[Serializable]
	public class EventBoundReactive<T> : EventBoundReactive
	{
		EventReactive<T> reactive;

		public EventReactive<T> Reactive
		{
			get
			{
				if (reactive == null)
				{
					Debug.Assert(Target, "Missing Target");
					var type = Target.GetType();
					var method = type.GetMethod(PropertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, Array.Empty<Type>(), null);
					Debug.AssertFormat(method != null, Target, "Missing Method {0}", PropertyName);
					var obj = method.Invoke(Target, null);
					Debug.AssertFormat(obj != null, Target, "Null reactive {0} from {1}", typeof(EventReactive<T>), PropertyName);
					reactive = method.Invoke(Target, null) as EventReactive<T>;
					Debug.AssertFormat(reactive != null, Target, "Invalid {0} from {1}", typeof(EventReactive<T>), PropertyName);
				}

				return reactive;
			}
		}
	}
}
