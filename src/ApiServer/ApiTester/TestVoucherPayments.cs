using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Org.BouncyCastle.Crypto;
using WomPlatform.Connector;
using WomPlatform.Connector.Models;
using WomPlatform.Web.Api;

namespace ApiTester {

    public class TestVoucherPayments {
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

        private VoucherCreatePayload.VoucherInfo[] CreateRandomVoucherRequests(int count, string aimCode = null) {
            var now = DateTime.UtcNow;
            var voucherInfos = new List<VoucherCreatePayload.VoucherInfo>();
            for (int i = 0; i < count; ++i) {
                voucherInfos.Add(new VoucherCreatePayload.VoucherInfo {
                    Aim = aimCode ?? "X",
                    Latitude = _rnd.NextBetween(5, 40),
                    Longitude = _rnd.NextBetween(5, 50),
                    Timestamp = now
                });

                now = now.Subtract(TimeSpan.FromMinutes(30));
            }
            return voucherInfos.ToArray();
        }

        private VoucherCreatePayload.VoucherInfo[] CreateRandomVoucherBlocks(int blocks, int countPerBlock, string aimCode = null) {
            var now = DateTime.UtcNow;
            var voucherInfos = new List<VoucherCreatePayload.VoucherInfo>(blocks);
            for(int i = 0; i < blocks; ++i) {
                voucherInfos.Add(new VoucherCreatePayload.VoucherInfo {
                    Aim = aimCode ?? "X",
                    Latitude = _rnd.NextBetween(5, 40),
                    Longitude = _rnd.NextBetween(5, 50),
                    Timestamp = now,
                    Count = countPerBlock
                });

                now = now.Subtract(TimeSpan.FromMinutes(30));
            }
            return voucherInfos.ToArray();
        }

        private async Task<(Guid Otc, string Password)> Collect(Pocket p, params VoucherCreatePayload.VoucherInfo[] vi) {
            int beforeCount = p.VoucherCount;
            int requestCount = (from v in vi select v.Count).Sum();

            var response = await _instrument.RequestVouchers(vi);
            await p.CollectVouchers(response.OtcGen, response.Password);

            Assert.That(beforeCount + requestCount == p.VoucherCount);

            return (response.OtcGen, response.Password);
        }

        private async Task<(string AckUrl, Guid Otc, string Password)> Pay(Pocket p, int amount, string pocketAckUrl, SimpleFilter filter = null) {
            int beforeCount = p.VoucherCount;

            var response = await _pos.RequestPayment(amount, pocketAckUrl, "https://example.org", filter, false);
            var ret = await p.PayWithRandomVouchers(response.OtcPay, response.Password);

            Assert.That(beforeCount - amount == p.VoucherCount);
            return (ret, response.OtcPay, response.Password);
        }

        [Test]
        public async Task CreateAndRedeemRandomVouchers() {
            var p = _womClient.CreatePocket();
            await Collect(p, CreateRandomVoucherRequests(10, "E"));

            Assert.That(10 == p.VoucherCount);
            foreach(var v in p.Vouchers) {
                Assert.That("E".Equals(v.Aim, StringComparison.Ordinal));
            }
        }

        [Test]
        public async Task CreateAndRedeemRandomVoucherBlocks() {
            var p = _womClient.CreatePocket();
            await Collect(p, CreateRandomVoucherBlocks(3, 10, "H"));

            Assert.That(30 == p.VoucherCount);
            foreach(var v in p.Vouchers) {
                Assert.That("H".Equals(v.Aim, StringComparison.Ordinal));
            }
        }

