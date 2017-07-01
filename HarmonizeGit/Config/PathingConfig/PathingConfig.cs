using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace HarmonizeGit
{
    public class PathingConfig
    {
        public string ReroutePathing = "C:/Program Files/HarmonizeGit/HarmonizeGit.exe";
        [XmlAttribute]
        public int Version = 1;
        public List<PathingListing> Paths = new List<PathingListing>();
        private Dictionary<string, PathingListing> pathsDict = new Dictionary<string, PathingListing>();
        [XmlIgnore]
        public string OriginalXML;

        public static PathingConfig Factory(Stream stream)
        {
            string originalStr;
            using (var reader = new StreamReader(stream))
            {
                originalStr = reader.ReadToEnd();
            }
            XmlDocument xml = new XmlDocument();
            xml.Load(new StringReader(originalStr));
            string xmlString = xml.OuterXml;

            using (StringReader read = new StringReader(xmlString))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(PathingConfig));
                using (XmlReader reader = new XmlTextReader(read))
                {
                    var ret = (PathingConfig)serializer.Deserialize(reader);
                    ret.OriginalXML = originalStr;
                    ret.Load();
                    return ret;
                }
            }
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
                    return PathingConfig.Factory(stream);
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
        
        public void Write(string targetPath, bool blockIfEqual = true)
        {
            if (!Properties.Settings.Default.ExportPathingConfigUpdates) return;

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

            if (blockIfEqual && object.Equals(this.OriginalXML, xmlStr)) return;

            using (LockManager.GetLock(LockType.Pathing, targetPath))
            {
                File.WriteAllText(Path.Combine(targetPath, HarmonizeGitBase.HarmonizePathingPath), xmlStr);
            }
        }
    }
}
