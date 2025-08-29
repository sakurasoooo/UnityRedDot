#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RedDotSystem.Editor
{
    [CustomEditor(typeof(RedDotView))]
    public class RedDotViewEditor : UnityEditor.Editor
    {
        private RedDotView _view;
        // [SerializeField] private RedDotSetting _view.config = _view.config;
        private string[] _availablePaths;
        private int _selectedPathIndex = -1;
        private bool _showPathSelection = true;
        private bool _showPreview = true;
        private string _searchFilter = "";

        private void OnEnable()
        {
            _view = (RedDotView)target;
            RefreshAvailablePaths();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("红点视图组件", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawSettingSelection();
            DrawPathSelection();
            DrawRedDotObjectField();
            DrawPreviewSection();
            DrawUtilityButtons();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_view);
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawSettingSelection()
        {
            EditorGUILayout.LabelField("配置文件", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();

            var newSetting =
                EditorGUILayout.ObjectField("RedDot Setting", _view.config, typeof(RedDotSetting), false) as
                    RedDotSetting;

            if (newSetting != _view.config)
            {
                _view.config = newSetting;
                RefreshAvailablePaths();
            }

            if (GUILayout.Button("自动查找", GUILayout.Width(80)))
            {
                AutoFindSetting();
            }

            EditorGUILayout.EndHorizontal();

            if (_view.config == null)
            {
                EditorGUILayout.HelpBox("请先设置或查找 RedDotSetting 配置文件", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField($"可用路径数量: {_availablePaths?.Length ?? 0}", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private Vector2 _pathScrollPosition; // 添加这个类级变量来保存滚动位置

        private void DrawPathSelection()
        {
            _showPathSelection = EditorGUILayout.Foldout(_showPathSelection, "路径选择", true);
            if (!_showPathSelection) return;

            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (_availablePaths == null || _availablePaths.Length == 0)
            {
                EditorGUILayout.HelpBox("没有可用的路径。请确保 RedDotSetting 已正确配置。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // 当前路径显示
            EditorGUILayout.LabelField("当前路径", EditorStyles.boldLabel);
            var redDotPathProperty = serializedObject.FindProperty("redDotPath");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(redDotPathProperty, GUIContent.none);

            if (GUILayout.Button("清空", GUILayout.Width(50)))
            {
                redDotPathProperty.stringValue = "";
                _selectedPathIndex = -1;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 搜索过滤
            EditorGUILayout.LabelField("路径选择器", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _searchFilter = EditorGUILayout.TextField("搜索路径", _searchFilter);
            if (GUILayout.Button("清空", GUILayout.Width(50)))
            {
                _searchFilter = "";
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 过滤路径
            var filteredPaths = string.IsNullOrEmpty(_searchFilter)
                ? _availablePaths
                : _availablePaths.Where(p => p.ToLower().Contains(_searchFilter.ToLower())).ToArray();

            if (filteredPaths.Length == 0)
            {
                EditorGUILayout.LabelField("没有匹配的路径", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                // 路径列表 - 修复滚动视图
                _pathScrollPosition = EditorGUILayout.BeginScrollView(_pathScrollPosition, GUILayout.MaxHeight(200));

                foreach (var path in filteredPaths)
                {
                    DrawPathItem(path, redDotPathProperty);
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawPathItem(string path, SerializedProperty pathProperty)
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);

            // 计算缩进
            int depth = path.Split('/').Length - 1;
            GUILayout.Space(depth * 15);

            // 路径信息
            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();

            // 路径名称
            bool isSelected = pathProperty.stringValue == path;
            if (isSelected)
            {
                GUI.color = Color.cyan;
            }

            EditorGUILayout.LabelField(path, isSelected ? EditorStyles.boldLabel : EditorStyles.label);

            GUI.color = Color.white;

            // 选择按钮
            if (GUILayout.Button(isSelected ? "已选择" : "选择", GUILayout.Width(60)))
            {
                if (!isSelected)
                {
                    pathProperty.stringValue = path;
                    serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUILayout.EndHorizontal();

            // 显示路径描述（如果有）
            if (_view.config != null)
            {
                var pathData = _view.config.GetPathData(path);
                if (pathData != null && !string.IsNullOrEmpty(pathData.description))
                {
                    EditorGUILayout.LabelField($"描述: {pathData.description}", EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRedDotObjectField()
        {
            EditorGUILayout.LabelField("红点对象", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            var redDotObjectProperty = serializedObject.FindProperty("redDotObject");
            EditorGUILayout.PropertyField(redDotObjectProperty, new GUIContent("红点GameObject"));

            if (redDotObjectProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("请设置要显示/隐藏的红点GameObject", MessageType.Warning);

                if (GUILayout.Button("自动查找子对象中的红点"))
                {
                    AutoFindRedDotObject();
                }
            }
            else
            {
                var redDotObj = redDotObjectProperty.objectReferenceValue as GameObject;
                EditorGUILayout.LabelField($"当前状态: {(redDotObj.activeSelf ? "显示" : "隐藏")}", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawPreviewSection()
        {
            _showPreview = EditorGUILayout.Foldout(_showPreview, "预览和测试", true);
            if (!_showPreview) return;

            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("预览功能需要在运行时使用", MessageType.Info);
            }
            else
            {
                // 显示当前路径的红点状态
                string currentPath = _view.redDotPath;
                if (!string.IsNullOrEmpty(currentPath))
                {
                    bool hasRedDot = RedDotManager.Instance.HasRedDot(currentPath);
                    int redDotCount = RedDotManager.Instance.GetRedDotCount(currentPath);

                    EditorGUILayout.LabelField($"路径: {currentPath}");
                    EditorGUILayout.LabelField($"红点状态: {(hasRedDot ? "有红点" : "无红点")}");
                    EditorGUILayout.LabelField($"红点数量: {redDotCount}");

                    EditorGUILayout.Space();

                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button("测试激活红点"))
                    {
                        RedDotManager.Instance.SetRedDotState(currentPath, true);
                    }

                    if (GUILayout.Button("测试清除红点"))
                    {
                        RedDotManager.Instance.SetRedDotState(currentPath, false);
                    }

                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.LabelField("请先设置红点路径");
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawUtilityButtons()
        {
            EditorGUILayout.LabelField("实用工具", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("验证配置"))
            {
                ValidateConfiguration();
            }

            if (GUILayout.Button("刷新路径列表"))
            {
                RefreshAvailablePaths();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("复制路径到剪贴板"))
            {
                if (!string.IsNullOrEmpty(_view.redDotPath))
                {
                    EditorGUIUtility.systemCopyBuffer = _view.redDotPath;
                    Debug.Log($"已复制路径到剪贴板: {_view.redDotPath}");
                }
            }

            if (GUILayout.Button("从剪贴板粘贴路径"))
            {
                string clipboardText = EditorGUIUtility.systemCopyBuffer;
                if (!string.IsNullOrEmpty(clipboardText))
                {
                    var pathProperty = serializedObject.FindProperty("redDotPath");
                    pathProperty.stringValue = clipboardText;
                    serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void RefreshAvailablePaths()
        {
            if (_view.config == null)
            {
                _availablePaths = new string[0];
                return;
            }

            var allPaths = _view.config.GetAllPaths();
            _availablePaths = allPaths.Select(p => p.fullPath).OrderBy(p => p).ToArray();

            // 更新选中索引
            if (!string.IsNullOrEmpty(_view.redDotPath))
            {
                _selectedPathIndex = System.Array.IndexOf(_availablePaths, _view.redDotPath);
            }
        }

        private void AutoFindSetting()
        {
            // 在项目中查找 RedDotSetting 类型的资源
            string[] guids = AssetDatabase.FindAssets("t:RedDotSetting");

            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("未找到", "项目中没有找到 RedDotSetting 资源", "确定");
                return;
            }

            if (guids.Length == 1)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _view.config = AssetDatabase.LoadAssetAtPath<RedDotSetting>(path);
                RefreshAvailablePaths();
                return;
            }

            // 多个配置文件，让用户选择
            GenericMenu menu = new GenericMenu();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                menu.AddItem(new GUIContent(fileName), false, () =>
                {
                    _view.config = AssetDatabase.LoadAssetAtPath<RedDotSetting>(path);
                    RefreshAvailablePaths();
                });
            }

            menu.ShowAsContext();
        }

        private void AutoFindRedDotObject()
        {
            // 在子对象中查找名称包含 "red" 或 "dot" 的GameObject
            Transform[] children = _view.GetComponentsInChildren<Transform>();

            List<GameObject> candidates = new List<GameObject>();
            foreach (Transform child in children)
            {
                if (child == _view.transform) continue;

                string name = child.name.ToLower();
                if (name.Contains("red") || name.Contains("dot") || name.Contains("point"))
                {
                    candidates.Add(child.gameObject);
                }
            }

            if (candidates.Count == 0)
            {
                EditorUtility.DisplayDialog("未找到", "没有找到可能的红点对象", "确定");
                return;
            }

            if (candidates.Count == 1)
            {
                var redDotObjectProperty = serializedObject.FindProperty("redDotObject");
                redDotObjectProperty.objectReferenceValue = candidates[0];
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // 多个候选对象，让用户选择
            GenericMenu menu = new GenericMenu();
            foreach (GameObject candidate in candidates)
            {
                menu.AddItem(new GUIContent(candidate.name), false, () =>
                {
                    var redDotObjectProperty = serializedObject.FindProperty("redDotObject");
                    redDotObjectProperty.objectReferenceValue = candidate;
                    serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }

        private void ValidateConfiguration()
        {
            List<string> issues = new List<string>();

            // 检查路径是否为空
            if (string.IsNullOrEmpty(_view.redDotPath))
            {
                issues.Add("红点路径为空");
            }
            else
            {
                // 检查路径是否存在于配置中
                if (_view.config == null)
                {
                    issues.Add("没有设置 RedDotSetting 配置文件");
                }
                else if (!_view.config.HasPath(_view.redDotPath))
                {
                    issues.Add($"路径 '{_view.redDotPath}' 在配置文件中不存在");
                }
            }

            // 检查红点对象
            if (_view.redDotObject == null)
            {
                issues.Add("没有设置红点GameObject");
            }

            // 显示验证结果
            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("验证通过", "RedDotView 配置正确！", "确定");
            }
            else
            {
                string message = "发现以下问题：\n\n" + string.Join("\n", issues);
                EditorUtility.DisplayDialog("验证失败", message, "确定");
            }
        }
    }
}
#endif