        [Test]
        public async Task CreateAndRedeemMultipleVouchers() {
            var now = DateTime.UtcNow;

            var voucherInfos = new VoucherCreatePayload.VoucherInfo[] {
                new VoucherCreatePayload.VoucherInfo {
                    Aim = "I",
                    Latitude = 12.34,
                    Longitude = 23.45,
                    Timestamp = now
                },
                new VoucherCreatePayload.VoucherInfo {
                    Aim = "I",
                    Latitude = 23.45,
                    Longitude = 34.56,
                    Timestamp = now,
                    Count = 2
                }
            };

            var p = _womClient.CreatePocket();
            await Collect(p, voucherInfos);

            Assert.That(3 == p.VoucherCount);
            foreach(var v in p.Vouchers) {
                Assert.That("I".Equals(v.Aim, StringComparison.Ordinal));
                Assert.That(now.ToSecondPrecision() == v.Timestamp.ToSecondPrecision());
            }

            Assert.That(voucherInfos[0].Latitude == p.Vouchers[0].Latitude);
            Assert.That(voucherInfos[0].Longitude == p.Vouchers[0].Longitude);
            Assert.That(voucherInfos[1].Latitude == p.Vouchers[1].Latitude);
            Assert.That(voucherInfos[1].Longitude == p.Vouchers[1].Longitude);
            Assert.That(voucherInfos[1].Latitude == p.Vouchers[2].Latitude);
            Assert.That(voucherInfos[1].Longitude == p.Vouchers[2].Longitude);
        }

        [Test]
        public async Task CreateLocationLessVouchers() {
            var now = DateTime.UtcNow;

            var voucherInfos = new VoucherCreatePayload.VoucherInfo[] {
                new VoucherCreatePayload.VoucherInfo {
                    Aim = "I",
                    Latitude = 12.34,
                    Longitude = 23.45,
                    Timestamp = now,
                    CreationMode = VoucherCreatePayload.VoucherCreationMode.SetLocationOnRedeem
                },
                new VoucherCreatePayload.VoucherInfo {
                    Aim = "I",
                    Latitude = 23.45,
                    Longitude = 34.56,
                    Timestamp = now,
                    Count = 2,
                    CreationMode = VoucherCreatePayload.VoucherCreationMode.SetLocationOnRedeem
                }
            };

            var response = await _instrument.RequestVouchers(voucherInfos);

            Console.WriteLine("OTC: {0}", response.OtcGen);
            Console.WriteLine("Password: {0}", response.Password);
            Console.WriteLine("Link: {0}", response.Link);
        }

        [Test]
        public async Task CreateVouchers() {
            var now = DateTime.UtcNow;

            var voucherInfos = new VoucherCreatePayload.VoucherInfo[] {
                new VoucherCreatePayload.VoucherInfo {
                    Aim = "H",
                    Latitude = 63.5269992,
                    Longitude = -19.510935,
                    Timestamp = now,
                    CreationMode = VoucherCreatePayload.VoucherCreationMode.Standard
                }
            };

            var response = await _instrument.RequestVouchers(voucherInfos);

            Console.WriteLine("OTC: {0}", response.OtcGen);
            Console.WriteLine("Password: {0}", response.Password);
            Console.WriteLine("Link: {0}", response.Link);
        }

        [Test]
        public async Task CreateVouchersAndProcessSimplePayment() {
            var p = _womClient.CreatePocket();
            await Collect(p, CreateRandomVoucherRequests(5, "E"));
            Assert.That(5 == p.VoucherCount);

            var ackUrl = string.Format("http://www.example.org/confirmation/{0:N}", Guid.NewGuid());
            (var respUrl, var otcPay, var pwdPay) = await Pay(p, 5, ackUrl, null);
            Assert.That(ackUrl.Equals(respUrl, StringComparison.Ordinal));

            // Try repeated payment
            Assert.ThrowsAsync<InvalidOperationException>(async () => {
                await p.PayWithRandomVouchers(otcPay, pwdPay);
            });

            // Try second payment without vouchers
            Assert.ThrowsAsync<InvalidOperationException>(async () => {
                await Pay(p, 5, ackUrl);
            });

            var status = await _pos.GetPaymentStatus(otcPay);
            Assert.That(!status.Response.Persistent);
            Assert.That(status.Response.HasBeenPerformed);
            Assert.That(status.Response.Confirmations.Count == 1);
            Assert.That(status.Response.Confirmations[0].PerformedAt <= DateTime.UtcNow);
            Assert.That(status.Response.Confirmations[0].PerformedAt >= DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(5)));
        }

