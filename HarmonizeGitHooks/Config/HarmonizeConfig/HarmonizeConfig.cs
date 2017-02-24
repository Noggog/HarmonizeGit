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
        [NonSerialized]
        public PathingConfig Pathing;

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

        public bool SetPathing(PathingConfig pathing, bool addMissing = true)
        {
            this.Pathing = pathing;
            bool added = false;
            foreach (var listing in this.ParentRepos)
            {
                PathingListing pathListing;
                if (!pathing.TryGetListing(listing.Nickname, out pathListing))
                {
                    if (!addMissing) continue;
                    pathListing = new PathingListing()
                    {
                        Nickname = listing.Nickname,
                        Path = "../" + listing.Nickname
                    };
                    pathing.Paths.Add(pathListing);
                    added = true;
                }

                listing.Path = pathListing.Path;
            }
            return added;
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
