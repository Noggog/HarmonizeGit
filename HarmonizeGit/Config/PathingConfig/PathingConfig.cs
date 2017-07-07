using System;
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
        public int Version = 1;
        public List<PathingListing> Paths = new List<PathingListing>();
        private Dictionary<string, PathingListing> pathsDict = new Dictionary<string, PathingListing>();
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
                    var ret = PathingConfig.Factory(stream);
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

        public void WriteToPath(string path)
        {
            if (!Settings.Instance.ExportPathingConfigUpdates) return;
            if (this.Equals(this.OriginalConfig)) return;

            using (LockManager.GetLock(LockType.Pathing, path))
            {
                path = Path.Combine(path, HarmonizeGitBase.HarmonizePathingPath);

                using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
                {
                    using (var writer = new XmlTextWriter(fileStream, Encoding.ASCII))
                    {
                        writer.Formatting = Formatting.Indented;
                        writer.Indentation = 3;

                        using (new ElementWrapper(writer, nameof(HarmonizeConfig)))
                        {
                            writer.WriteAttributeString(nameof(Version), this.Version.ToString());
                            using (new ElementWrapper(writer, nameof(ReroutePathing)))
                            {
                                writer.WriteValue(this.ReroutePathing);
                            }
                            using (new ElementWrapper(writer, nameof(Paths)))
                            {
                                foreach (var item in this.Paths)
                                {
                                    using (new ElementWrapper(writer, nameof(PathingListing)))
                                    {
                                        using (new ElementWrapper(writer, nameof(PathingListing.Nickname)))
                                        {
                                            writer.WriteValue(item.Nickname);
                                        }
                                        using (new ElementWrapper(writer, nameof(PathingListing.Path)))
                                        {
                                            writer.WriteValue(item.Path);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
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
