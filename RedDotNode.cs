using System.Collections.Generic;
using UnityEngine;

namespace RedDotSystem
{
    /// <summary>
    /// Represents a single node in the Red Dot Trie.
    /// </summary>
    public class RedDotNode
    {
        public string Key { get; }
        public string FullPath { get; }
        public string description;

        private int _redDotCount = 0;
        public int RedDotCount => _redDotCount;

        private RedDotNode _parent;
        private Dictionary<string, RedDotNode> _children;

        public bool HasChildren => _children != null && _children.Count > 0;

        public RedDotNode(string key, RedDotNode parent = null)
        {
            Key = key;
            _parent = parent;

            if (parent != null && parent.Key != "Root")
            {
                FullPath = $"{parent.FullPath}/{key}";
            }
            else
            {
                FullPath = key;
            }
        }

        public RedDotNode AddChild(string key)
        {
            if (_children == null)
            {
                _children = new Dictionary<string, RedDotNode>();
            }

            if (!_children.ContainsKey(key))
            {
                _children[key] = new RedDotNode(key, this);
            }

            return _children[key];
        }

        public RedDotNode GetChild(string key)
        {
            if (_children != null && _children.TryGetValue(key, out var child))
            {
                return child;
            }

            return null;
        }

        /// <summary>
        /// Changes the red dot count and propagates the change upwards.
        /// </summary>
        /// <param name="change">The amount to change by (e.g., +1 or -1).</param>
        public void ChangeCount(int change)
        {
            if (change == 0) return;

            int oldCount = _redDotCount;
            _redDotCount = Mathf.Max(0, _redDotCount + change);

            if (oldCount != _redDotCount)
            {
                // Notify listeners for this specific node
                RedDotManager.Instance.Notify(this.FullPath, _redDotCount > 0);

                // Propagate change to parent
                if (_parent != null)
                {
                    int parentChange = 0;
                    if (oldCount == 0 && _redDotCount > 0) // Became active
                    {
                        parentChange = 1;
                    }
                    else if (oldCount > 0 && _redDotCount == 0) // Became inactive
                    {
                        parentChange = -1;
                    }

                    if (parentChange != 0)
                    {
                        _parent.ChangeCount(parentChange);
                    }
                }
            }
        }
    }
}