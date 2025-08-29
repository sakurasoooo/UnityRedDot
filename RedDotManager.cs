using UnityEngine;
using System;
using System.Collections.Generic;

namespace RedDotSystem
{
    /// <summary>
    /// Manages the entire red dot system. It's a singleton.
    /// It builds the Trie from a ScriptableObject config and handles state changes.
    /// </summary>
    public class RedDotManager
    {
        private static readonly Lazy<RedDotManager> _instance = new Lazy<RedDotManager>(() => new RedDotManager());
        public static RedDotManager Instance => _instance.Value;

        private RedDotNode _root;
        private Dictionary<string, Action<bool>> _eventHandlers = new Dictionary<string, Action<bool>>();
        private RedDotSetting _setting;

        private RedDotManager()
        {
            _root = new RedDotNode("Root");
        }

        /// <summary>
        /// Initializes the manager by building the Trie from a ScriptableObject configuration.
        /// </summary>
        /// <param name="setting">The RedDotSetting ScriptableObject defining the red dot tree.</param>
        public void Initialize(RedDotSetting setting)
        {
            if (setting == null)
            {
                 HLogger.LogError("[RedDotManager] RedDotSetting is null!");
                return;
            }

            _setting = setting;

            // 验证配置
            List<string> errors;
            if (!_setting.ValidateConfiguration(out errors))
            {
                 HLogger.LogError($"[RedDotManager] Configuration validation failed:\n{string.Join("\n", errors)}");
                return;
            }

            // 清空现有树
            _root = new RedDotNode("Root");

            // 按层级深度排序，确保父节点先创建
            var sortedPaths = _setting.GetAllPaths();
            sortedPaths.Sort((a, b) => a.GetDepth().CompareTo(b.GetDepth()));

            // 构建树结构
            foreach (var pathData in sortedPaths)
            {
                CreateNodeFromPath(pathData);
            }

             HLogger.Log($"[RedDotManager] Red Dot System Initialized with {_setting.paths.Count} paths.");
        }

        /// <summary>
        /// 从路径数据创建节点
        /// </summary>
        private void CreateNodeFromPath(RedDotPathData pathData)
        {
            if (string.IsNullOrEmpty(pathData.fullPath)) return;

            var pathParts = pathData.fullPath.Split('/');
            RedDotNode currentNode = _root;

            // 逐级创建或获取节点
            for (int i = 0; i < pathParts.Length; i++)
            {
                currentNode = currentNode.AddChild(pathParts[i]);
            }

            // 设置节点描述
            currentNode.description = pathData.description;
        }

        /// <summary>
        /// 从Resources文件夹加载配置并初始化
        /// </summary>
        /// <param name="resourcePath">Resources文件夹中的路径（不包含扩展名）</param>
        public void InitializeFromResources(string resourcePath = "RedDotSetting")
        {
            var setting = Resources.Load<RedDotSetting>(resourcePath);
            if (setting == null)
            {
                 HLogger.LogError($"[RedDotManager] Could not load RedDotSetting from Resources/{resourcePath}");
                return;
            }

            Initialize(setting);
        }

        /// <summary>
        /// Sets the red dot state for a specific leaf node.
        /// This is the primary way to trigger a red dot.
        /// </summary>
        /// <param name="path">The full path to the node (e.g., "Mail/System").</param>
        /// <param name="show">True to show/increment, false to hide/decrement.</param>
        public void SetRedDotState(string path, bool show)
        {
            RedDotNode node = GetNode(path);
            if (node == null)
            {
                 HLogger.LogWarning($"[RedDotManager] Path not found: {path}");
                return;
            }

            if (node.HasChildren)
            {
                 HLogger.LogWarning(
                    $"[RedDotManager] Cannot set state on a non-leaf node directly: {path}. State is determined by children.");
                return;
            }

            node.ChangeCount(show ? 1 : -1);
        }

        /// <summary>
        /// Gets the current red dot count for a given path.
        /// </summary>
        /// <param name="path">The full path to the node.</param>
        /// <returns>The number of active red dots at or below this path.</returns>
        public int GetRedDotCount(string path)
        {
            RedDotNode node = GetNode(path);
            return node?.RedDotCount ?? 0;
        }

