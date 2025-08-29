using System;
using System.Collections.Generic;

namespace RedDotSystem
{
    /// <summary>
    /// This class maps to the structure of the JSON config file.
    /// Used for deserialization.
    /// </summary>
    [Serializable]
    public class RedDotDefinition
    {
        public string key;
        public string description;
        public List<RedDotDefinition> children;
    }
}