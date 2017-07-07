using LibGit2Sharp;
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
    public class HarmonizeConfig : IEquatable<HarmonizeConfig>
    {
        [XmlAttribute]
        public int Version = 1;
        public List<RepoListing> ParentRepos = new List<RepoListing>();
        [XmlIgnore]
        public PathingConfig Pathing;
        [XmlIgnore]
        public HarmonizeConfig OriginalConfig;

        public static HarmonizeConfig Factory(
            HarmonizeGitBase harmonize,
            string path,
            Stream stream,
            PathingConfig pathing)
        {
            HarmonizeConfig ret = new HarmonizeConfig();
            XDocument xml;
            using (var reader = new StreamReader(stream))
            {
                xml = XDocument.Parse(reader.ReadToEnd());
            }

            if (int.TryParse(xml.Root.Attribute(XName.Get(nameof(Version)))?.Value, out int ver))
            {
                ret.Version = ver;
            }
            var reposElem = xml.Root.Element(XName.Get(nameof(ParentRepos)));
            if (reposElem != null)
            {
                foreach (var repoListing in reposElem.Elements(XName.Get(nameof(RepoListing))))
                {
                    var listing = new RepoListing();
                    listing.Nickname = repoListing.Element(XName.Get(nameof(RepoListing.Nickname)))?.Value ?? listing.Nickname;
                    listing.Sha = repoListing.Element(XName.Get(nameof(RepoListing.Sha)))?.Value ?? listing.Sha;
                    listing.Description = repoListing.Element(XName.Get(nameof(RepoListing.Description)))?.Value ?? listing.Description;
                    listing.Author = repoListing.Element(XName.Get(nameof(RepoListing.Author)))?.Value ?? listing.Author;
                    listing.CommitDate = repoListing.Element(XName.Get(nameof(RepoListing.CommitDate)))?.Value ?? listing.CommitDate;
                    listing.SuggestedPath = repoListing.Element(XName.Get(nameof(RepoListing.SuggestedPath)))?.Value ?? listing.SuggestedPath;
                    ret.ParentRepos.Add(listing);
                }
            }

            ret.SetPathing(pathing, addMissing: true);
            ret.OriginalConfig = ret.GetCopy();
            return ret;
        }

        public static HarmonizeConfig Factory(
            HarmonizeGitBase harmonize,
            string path,
            Commit commit)
        {
            var entry = commit[HarmonizeGitBase.HarmonizeConfigPath];
            var blob = entry?.Target as Blob;
            if (blob == null)
            {
                return null;
            }

            var contentStream = blob.GetContentStream();
            using (var tr = new StreamReader(contentStream, Encoding.UTF8))
            {
                return Factory(
                    harmonize,
                    path,
                    tr.BaseStream,
                    harmonize.ConfigLoader.GetPathing(path));
            }
        }

        public bool Equals(HarmonizeConfig other)
        {
            if (other == null) return false;
            if (this.Version != other.Version) return false;
            if (!object.Equals(this.Pathing, other.Pathing)) return false;
            if (!ParentRepos.SequenceEqual(other.ParentRepos)) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is HarmonizeConfig config)) return false;
            return Equals(config);
        }

        public override int GetHashCode()
        {
            return this.Version.GetHashCode()
                .CombineHashCode(this.ParentRepos)
                .CombineHashCode(this.Pathing);
        }

        public HarmonizeConfig GetCopy()
        {
            var ret = new HarmonizeConfig()
            {
                Version = this.Version,
                Pathing = this.Pathing.GetCopy()
            };
            ret.ParentRepos.AddRange(this.ParentRepos.Select((pr) => pr.GetCopy()));
            return ret;
        }

        public string GetXmlStr()
        {
            string xmlStr;
            XmlSerializer xsSubmit = new XmlSerializer(typeof(HarmonizeConfig));
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
            return xmlStr;
        }
        
        public bool SetPathing(PathingConfig pathing, bool addMissing = true)
        {
            this.Pathing = pathing;
            bool added = false;
            foreach (var listing in this.ParentRepos)
            {
                if (!pathing.TryGetListing(listing.Nickname, out PathingListing pathListing))
                {
                    if (!addMissing) continue;
                    string missingPath;
                    if (string.IsNullOrWhiteSpace(listing.SuggestedPath))
                    {
                        missingPath = "../" + listing.Nickname;
                    }
                    else
                    {
                        missingPath = listing.SuggestedPath;
                    }
                    pathListing = new PathingListing()
                    {
                        Nickname = listing.Nickname,
                        Path = missingPath
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
