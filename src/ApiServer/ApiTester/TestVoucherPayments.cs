using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Org.BouncyCastle.Crypto;
using RestSharp;
using WomPlatform.Web.Api;
using WomPlatform.Web.Api.Models;

namespace ApiTester {

    public class TestVoucherPayments {

        protected RestClient Client {
            get {
                return new RestClient($"http://dev.wom.social/api/v1");
            }
        }

        protected CryptoProvider Crypto { get; } = new CryptoProvider(new ConsoleLogger<CryptoProvider>());

        protected Random Random { get; } = new Random();

        protected RestRequest CreateJsonRequest(string urlPath, object jsonBody) {
            var request = new RestRequest(urlPath, Method.POST) {
                RequestFormat = DataFormat.Json
            };
            request.AddHeader("Accept", "application/json");
            if (jsonBody != null) {
                request.AddJsonBody(jsonBody);
            }
            return request;
        }

        protected T PerformRequest<T>(RestRequest request) {
            var response = Client.Execute(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK) {
                throw new InvalidOperationException(string.Format("API status code {0}", response.StatusCode));
            }

            return JsonConvert.DeserializeObject<T>(response.Content);
        }

        AsymmetricKeyParameter _keyPos, _keyInstrument, _keyRegistry;
        byte[] _keySession;

        [SetUp]
        public void Setup() {
            _keyPos = CryptoHelper.LoadKeyFromPem<AsymmetricCipherKeyPair>("pos1.pem").Private;
            _keyInstrument = CryptoHelper.LoadKeyFromPem<AsymmetricCipherKeyPair>("source1.pem").Private;
            _keyRegistry = CryptoHelper.LoadKeyFromPem<AsymmetricCipherKeyPair>("registry.pem").Public;

            var rnd = new Random();
            byte[] sessionKey = new byte[256 / 8];
            rnd.NextBytes(sessionKey);
            _keySession = sessionKey;
        }

        private Guid CreateVouchers(string password, params VoucherCreatePayload.VoucherInfo[] vouchers) {
            var nonce = Guid.NewGuid().ToString("N");
            var request = CreateJsonRequest("voucher/create", new VoucherCreatePayload {
                SourceId = 1,
                Nonce = nonce,
                Payload = Crypto.Encrypt(new VoucherCreatePayload.Content {
                    SourceId = 1,
                    Nonce = nonce,
                    Password = password,
                    Vouchers = vouchers
                }, _keyRegistry)
            });

            var response = PerformRequest<VoucherCreateResponse>(request);
            var responseContent = Crypto.Decrypt<VoucherCreateResponse.Content>(response.Payload, _keyInstrument);

            request = CreateJsonRequest("voucher/verify", new VoucherVerifyPayload {
                Payload = Crypto.Encrypt(new VoucherVerifyPayload.Content {
                    Otc = responseContent.Otc
                }, _keyRegistry)
            });
            PerformRequest<VoucherCreateResponse>(request);

            return responseContent.Otc;
        }

        private VoucherRedeemResponse.Content RedeemVouchers(Guid otc, string password) {
            var request = CreateJsonRequest("voucher/redeem", new VoucherRedeemPayload {
                Payload = Crypto.Encrypt(new VoucherRedeemPayload.Content {
                    Otc = otc,
                    Password = password,
                    SessionKey = _keySession.ToBase64()
                }, _keyRegistry)
            });

            var response = PerformRequest<VoucherRedeemResponse>(request);
            return Crypto.Decrypt<VoucherRedeemResponse.Content>(response.Payload, _keySession);
        }

        private VoucherCreatePayload.VoucherInfo[] CreateRandomVoucherRequests(int count, string aimCode = null) {
            var now = DateTime.UtcNow;
            var voucherInfos = new List<VoucherCreatePayload.VoucherInfo>();
            for (int i = 0; i < count; ++i) {
                voucherInfos.Add(new VoucherCreatePayload.VoucherInfo {
                    Aim = aimCode ?? "X",
                    Latitude = Random.NextBetween(5, 40),
                    Longitude = Random.NextBetween(5, 50),
                    Timestamp = now
                });

                now = now.Subtract(TimeSpan.FromMinutes(30));
            }
            return voucherInfos.ToArray();
        }

        private Guid CreatePayment(string password, int amount, string pocketAckUrl, SimpleFilter filter = null, bool persistent = false) {
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
        }

        private PaymentConfirmResponse.Content ProcessPayment(Guid otc, string password, params PaymentConfirmPayload.VoucherInfo[] vouchers) {
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
        }

