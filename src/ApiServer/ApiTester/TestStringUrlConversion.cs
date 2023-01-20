using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using WomPlatform.Web.Api;

namespace ApiTester {
    public class TestStringUrlConversion {

        [Test]
        public void TestNormalPosNames() {
            Assert.AreEqual("digit-srl", "DIGIT srl".ToCleanUrl());
            Assert.AreEqual("libreria-montefeltro", "Libreria Montefeltro".ToCleanUrl());
            Assert.AreEqual("edicola-il-chiosco", "Edicola “Il Chiosco”".ToCleanUrl());
            Assert.AreEqual("piadineria-l-aquilone", "Piadineria “L’Aquilone”".ToCleanUrl());
            Assert.AreEqual("caffe-dell-accademia", "Caffè dell’Accademia".ToCleanUrl());
            Assert.AreEqual("la-cucina-di-taty", "La Cucina di Taty".ToCleanUrl());
        }

        [Test]
        public void TestMalformattedPosNames() {
            Assert.AreEqual("digit-srl", " DIGIT srl ".ToCleanUrl());
            Assert.AreEqual("libreria-montefeltro", "'Libreria _Möntefeltro--".ToCleanUrl());
            Assert.AreEqual("edicola-il-chiosco", "Èdicola  “Íl Chiósco” ".ToCleanUrl());
            Assert.AreEqual("piadineria-l-aquilone", "  Piadineria  “L’ Aquilone” ".ToCleanUrl());
            Assert.AreEqual("caffe-dell-accademia", "☕ Caffè dell’Accademia".ToCleanUrl());
        }

    }
}
