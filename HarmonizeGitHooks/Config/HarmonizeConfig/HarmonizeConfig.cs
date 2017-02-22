using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace HarmonizeGitHooks
{
    public class HarmonizeConfig
    {
        [XmlAttribute]
        public int Version = 1;
        public List<RepoListing> ParentRepos = new List<RepoListing>();

        public static HarmonizeConfig Factory(Stream stream)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(stream);
            string xmlString = xml.OuterXml;

            using (StringReader read = new StringReader(xmlString))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(HarmonizeConfig));
                using (XmlReader reader = new XmlTextReader(read))
                {
                    var ret = (HarmonizeConfig)serializer.Deserialize(reader);
                    return ret;
                }
            }
        }

        public override bool Equals(object obj)
        {
            var rhs = obj as HarmonizeConfig;
            if (rhs == null) return false;
            if (this.Version != rhs.Version) return false;
            if (this.ParentRepos.Count != rhs.ParentRepos.Count) return false;
            return this.ParentRepos.SequenceEqual(rhs.ParentRepos);
        }
    }
}
