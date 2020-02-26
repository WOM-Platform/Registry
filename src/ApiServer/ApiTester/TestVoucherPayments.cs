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

        AsymmetricKeyParameter _keyPos, _keyInstrument1, _keyInstrument2, _keyRegistry;
        byte[] _keySession;

        Client _womClient;

        Random _rnd;

        [SetUp]
        public void Setup() {
            _keyPos = CryptoHelper.LoadKeyFromPem<AsymmetricCipherKeyPair>("pos1.pem").Private;
            _keyInstrument1 = CryptoHelper.LoadKeyFromPem<AsymmetricCipherKeyPair>("source1.pem").Private;
            _keyInstrument2 = CryptoHelper.LoadKeyFromPem<AsymmetricCipherKeyPair>("source2.pem").Private;
            _keyRegistry = CryptoHelper.LoadKeyFromPem<AsymmetricCipherKeyPair>("registry.pem").Public;

            _rnd = new Random();
            byte[] sessionKey = new byte[256 / 8];
            _rnd.NextBytes(sessionKey);
            _keySession = sessionKey;

            _womClient = new Client("dev.wom.social", new ConsoleLoggerFactory(), _keyRegistry);
        }

        /*private Guid CreateVouchers(int sourceId, AsymmetricKeyParameter sourceKey,
            string password, VoucherCreatePayload.VoucherInfo[] vouchers,
            Guid? nonce = null) {
            if(!nonce.HasValue) {
                nonce = Guid.NewGuid();
            }

            var request = CreateJsonRequest("voucher/create", new VoucherCreatePayload {
                SourceId = sourceId,
                Nonce = nonce.Value.ToString("N"),
                Payload = Crypto.Encrypt(new VoucherCreatePayload.Content {
                    SourceId = sourceId,
                    Nonce = nonce.Value.ToString("N"),
                    Password = password,
                    Vouchers = vouchers
                }, _keyRegistry)
            });

            var response = PerformRequest<VoucherCreateResponse>(request);
            var responseContent = Crypto.Decrypt<VoucherCreateResponse.Content>(response.Payload, sourceKey);

            request = CreateJsonRequest("voucher/verify", new VoucherVerifyPayload {
                Payload = Crypto.Encrypt(new VoucherVerifyPayload.Content {
                    Otc = responseContent.Otc
                }, _keyRegistry)
            });
            PerformRequest<VoucherCreateResponse>(request);

            return responseContent.Otc;
        }*/

        /*private VoucherRedeemResponse.Content RedeemVouchers(Guid otc, string password) {
            var request = CreateJsonRequest("voucher/redeem", new VoucherRedeemPayload {
                Payload = Crypto.Encrypt(new VoucherRedeemPayload.Content {
                    Otc = otc,
                    Password = password,
                    SessionKey = _keySession.ToBase64()
                }, _keyRegistry)
            });

            var response = PerformRequest<VoucherRedeemResponse>(request);
            return Crypto.Decrypt<VoucherRedeemResponse.Content>(response.Payload, _keySession);
        }*/

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

        /*private Guid CreatePayment(string password, int amount, string pocketAckUrl, SimpleFilter filter = null, bool persistent = false) {
            var nonce = Guid.NewGuid().ToString("N");
            var request = CreateJsonRequest("payment/register", new PaymentRegisterPayload {
                PosId = 1,
                Nonce = nonce,
                Payload = Crypto.Encrypt(new PaymentRegisterPayload.Content {
                    PosId = 1,
                    Nonce = nonce,
                    Password = password,
                    Amount = amount,
                    PosAckUrl = string.Format("https://example.org/pos/test/{0:N}", nonce),
                    PocketAckUrl = pocketAckUrl,
                    SimpleFilter = filter,
                    Persistent = persistent
                }, _keyRegistry)
            });

            var response = PerformRequest<PaymentRegisterResponse>(request);
            var responseContent = Crypto.Decrypt<PaymentRegisterResponse.Content>(response.Payload, _keyPos);

            request = CreateJsonRequest("payment/verify", new PaymentVerifyPayload {
                Payload = Crypto.Encrypt(new PaymentVerifyPayload.Content {
                    Otc = responseContent.Otc
                }, _keyRegistry)
            });

            PerformRequest<VoucherCreateResponse>(request);

            return responseContent.Otc;
        }*/

        /*private PaymentConfirmResponse.Content ProcessPayment(Guid otc, string password, params PaymentConfirmPayload.VoucherInfo[] vouchers) {
            var request = CreateJsonRequest("payment/confirm", new PaymentConfirmPayload {
                Payload = Crypto.Encrypt(new PaymentConfirmPayload.Content {
                    Otc = otc,
                    Password = password,
                    SessionKey = _keySession.ToBase64(),
                    Vouchers = vouchers
                }, _keyRegistry)
            });

            var response = PerformRequest<PaymentConfirmResponse>(request);
            return Crypto.Decrypt<PaymentConfirmResponse.Content>(response.Payload, _keySession);
        }*/

        /*private PaymentInfoResponse.Content GetPaymentInfo(Guid otc, string password) {
            var request = CreateJsonRequest("payment/info", new PaymentInfoPayload {
                Payload = Crypto.Encrypt(new PaymentInfoPayload.Content {
                    Otc = otc,
                    Password = password,
                    SessionKey = _keySession.ToBase64()
                }, _keyRegistry)
            });

            var response = PerformRequest<PaymentInfoResponse>(request);
            return Crypto.Decrypt<PaymentInfoResponse.Content>(response.Payload, _keySession);
        }*/

        [Test]
        public async Task CreateAndRedeemRandomVouchers() {
            var instrument = _womClient.CreateInstrument(1, _keyInstrument1);
            (var reqOtc, var reqPwd) = await instrument.RequestVouchers(CreateRandomVoucherRequests(10, "E"));

            var pocket = _womClient.CreatePocket();
            await pocket.CollectVouchers(reqOtc, reqPwd);

            Assert.AreEqual(10, pocket.VoucherCount);
            foreach(var v in pocket.Vouchers) {
                Assert.AreEqual("E", v.Aim);
            }
        }

        [Test]
        public async Task CreateVouchersMultipleSources() {
            var voucherSpec = CreateRandomVoucherRequests(_rnd.Next(5) + 5);

            var instr1 = _womClient.CreateInstrument(1, _keyInstrument1);
            (var otc1, var pwd1) = await instr1.RequestVouchers(voucherSpec);

            var instr2 = _womClient.CreateInstrument(2, _keyInstrument2);
            (var otc2, var pwd2) = await instr1.RequestVouchers(voucherSpec);

            Assert.AreNotEqual(otc1, otc2);
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

            var instr = _womClient.CreateInstrument(2, _keyInstrument2);
            (var otc, var pwd) = await instr.RequestVouchers(voucherInfos);

            var pocket = _womClient.CreatePocket();
            await pocket.CollectVouchers(otc, pwd);

            Assert.AreEqual(3, pocket.VoucherCount);
            foreach(var v in pocket.Vouchers) {
                Assert.AreEqual("I", v.Aim);
                Assert.AreEqual(now.ToSecondPrecision(), v.Timestamp.ToSecondPrecision());
            }
            

            Assert.AreEqual(voucherInfos[0].Latitude, pocket.Vouchers[0].Latitude);
            Assert.AreEqual(voucherInfos[0].Longitude, pocket.Vouchers[0].Longitude);
            Assert.AreEqual(voucherInfos[1].Latitude, pocket.Vouchers[1].Latitude);
            Assert.AreEqual(voucherInfos[1].Longitude, pocket.Vouchers[1].Longitude);
            Assert.AreEqual(voucherInfos[1].Latitude, pocket.Vouchers[2].Latitude);
            Assert.AreEqual(voucherInfos[1].Longitude, pocket.Vouchers[2].Longitude);
        }

        [Test]
        public async Task CreateVouchersAndProcessSimplePayment() {
            var voucherInfos = CreateRandomVoucherRequests(5, "E");

            var instr = _womClient.CreateInstrument(2, _keyInstrument2);
            (var otcGen, var pwdGen) = await instr.RequestVouchers(voucherInfos, password: "987");
            Assert.AreEqual("987", pwdGen);

            var pocket = _womClient.CreatePocket();
            await pocket.CollectVouchers(otcGen, pwdGen);
            Assert.AreEqual(5, pocket.VoucherCount);

            var ackUrl = string.Format("http://www.example.org/confirmation/{0:N}", Guid.NewGuid());
            var pos = _womClient.CreatePos(1, _keyPos);
            (var otcPay1, var pwdPay1) = await pos.RequestPayment(5, ackUrl);

            // TODO: test get info on payment

            var respUrl = await pocket.PayWithRandomVouchers(otcPay1, pwdPay1);
            Assert.AreEqual(ackUrl, respUrl);

            // Try repeated payment
            Assert.ThrowsAsync<InvalidOperationException>(async () => {
                await pocket.PayWithRandomVouchers(otcPay1, pwdPay1);
            });

            // Try second payment without vouchers
            (var otcPay2, var pwdPay2) = await pos.RequestPayment(5, ackUrl);
            Assert.ThrowsAsync<InvalidOperationException>(async () => {
                await pocket.PayWithRandomVouchers(otcPay2, pwdPay2);
            });
        }

        /*[Test]
        public void CreateVouchersAndProcessMultiplePayment() {
            var voucherInfos = CreateRandomVoucherRequests(2);
            var otcGen = CreateVouchers(1, _keyInstrument1, "1234", voucherInfos);
            var response = RedeemVouchers(otcGen, "1234");

            Assert.AreEqual(2, response.Vouchers.Length);
            Assert.AreEqual(1, response.SourceId);

            var ackUrl = string.Format("http://www.example.org/confirmation/{0:N}", Guid.NewGuid());

            var payOtc = CreatePayment("2345", 1, ackUrl, null, true);

            var payInfo = GetPaymentInfo(payOtc, "2345");
            Assert.AreEqual(1, payInfo.Amount);
            Assert.AreEqual(1, payInfo.PosId);
            Assert.AreEqual("Sample POS 1", payInfo.PosName);
            Assert.IsNull(payInfo.SimpleFilter);
            Assert.AreEqual(true, payInfo.Persistent);

            var paymentVouchers = (from v in response.Vouchers
                                   select new PaymentConfirmPayload.VoucherInfo {
                                       Id = v.Id,
                                       Secret = v.Secret
                                   });

            var payResponse = ProcessPayment(payOtc, "2345", paymentVouchers.Take(1).ToArray());
            Assert.AreEqual(ackUrl, payResponse.AckUrl);

            payResponse = ProcessPayment(payOtc, "2345", paymentVouchers.Skip(1).ToArray());
            Assert.AreEqual(ackUrl, payResponse.AckUrl);
        }

        [Test]
        public void FailedPaymentInsufficientVouchers() {
            var voucherInfos = CreateRandomVoucherRequests(1);
            var otcGen = CreateVouchers(1, _keyInstrument1, "1234", voucherInfos);
            var response = RedeemVouchers(otcGen, "1234");

            Assert.AreEqual(1, response.Vouchers.Length);
            Assert.AreEqual(1, response.SourceId);

            var ackUrl = string.Format("http://www.example.org/confirmation/{0:N}", Guid.NewGuid());

            var payOtc = CreatePayment("2345", 2, ackUrl);

            Assert.Throws<InvalidOperationException>(() => {
                ProcessPayment(payOtc, "2345", (from v in response.Vouchers
                                                select new PaymentConfirmPayload.VoucherInfo {
                                                    Id = v.Id,
                                                    Secret = v.Secret
                                                }).ToArray());
            });
        }

        [Test]
        public void FailedPaymentWrongAim() {
            var otcGen = CreateVouchers(1, _keyInstrument1, "1234",
                new VoucherCreatePayload.VoucherInfo[] {
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
                }
            );
            var response = RedeemVouchers(otcGen, "1234");

            Assert.AreEqual(2, response.Vouchers.Length);
            Assert.AreEqual("S", response.Vouchers[0].Aim);
            Assert.AreEqual("IM", response.Vouchers[1].Aim);

            var ackUrl = string.Format("http://www.example.org/confirmation/{0:N}", Guid.NewGuid());

            var payOtc = CreatePayment("2345", 2, ackUrl, new SimpleFilter {
                Aim = "1"
            });

            var payInfo = GetPaymentInfo(payOtc, "2345");
            Assert.AreEqual(1, payInfo.PosId);
            Assert.AreEqual(2, payInfo.Amount);
            Assert.IsNotNull(payInfo.SimpleFilter);
            Assert.AreEqual("1", payInfo.SimpleFilter.Aim);
            Assert.IsNull(payInfo.SimpleFilter.Bounds);
            Assert.IsNull(payInfo.SimpleFilter.MaxAge);

            Assert.Throws<InvalidOperationException>(() => {
                ProcessPayment(payOtc, "2345", (from v in response.Vouchers
                                                select new PaymentConfirmPayload.VoucherInfo {
                                                    Id = v.Id,
                                                    Secret = v.Secret
                                                }).ToArray());
            });
        }

        [Test]
        public void PaymentGeoBounds() {
            var otcGen = CreateVouchers(1, _keyInstrument1, "1234",
                new VoucherCreatePayload.VoucherInfo[] {
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
                }
            );
            var response = RedeemVouchers(otcGen, "1234");

            Assert.AreEqual(3, response.Vouchers.Length);

            // Correct payment with voucher 1
            var ackUrl = string.Format("http://www.example.org/geo-test/{0:N}", Guid.NewGuid());
            var payOtc = CreatePayment("2345", 1, ackUrl, new SimpleFilter {
                Bounds = new Bounds {
                    LeftTop = new double[] { 15, 5 },
                    RightBottom = new double[] { 5, 15 }
                }
            });
            var payReponse = ProcessPayment(payOtc, "2345",
                new PaymentConfirmPayload.VoucherInfo {
                    Id = response.Vouchers[0].Id,
                    Secret = response.Vouchers[0].Secret
                }
            );
            Assert.AreEqual(ackUrl, payReponse.AckUrl);

            // Fail payment with voucher 2
            payOtc = CreatePayment("2345", 1, ackUrl, new SimpleFilter {
                Bounds = new Bounds {
                    LeftTop = new double[] { 30, 25 },
                    RightBottom = new double[] { 10, 35 }
                }
            });
            Assert.Throws<InvalidOperationException>(() => {
                ProcessPayment(payOtc, "2345",
                    new PaymentConfirmPayload.VoucherInfo {
                        Id = response.Vouchers[1].Id,
                        Secret = response.Vouchers[1].Secret
                    }
                );
            });
        }*/

    }

}
