using NUnit.Framework;
using WomPlatform.Web.Api;

namespace ApiTester {
    public class TestStringConcealment {

        [Test]
        public void TestEmails() {
            Assert.That("lck@klopfenstein.net".ConcealEmail().Equals("l*k@k***********.net", System.StringComparison.Ordinal));
            Assert.That("io@example.org".ConcealEmail().Equals("**@e******.org", System.StringComparison.Ordinal));
            Assert.That("pippo@disney.com".ConcealEmail().Equals("p***o@d*****.com", System.StringComparison.Ordinal));
            Assert.That("pippo@a.com".ConcealEmail().Equals("p***o@*.com", System.StringComparison.Ordinal));
            Assert.That("pippo@ab.com".ConcealEmail().Equals("p***o@**.com", System.StringComparison.Ordinal));
            Assert.That("pippo@abc.com".ConcealEmail().Equals("p***o@a**.com", System.StringComparison.Ordinal));
        }

    }
}
