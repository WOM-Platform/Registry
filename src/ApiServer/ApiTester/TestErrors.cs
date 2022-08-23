using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Org.BouncyCastle.Crypto;
using WomPlatform.Connector;

namespace ApiTester {
    public class TestErrors {

        AsymmetricKeyParameter _keyPos, _keyInstrument1, _keyRegistry;
        string _idPos = "5e74205c5f21bb265a2d26d8";
        string _idSource = "5e74203f5f21bb265a2d26bd";

        Client _womClient;
        Instrument _instrument;
        PointOfSale _pos;

        Random _rnd;

        [SetUp]
        public void Setup() {
            _keyPos = CryptoHelper.LoadKeyFromPem<AsymmetricCipherKeyPair>("pos1.pem").Private;
            _keyInstrument1 = CryptoHelper.LoadKeyFromPem<AsymmetricCipherKeyPair>("source1.pem").Private;
            _keyRegistry = CryptoHelper.LoadKeyFromPem<AsymmetricCipherKeyPair>("registry.pem").Public;

            _rnd = new Random();

            _womClient = new Client("dev.wom.social", new ConsoleLoggerFactory(), _keyRegistry);
            _instrument = _womClient.CreateInstrument(_idSource, _keyInstrument1);
            _pos = _womClient.CreatePos(_idPos, _keyPos);
        }

    }
}
