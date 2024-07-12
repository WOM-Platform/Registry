using NUnit.Framework;
using WomPlatform.Web.Api;

namespace ApiTester {
    public class TestStringUrlConversion {

        [Test]
        public void TestNormalPosNames() {
            Assert.That("DIGIT srl".ToCleanUrl().Equals("digit-srl", System.StringComparison.Ordinal));
            Assert.That("Libreria Montefeltro".ToCleanUrl().Equals("libreria-montefeltro", System.StringComparison.Ordinal));
            Assert.That("Edicola “Il Chiosco”".ToCleanUrl().Equals("edicola-il-chiosco", System.StringComparison.Ordinal));
            Assert.That("Piadineria “L’Aquilone”".ToCleanUrl().Equals("piadineria-l-aquilone", System.StringComparison.Ordinal));
            Assert.That("Caffè dell’Accademia".ToCleanUrl().Equals("caffe-dell-accademia", System.StringComparison.Ordinal));
            Assert.That("La Cucina di Taty".ToCleanUrl().Equals("la-cucina-di-taty", System.StringComparison.Ordinal));
        }

        [Test]
        public void TestMalformattedPosNames() {
            Assert.That(" DIGIT srl ".ToCleanUrl().Equals("digit-srl", System.StringComparison.Ordinal));
            Assert.That("'Libreria _Möntefeltro--".ToCleanUrl().Equals("libreria-montefeltro", System.StringComparison.Ordinal));
            Assert.That("Èdicola  “Íl Chiósco” ".ToCleanUrl().Equals("edicola-il-chiosco", System.StringComparison.Ordinal));
            Assert.That("  Piadineria  “L’ Aquilone” ".ToCleanUrl().Equals("piadineria-l-aquilone", System.StringComparison.Ordinal));
            Assert.That("☕ Caffè dell’Accademia".ToCleanUrl().Equals("caffe-dell-accademia", System.StringComparison.Ordinal));
        }

    }
}
