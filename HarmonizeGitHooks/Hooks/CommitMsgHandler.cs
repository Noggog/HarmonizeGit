using HarmonizeGitHooks.MetaData;
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
    class CommitMsgHandler : TypicalHandlerBase
    {
        public CommitMsgHandler(HarmonizeGitBase harmonize)
            : base(harmonize)
        {
        }

        public override void Handle(List<string> args)
        {
            if (args.Count < 1)
            {
                throw new ArgumentException("Commit-Msg args were shorter than expected: " + args.Count);
            }
            
            string pathToFile = args[0];
            
            var config = this.harmonize.Config.Value;

            HarmonizeGitMeta meta = new HarmonizeGitMeta();
            foreach (var listing in config.ParentRepos)
            {
                using (var repo = new Repository(listing.Path))
                {
                    meta.Refs.Add(
                        new Ref()
                        {
                            Nickname = listing.Nickname,
                            Sha = repo.Head.Tip.Sha
                        });
                }
            }

            string xmlStr;
            XmlSerializer xsSubmit = new XmlSerializer(typeof(HarmonizeGitMeta));
            var settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.OmitXmlDeclaration = true;
            var emptyNs = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            using (var sw = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(sw, settings))
                {
                    xsSubmit.Serialize(writer, meta, emptyNs);
                    xmlStr = sw.ToString();
                }
            }

            File.AppendAllText(pathToFile, "\n" + xmlStr);
        }
    }
}