        private PaymentInfoResponse.Content GetPaymentInfo(Guid otc, string password) {
            var request = CreateJsonRequest("payment/info", new PaymentInfoPayload {
                Payload = Crypto.Encrypt(new PaymentInfoPayload.Content {
                    Otc = otc,
                    Password = password,
                    SessionKey = _keySession.ToBase64()
                }, _keyRegistry)
            });

            var response = PerformRequest<PaymentInfoResponse>(request);
            return Crypto.Decrypt<PaymentInfoResponse.Content>(response.Payload, _keySession);
        }

        [Test]
        public void CreateAndRedeemRandomVouchers() {
            var voucherInfos = CreateRandomVoucherRequests(Random.Next(5) + 5);
            var otcGen = CreateVouchers("1234", voucherInfos);
            var response = RedeemVouchers(otcGen, "1234");

            Assert.AreEqual(voucherInfos.Length, response.Vouchers.Length);
            Console.WriteLine("Redeemed vouchers {0}", string.Join(", ", from v in response.Vouchers select v.Id));

            Assert.AreEqual(1, response.SourceId);
            Assert.AreEqual("Sample source 1", response.SourceName);

            var zip = (from v in voucherInfos orderby v.Timestamp select v)
                .Zip(from v in response.Vouchers orderby v.Timestamp select v, (a, b) => (a, b));
            foreach((var a, var b) in zip) {
                Assert.IsTrue(b.Aim.EndsWith(a.Aim));
                Assert.AreEqual(a.Latitude, b.Latitude);
                Assert.AreEqual(a.Longitude, b.Longitude);
                Assert.AreEqual(a.Timestamp.ToSecondPrecision(), b.Timestamp);
            }
        }

        [Test]
        public void CreateAndRedeemMultipleVouchers() {
            var voucherInfos = new VoucherCreatePayload.VoucherInfo[] {
                new VoucherCreatePayload.VoucherInfo {
                    Aim = "I",
                    Latitude = 12.34,
                    Longitude = 23.45,
                    Timestamp = DateTime.UtcNow
                },
                new VoucherCreatePayload.VoucherInfo {
                    Aim = "I",
                    Latitude = 23.45,
                    Longitude = 34.56,
                    Timestamp = DateTime.UtcNow,
                    Count = 2
                }
            };
            var otcGen = CreateVouchers("1234", voucherInfos);
            var response = RedeemVouchers(otcGen, "1234");

            Assert.AreEqual(3, response.Vouchers.Length);
            Assert.AreEqual(1, response.SourceId);
            Assert.AreEqual("Sample source 1", response.SourceName);

            Assert.AreEqual(voucherInfos[0].Latitude, response.Vouchers[0].Latitude);
            Assert.AreEqual(voucherInfos[0].Longitude, response.Vouchers[0].Longitude);
            Assert.AreEqual(voucherInfos[1].Latitude, response.Vouchers[1].Latitude);
            Assert.AreEqual(voucherInfos[1].Longitude, response.Vouchers[1].Longitude);
            Assert.AreEqual(voucherInfos[1].Latitude, response.Vouchers[2].Latitude);
            Assert.AreEqual(voucherInfos[1].Longitude, response.Vouchers[2].Longitude);
        }

        [Test]
        public void CreateVouchersAndProcessSimplePayment() {
            var voucherInfos = CreateRandomVoucherRequests(5);
            var otcGen = CreateVouchers("1234", voucherInfos);
            var response = RedeemVouchers(otcGen, "1234");

            Assert.AreEqual(5, response.Vouchers.Length);
            Assert.AreEqual(1, response.SourceId);

            var ackUrl = string.Format("http://www.example.org/confirmation/{0:N}", Guid.NewGuid());

            var payOtc = CreatePayment("2345", 5, ackUrl);

            var payInfo = GetPaymentInfo(payOtc, "2345");
            Assert.AreEqual(5, payInfo.Amount);
            Assert.AreEqual(1, payInfo.PosId);
            Assert.AreEqual("Sample POS 1", payInfo.PosName);
            Assert.IsNull(payInfo.SimpleFilter);
            Assert.AreEqual(false, payInfo.Persistent);

            var paymentVouchers = (from v in response.Vouchers
                                   select new PaymentConfirmPayload.VoucherInfo {
                                       Id = v.Id,
                                       Secret = v.Secret
                                   }).ToArray();

            var payResponse = ProcessPayment(payOtc, "2345", paymentVouchers);

            Assert.AreEqual(ackUrl, payResponse.AckUrl);

            Assert.Throws<InvalidOperationException>(() => {
                ProcessPayment(payOtc, "2345", paymentVouchers);
            });
        }

        [Test]
        public void CreateVouchersAndProcessMultiplePayment() {
            var voucherInfos = CreateRandomVoucherRequests(2);
            var otcGen = CreateVouchers("1234", voucherInfos);
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
            var otcGen = CreateVouchers("1234", voucherInfos);
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
            var otcGen = CreateVouchers("1234",
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
            var otcGen = CreateVouchers("1234",
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
        }

    }

}
