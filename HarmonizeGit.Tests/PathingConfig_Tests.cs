using FishingWithGit.Tests.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace HarmonizeGit.Tests
{
    public class PathingConfig_Tests
    {
        [Fact]
        public void Typical_Import()
        {
            var config = PathingConfig.Factory(@"..\..\");
            Assert.Equal(@"C:/Program Files/HarmonizeGit/HarmonizeGit.exe", config.ReroutePathing);
            Assert.Equal(2, config.Paths.Count);
            var path = config.Paths[0];
            Assert.Equal(@"Parent", path.Nickname);
            Assert.Equal(@"../Parent", path.Path);
            path = config.Paths[1];
            Assert.Equal(@"SuperParent", path.Nickname);
            Assert.Equal(@"../SuperParent", path.Path);
        }

        [Fact]
        public void Typical_Export()
        {
            var config = PathingConfig.Factory(@"..\..\");
            var tmpDir = Utility.GetTemporaryDirectory();
            Directory.CreateDirectory(tmpDir.FullName + "/.git/");
            var tmp = tmpDir + "/.git/.harmonize-pathing";
            config.Write(tmpDir.FullName, blockIfEqual: false);
            Assert.Equal(File.ReadAllBytes(@"..\..\.git\.harmonize-pathing"), File.ReadAllBytes(tmp));
            tmpDir.Delete(recursive: true);
        }
    }
}