        /// <summary>
        /// 检查指定路径是否有红点
        /// </summary>
        /// <param name="path">路径</param>
        /// <returns>是否有红点</returns>
        public bool HasRedDot(string path)
        {
            return GetRedDotCount(path) > 0;
        }

        /// <summary>
        /// 获取所有有红点的叶子节点路径
        /// </summary>
        /// <returns>有红点的叶子节点路径列表</returns>
        public List<string> GetActiveLeafPaths()
        {
            List<string> activePaths = new List<string>();
            if (_setting != null)
            {
                foreach (string leafPath in _setting.GetAllLeafPaths())
                {
                    if (HasRedDot(leafPath))
                    {
                        activePaths.Add(leafPath);
                    }
                }
            }
            return activePaths;
        }

        /// <summary>
        /// 清除指定路径的所有红点
        /// </summary>
        /// <param name="path">路径</param>
        public void ClearRedDots(string path)
        {
            RedDotNode node = GetNode(path);
            if (node == null) return;

            // 如果是叶子节点，直接清除
            if (!node.HasChildren)
            {
                if (node.RedDotCount > 0)
                {
                    node.ChangeCount(-node.RedDotCount);
                }
                return;
            }

            // 如果是父节点，清除所有子叶子节点
            if (_setting != null)
            {
                foreach (string leafPath in _setting.GetAllLeafPaths())
                {
                    if (leafPath.StartsWith(path + "/") || leafPath == path)
                    {
                        var leafNode = GetNode(leafPath);
                        if (leafNode != null && leafNode.RedDotCount > 0)
                        {
                            leafNode.ChangeCount(-leafNode.RedDotCount);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Registers a callback to be invoked when a node's red dot state changes.
        /// </summary>
        /// <param name="path">The path to listen to.</param>
        /// <param name="callback">The action to execute. The boolean parameter is true if a red dot is active.</param>
        public void Register(string path, Action<bool> callback)
        {
            if (!_eventHandlers.ContainsKey(path))
            {
                _eventHandlers[path] = delegate { };
            }

            _eventHandlers[path] += callback;

            // Immediately invoke with current state
            RedDotNode node = GetNode(path);
            callback?.Invoke(node != null && node.RedDotCount > 0);
        }

        /// <summary>
        /// Unregisters a callback.
        /// </summary>
        /// <param name="path">The path to stop listening to.</param>
        /// <param name="callback">The specific action to remove.</param>
        public void Unregister(string path, Action<bool> callback)
        {
            if (_eventHandlers.ContainsKey(path))
            {
                _eventHandlers[path] -= callback;
            }
        }

        /// <summary>
        /// 获取配置信息（只读）
        /// </summary>
        public RedDotSetting GetSetting()
        {
            return _setting;
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        public void ReloadConfiguration()
        {
            if (_setting != null)
            {
                Initialize(_setting);
            }
        }

        internal void Notify(string path, bool isActive)
        {
            if (_eventHandlers.TryGetValue(path, out var handler))
            {
                handler?.Invoke(isActive);
            }
        }

        private RedDotNode GetNode(string path)
        {
            if (string.IsNullOrEmpty(path)) return _root;

            var keys = path.Split('/');
            RedDotNode currentNode = _root;
            foreach (var key in keys)
            {
                currentNode = currentNode.GetChild(key);
                if (currentNode == null)
                {
                    return null;
                }
            }

            return currentNode;
        }

        /// <summary>
        /// 调试用：打印当前红点树状态
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugPrintTree()
        {
            if (_setting == null)
            {
                 HLogger.Log("[RedDotManager] No configuration loaded.");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Red Dot Tree Status ===");
            
            var sortedPaths = _setting.GetAllPaths();
            sortedPaths.Sort((a, b) => a.GetDepth().CompareTo(b.GetDepth()));
            
            foreach (var pathData in sortedPaths)
            {
                string indent = new string(' ', pathData.GetDepth() * 2);
                int count = GetRedDotCount(pathData.fullPath);
                string status = count > 0 ? $"[{count}]" : "";
                string leafMark = pathData.isLeaf ? "(叶子)" : "";
                
                sb.AppendLine($"{indent}- {pathData.fullPath} {status} {leafMark}");
            }
            
             HLogger.Log(sb.ToString());
        }
    }
}