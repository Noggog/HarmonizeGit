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
        public List<RepoListing> ParentRepos = new List<RepoListing>();

        public static HarmonizeConfig Factory(string filePath)
        {
            return Factory(new FileStream(filePath, FileMode.Open, FileAccess.Read));
        }

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
                    return (HarmonizeConfig)serializer.Deserialize(reader);
                }
            }
        }

        public void Export(string filePath)
        {
            XmlDocument xmlDocument = new XmlDocument();
            XmlSerializer serializer = new XmlSerializer(this.GetType());
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, this);
                stream.Position = 0;
                xmlDocument.Load(stream);
                xmlDocument.Save(filePath);
                stream.Close();
            }
        }
    }
}