        [Test]
        public async Task CreateAndUseDemoVouchers() {
            var p = _womClient.CreatePocket();
            await Collect(p, CreateRandomVoucherBlocks(1, 5, "H"));

            // Pay with normal vouchers fails
            Assert.ThrowsAsync<InvalidOperationException>(async () => {
                await Pay(p, 5, "https://ciao.com", new SimpleFilter {
                    Aim = "0"
                });
            });
            Assert.That(5 == p.VoucherCount);

            p = _womClient.CreatePocket();
            await Collect(p, CreateRandomVoucherBlocks(1, 5, "0"));

            // Pay normal payment with demo vouchers fails
            Assert.ThrowsAsync<InvalidOperationException>(async () => {
                await Pay(p, 5, "https://ciao.com");
            });
            Assert.That(5 == p.VoucherCount);

            // Demo payment succeeds
            (var ackUrl, _, _) = await Pay(p, 5, "https://ciao.com", new SimpleFilter {
                Aim = "0"
            });
            Assert.That(0 == p.VoucherCount);
            Assert.That("https://ciao.com".Equals(ackUrl, StringComparison.Ordinal));
        }

        [Test]
        public async Task CreateVouchersAndProcessMultiplePayment() {
            var p = _womClient.CreatePocket();
            await Collect(p, CreateRandomVoucherBlocks(1, 5));
            Assert.That(5 == p.VoucherCount);

            await Pay(p, 2, "https://ciao.com");
            Assert.That(3 == p.VoucherCount);

            await Pay(p, 2, "https://ciao.com");
            Assert.That(1 == p.VoucherCount);

            await Pay(p, 1, "https://ciao.com");
            Assert.That(0 == p.VoucherCount);
        }

        [Test]
        public async Task FailedPaymentInsufficientVouchers() {
            var p = _womClient.CreatePocket();
            await Collect(p, CreateRandomVoucherBlocks(1, 10, "H"));
            Assert.That(10 == p.VoucherCount);

            Assert.ThrowsAsync<InvalidOperationException>(async () => {
                await Pay(p, 20, "https://pay.com");
            });
        }

        [Test]
        public async Task FailedPaymentWrongAim() {
            var p = _womClient.CreatePocket();
            await Collect(p,
                new VoucherCreatePayload.VoucherInfo {
                    Aim = "S",
                    Latitude = 12,
                    Longitude = 12,
                    Timestamp = DateTime.UtcNow
                },
                new VoucherCreatePayload.VoucherInfo {
                    Aim = "IM",
                    Latitude = 12,
                    Longitude = 12,
                    Timestamp = DateTime.UtcNow
                }
            );

            Assert.ThrowsAsync<InvalidOperationException>(async () => {
                await Pay(p, 2, "https://example.com", new SimpleFilter {
                    Aim = "1"
                });
            });
        }

        [Test]
        public async Task PaymentGeoBounds() {
            var p = _womClient.CreatePocket();
            await Collect(p,
                new VoucherCreatePayload.VoucherInfo {
                    Aim = "N",
                    Latitude = 10,
                    Longitude = 10,
                    Timestamp = DateTime.UtcNow
                },
                new VoucherCreatePayload.VoucherInfo {
                    Aim = "N",
                    Latitude = 20,
                    Longitude = 20,
                    Timestamp = DateTime.UtcNow
                },
                new VoucherCreatePayload.VoucherInfo {
                    Aim = "N",
                    Latitude = -60,
                    Longitude = -60,
                    Timestamp = DateTime.UtcNow
                }
            );
            Assert.That(3 == p.VoucherCount);

            // Correct payment with voucher 1
            var ackUrl = string.Format("http://www.example.org/geo-test/{0:N}", Guid.NewGuid());
            (var respUrl, _, _) = await Pay(p, 1, ackUrl, new SimpleFilter {
                Bounds = new Bounds {
                    LeftTop = [15, 5],
                    RightBottom = [5, 15]
                }
            });
            Assert.That(2 == p.VoucherCount);
            Assert.That(respUrl.Equals(ackUrl, StringComparison.Ordinal));

            Assert.ThrowsAsync<InvalidOperationException>(async () => {
                await Pay(p, 1, ackUrl, new SimpleFilter {
                    Bounds = new Bounds {
                        LeftTop = [30, 25],
                        RightBottom = [10, 35]
                    }
                });
            });
        }
    }
}
