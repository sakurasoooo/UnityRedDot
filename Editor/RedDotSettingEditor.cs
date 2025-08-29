#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RedDotSystem.Editor
{
    [CustomEditor(typeof(RedDotSetting))]
    public class RedDotSettingEditor : UnityEditor.Editor
    {
        private RedDotSetting _setting;
        private Vector2 _scrollPosition;
        private string _newPathInput = "";
        private string _newDescriptionInput = "";
        private string _selectedParentPath = "";
        private bool _isNewPathLeaf = true;
        private bool _showAddPathFoldout = true;
        private bool _showValidationFoldout = true;
        private string _searchFilter = "";
        
        private void OnEnable()
        {
            _setting = (RedDotSetting)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("红点路径配置管理器", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawValidationSection();
            DrawAddPathSection();
            DrawPathListSection();
            DrawDebugSection();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_setting);
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawValidationSection()
        {
            _showValidationFoldout = EditorGUILayout.Foldout(_showValidationFoldout, "配置验证", true);
            if (_showValidationFoldout)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                
                if (GUILayout.Button("验证配置", GUILayout.Height(25)))
                {
                    ValidateConfiguration();
                }
                
                EditorGUILayout.LabelField($"总路径数量: {_setting.paths.Count}");
                EditorGUILayout.LabelField($"叶子节点数量: {_setting.GetAllLeafPaths().Count}");
                
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.Space();
        }

        private void DrawAddPathSection()
        {
            _showAddPathFoldout = EditorGUILayout.Foldout(_showAddPathFoldout, "添加新路径", true);
            if (_showAddPathFoldout)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                
                EditorGUILayout.LabelField("新路径信息", EditorStyles.boldLabel);
                
                _newPathInput = EditorGUILayout.TextField("路径", _newPathInput);
                _newDescriptionInput = EditorGUILayout.TextField("描述", _newDescriptionInput);
                
                // 父路径选择
                DrawParentPathSelection();
                
                _isNewPathLeaf = EditorGUILayout.Toggle("是叶子节点", _isNewPathLeaf);
                
                EditorGUILayout.Space();
                
                EditorGUILayout.BeginHorizontal();
                
                GUI.enabled = !string.IsNullOrEmpty(_newPathInput) && !_setting.HasPath(_newPathInput);
                if (GUILayout.Button("添加路径"))
                {
                    AddNewPath();
                }
                GUI.enabled = true;
                
                if (GUILayout.Button("清空输入"))
                {
                    ClearInput();
                }
                
                EditorGUILayout.EndHorizontal();
                
                // 显示错误信息
                if (!string.IsNullOrEmpty(_newPathInput) && _setting.HasPath(_newPathInput))
                {
                    EditorGUILayout.HelpBox("路径已存在！", MessageType.Warning);
                }
                
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.Space();
        }

        private void DrawParentPathSelection()
        {
            var availablePaths = _setting.paths.Select(p => p.fullPath).ToList();
            availablePaths.Insert(0, "无父路径");
            
            int currentIndex = string.IsNullOrEmpty(_selectedParentPath) ? 0 : 
                availablePaths.FindIndex(p => p == _selectedParentPath);
            if (currentIndex == -1) currentIndex = 0;
            
            int newIndex = EditorGUILayout.Popup("父路径", currentIndex, availablePaths.ToArray());
            _selectedParentPath = newIndex == 0 ? "" : availablePaths[newIndex];
        }

        private void DrawPathListSection()
        {
            EditorGUILayout.LabelField("路径列表", EditorStyles.boldLabel);
            
            // 搜索过滤
            EditorGUILayout.BeginHorizontal();
            _searchFilter = EditorGUILayout.TextField("搜索路径", _searchFilter);
            if (GUILayout.Button("清空", GUILayout.Width(50)))
            {
                _searchFilter = "";
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MaxHeight(300));
            
            var filteredPaths = string.IsNullOrEmpty(_searchFilter) ? 
                _setting.paths : 
                _setting.paths.Where(p => p.fullPath.ToLower().Contains(_searchFilter.ToLower())).ToList();
            
            // 按层级和字母顺序排序
            filteredPaths = filteredPaths.OrderBy(p => p.GetDepth()).ThenBy(p => p.fullPath).ToList();
            
            for (int i = 0; i < filteredPaths.Count; i++)
            {
                DrawPathItem(filteredPaths[i], i);
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("清空所有路径"))
            {
                if (EditorUtility.DisplayDialog("确认", "确定要清空所有路径吗？", "确定", "取消"))
                {
                    _setting.paths.Clear();
                }
            }
            
            if (GUILayout.Button("添加示例路径"))
            {
                AddExamplePaths();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPathItem(RedDotPathData pathData, int index)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            EditorGUILayout.BeginHorizontal();
            
            // 缩进显示层级
            int depth = pathData.GetDepth();
            GUILayout.Space(depth * 15);
            
            // 路径信息
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(pathData.fullPath, EditorStyles.boldLabel);
            
            // 标签
            if (pathData.isLeaf)
            {
                GUI.color = Color.green;
                GUILayout.Label("叶子", GUILayout.Width(30));
                GUI.color = Color.white;
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (!string.IsNullOrEmpty(pathData.description))
            {
                EditorGUILayout.LabelField($"描述: {pathData.description}", EditorStyles.miniLabel);
            }
            
            if (!string.IsNullOrEmpty(pathData.parentPath))
            {
                EditorGUILayout.LabelField($"父路径: {pathData.parentPath}", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
            
            // 操作按钮
            EditorGUILayout.BeginVertical(GUILayout.Width(80));
            
            if (GUILayout.Button("编辑", GUILayout.Width(50)))
            {
                EditPath(pathData);
            }
            
            GUI.color = Color.red;
            if (GUILayout.Button("删除", GUILayout.Width(50)))
            {
                if (EditorUtility.DisplayDialog("确认删除", $"确定要删除路径 '{pathData.fullPath}' 吗？", "删除", "取消"))
                {
                    _setting.paths.Remove(pathData);
                }
            }
            GUI.color = Color.white;
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawDebugSection()
        {
            EditorGUILayout.LabelField("调试信息", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            var debugInfoProperty = serializedObject.FindProperty("debugInfo");
            EditorGUILayout.PropertyField(debugInfoProperty);
            
            if (GUILayout.Button("更新调试信息"))
            {
                UpdateDebugInfo();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void ValidateConfiguration()
        {
            List<string> errors;
            bool isValid = _setting.ValidateConfiguration(out errors);
            
            if (isValid)
            {
                EditorUtility.DisplayDialog("验证结果", "配置验证通过！", "确定");
            }
            else
            {
                string errorMessage = "发现以下错误：\n\n" + string.Join("\n", errors);
                EditorUtility.DisplayDialog("验证结果", errorMessage, "确定");
            }
        }

        private void AddNewPath()
        {
            var newPath = new RedDotPathData(_newPathInput, _newDescriptionInput, _selectedParentPath, _isNewPathLeaf);
            _setting.paths.Add(newPath);
            
            // 如果添加了非叶子节点，需要更新父节点的叶子状态
            if (!string.IsNullOrEmpty(_selectedParentPath))
            {
                var parentData = _setting.GetPathData(_selectedParentPath);
                if (parentData != null)
                {
                    parentData.isLeaf = false;
                }
            }
            
            ClearInput();
            UpdateDebugInfo();
        }

        private void ClearInput()
        {
            _newPathInput = "";
            _newDescriptionInput = "";
            _selectedParentPath = "";
            _isNewPathLeaf = true;
        }

        private void EditPath(RedDotPathData pathData)
        {
            _newPathInput = pathData.fullPath;
            _newDescriptionInput = pathData.description;
            _selectedParentPath = pathData.parentPath;
            _isNewPathLeaf = pathData.isLeaf;
            
            // 删除原路径，等待重新添加
            _setting.paths.Remove(pathData);
        }

        private void AddExamplePaths()
        {
            if (EditorUtility.DisplayDialog("添加示例", "这将添加一些示例路径，继续吗？", "确定", "取消"))
            {
                var examples = new List<RedDotPathData>
                {
                    new RedDotPathData("Mail", "邮件系统", "", false),
                    new RedDotPathData("Mail/System", "系统邮件", "Mail", true),
                    new RedDotPathData("Mail/Friend", "好友邮件", "Mail", true),
                    new RedDotPathData("Shop", "商店系统", "", false),
                    new RedDotPathData("Shop/Weapon", "武器商店", "Shop", true),
                    new RedDotPathData("Shop/Armor", "装备商店", "Shop", true),
                    new RedDotPathData("Achievement", "成就系统", "", false),
                    new RedDotPathData("Achievement/Daily", "日常成就", "Achievement", true),
                    new RedDotPathData("Achievement/Main", "主线成就", "Achievement", true)
                };
                
                foreach (var example in examples)
                {
                    if (!_setting.HasPath(example.fullPath))
                    {
                        _setting.paths.Add(example);
                    }
                }
                
                UpdateDebugInfo();
            }
        }

        private void UpdateDebugInfo()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine("红点路径配置");
            info.AppendLine($"总数量: {_setting.paths.Count}");
            info.AppendLine($"叶子节点: {_setting.GetAllLeafPaths().Count}");
            info.AppendLine();
            info.AppendLine("所有路径:");
            
            var sortedPaths = _setting.paths.OrderBy(p => p.GetDepth()).ThenBy(p => p.fullPath);
            foreach (var path in sortedPaths)
            {
                string indent = new string(' ', path.GetDepth() * 2);
                string leafMark = path.isLeaf ? " [叶子]" : "";
                info.AppendLine($"{indent}- {path.fullPath}{leafMark}");
            }
            
            _setting.debugInfo = info.ToString();
        }
    }
}
#endif