using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace WomPlatform.Web.Api.Service {
    public class AimService {

        public class Aim {
            public Aim(string code, Dictionary<string, string> titles, Aim[] subaims = null, bool hidden = false) {
                Code = code;
                Titles = titles;
                Aims = subaims ?? [];
                Hidden = hidden;
            }

            public string Code { get; init; }

            public Dictionary<string, string> Titles { get; init; }

            public Aim[] Aims { get; init; }

            public bool Hidden { get; init; }
        }

        private static Dictionary<string, string> Titles(string singleTitle) {
            return new Dictionary<string, string>() {
                ["en"] = singleTitle,
                ["it"] = singleTitle,
            };
        }

        private static Dictionary<string, string> Titles(string english, string italian) {
            return new Dictionary<string, string>() {
                ["en"] = english,
                ["it"] = italian,
            };
        }

        private static Aim[] Aims = [
            new Aim("0", Titles("Demo"), hidden: true),
            new Aim("C", Titles("Culture", "Cultura")),
            new Aim("E", Titles("Education", "Istruzione")),
            new Aim("H", Titles("Health and Wellbeing", "Salute e benessere"), [
                new Aim("HE", Titles("Epidemic Containment", "Contenimento di epidemia")),
            ]),
            new Aim("I", Titles("Infrastructure and Services", "Infrastruttura e servizi"), [
                new Aim("IC", Titles("Collaboration", "Collaborazione")),
                new Aim("IG", Titles("Management", "Gestione")),
                new Aim("IM", Titles("Infrastructure Monitoring", "Monitoraggio infrastrutturale")),
                new Aim("IS", Titles("Service", "Servizio")),
            ]),
            new Aim("N", Titles("Natural Environment", "Ambiente naturale")),
            new Aim("P", Titles("Participation", "Partecipazione")),
            new Aim("R", Titles("Human Rights", "Diritti umani")),
            new Aim("S", Titles("Safety", "Sicurezza")),
            new Aim("T", Titles("Cultural Heritage", "Patrimonio culturale")),
            new Aim("U", Titles("Urban Environment", "Ambiente urbano"), [
                new Aim("UM", Titles("Urban Mobility", "Mobilità urbana")),
            ]),
            new Aim("X", Titles("Social Cohesion", "Coesione sociale"), [
                new Aim("XX", Titles("Gratitude", "Gratitudine")),
            ]),
        ];

        private static ImmutableDictionary<string, Aim> AimsByCode = ImmutableDictionary<string, Aim>.Empty;

        private static ImmutableArray<Aim> FlatAims = [];

        static AimService() {
            // Prepare dictionary access by code
            void RecursiveAddToDictionary(Aim[] list, Dictionary<string, Aim> target) {
                foreach(var aim in list) {
                    target.Add(aim.Code, aim);
                    RecursiveAddToDictionary(aim.Aims, target);
                }
            }
            Dictionary<string, Aim> aimsByCode = [];
            RecursiveAddToDictionary(Aims, aimsByCode);
            AimsByCode = aimsByCode.ToImmutableDictionary();

            // Prepare flat list
            void RecursiveAddToList(Aim[] list, List<Aim> target) {
                foreach(var aim in list) {
                    target.Add(aim);
                    RecursiveAddToList(aim.Aims, target);
                }
            }
            List<Aim> flatAims = [];
            RecursiveAddToList(Aims, flatAims);
            FlatAims = (from a in flatAims
                        orderby a.Code
                        select a).ToImmutableArray();
        }

        private readonly ILogger<BaseService> _logger;

        public AimService(ILogger<BaseService> logger) {
            _logger = logger;
        }

        protected ILogger<BaseService> Logger { get { return _logger; } }

        /// <summary>
        /// Get all aims sorted by code.
        /// </summary>
        public IReadOnlyList<Aim> GetFlatAims() {
            return FlatAims;
        }

        /// <summary>
        /// Get all root aims.
        /// </summary>
        public IReadOnlyList<Aim> GetAims() {
            return Aims.ToImmutableArray();
        }

        /// <summary>
        /// Get all root aim codes.
        /// </summary>
        public string[] GetRootAimCodes() {
            return (from a in Aims
                    orderby a.Code
                    select a.Code).ToArray();
        }

        /// <summary>
        /// Get all aim codes.
        /// </summary>
        public string[] GetAllAimCodes() {
            return (from a in FlatAims
                    select a.Code).ToArray();
        }

        /// <summary>
        /// Get aim by unique code.
        /// </summary>
        public Aim GetAimByCode(string code) {
            return AimsByCode.TryGetValue(code, out Aim value) ? value : null;
        }

    }
}
