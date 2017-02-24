﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace HarmonizeGitHooks
{
    public class PathingConfig
    {
        [XmlAttribute]
        public int Version = 1;
        public List<PathingListing> Paths = new List<PathingListing>();
        private Dictionary<string, PathingListing> pathsDict = new Dictionary<string, PathingListing>();

        public static PathingConfig Factory(Stream stream)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(stream);
            string xmlString = xml.OuterXml;

            using (StringReader read = new StringReader(xmlString))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(PathingConfig));
                using (XmlReader reader = new XmlTextReader(read))
                {
                    var ret = (PathingConfig)serializer.Deserialize(reader);
                    ret.Load();
                    return ret;
                }
            }
        }

        private void Load()
        {
            foreach (var path in this.Paths)
            {
                pathsDict[path.Nickname] = path;
            }
        }

        public bool TryGetListing(string name, out PathingListing listing)
        {
            return this.pathsDict.TryGetValue(name, out listing);
        }

        public PathingListing GetListing(string name)
        {
            return this.pathsDict[name];
        }

        public override bool Equals(object obj)
        {
            var rhs = obj as PathingConfig;
            if (rhs == null) return false;
            if (this.Version != rhs.Version) return false;
            if (this.Paths.Count != rhs.Paths.Count) return false;
            return this.Paths.SequenceEqual(rhs.Paths);
        }
    }
}