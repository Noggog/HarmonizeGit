﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace HarmonizeGit
{
    public class PathingConfig : IEquatable<PathingConfig>
    {
        public string ReroutePathing = "C:/Program Files/HarmonizeGit/HarmonizeGit.exe";
        [XmlAttribute]
        public int Version = 1;
        public List<PathingListing> Paths = new List<PathingListing>();
        private Dictionary<string, PathingListing> pathsDict = new Dictionary<string, PathingListing>();
        [XmlIgnore]
        public PathingConfig OriginalConfig;

        public static PathingConfig Factory(Stream stream)
        {
            PathingConfig ret = new PathingConfig();
            XDocument xml;
            using (var reader = new StreamReader(stream))
            {
                xml = XDocument.Parse(reader.ReadToEnd());
            }

            if (int.TryParse(xml.Root.Attribute(XName.Get(nameof(Version)))?.Value, out int ver))
            {
                ret.Version = ver;
            }
            ret.ReroutePathing = xml.Root.Element(XName.Get(nameof(ReroutePathing)))?.Value ?? ret.ReroutePathing;
            var pathElem = xml.Root.Element(XName.Get(nameof(Paths)));
            if (pathElem == null) return ret;
            foreach (var pathListing in pathElem.Elements(XName.Get(nameof(PathingListing))))
            {
                var listing = new PathingListing();
                listing.Nickname = pathListing.Element(XName.Get(nameof(PathingListing.Nickname)))?.Value ?? listing.Nickname;
                listing.Path = pathListing.Element(XName.Get(nameof(PathingListing.Path)))?.Value ?? listing.Path;
                ret.Paths.Add(listing);
            }

            ret.OriginalConfig = ret.GetCopy();
            return ret;
        }

        public static PathingConfig Factory(string path)
        {
            using (LockManager.GetLock(LockType.Pathing, path))
            {
                FileInfo file = new FileInfo(path + "/" + HarmonizeGitBase.HarmonizePathingPath);
                if (!file.Exists)
                {
                    return new PathingConfig();
                }

                using (var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
                {
                    var ret =  PathingConfig.Factory(stream);
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
        
        public void Write(string targetPath)
        {
            if (!Settings.Instance.ExportPathingConfigUpdates) return;
            if (this.Equals(this.OriginalConfig)) return;

            string xmlStr;
            XmlSerializer xsSubmit = new XmlSerializer(typeof(PathingConfig));
            var settings = new XmlWriterSettings()
            {
                Indent = true,
                OmitXmlDeclaration = true
            };
            var emptyNs = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            using (var sw = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(sw, settings))
                {
                    xsSubmit.Serialize(writer, this, emptyNs);
                    xmlStr = sw.ToString();
                }
            }

            if (object.Equals(this, xmlStr)) return;

            using (LockManager.GetLock(LockType.Pathing, targetPath))
            {
                File.WriteAllText(Path.Combine(targetPath, HarmonizeGitBase.HarmonizePathingPath), xmlStr);
            }
        }

        public bool Equals(PathingConfig other)
        {
            if (other == null) return false;
            if (this.Version != other.Version) return false;
            if (!object.Equals(this.ReroutePathing, other.ReroutePathing)) return false;
            if (!this.Paths.SequenceEqual(other.Paths)) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PathingConfig config)) return false;
            return Equals(config);
        }

        public override int GetHashCode()
        {
            return this.Version.GetHashCode()
                .CombineHashCode(this.ReroutePathing)
                .CombineHashCode(this.Paths);
        }

        public PathingConfig GetCopy()
        {
            var ret = new PathingConfig()
            {
                Version = this.Version,
                ReroutePathing = this.ReroutePathing
            };
            ret.Paths.AddRange(this.Paths.Select((p) => p.GetCopy()));
            ret.Load();
            return ret;
        }
    }
}
