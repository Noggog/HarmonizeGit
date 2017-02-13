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
    class CommitHandler : TypicalHandlerBase
    {
        public CommitHandler(HarmonizeGitBase harmonize)
            : base(harmonize)
        {
        }

        public override void Handle(List<string> args)
        {            
            var config = this.harmonize.Config.Value;
            
            foreach (var listing in config.ParentRepos)
            {
                using (var repo = new Repository(listing.Path))
                {
                    listing.Sha = repo.Head.Tip.Sha;
                }
            }

            string xmlStr;
            XmlSerializer xsSubmit = new XmlSerializer(typeof(HarmonizeConfig));
            var settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.OmitXmlDeclaration = true;
            var emptyNs = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            using (var sw = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(sw, settings))
                {
                    xsSubmit.Serialize(writer, config, emptyNs);
                    xmlStr = sw.ToString();
                }
            }

            File.WriteAllText(HarmonizeGitBase.HarmonizeConfigPath, xmlStr);

            using (var repo = new Repository("."))
            {
                Commands.Stage(repo, HarmonizeGitBase.HarmonizeConfigPath);
            }
        }
    }
}
