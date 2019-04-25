using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Azure.VirtualKubelet.Edge.Provider.Test
{

    public class EdgeModuleSettingsTest
    {
        [Theory]
        [InlineData("image:latest","{}","{\"image\":\"image:latest\",\"createOptions\":\"{}\"}")]
        [InlineData("image:latest",
            "{\"HostConfig\":{\"Privileged\":\"true\",\"Devices\":[{\"PathOnHost\":\"/dev/video0\",\"PathInContainer\":\"/dev/video0\",\"CgroupPermissions\":\"rwm\"},{\"PathOnHost\":\"/dev/video1\",\"PathInContainer\":\"/dev/video1\",\"CgroupPermissions\":\"rwm\"},{\"PathOnHost\":\"/dev/video2\",\"PathInContainer\":\"/dev/video2\",\"CgroupPermissions\":\"rwm\"},{\"PathOnHost\":\"/dev/video5\",\"PathInContainer\":\"/dev/video5\",\"CgroupPermissions\":\"rwm\"},{\"PathOnHost\":\"/dev/video6\",\"PathInContainer\":\"/dev/video6\",\"CgroupPermissions\":\"rwm\"},{\"PathOnHost\":\"/dev/video7\",\"PathInContainer\":\"/dev/video7\",\"CgroupPermissions\":\"rwm\"}]}}",
            "{\"image\":\"image:latest\",\"createOptions\":\"{\\\"HostConfig\\\":{\\\"Privileged\\\":\\\"true\\\",\\\"Devices\\\":[{\\\"PathOnHost\\\":\\\"/dev/video0\\\",\\\"PathInContainer\\\":\\\"/dev/video0\\\",\\\"CgroupPermissions\\\":\\\"rwm\\\"},{\\\"PathOnHost\\\":\\\"/dev/video1\\\",\\\"PathInContainer\\\":\\\"/dev/video1\\\",\\\"CgroupPermissions\\\":\\\"rwm\\\"},{\\\"PathOnHost\\\":\\\"/dev/video2\\\",\\\"PathInContainer\\\":\\\"/dev/video2\\\",\\\"CgroupPermissions\\\":\\\"rwm\\\"},{\\\"PathOnHost\\\":\\\"/dev/video5\\\",\\\"PathInContainer\\\":\\\"/dev/video5\\\",\\\"CgroupPermissions\\\":\\\"rwm\\\"},{\\\"PathOnHost\\\":\\\"/dev/video6\\\",\\\"PathInContainer\\\":\\\"/dev/video6\\\",\\\"CgroupPermissions\\\":\\\"rwm\\\"},{\\\"PathOnHost\\\":\\\"/dev/video7\\\",\\\"Pa\",\"createOptions01\":\"thInContainer\\\":\\\"/dev/video7\\\",\\\"CgroupPermissions\\\":\\\"rwm\\\"}]}}\"}")]
        public void TestSerializeChunks(string image, string createOptionsJson, string expected)
        {
            var moduleSettings = new EdgeModuleSettings(image,createOptionsJson);

            var serialized = JsonConvert.SerializeObject(moduleSettings);

            Assert.Equal(expected,serialized);

        }
    }
}
