using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedDotSystem
{
    [CreateAssetMenu(fileName = "RedDotSetting", menuName = "RedDotSystem/Red Dot Setting", order = 1)]
    public class RedDotSetting : ScriptableObject
    {
        [SerializeField]
        public List<RedDotPathData> paths = new List<RedDotPathData>();
        
        [Header("Debug Info")]
        [SerializeField, TextArea(5, 10)]
        public string debugInfo = "红点路径配置\n\n使用编辑器工具添加和管理路径";

        /// <summary>
        /// 获取所有叶子节点路径
        /// </summary>
        public List<string> GetAllLeafPaths()
        {
            List<string> leafPaths = new List<string>();
            foreach (var path in paths)
            {
                if (path.isLeaf)
                {
                    leafPaths.Add(path.fullPath);
                }
            }
            return leafPaths;
        }

        /// <summary>
        /// 获取所有路径数据
        /// </summary>
        public List<RedDotPathData> GetAllPaths()
        {
            return new List<RedDotPathData>(paths);
        }

        /// <summary>
        /// 检查路径是否存在
        /// </summary>
        public bool HasPath(string path)
        {
            return paths.Exists(p => p.fullPath == path);
        }

        /// <summary>
        /// 获取指定路径的数据
        /// </summary>
        public RedDotPathData GetPathData(string path)
        {
            return paths.Find(p => p.fullPath == path);
        }

        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        public bool ValidateConfiguration(out List<string> errors)
        {
            errors = new List<string>();
            
            // 检查重复路径
            HashSet<string> uniquePaths = new HashSet<string>();
            foreach (var path in paths)
            {
                if (!uniquePaths.Add(path.fullPath))
                {
                    errors.Add($"重复路径: {path.fullPath}");
                }
            }
            
            // 检查父子关系
            foreach (var path in paths)
            {
                if (!string.IsNullOrEmpty(path.parentPath))
                {
                    if (!HasPath(path.parentPath))
                    {
                        errors.Add($"路径 '{path.fullPath}' 的父路径 '{path.parentPath}' 不存在");
                    }
                }
            }
            
            return errors.Count == 0;
        }
    }

    [Serializable]
    public class RedDotPathData
    {
        [Header("路径信息")]
        public string fullPath;
        public string description;
        
        [Header("层级关系")]
        public string parentPath;
        public bool isLeaf = true;
        
        [Header("显示设置")]
        public bool isVisible = true;
        public int priority = 0;
        
        public RedDotPathData()
        {
        }
        
        public RedDotPathData(string path, string desc = "", string parent = "", bool leaf = true)
        {
            fullPath = path;
            description = desc;
            parentPath = parent;
            isLeaf = leaf;
        }
        
        /// <summary>
        /// 获取路径的最后一个部分（节点名）
        /// </summary>
        public string GetNodeName()
        {
            if (string.IsNullOrEmpty(fullPath)) return "";
            
            int lastSlash = fullPath.LastIndexOf('/');
            return lastSlash >= 0 ? fullPath.Substring(lastSlash + 1) : fullPath;
        }
        
        /// <summary>
        /// 获取路径层级深度
        /// </summary>
        public int GetDepth()
        {
            if (string.IsNullOrEmpty(fullPath)) return 0;
            return fullPath.Split('/').Length;
        }
    }
}