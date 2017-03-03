using LibGit2Sharp;
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
        [XmlIgnore]
        public PathingConfig Pathing;
        [XmlIgnore]
        public string OriginalXML;

        public static HarmonizeConfig Factory(
            HarmonizeGitBase harmonize,
            string path,
            Stream stream,
            PathingConfig pathing)
        {
            string originalStr;
            using (var reader = new StreamReader(stream))
            {
                originalStr = reader.ReadToEnd();
            }
            XmlDocument xml = new XmlDocument();
            xml.Load(new StringReader(originalStr));
            string xmlString = xml.OuterXml;

            HarmonizeConfig ret;
            using (StringReader read = new StringReader(xmlString))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(HarmonizeConfig));
                using (XmlReader reader = new XmlTextReader(read))
                {
                    ret = (HarmonizeConfig)serializer.Deserialize(reader);
                    ret.OriginalXML = originalStr;
                }
            }

            ret.SetPathing(pathing, addMissing: true);
            foreach (var listing in ret.ParentRepos)
            {
                harmonize.WriteLine($"{listing.Nickname} set to path {listing.Path}.");
            }
            return ret;
        }

        public static HarmonizeConfig Factory(
            HarmonizeGitBase harmonize,
            string path,
            Commit commit,
            PathingConfig pathing)
        {
            var entry = commit[HarmonizeGitBase.HarmonizeConfigPath];
            var blob = entry?.Target as Blob;
            if (blob == null)
            {
                harmonize.WriteLine("No harmonize config at target commit.  Exiting without syncing.");
                return null;
            }

            var contentStream = blob.GetContentStream();
            using (var tr = new StreamReader(contentStream, Encoding.UTF8))
            {
                return Factory(
                    harmonize,
                    path,
                    tr.BaseStream,
                    pathing);
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
    }
}
