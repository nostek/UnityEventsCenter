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
			var propTarget = property.FindPropertyRelative("target");
			var propPropertyName = property.FindPropertyRelative("propertyName");

			position.height = EditorGUIUtility.singleLineHeight;
			EditorGUI.BeginProperty(position, label, property);
			EditorGUI.PropertyField(position, propTarget, label);

			EditorGUI.indentLevel++;
			position.y += EditorGUIUtility.singleLineHeight + 2;

			(var methods, var targets) = GetMethods(propTarget);

			int current = 0;
			for (int i = 0; i < methods.Length; i++)
				if (targets[i] == propTarget.objectReferenceValue && methods[i].tooltip == propPropertyName.stringValue)
				{
					current = i;
					break;
				}

			int selected = 0;

			if (property.type == "EventBoundReactiveValue`1" && propTarget.objectReferenceValue == null)
			{
				EditorGUI.PropertyField(position, property.FindPropertyRelative("initialValue"), new GUIContent("Initial Value"));
			}
			else
			{
				var propertyNameLabel = new GUIContent("Property", current == 0 ? EditorGUIUtility.IconContent("console.warnicon.sml").image : null);
				selected = EditorGUI.Popup(position, propertyNameLabel, current, methods);
			}

			if (!string.IsNullOrEmpty(methods[selected].tooltip) && propTarget.objectReferenceValue != targets[selected])
				propTarget.objectReferenceValue = targets[selected];

			if (string.IsNullOrEmpty(methods[selected].tooltip) && propPropertyName.stringValue != null)
				propPropertyName.stringValue = null;
			else if (methods[selected].tooltip != propPropertyName.stringValue)
				propPropertyName.stringValue = methods[selected].tooltip;

			EditorGUI.indentLevel--;
			EditorGUI.EndProperty();
		}

		static (GUIContent[] methods, List<Object> targets) GetMethods(SerializedProperty propTarget)
		{
			var methods = new List<GUIContent> { new("[Invalid]") };
			var targets = new List<Object>() { null };

			var target = propTarget.objectReferenceValue;
			if (target == null)
				goto exit;

			GameObject collection = null;

			if (target is GameObject go)
				collection = go;
			else if (target is Component component)
				collection = component.gameObject;

			if (collection)
			{
				var subs = collection.GetComponentsInChildren<Component>();
				int index = 0;
				foreach (var sub in subs)
				{
					var type = sub.GetType();
					foreach (var methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
					{
						if (methodInfo.ReturnType.Name != "EventReactive`1")
							continue;

						methods.Add(new GUIContent($"[{index}] {type.Name}/{(methodInfo.Name.StartsWith("get_") ? methodInfo.Name[4..] : methodInfo.Name)}", methodInfo.Name));
						targets.Add(sub);
					}

					index++;
				}
			}
			else
			{
				var type = target.GetType();
				foreach (var methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
				{
					if (methodInfo.ReturnType.Name != "EventReactive`1")
						continue;

					methods.Add(new GUIContent($"{type.Name}.{(methodInfo.Name.StartsWith("get_") ? methodInfo.Name[4..] : methodInfo.Name)}", methodInfo.Name));
					targets.Add(target);
				}
			}

		exit:
			return (methods.ToArray(), targets);
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
		[SerializeField] protected Object target;
		[SerializeField] protected string propertyName;

		public Object Target => target;
		public bool IsBound => target != null;
	}

	[Serializable]
	public class EventBoundReactive<T> : EventBoundReactive
	{
		protected EventReactive<T> reactive;

		public EventReactive<T> Reactive
		{
			get
			{
				if (reactive == null)
				{
					Debug.Assert(target, "Missing Target");
					var type = target.GetType();
					var method = type.GetMethod(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, Array.Empty<Type>(), null);
					Debug.AssertFormat(method != null, target, "Missing Method {0}", propertyName);
					var obj = method.Invoke(target, null);
					Debug.AssertFormat(obj != null, target, "Null reactive {0} from {1}", typeof(EventReactive<T>), propertyName);
					reactive = method.Invoke(target, null) as EventReactive<T>;
					Debug.AssertFormat(reactive != null, target, "Invalid {0} from {1}", typeof(EventReactive<T>), propertyName);
				}

				return reactive;
			}
		}
	}

	[Serializable]
	public class EventBoundReactiveValue<T> : EventBoundReactive<T>
	{
		[SerializeField] T initialValue;

		public EventBoundReactiveValue(T defaultValue)
		{
			initialValue = defaultValue;
		}

		public EventBoundReactiveValue()
		{

		}

		public T InitialValue => initialValue;

		public new EventReactive<T> Reactive
		{
			get
			{
				if (reactive == null && !IsBound)
					reactive = new EventReactive<T>(initialValue);

				return base.Reactive;
			}
		}
	}
}
