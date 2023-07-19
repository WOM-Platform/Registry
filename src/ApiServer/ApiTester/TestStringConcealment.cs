using NUnit.Framework;
using WomPlatform.Web.Api;

namespace ApiTester {
    public class TestStringConcealment {

        [Test]
        public void TestEmails() {
            Assert.AreEqual("l*k@k***********.net", "lck@klopfenstein.net".ConcealEmail());
            Assert.AreEqual("**@e******.org", "io@example.org".ConcealEmail());
            Assert.AreEqual("p***o@d*****.com", "pippo@disney.com".ConcealEmail());
            Assert.AreEqual("p***o@*.com", "pippo@a.com".ConcealEmail());
            Assert.AreEqual("p***o@**.com", "pippo@ab.com".ConcealEmail());
            Assert.AreEqual("p***o@a**.com", "pippo@abc.com".ConcealEmail());
        }

    }
}
